using System.Threading.Tasks;

namespace WebServiceClientManager.Interfaces
{
    public interface ITokenRefreshHandler
    {
        string RefreshToken();
        Task<string> RefreshTokenAsync();
    }
}
