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
        public string FirstErrorMessage { get; set; } = string.Empty;

        private NotePersister Persister => ((App)Application.Current).Persister;
        private AuthAzureAD AzureAD => ((App)Application.Current).Auth;
        private SettingPersister Setting => ((App)Application.Current).Setting;
        private StoryNode StoryOnlineRoot;
        private StoryNode StoryLocalRoot;

        public AuthPage()
        {
            this.InitializeComponent();

            Setting.LoadFile();
            LocalModeButton.IsEnabled = !string.IsNullOrEmpty(Setting.LastUserObjectID);

            // Story Build
            var showError = new StoryNode
            {
                CutAction = AuthenticationError,
                CutActionName = "Show Authentication Error",
                Success = new StoryNode
                {
                    CutAction = ResetControl,
                    CutActionName = "Reset Control",
                },
            };
            var moveToNext = new StoryNode
            {
                CutAction = NextPage,
                CutActionName = "Next Page",
            };
            var loadData = new StoryNode
            {
                CutAction = cut => LoadData(cut),
                CutActionName = $"Load Data : {Setting.LastUserObjectID}",
                Success = moveToNext,
                Error = showError,
            };
            var loadPrivacy = new StoryNode
            {
                CutAction = LoadPrivacy,
                CutActionName = "Load Privacy",
                Success = loadData,
                Error = loadData,
            };
            var pinCheck = new StoryNode
            {
                CutAction = DeviceAuthentication,
                CutActionName = "Pin Authentication",
                Success = loadPrivacy,
                Error = showError,
            };
            var authInteractive = new StoryNode
            {
                CutAction = LoginInteractive,                        // ID/PW authentication
                CutActionName = "Login Interactive",
                Success = loadPrivacy,
                Error = showError,
            };

            StoryOnlineRoot = new StoryNode
            {
                MessageBuffer = new StreamWriter(new MemoryStream()), // for Root Node
                CTS = new CancellationTokenSource(),            // for Root Node
                CutAction = SetAzureADMode,
                CutActionName = "[AzureAD ROOT] SetAzureADMode",
                Success = new StoryNode
                {
                    MessageBuffer = new StreamWriter(new MemoryStream()),
                    CTS = new CancellationTokenSource(),
                    CutAction = LoginSilent,                             // silent authentication
                    CutActionName = "Login Silent",
                    Success = new StoryNode
                    {
                        CutAction = cut => SaveSetting(cut, userObjectID: AzureAD.UserObjectID),
                        CutActionName = "Save Setting",
                        Success = pinCheck,
                        Error = showError,
                    },
                    Error = authInteractive,
                },
                Error = showError,
            };

            StoryLocalRoot = new StoryNode
            {
                MessageBuffer = new StreamWriter(new MemoryStream()), // for Root Node
                CTS = new CancellationTokenSource(),            // for Root Node
                CutAction = SetLocalMode,
                CutActionName = "[LOCAL ROOT] Set Offline Mode",
                Success = new StoryNode
                {
                    CutAction = DeviceAuthentication,
                    CutActionName = "Pin Authentication",
                    Success = loadData,
                    Error = showError,
                },
            };
        }

        private void LoginSilent(StoryNode cut)
        {
            var task = AzureAD.LoginSilentAsync(() => cut);
            task.ContinueWith(delegate
            {
                cut.TaskResult = task.Result;
            });
        }

        private void LoginInteractive(StoryNode cut)
        {
            var backgroundScheduler = TaskScheduler.Default;
            var uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            var task = AzureAD.LoginInteractiveAsync(() => cut);
            task.ContinueWith(delegate
            {
                if (string.IsNullOrEmpty(AzureAD.UserObjectID))
                {
                    cut.TaskResult = false;
                }
                else
                {
                    SaveSetting(cut, userObjectID: AzureAD.UserObjectID); // including cut.TaskResult = ...
                }
            });
        }

        private void SaveSetting(StoryNode cut, string userObjectID = null, string displayName = null)
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
                cut.TaskResult = true;
            }
            catch (Exception ex)
            {
                cut.MessageBuffer.WriteLine($"Save setting exception : {ex.Message}");
                cut.MessageBuffer.WriteLine("(E904)");
                cut.TaskResult = false;
            }
        }

        private void LoadData(StoryNode cut)
        {
            var userObjectID = Setting.LastUserObjectID;
            if (string.IsNullOrEmpty(userObjectID))
            {
                cut.MessageBuffer.WriteLine($"User Object ID is not set yet.");
                cut.MessageBuffer.WriteLine($"(E905)");
                cut.TaskResult = false;
            }
            try
            {
                Persister.LoadFile(userObjectID, isForceReload: true);
                cut.TaskResult = true;
            }
            catch (Exception ex)
            {
                cut.MessageBuffer.WriteLine($"Load Data Exception {ex.Message}");
                cut.MessageBuffer.WriteLine($"(E903)");
                cut.TaskResult = false;
            }
        }

        private void SetAzureADMode(StoryNode cut)
        {
            Setting.LoadFile();
            StartButton.IsEnabled = false;
            ErrorMessage.Text = "";
            cut.TaskResult = true;
        }

        private void SetLocalMode(StoryNode cut)
        {
            Setting.LoadFile();

            if (string.IsNullOrEmpty(Setting.LastDisplayName) == false)
            {
                ApplicationView.GetForCurrentView().Title = $"[LOCAL] {(Setting.LastDisplayName ?? "")}";
            }
            else
            {
                ApplicationView.GetForCurrentView().Title = $"[LOCAL]";
            }
            cut.TaskResult = true;
        }

        private void LoadPrivacy(StoryNode cut)
        {
            var task = AzureAD.GetPrivacyDataAsync(() => cut);
            task.ContinueWith(delegate
            {
                if (task.Result)
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
                cut.TaskResult = task.Result;
            });
        }

        private void ResetControl(StoryNode cut)
        {
            LocalModeButton.IsEnabled = !string.IsNullOrEmpty(Setting.LastUserObjectID);
            LocalModeButton.Visibility = Visibility.Visible;
            StartButton.IsEnabled = true;
            StartButton.Visibility = Visibility.Visible;
            cut.TaskResult = true;
        }

        private void AuthenticationError(StoryNode cut)
        {
            ErrorMessage.Text = GetMessage(cut);
            ErrorMessage.Visibility = string.IsNullOrEmpty(ErrorMessage.Text.Trim()) ? Visibility.Collapsed : Visibility.Visible;
            cut.TaskResult = true;
        }

        private void NextPage(StoryNode cut)
        {
            ConfigUtil.Set("LoginUtc", DateTime.UtcNow.ToString());
            Frame.Navigate(typeof(NoteListPage), null, new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight,
            });
            cut.TaskResult = true;
        }

        private void DeviceAuthentication(StoryNode cut)
        {
            var t1 = UserConsentVerifier.CheckAvailabilityAsync().AsTask();
            t1.ContinueWith(delegate
            {
                var available = t1.Result;
                if (available == UserConsentVerifierAvailability.Available)
                {
                    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        var t2 = UserConsentVerifier.RequestVerificationAsync("tSecret").AsTask();
                        t2.ContinueWith(delegate
                        {
                            if (t2.Result == UserConsentVerificationResult.Verified)
                            {
                                ConfigUtil.Set("LoginUtc", DateTime.UtcNow.ToString());
                                cut.TaskResult = true;
                            }
                            else
                            {
                                cut.MessageBuffer.WriteLine($"PIN Authentication stopped : {t2.Result}");
                                cut.TaskResult = false;
                            }
                        });
                    });
                }
                else
                {
                    cut.MessageBuffer.WriteLine($"PIN Authentication is not available.");
                    cut.MessageBuffer.WriteLine($"(E902)");
                    cut.TaskResult = false;
                }
            });
            //        catch (Exception ex)
            //        {
            //            cut.MessageBuffer.WriteLine($"Local Authentication exception");
            //            cut.MessageBuffer.WriteLine($"(E901-2)");
            //            cut.MessageBuffer.WriteLine(ex.Message);
            //            cut.TaskResult = false;
            //        }
        }

        private string GetMessage(StoryNode cut)
        {
            cut.MessageBuffer.Flush();
            cut.MessageBuffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var sr = new StreamReader(cut.MessageBuffer.BaseStream, Encoding.UTF8);
            var ret = sr.ReadToEnd();
            cut.MessageBuffer.BaseStream.SetLength(0);
            return ret;
        }

        private CancellationTokenSource _currentCTS = null;

        private enum Statues
        {
            WaitingExec,
            WaitingCompleted,
            WaitingNextCut,
        }

        private void StartStory(StoryNode rootNode)
        {
            var cts = _currentCTS = new CancellationTokenSource();
            var mes = rootNode.MessageBuffer;
            var cut = rootNode;
            cut.CTS = cts;
            var status = Statues.WaitingExec;

            var porlingTimer = new DispatcherTimer();
            porlingTimer.Interval = TimeSpan.FromMilliseconds(20);
            porlingTimer.Tick += (s, e) =>
            {
                porlingTimer.Stop();
                if (cut != null && cut.CTS.Token.IsCancellationRequested == false)
                {
                    switch (status)
                    {
                        case Statues.WaitingExec:
                            status = Statues.WaitingCompleted;
                            cut.MessageBuffer = mes;
                            cut.TaskResult = null;
                            cut.CutAction(cut);
                            break;
                        case Statues.WaitingCompleted:
                            if (cut.TaskResult != null)
                            {
                                status = Statues.WaitingNextCut;
                            }
                            break;
                        case Statues.WaitingNextCut:
                            cut = cut.TaskResult == true ? cut.Success : cut.Error;
                            if (cut != null)
                            {
                                cut.CTS = cts;
                            }
                            status = Statues.WaitingExec;
                            break;
                    }
                    porlingTimer.Start();
                }
            };
            porlingTimer.Start();
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
            StartStory(StoryOnlineRoot);
        }

        private void LocalModeButton_Click(object sender, RoutedEventArgs e) // giveup thread pool control
        {
            if (_currentCTS != null)
            {
                _currentCTS.Cancel();
            }

            StartButton.IsEnabled = false;
            LocalModeButton.IsEnabled = false;
            StartStory(StoryLocalRoot);
        }
    }
}
