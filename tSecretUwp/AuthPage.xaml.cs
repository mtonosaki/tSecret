// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Tono;
using Tono.Gui.Uwp;
using tSecretCommon;
using Windows.Security.Credentials.UI;
using Windows.Services.Maps;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace tSecretUwp
{
    public sealed partial class AuthPage : Page
    {
        public string FirstErrorMessage { get; set; } = string.Empty;
        private Authenticator AzureAD => ((App)Application.Current).Auth;
        private NotePersister Persister => ((App)Application.Current).Persister;

        public AuthPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New)
            {
                DelayUtil.Start(TimeSpan.FromMilliseconds(97), () =>
                {
                    var dmy = AuthenticateAsync();
                });
            }
            base.OnNavigatedTo(e);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var dmy = AuthenticateAsync();
        }

        private async Task SetErrorMessage(string mes, bool? StartButtonVisible = null)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ErrorMessage.Visibility = Visibility.Collapsed;
                FirstErrorMessage = string.Empty;
                if (StartButtonVisible != null)
                {
                    StartButton.IsEnabled = StartButtonVisible ?? false;
                }
            });
        }

        private async Task<bool> AuthenticateAsync()
        {
            await SetErrorMessage("");
            if (AzureAD.IsAuthenticated)
            {
                AzureAD.IsSilentLogin = true;
            }
            var azureRes = AzureAD.IsAuthenticated ? true : await AzureAD.LoginAsync();  // Get AzureAD credential
            if (azureRes)
            {
                var dmy = Task.Run(async () =>
                {
                    var content = await AzureAD.GetPrivacyData();
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        var s1 = StrUtil.MidSkip(content ?? "", "\"displayName\"\\s*:\\s*\"");
                        var displayName = StrUtil.LeftBefore(s1 + "\"", "\"").Trim();           // pick displayName part
                        if (string.IsNullOrEmpty(displayName) == false)
                        {
                            ApplicationView.GetForCurrentView().Title = displayName;
                        }
                    });
                });
            }
            else
            {
                await SetErrorMessage("Authentication error");
                return false;
            }

            Persister.Load(isForceReload: true);

            var pinres = AzureAD.IsSilentLogin ? await PinAuthentication() : AzureAD.IsAuthenticated;
            if (pinres)
            {
                ConfigUtil.Set("LoginUtc", DateTime.UtcNow.ToString());
                this.Frame.Navigate(typeof(NoteListPage), null, new SlideNavigationTransitionInfo
                {
                    Effect = SlideNavigationTransitionEffect.FromRight,
                });
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<bool> PinAuthentication()
        {
            try
            {
                var res = await UserConsentVerifier.RequestVerificationAsync("tSecret");
                switch (res)
                {
                    case UserConsentVerificationResult.Verified:
                        return true;
                    default:
                        await SetErrorMessage("Authentication error");
                        return false;
                }
            }
            catch (Exception)
            {
                await SetErrorMessage("ERROR: Cannot use tSecret on this platform", true);
                return false;
            }
        }
    }
}
