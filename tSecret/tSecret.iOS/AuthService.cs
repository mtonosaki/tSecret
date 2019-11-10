using Foundation;
using LocalAuthentication;
using System.Threading.Tasks;
using tSecret.iOS;
using Xamarin.Forms;

[assembly: Dependency(typeof(AuthService))]
namespace tSecret.iOS
{
    public class AuthService : IAuthService
    {
        public Task<bool> GetAuthentication()
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            LAContext context = new LAContext();
            NSString caption = new NSString("Login to tSecret");
            if (context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out NSError ae))
            {
                LAContextReplyHandler replyHandler = new LAContextReplyHandler((success, error) =>
                {
                    tcs.SetResult(success);
                });
                context.EvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, caption, replyHandler);
            };
            return tcs.Task;
        }
    }
}