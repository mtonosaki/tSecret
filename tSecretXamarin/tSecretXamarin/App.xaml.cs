﻿// (c) 2020 Manabu Tonosaki
// Licensed under the MIT license.

using tSecretCommon;
using Xamarin.Forms;

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
        }
    }
}
