using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System;
using WebServiceClientManager.Interfaces;
using WebServiceClientManager.Models;
using Newtonsoft.Json;

namespace WebServiceClientManager
{
    public class WebServiceClient : IWebServiceClient
    {
        public event Action<HttpResponseMessage> UnauthorizedResponseReceived;
        public event Action<HttpResponseMessage> UnauthorizedRetriedResponseReceived;
        
        private Func<string> methodToRefreshToken;
        private Func<Task<string>> methodToRefreshTokenAsync;

        private string _authorizationToken;
        private string _baseUri;

        public string AuthorizationType { get; set; } = "Bearer";
        private HttpClient _httpClient;

        private readonly ITokenManager _tokenManager;

        public WebServiceClient(string baseUrl)
        {
            this._baseUri = baseUrl;
            this._httpClient = new HttpClient();
        }

        public WebServiceClient(string baseUrl, ITokenManager tokenManager)
        {
            this._baseUri = baseUrl;
            this._httpClient = new HttpClient();
            _tokenManager = tokenManager;
        }

        public WebServiceClient(string baseUri, string authorizationToken)
        {
            this._baseUri = baseUri;
            _authorizationToken = authorizationToken;

            if (_httpClient == null)
                _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthorizationType, _authorizationToken);
        }

        public void SetAuthorizationToken(string authorizationToken)
        {
            _authorizationToken = authorizationToken;

            if (_httpClient == null)
                _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthorizationType, _authorizationToken);
        }

        public void MethodToRefreshToken(Func<string> methodToRefreshToken)
        {
            this.methodToRefreshToken = methodToRefreshToken;
        }

        public void MethodToRefreshTokenAsync(Func<Task<string>> methodToRefreshTokenAsync)
        {
            this.methodToRefreshTokenAsync = methodToRefreshTokenAsync;
        }

        //Metodos sincronos
        public ClientResponse<TResponse> Get<TResponse>(string endpoint)
        {
            return SendRequest<TResponse>(endpoint, HttpMethod.Get, null, EContentType.application_json);
        }

        public ClientResponse<TResponse> Get<TResponse>(string endpoint, object content, EContentType contentType)
        {
            return SendRequest<TResponse>(endpoint, HttpMethod.Get, content, contentType);
        }

        public ClientResponse<TResponse> Post<TResponse>(string endpoint, object content, EContentType contentType)
        {
            return SendRequest<TResponse>(endpoint, HttpMethod.Post, content, contentType);
        }

        public ClientResponse<TResponse> Put<TResponse>(string endpoint, object content, EContentType contentType)
        {
            return SendRequest<TResponse>(endpoint, HttpMethod.Put, content, contentType);
        }

        public ClientResponse<TResponse> Delete<TResponse>(string endpoint)
        {
            return SendRequest<TResponse>(endpoint, HttpMethod.Delete, null, EContentType.application_json);
        }

        public ClientResponse<TResponse> Delete<TResponse>(string endpoint, object content, EContentType contentType)
        {
            return SendRequest<TResponse>(endpoint, HttpMethod.Delete, content, contentType);
        }

        public ClientResponse<TResponse> Patch<TResponse>(string endpoint, object content, EContentType contentType)
        {
            return SendRequest<TResponse>(endpoint, new HttpMethod("PATCH"), content, contentType);
        }


        //Metodos asincronos
        public async Task<ClientResponse<TResponse>> GetAsync<TResponse>(string endpoint)
        {
            return await SendRequestAsync<TResponse>(endpoint, HttpMethod.Get, null, EContentType.application_json);
        }

        public async Task<ClientResponse<TResponse>> GetAsync<TResponse>(string endpoint, object content, EContentType contentType)
        {
            return await SendRequestAsync<TResponse>(endpoint, HttpMethod.Get, content, contentType);
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

        public async Task<ClientResponse<TResponse>> DeleteAsync<TResponse>(string endpoint, object content, EContentType contentType)
        {
            return await SendRequestAsync<TResponse>(endpoint, HttpMethod.Delete, content, contentType);
        }

        public async Task<ClientResponse<TResponse>> PatchAsync<TResponse>(string endpoint, object content, EContentType contentType)
        {
            return await SendRequestAsync<TResponse>(endpoint, new HttpMethod("PATCH"), content, contentType);
        }

        private ClientResponse<TResponse> SendRequest<TResponse>(string endpoint, HttpMethod method, object content, EContentType contentType)
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

                var response = _httpClient.SendAsync(request).Result;
                responseManager.StatusCode = response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = response.Content.ReadAsStringAsync().Result;

                    JsonSerializerSettings settings = new JsonSerializerSettings();
                    settings.MissingMemberHandling = MissingMemberHandling.Ignore;
                    responseManager.Content = JsonConvert.DeserializeObject<TResponse>(jsonResponse, settings);
                    responseManager.IsSuccess = true;
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        UnauthorizedResponseReceived?.Invoke(response);

                        if (methodToRefreshToken != null)
                        {
                            if (!hasRetried) // Verifica si no se ha intentado un reintento antes
                            {
                                //Refrescamos el token y reintentamos la solicitud
                                var token = methodToRefreshToken();
                                if (!string.IsNullOrEmpty(token))
                                {
                                    _tokenManager.SetToken(token);
                                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthorizationType, token);
                                    SetAuthorizationToken(token);
                                    hasRetried = true;
                                    return SendRequest<TResponse>(endpoint, method, content, contentType);
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

                    JsonSerializerSettings settings = new JsonSerializerSettings();
                    settings.MissingMemberHandling = MissingMemberHandling.Ignore;
                    responseManager.Content = JsonConvert.DeserializeObject<TResponse>(jsonResponse, settings);
                    responseManager.IsSuccess = true;
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        UnauthorizedResponseReceived?.Invoke(response);

                        if (methodToRefreshTokenAsync != null)
                        {
                            if (!hasRetried) // Verifica si no se ha intentado un reintento antes
                            {
                                //Refrescamos el token y reintentamos la solicitud
                                var token = await methodToRefreshTokenAsync();
                                if (!string.IsNullOrEmpty(token))
                                {
                                    _tokenManager.SetToken(token);
                                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthorizationType, token);
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

        public string GenerateQueryParamsFromObject(object parametros)
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
