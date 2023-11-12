using System;
using System.Collections.Generic;
using System.Text;

namespace WebServiceClientManager.Interfaces
{
    public interface ITokenManager
    {
        string GetToken();
        void SetToken(string token);
    }
}
