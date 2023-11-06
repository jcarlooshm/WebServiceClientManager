using System.Net;

namespace WebServiceClientManager.Models
{
    public class ClientResponse<TResponse>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public TResponse Content { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}
