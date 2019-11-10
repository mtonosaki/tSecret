using System;
using System.Diagnostics;
using Xamarin.Forms;

namespace tSecret
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new AuthPage
            {
                IsAutoAuthentication = true,
            });
        }

        protected override void OnStart()
        {
            Debug.WriteLine($"{DateTime.Now} App.OnStart()");
        }

        protected override void OnSleep()
        {
            Debug.WriteLine($"{DateTime.Now} App.OnSleep()");
        }

        protected override void OnResume()
        {
            Debug.WriteLine($"{DateTime.Now} App.OnResume()");

            if (Current.Properties.ContainsKey("LoginUtc"))
            {
                DateTime dt = (DateTime)Application.Current.Properties["LoginUtc"];
                if (DateTime.UtcNow - dt > TimeSpan.FromSeconds(30))
                {
                    MainPage = new NavigationPage(new AuthPage
                    {
                        IsAutoAuthentication = false,
                        FirstErrorMessage = "Login-timeout",
                    });
                }
            }
        }
    }
}
