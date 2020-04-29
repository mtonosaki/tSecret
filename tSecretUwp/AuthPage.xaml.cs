// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        private NotePersister Persister => ((App)Application.Current).Persister;
        private AuthenticatorAzureAD AzureAD => (AuthenticatorAzureAD)((App)Application.Current).Auth;
        private SettingPersister Setting => ((App)Application.Current).Setting;

        private CancellationTokenSource CTS = new CancellationTokenSource();
        private StreamWriter Message = new StreamWriter(new MemoryStream(), Encoding.UTF8);

        public class StoryNode
        {
            public Func<Task<bool>> Task { get; set; }
            public StoryNode Success { get; set; }
            public StoryNode Error { get; set; }
        }

        private StoryNode StoryRoot;

        public AuthPage()
        {
            this.InitializeComponent();

            // Story Build
            var moveToNext = new StoryNode
            {
                Task = () => NextPageAsync(),
            };
            var loadPrivacy = new StoryNode
            {
                Task = () => LoadPrivacy(),
                Success = moveToNext,
                Error = moveToNext,
            };
            var showError = new StoryNode
            {
                Task = () => AuthenticationErrorAsync(),
            };
            var pinCheck = new StoryNode
            {
                Task = () => PinAuthenticationAsync(),
                Success = loadPrivacy,
                Error = showError,
            };
            var authInteractive = new StoryNode
            {
                Task = () => AzureAD.LoginInteractiveAsync(() => CTS.Token, () => Message), // ID/PW authentication
                Success = loadPrivacy,
                Error = showError,
            };

            StoryRoot = new StoryNode
            {
                Task = () => AzureAD.LoginSilentAsync(() => CTS.Token, () => Message),  // silent authentication
                Success = pinCheck,
                Error = authInteractive,
            };
        }

        private string GetMessage()
        {
            var ms = (MemoryStream)Message.BaseStream;
            ms.Seek(0, SeekOrigin.Begin);
            var sr = new StreamReader(ms, Encoding.UTF8);
            var ret = sr.ReadToEnd();
            Message = new StreamWriter(new MemoryStream(), Encoding.UTF8);
            return ret;
        }

        private async Task<bool> LoadPrivacy()
        {
            var ret = await AzureAD.GetPrivacyDataAsync(() => CTS.Token, () => Message);
            if (ret)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ApplicationView.GetForCurrentView().Title = AzureAD.DisplayName ?? "";
                });
            }
            return ret;
        }

        private async Task<bool> AuthenticationErrorAsync()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ErrorMessage.Text = GetMessage();
                ErrorMessage.Visibility = string.IsNullOrEmpty(ErrorMessage.Text.Trim()) ? Visibility.Collapsed : Visibility.Visible;
            });
            await Task.Delay(300, CTS.Token);
            return true;
        }

        private async Task<bool> NextPageAsync()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ConfigUtil.Set("LoginUtc", DateTime.UtcNow.ToString());
                Frame.Navigate(typeof(NoteListPage), null, new SlideNavigationTransitionInfo
                {
                    Effect = SlideNavigationTransitionEffect.FromRight,
                });
            });
            return true;
        }

        private async Task<bool> PinAuthenticationAsync()
        {
            try
            {
                var res = await UserConsentVerifier.RequestVerificationAsync("tSecret");
                switch (res)
                {
                    case UserConsentVerificationResult.Verified:
                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task SetSceneAsync(StoryNode node, CancellationTokenSource mycts)
        {
            for (var scene = node; scene != null && mycts.Token.IsCancellationRequested == false;)
            {
                var task = scene.Task();    // Run Task
                var ret = await task.ConfigureAwait(false); // Wait Task finish
                scene = ret ? scene.Success : scene.Error;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New)
            {
                DelayUtil.Start(TimeSpan.FromMilliseconds(23), () =>
                {
                    StartButton_Click(this, null);
                });
            }
            base.OnNavigatedTo(e);
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e) // giveup thread pool control
        {
            StartButton.IsEnabled = false;
            await SetSceneAsync(StoryRoot, CTS);
        }

        private async void OfflineButton_Click(object sender, RoutedEventArgs e) // giveup thread pool control
        {
            CTS.Cancel();
            CTS = new CancellationTokenSource();
        }
    }
}
