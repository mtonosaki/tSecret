// (c) 2020 Manabu Tonosaki
// Licensed under the MIT license.

using Foundation;
using LocalAuthentication;
using System.Threading.Tasks;
using tSecretXamarin.iOS;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(AuthService))]
namespace tSecretXamarin.iOS
{
    public class AuthService : IAuthService
    {
        public Task<bool> GetAuthentication()
        {
            var tcs = new TaskCompletionSource<bool>();
            var lac = new LAContext();
            if (lac.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out var authError))
            {
                if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                {
                    lac.LocalizedReason = "Authorize for access to secrets"; // iOS 11
                    lac.EvaluatePolicy(
                        LAPolicy.DeviceOwnerAuthenticationWithBiometrics,
                        new NSString("Login to tSecret"),
                        new LAContextReplyHandler((isSuccess, error) =>
                        {
                            tcs.SetResult(isSuccess);
                        })
                    );
                }
            }
            else if (lac.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthentication, out var authError2))
            {
                lac.EvaluatePolicy(
                    LAPolicy.DeviceOwnerAuthentication,
                    new NSString("Login to tSecret"),
                    new LAContextReplyHandler((isSuccess, error) =>
                    {
                        tcs.SetResult(isSuccess);
                    })
                );
            }
            return tcs.Task;
        }
    }
}