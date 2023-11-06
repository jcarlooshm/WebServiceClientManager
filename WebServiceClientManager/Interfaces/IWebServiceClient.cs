using System;
using System.Net.Http;
using System.Threading.Tasks;
using WebServiceClientManager.Enums;
using WebServiceClientManager.Models;

namespace WebServiceClientManager.Interfaces
{
    public interface IWebServiceClient
    {
        event Action<HttpResponseMessage> UnauthorizedResponseReceived;
        event Action<HttpResponseMessage> UnauthorizedRetriedResponseReceived;

        string CreateQueryParameters(object parametros);
        ClientResponse<TResponse> Delete<TResponse>(string endpoint);
        Task<ClientResponse<TResponse>> DeleteAsync<TResponse>(string endpoint);
        ClientResponse<TResponse> Get<TResponse>(string endpoint);
        Task<ClientResponse<TResponse>> GetAsync<TResponse>(string endpoint);
        ClientResponse<TResponse> Patch<TResponse>(string endpoint, object content, EContentType contentType);
        Task<ClientResponse<TResponse>> PatchAsync<TResponse>(string endpoint, object content, EContentType contentType);
        ClientResponse<TResponse> Post<TResponse>(string endpoint, object content, EContentType contentType);
        Task<ClientResponse<TResponse>> PostAsync<TResponse>(string endpoint, object content, EContentType contentType);
        ClientResponse<TResponse> Put<TResponse>(string endpoint, object content, EContentType contentType);
        Task<ClientResponse<TResponse>> PutAsync<TResponse>(string endpoint, object content, EContentType contentType);
        void SetAuthorizationToken(string authorizationToken);
        void SetTokenRefreshFunc(Func<Task<string>> refreshFunc);
    }
}
