using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tSecretCommon;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace tSecretXamarin
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AuthPage : ContentPage
    {
        public string FirstErrorMessage { get; set; } = string.Empty;

        private NotePersister Persister => ((App)Application.Current).Persister;
        private AuthAzureAD AzureAD => ((App)Application.Current).Auth;
        private SettingPersister Setting => ((App)Application.Current).Setting;
        private StoryNode StoryOnlineRoot;
        private StoryNode StoryLocalRoot;

        public AuthPage()
        {
            InitializeComponent();

            Setting.LoadFile();

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
                cut.TaskResult = task.Result;
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
                Application.Current.MainPage.Title = $"[LOCAL] {(Setting.LastDisplayName ?? "")}";
            }
            else
            {
                Application.Current.MainPage.Title = $"[LOCAL]";
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
                    Application.Current.MainPage.Title = AzureAD.DisplayName ?? "";
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
                    Application.Current.MainPage.Title = title;
                }
                cut.TaskResult = task.Result;
            });
        }

        private void ResetControl(StoryNode cut)
        {
            LocalModeButton.IsEnabled = true;
            LocalModeButton.IsVisible = true;
            StartButton.IsEnabled = true;
            StartButton.IsVisible = true;
            cut.TaskResult = true;
        }

        private void AuthenticationError(StoryNode cut)
        {
            ErrorMessage.Text = GetMessage(cut);
            ErrorMessage.IsVisible = !string.IsNullOrEmpty(ErrorMessage.Text.Trim());
            cut.TaskResult = true;
        }

        private void NextPage(StoryNode cut)
        {
            Application.Current.Properties["LoginUtc"] = DateTime.UtcNow;
            _ = Navigation.PushAsync(new NoteListPage(), true);   // give-up to control task scheduling.
            cut.TaskResult = true;
        }

        private void DeviceAuthentication(StoryNode cut)
        {
            var ti = DependencyService.Get<IAuthService>(); // get device authentication service
            if (ti == null)
            {
                cut.MessageBuffer.WriteLine($"No device authentication capability");
                cut.MessageBuffer.WriteLine($"(E902)");
                cut.TaskResult = false;
                return;
            }
            var task = ti.GetAuthentication();
            task.ContinueWith(delegate
            {
                if (task.Result)
                {
                    Application.Current.Properties["LoginUtc"] = DateTime.UtcNow;
                    cut.TaskResult = true;
                }
                else
                {
                    cut.MessageBuffer.WriteLine($"Device Authentication exception");
                    cut.MessageBuffer.WriteLine($"(E901)");
                    cut.TaskResult = false;
                }
            });
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

            Device.StartTimer(TimeSpan.FromMilliseconds(100), () =>
            {
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
                            if( cut != null)
                            {
                                cut.CTS = cts;
                            }
                            status = Statues.WaitingExec;
                            break;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            StartButton.IsEnabled = true;
            LocalModeButton.IsEnabled = true;

            if (string.IsNullOrEmpty(FirstErrorMessage) == false)
            {
                ErrorMessage.Text = FirstErrorMessage;
                ErrorMessage.IsVisible = true;
            }
            else
            {
                ErrorMessage.Text = "";
                ErrorMessage.IsVisible = false;
            }
            //Device.StartTimer(TimeSpan.FromMilliseconds(97), () =>
            //{
            //    OnStartClicked(null, EventArgs.Empty);
            //    return false;
            //});
        }

        private void OnStartClicked(object sender, EventArgs e)
        {
            if (_currentCTS != null)
            {
                _currentCTS.Cancel();
            }
            StartButton.IsEnabled = false;
            LocalModeButton.IsEnabled = true;
            StartStory(StoryOnlineRoot);    // giveup thread pool control
        }

        private void LocalModeButtonClicked(object sender, EventArgs e)
        {
            if (_currentCTS != null)
            {
                _currentCTS.Cancel();
            }

            StartButton.IsEnabled = false;
            LocalModeButton.IsEnabled = false;
            StartStory(StoryLocalRoot);    // giveup thread pool control
        }
    }
}