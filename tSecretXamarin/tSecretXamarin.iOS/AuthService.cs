using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using LocalAuthentication;
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
            var context = new LAContext();
            var caption = new NSString("Login to tSecret");
            if (context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out var ae))
            {
                var replyHandler = new LAContextReplyHandler((success, error) =>
                {
                    tcs.SetResult(success);
                });
                context.EvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, caption, replyHandler);
            };
            return tcs.Task;
        }
    }
}