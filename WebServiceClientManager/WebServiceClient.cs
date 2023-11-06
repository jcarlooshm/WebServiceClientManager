using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System;
using WebServiceClientManager.Enums;
using WebServiceClientManager.Interfaces;
using WebServiceClientManager.Models;
using Newtonsoft.Json;

namespace WebServiceClientManager
{
    public class WebServiceClient : IWebServiceClient
    {
        private HttpClient _httpClient;
        private string _baseUri;
        private string _authorizationToken;
        private Func<Task<string>> tokenRefreshFuncAsync;
        public event Action<HttpResponseMessage> UnauthorizedResponseReceived;
        public event Action<HttpResponseMessage> UnauthorizedRetriedResponseReceived;


        public WebServiceClient(string baseUrl)
        {
            this._baseUri = baseUrl;
            this._httpClient = new HttpClient();
        }

        public WebServiceClient(string baseUri, string authorizationToken)
        {
            this._baseUri = baseUri;
            _authorizationToken = authorizationToken;

            if (_httpClient == null)
                _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authorizationToken);
        }

        public void SetAuthorizationToken(string authorizationToken)
        {
            _authorizationToken = authorizationToken;

            if (_httpClient == null)
                _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authorizationToken);
        }


        public void SetTokenRefreshFunc(Func<Task<string>> refreshFunc)
        {
            this.tokenRefreshFuncAsync = refreshFunc;
        }

        //Metodos sincronos
        public ClientResponse<TResponse> Get<TResponse>(string endpoint)
        {
            return GetAsync<TResponse>(endpoint).Result;
        }

        public ClientResponse<TResponse> Post<TResponse>(string endpoint, object content, EContentType contentType)
        {
            return PostAsync<TResponse>(endpoint, content, contentType).Result;
        }

        public ClientResponse<TResponse> Put<TResponse>(string endpoint, object content, EContentType contentType)
        {
            return PutAsync<TResponse>(endpoint, content, contentType).Result;
        }

        public ClientResponse<TResponse> Delete<TResponse>(string endpoint)
        {
            return DeleteAsync<TResponse>(endpoint).Result;
        }

        public ClientResponse<TResponse> Patch<TResponse>(string endpoint, object content, EContentType contentType)
        {
            return PatchAsync<TResponse>(endpoint, content, contentType).Result;
        }


        //Metodos asincronos
        public async Task<ClientResponse<TResponse>> GetAsync<TResponse>(string endpoint)
        {
            return await SendRequestAsync<TResponse>(endpoint, HttpMethod.Get, null, EContentType.application_json);
        }

        public async Task<ClientResponse<TResponse>> PostAsync<TResponse>(string endpoint, object content, EContentType contentType)
        {
            return await SendRequestAsync<TResponse>(endpoint, HttpMethod.Post, content, contentType);
        }

        public async Task<ClientResponse<TResponse>> PutAsync<TResponse>(string endpoint, object content, EContentType contentType)
        {
            return await SendRequestAsync<TResponse>(endpoint, HttpMethod.Put, content, contentType);
        }

        public async Task<ClientResponse<TResponse>> DeleteAsync<TResponse>(string endpoint)
        {
            return await SendRequestAsync<TResponse>(endpoint, HttpMethod.Delete, null, EContentType.application_json);
        }

        public async Task<ClientResponse<TResponse>> PatchAsync<TResponse>(string endpoint, object content, EContentType contentType)
        {
            return await SendRequestAsync<TResponse>(endpoint, new HttpMethod("PATCH"), content, contentType);
        }

        private async Task<ClientResponse<TResponse>> SendRequestAsync<TResponse>(string endpoint, HttpMethod method, object content, EContentType contentType)
        {
            ClientResponse<TResponse> responseManager = new ClientResponse<TResponse>();
            bool hasRetried = false;

            try
            {
                var request = new HttpRequestMessage(method, _baseUri + endpoint);

                if (content != null)
                {
                    HttpContent serializedContent = CreateHttpContent(content, contentType);
                    request.Content = serializedContent;
                }

                var response = await _httpClient.SendAsync(request);
                responseManager.StatusCode = response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    responseManager.Content = JsonConvert.DeserializeObject<TResponse>(jsonResponse);
                    responseManager.IsSuccess = true;
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        UnauthorizedResponseReceived?.Invoke(response);

                        if (tokenRefreshFuncAsync != null)
                        {
                            if (!hasRetried) // Verifica si no se ha intentado un reintento antes
                            {
                                //Refrescamos el token y reintentamos la solicitud
                                var token = await tokenRefreshFuncAsync();
                                if (!string.IsNullOrEmpty(token))
                                {
                                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                                    SetAuthorizationToken(token);
                                    hasRetried = true;
                                    return await SendRequestAsync<TResponse>(endpoint, method, content, contentType);
                                }
                            }
                            else
                            {
                                UnauthorizedRetriedResponseReceived?.Invoke(response);
                            }

                            responseManager.Message = "error al refrescar token";
                        }
                    }
                    else
                    {
                        responseManager.Message = response.ReasonPhrase;
                    }
                }
            }
            catch (Exception ex)
            {
                responseManager.StatusCode = HttpStatusCode.BadRequest;
                responseManager.Message = GetMessageFromException(ex);
            }

            return responseManager;
        }

        private HttpContent CreateHttpContent(object data, EContentType contentType)
        {
            HttpContent content;
            switch (contentType)
            {
                case EContentType.application_json:
                    string jsonData = JsonConvert.SerializeObject(data);
                    content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                    break;

                case EContentType.multipart_form_data:
                    var formData = new MultipartFormDataContent();
                    var properties = TypeDescriptor.GetProperties(data);

                    foreach (PropertyDescriptor property in properties)
                        formData.Add(new StringContent(property.GetValue(data)?.ToString() ?? ""), property.Name);

                    content = formData;
                    break;
                case EContentType.application_x_www_form_urlencoded:
                    var values = new List<KeyValuePair<string, string>>();
                    properties = TypeDescriptor.GetProperties(data);

                    foreach (PropertyDescriptor property in properties)
                        values.Add(new KeyValuePair<string, string>(property.Name, property.GetValue(data)?.ToString() ?? ""));

                    content = new FormUrlEncodedContent(values);
                    break;
                default:
                    throw new ArgumentException("Tipo de contenido no admitido");
            }

            content.Headers.ContentType = new MediaTypeHeaderValue(GetContentTypeHeader(contentType));
            return content;
        }

        public string CreateQueryParameters(object parametros)
        {
            var properties = parametros.GetType().GetProperties();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            foreach (var property in properties)
            {
                var value = property.GetValue(parametros, null);
                if (value != null)
                {
                    queryString[property.Name] = value.ToString();
                }
            }

            return $"?{queryString}";
        }
        private string GetContentTypeHeader(EContentType contentType)
        {
            switch (contentType)
            {
                case EContentType.application_json:
                    return "application/json";
                case EContentType.multipart_form_data:
                    return "multipart/form-data";
                case EContentType.application_x_www_form_urlencoded:
                    return "application/x-www-form-urlencoded";
                default:
                    throw new ArgumentException("Tipo de contenido no admitido");
            }
        }


        private string GetMessageFromException(Exception ex)
        {
            string error = "";
            error = ex.Message;
            for (var inEx = ex?.InnerException; inEx != null;)
            {
                error += "; " + inEx.Message;
                inEx = inEx.InnerException;
            }

            return error;
        }
    }
}
