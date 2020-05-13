// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

using System;
using Tono.Gui.Uwp;
using tSecretCommon;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace tSecretUwp
{
    public sealed partial class App : Application
    {
        public AuthAzureAD Auth { get; }
        public NotePersister Persister { get; }
        public SettingPersister Setting { get; set; }

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;

            Setting = new SettingPersister();
            Auth = new AuthAzureAD();
            Persister = new NotePersister();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Setting.LoadFile();

            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(AuthPage), e.Arguments);
                }
                Window.Current.Activate();
            }
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            ClipboardUtil.Current.Set("");

            if (Auth.IsAuthenticated)
            {
                Persister.SaveFile();
            }
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }
    }
}
