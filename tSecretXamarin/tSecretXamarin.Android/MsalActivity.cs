
using Android.App;
using Android.Content;
using Microsoft.Identity.Client;
using tSecretCommon;

namespace tSecretXamarin.Droid
{
    [Activity]
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
        DataHost = "auth",
        DataScheme = MySecretParameterXamarin.RedirectUrlScheme)]
    public class MsalActivity : BrowserTabActivity
    {
    }
}