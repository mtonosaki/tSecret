using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace tSecretXamarin
{
    public interface IAuthService
    {
        Task<bool> GetAuthentication();
    }
}
