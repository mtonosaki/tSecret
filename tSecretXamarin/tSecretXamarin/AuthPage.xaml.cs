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
                Task = AuthenticationError,
                TaskName = "Show Authentication Error",
                Success = new StoryNode
                {
                    Task = ResetControl,
                    TaskName = "Reset Control",
                },
            };
            var moveToNext = new StoryNode
            {
                Task = NextPage,
                TaskName = "Next Page",
            };
            var loadData = new StoryNode
            {
                Task = scene => LoadData(scene),
                TaskName = $"Load Data : {Setting.LastUserObjectID}",
                Success = moveToNext,
                Error = showError,
            };
            var loadPrivacy = new StoryNode
            {
                Task = LoadPrivacy,
                TaskName = "Load Privacy",
                Success = loadData,
                Error = loadData,
            };
            var pinCheck = new StoryNode
            {
                Task = DeviceAuthentication,
                TaskName = "Pin Authentication",
                Success = loadPrivacy,
                Error = showError,
            };
            var authInteractive = new StoryNode
            {
                Task = LoginInteractive,                        // ID/PW authentication
                TaskName = "Login Interactive",
                Success = loadPrivacy,
                Error = showError,
            };

            StoryOnlineRoot = new StoryNode
            {
                Message = new StreamWriter(new MemoryStream()), // for Root Node
                CTS = new CancellationTokenSource(),            // for Root Node
                Task = LoginSilent,                             // silent authentication
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
                Task = SetLocalMode,
                TaskName = "Set Offline Mode",
                Success = new StoryNode
                {
                    Task = DeviceAuthentication,
                    TaskName = "Pin Authentication",
                    Success = loadData,
                    Error = showError,
                },
            };
        }

        private void LoginSilent(StoryNode scene)
        {
            var task = AzureAD.LoginSilentAsync(() => scene);
            task.ContinueWith(delegate
            {
                scene.TaskResult = task.Result;
            });
        }

        private void LoginInteractive(StoryNode scene)
        {
            var backgroundScheduler = TaskScheduler.Default;
            var uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            var task = AzureAD.LoginInteractiveAsync(() => scene);
            task.ContinueWith(delegate
            {
                scene.TaskResult = task.Result;
            });
        }

        private void SaveSetting(StoryNode scene, string userObjectID = null, string displayName = null)
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
                scene.TaskResult = true;
            }
            catch (Exception ex)
            {
                scene.Message.WriteLine($"Save setting exception : {ex.Message}");
                scene.Message.WriteLine("(E904)");
                scene.TaskResult = false;
            }
        }

        private void LoadData(StoryNode scene)
        {
            var userObjectID = Setting.LastUserObjectID;
            if (string.IsNullOrEmpty(userObjectID))
            {
                scene.Message.WriteLine($"User Object ID is not set yet.");
                scene.Message.WriteLine($"(E905)");
                scene.TaskResult = false;
            }
            try
            {
                Persister.LoadFile(userObjectID, isForceReload: true);
                scene.TaskResult = true;
            }
            catch (Exception ex)
            {
                scene.Message.WriteLine($"Load Data Exception {ex.Message}");
                scene.Message.WriteLine($"(E903)");
                scene.TaskResult = false;
            }
        }

        private void SetLocalMode(StoryNode scene)
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
            scene.TaskResult = true;
        }

        private void LoadPrivacy(StoryNode scene)
        {
            var task = AzureAD.GetPrivacyDataAsync(() => scene);
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
                scene.TaskResult = task.Result;
            });
        }

        private void ResetControl(StoryNode scene)
        {
            LocalModeButton.IsEnabled = true;
            LocalModeButton.IsVisible = true;
            StartButton.IsEnabled = true;
            StartButton.IsVisible = true;
            scene.TaskResult = true;
        }

        private void AuthenticationError(StoryNode scene)
        {
            ErrorMessage.Text = GetMessage(scene);
            ErrorMessage.IsVisible = !string.IsNullOrEmpty(ErrorMessage.Text.Trim());
            scene.TaskResult = true;
        }

        private void NextPage(StoryNode scene)
        {
            Application.Current.Properties["LoginUtc"] = DateTime.UtcNow;
            _ = Navigation.PushAsync(new NoteListPage());   // give-up to control task scheduling.
            scene.TaskResult = true;
        }

        private void DeviceAuthentication(StoryNode scene)
        {
            var ti = DependencyService.Get<IAuthService>(); // get device authentication service
            if (ti == null)
            {
                scene.Message.WriteLine($"No device authentication capability");
                scene.Message.WriteLine($"(E902)");
                scene.TaskResult = false;
            }
            var task = ti.GetAuthentication();
            task.ContinueWith(delegate
            {
                if (task.Result)
                {
                    Application.Current.Properties["LoginUtc"] = DateTime.UtcNow;
                    scene.TaskResult = true;
                }
                else
                {
                    scene.Message.WriteLine($"Device Authentication exception");
                    scene.Message.WriteLine($"(E901)");
                    scene.TaskResult = false;
                }
            });
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

        private CancellationTokenSource _currentCTS = null;

        private enum Statues
        {
            WaitingExec,
            WaitingCompleted,
            WaitingNextScene,
        }

        private void StartStory(StoryNode rootNode)
        {
            var cts = _currentCTS = new CancellationTokenSource();
            var mes = rootNode.Message;
            var scene = rootNode;
            var status = Statues.WaitingExec;

            Device.StartTimer(TimeSpan.FromMilliseconds(20), () =>
            {
                if (scene != null)
                {
                    switch (status)
                    {
                        case Statues.WaitingExec:
                            status = Statues.WaitingCompleted;
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                scene.CTS = cts;
                                scene.Message = mes;
                                scene.Task(scene);
                            });
                            break;
                        case Statues.WaitingCompleted:
                            if (scene.TaskResult != null)
                            {
                                status = Statues.WaitingNextScene;
                            }
                            break;
                        case Statues.WaitingNextScene:
                            scene = scene.TaskResult == true ? scene.Success : scene.Error;
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
            Device.StartTimer(TimeSpan.FromMilliseconds(97), () =>
            {
                OnStartClicked(null, EventArgs.Empty);
                return false;
            });
        }

        private void OnStartClicked(object sender, EventArgs e)
        {
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