using Microsoft.Identity.Client;
using System;
using tSecretCommon;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace tSecretXamarin
{
    public partial class App : Application
    {
        public static object ParentWindow { get; set; }
        public object MainActivity { get; set; }

        public AuthAzureAD Auth { get; }
        public NotePersister Persister { get; }
        public SettingPersister Setting { get; set; }

        public App()
        {
            InitializeComponent();

            Setting = new SettingPersister();
            Persister = new NotePersister();
            Auth = new AuthAzureAD
            {
                UIParent = () => ParentWindow,
                MainActivity = () => MainActivity,
            };
            ParentWindow = MainPage = new NavigationPage(new AuthPage());
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
            if (Current.Properties.ContainsKey("LoginUtc"))
            {
                var dt = (DateTime)Current.Properties["LoginUtc"];
                if (DateTime.UtcNow - dt > TimeSpan.FromSeconds(30))
                {
                    MainPage = new NavigationPage(new AuthPage
                    {
                        FirstErrorMessage = "Login-timeout",
                    });
                }
            }
        }
    }
}
