// (c) 2020 Manabu Tonosaki
// Licensed under the MIT license.

using System.Threading.Tasks;

namespace tSecretXamarin
{
    public interface IAuthService
    {
        Task<bool> GetAuthentication();
    }
}
