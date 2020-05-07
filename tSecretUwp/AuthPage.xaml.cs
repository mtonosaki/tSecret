// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tono.Gui.Uwp;
using tSecretCommon;
using Windows.Security.Credentials.UI;
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
        private StoryNode StoryOnlineRoot;
        private StoryNode StoryLocalRoot;

        public AuthPage()
        {
            this.InitializeComponent();

            Setting.LoadFile();

            // Story Build
            var showError = new StoryNode
            {
                Task = scene => AuthenticationErrorAsync(scene),
                TaskName = "Show Authentication Error",
                Success = new StoryNode
                {
                    Task = scene => ResetControl(scene),
                    TaskName = "Reset Control",
                },
            };
            var moveToNext = new StoryNode
            {
                Task = scene => NextPageAsync(scene),
                TaskName = "Next Page",
            };
            var loadData = new StoryNode
            {
                Task = scene => LoadData(scene, Setting.LastUserObjectID),
                TaskName = $"Load Data : {Setting.LastUserObjectID}",
                Success = moveToNext,
                Error = showError,
            };
            var loadPrivacy = new StoryNode
            {
                Task = scene => LoadPrivacyAsync(scene),
                TaskName = "Load Privacy",
                Success = loadData,
                Error = loadData,
            };
            var pinCheck = new StoryNode
            {
                Task = scene => PinAuthenticationAsync(scene),
                TaskName = "Pin Authentication",
                Success = loadPrivacy,
                Error = showError,
            };
            var authInteractive = new StoryNode
            {
                Task = scene => AzureAD.LoginInteractiveAsync(() => scene), // ID/PW authentication
                TaskName = "Login Interactive",
                Success = loadPrivacy,
                Error = showError,
            };

            StoryOnlineRoot = new StoryNode
            {
                Message = new StreamWriter(new MemoryStream()), // for Root Node
                CTS = new CancellationTokenSource(),            // for Root Node
                Task = scene => AzureAD.LoginSilentAsync(() => scene),  // silent authentication
                TaskName = "Login Silent",
                Success = new StoryNode
                {
                    Task = scene => SaveSetting(scene, userObjectID: AzureAD.UserObjectID),
                    TaskName = "Save Setting",
                    Success = pinCheck,
                    Error = showError,
                },
                Error = authInteractive,
            };

            StoryLocalRoot = new StoryNode
            {
                Message = new StreamWriter(new MemoryStream()), // for Root Node
                CTS = new CancellationTokenSource(),            // for Root Node
                Task = scene => SetLocalModeAsync(scene),
                TaskName = "Set Offline Mode",
                Success = new StoryNode
                {
                    Task = scene => PinAuthenticationAsync(scene),
                    TaskName = "Pin Authentication",
                    Success = loadData,
                    Error = showError,
                },
            };
        }

        private string GetMessage(StoryNode scene)
        {
            scene.Message.Flush();
            scene.Message.BaseStream.Seek(0, SeekOrigin.Begin);
            var sr = new StreamReader(scene.Message.BaseStream, Encoding.UTF8);
            var ret = sr.ReadToEnd();
            scene.Message.BaseStream.SetLength(0);
            return ret;
        }

        private async Task<bool> SaveSetting(StoryNode scene, string userObjectID = null, string displayName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userObjectID) == false)
                {
                    Setting.LastUserObjectID = userObjectID;
                }
                if (string.IsNullOrEmpty(displayName) == false)
                {
                    Setting.LastDisplayName = displayName;
                }
                Setting.LastLoginTimeUtc = DateTime.UtcNow;
                Setting.SaveFile();
                return true;
            }
            catch (Exception ex)
            {
                await Task.Delay(0);
                scene.Message.WriteLine($"Save setting exception : {ex.Message}");
                scene.Message.WriteLine("(E904)");
                return false;
            }
        }

        private async Task<bool> LoadData(StoryNode scene, string userObjectID)
        {
            if (string.IsNullOrEmpty(userObjectID))
            {
                scene.Message.WriteLine($"User Object ID is not set yet.");
                scene.Message.WriteLine($"(E905)");
                return false;
            }
            try
            {
                await Task.Delay(0);
                Persister.LoadFile(userObjectID, isForceReload: true);
                return true;
            }
            catch (Exception ex)
            {
                scene.Message.WriteLine($"Load Data Exception {ex.Message}");
                scene.Message.WriteLine($"(E903)");
                return false;
            }
        }

        private async Task<bool> SetLocalModeAsync(StoryNode scene)
        {
            await Task.Delay(0);
            Setting.LoadFile();

            if (string.IsNullOrEmpty(Setting.LastDisplayName) == false)
            {
                ApplicationView.GetForCurrentView().Title = $"[LOCAL] {(Setting.LastDisplayName ?? "")}";
            }
            else
            {
                ApplicationView.GetForCurrentView().Title = $"[LOCAL]";
            }
            return true;
        }

        private async Task<bool> LoadPrivacyAsync(StoryNode scene)
        {
            var ret = await AzureAD.GetPrivacyDataAsync(() => scene);
            if (ret)
            {
                Setting.LastDisplayName = AzureAD.DisplayName;
                Setting.SaveFile();
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ApplicationView.GetForCurrentView().Title = AzureAD.DisplayName ?? "";
                });
            }
            else
            {
                var title = "";
                if (string.IsNullOrEmpty(Setting.LastDisplayName) == false)
                {
                    title = $"[OFFLINE?] {(Setting.LastDisplayName ?? "")}";
                }
                else
                {
                    title = $"[OFFLINE?]";
                }
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ApplicationView.GetForCurrentView().Title = title;
                });
            }
            return ret;
        }

        private async Task<bool> ResetControl(StoryNode scene)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                LocalModeButton.IsEnabled = true;
                LocalModeButton.Visibility = Visibility.Visible;
                StartButton.IsEnabled = true;
                StartButton.Visibility = Visibility.Visible;
            });
            return true;
        }

        private async Task<bool> AuthenticationErrorAsync(StoryNode scene)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ErrorMessage.Text = GetMessage(scene);
                ErrorMessage.Visibility = string.IsNullOrEmpty(ErrorMessage.Text.Trim()) ? Visibility.Collapsed : Visibility.Visible;
            });
            await Task.Delay(300, scene.CTS.Token);
            return true;
        }

        private async Task<bool> NextPageAsync(StoryNode scene)
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

        private async Task<bool> PinAuthenticationAsync(StoryNode scene)
        {
            try
            {
                var available = await UserConsentVerifier.CheckAvailabilityAsync();
                if (available == UserConsentVerifierAvailability.Available)
                {
                    var res = await UserConsentVerifier.RequestVerificationAsync("tSecret");
                    switch (res)
                    {
                        case UserConsentVerificationResult.Verified:
                            return true;
                        default:
                            scene.Message.WriteLine($"PIN Authentication stopped : {res.ToString()}");
                            return false;
                    }
                }
                else
                {
                    scene.Message.WriteLine($"PIN Authentication is not available.");
                    scene.Message.WriteLine($"(E902)");
                    return false;
                }
            }
            catch (Exception ex)
            {
                scene.Message.WriteLine($"PIN Authentication exception");
                scene.Message.WriteLine($"(E901)");
                scene.Message.WriteLine(ex.Message);
                return false;
            }
        }

        private CancellationTokenSource _currentCTS = null;

        private async Task StartStoryAsync(StoryNode rootNode)
        {
            var cts = _currentCTS = new CancellationTokenSource();
            var mes = rootNode.Message;
            for (var scene = rootNode; scene != null && cts.IsCancellationRequested == false;)
            {
                scene.CTS = cts;                    // inherit CTS
                scene.Message = mes;                // inherit Message
                var ret = await scene.Task(scene);  // Run Task
                scene = ret ? scene.Success : scene.Error;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Reset control state
            StartButton.IsEnabled = true;
            LocalModeButton.IsEnabled = true;
            ErrorMessage.Text = "";
            ErrorMessage.Visibility = Visibility.Collapsed;

            if (e.NavigationMode == NavigationMode.New)
            {
                DelayUtil.Start(TimeSpan.FromMilliseconds(23), () =>
                {
                    StartButton_Click(this, null);
                });
            }
            base.OnNavigatedTo(e);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e) // giveup thread pool control
        {
            StartButton.IsEnabled = false;
            LocalModeButton.IsEnabled = true;

            _ = StartStoryAsync(StoryOnlineRoot);
        }

        private void LocalModeButton_Click(object sender, RoutedEventArgs e) // giveup thread pool control
        {
            if (_currentCTS != null)
            {
                _currentCTS.Cancel();
            }

            StartButton.IsEnabled = false;
            LocalModeButton.IsEnabled = false;

            _ = StartStoryAsync(StoryLocalRoot);
        }
    }
}
