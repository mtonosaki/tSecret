using System;
using System.Threading.Tasks;
using tSecret.UWP;
using Windows.Security.Credentials.UI;
using Xamarin.Forms;

[assembly: Dependency(typeof(AuthService))]
namespace tSecret.UWP
{
    public class AuthService : IAuthService
    {
        public Task<bool> GetAuthentication()
        {
            return Authenticate();
        }

        public async Task<bool> Authenticate()
        {
            UserConsentVerificationResult res = await UserConsentVerifier.RequestVerificationAsync("tSecret");
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