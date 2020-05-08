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
                Application.Current.MainPage.Title = $"[LOCAL] {(Setting.LastDisplayName ?? "")}";
            }
            else
            {
                Application.Current.MainPage.Title = $"[LOCAL]";
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
                Device.BeginInvokeOnMainThread(() =>
                {
                    Application.Current.MainPage.Title = AzureAD.DisplayName ?? "";
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
                Device.BeginInvokeOnMainThread(() =>
                {
                    Application.Current.MainPage.Title = title;
                });
            }
            return ret;
        }

        private async Task<bool> ResetControl(StoryNode scene)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                LocalModeButton.IsEnabled = true;
                LocalModeButton.IsVisible = true;
                StartButton.IsEnabled = true;
                StartButton.IsVisible = true;
            });
            await Task.Delay(0);
            return true;
        }

        private async Task<bool> AuthenticationErrorAsync(StoryNode scene)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                ErrorMessage.Text = GetMessage(scene);
                ErrorMessage.IsVisible = !string.IsNullOrEmpty(ErrorMessage.Text.Trim());
            });
            await Task.Delay(300, scene.CTS.Token);
            return true;
        }

        private async Task<bool> NextPageAsync(StoryNode scene)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Application.Current.Properties["LoginUtc"] = DateTime.UtcNow;
                // await Navigation.PushAsync(new NoteListPage()); // TODO:
            });
            await Task.Delay(0);
            return true;
        }

        private async Task<bool> PinAuthenticationAsync(StoryNode scene)
        {
            var ti = DependencyService.Get<IAuthService>(); // get device authentication service
            if (ti == null)
            {
                scene.Message.WriteLine($"ERROR: No capability of local authentication");
                scene.Message.WriteLine($"(E902)");
                return false;
            }
            bool ret = await ti.GetAuthentication();
            if (ret)
            {
                Application.Current.Properties["LoginUtc"] = DateTime.UtcNow;
            }
            else
            {
                scene.Message.WriteLine($"PIN Authentication exception");
                scene.Message.WriteLine($"(E901)");
            }
            return ret;
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

            _ = StartStoryAsync(StoryOnlineRoot);    // giveup thread pool control
        }

        private void LocalModeButtonClicked(object sender, EventArgs e)
        {
            if (_currentCTS != null)
            {
                _currentCTS.Cancel();
            }

            StartButton.IsEnabled = false;
            LocalModeButton.IsEnabled = false;

            _ = StartStoryAsync(StoryLocalRoot);    // giveup thread pool control
        }
    }
}