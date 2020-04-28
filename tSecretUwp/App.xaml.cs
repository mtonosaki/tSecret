﻿// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

using System;
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
        public Authenticator Auth { get; }
        public NotePersister Persister { get; }

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
            Auth = new AuthenticatorAzureAD();
            Persister = new NotePersister
            {
                Auth = Auth,
            };
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
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
            if (Auth.IsAuthenticated)
            {
                Persister.Save();
            }
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }
    }
}
