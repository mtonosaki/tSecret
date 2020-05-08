using Microsoft.Identity.Client;
using System;
using tSecretCommon;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace tSecretXamarin
{
    public partial class App : Application
    {
        public static IPublicClientApplication AuthenticationClient { get; private set; }
        public static object ParentWindow { get; set; }
        public object MainActivity { get; set; }

        public App()
        {
            InitializeComponent();
            var param = new MySecretParameterXamarin();

            AuthenticationClient = PublicClientApplicationBuilder.Create(param.AzureADClientId)
                    .WithAuthority(param.AuthorityAudience)
                    .WithRedirectUri(param.PublicClientRedirectUri)
                    .WithIosKeychainSecurityGroup(param.IosKeychainSecurityGroups)
                    .WithParentActivityOrWindow(() => MainActivity ?? ParentWindow) // for Android ?
                    .Build();

            ParentWindow = MainPage = new MainPage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
