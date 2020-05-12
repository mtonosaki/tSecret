using System;
using System.Threading.Tasks;
using tSecretXamarin.UWP;
using Windows.Security.Credentials.UI;
using Xamarin.Forms;

[assembly: Dependency(typeof(AuthService))]
namespace tSecretXamarin.UWP
{
    public class AuthService : IAuthService
    {
        public Task<bool> GetAuthentication()
        {
            return Authenticate();
        }

        public async Task<bool> Authenticate()
        {
            var res = await UserConsentVerifier.RequestVerificationAsync("tSecret");
            switch (res)
            {
                case UserConsentVerificationResult.Verified:
                    return true;
                default:
                    return false;
            }
        }
    }
}
