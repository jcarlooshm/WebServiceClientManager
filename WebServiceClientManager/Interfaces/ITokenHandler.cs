namespace WebServiceClientManager.Interfaces
{
    public interface ITokenHandler
    {
        string GetToken();
        void SetToken(string token);
    }
}
