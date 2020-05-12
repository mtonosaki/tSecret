using Plugin.Clipboard;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Tono;
using tSecretCommon;
using tSecretCommon.Models;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.Xaml;

namespace tSecretXamarin
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NoteListPage : ContentPage
    {
        protected NotePersister Persister => ((App)Application.Current).Persister;
        protected AuthAzureAD Auth => ((App)Application.Current).Auth;
        protected SettingPersister Setting => ((App)Application.Current).Setting;

        public NoteListPage()
        {
            InitializeComponent();
            listView.Refreshing += OnCloudSyncByListViewRefresh;
            listView.IsPullToRefreshEnabled = true;

            MessagingCenter.Subscribe<IKeyboardListener, string>(this, "KeyboardListener", OnKeyDown);
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Clear page change history
            XamarinUtil.ClearPageTrace(Navigation, this);
            Application.Current.Properties["LoginUtc"] = DateTime.UtcNow;
            Refresh();
        }
        /// <summary>
        /// KeyDown Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="key"></param>
        private void OnKeyDown(object sender, string key)
        {
            if ("1ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(key) >= 0)
            {
                foreach (RecordGroup items in listView.ItemsSource)
                {
                    if (items.Rubi1.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                    {
                        listView.ScrollTo(items.First(), ScrollToPosition.Center, false);
                        return;
                    }
                }
            }
        }

        #region Kurukuru
        private int kurukuruCounter = 0;
        private readonly object kurukuruLock = new object();

        private void KurukuruIn()
        {
            lock (kurukuruLock)
            {
                kurukuruCounter++;
                Device.BeginInvokeOnMainThread(() =>
                {
                    Progress.IsVisible = true;
                    Kurukuru.IsVisible = true;
                });
            }
        }
        private void KurukuruOut()
        {
            lock (kurukuruLock)
            {
                if (kurukuruCounter > 0)
                {
                    kurukuruCounter--;
                    if (kurukuruCounter < 1)
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            Progress.IsVisible = false;
                            Kurukuru.IsVisible = false;
                        });
                    }
                }
            }
        }
        #endregion

        public class RecordGroup : ObservableCollection<Note>
        {
            public string Rubi1 { get; set; }
        }

        private void Refresh()
        {
            var dat = new List<RecordGroup>();
            RecordGroup g = null;
            foreach (Note note in Persister
                                    .Where(d => IsShowAll || d.IsDeleted == false)
                                    .OrderBy(d => $"{d.CaptionRubi1}--{d.CaptionRubi}")
            )
            {
                if (g == null)
                {
                    dat.Add(g = new RecordGroup
                    {
                        Rubi1 = note.CaptionRubi1,
                    });
                }
                else
                {
                    if (note.CaptionRubi1 != g.Rubi1)
                    {
                        dat.Add(g = new RecordGroup
                        {
                            Rubi1 = note.CaptionRubi1,
                        });
                    }
                }
                g.Add(note);
            }
            listView.ItemsSource = dat;
        }

        private async void OnNoteAddedClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new NoteEntryPage
            {
                BindingContext = new Note
                {
                    ID = Guid.NewGuid(),
                    CreatedDateTime = DateTime.Now,
                },
            });
        }

        private async void OnListViewItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
            {
                Note item = e.SelectedItem as Note;
                if (sender is ListView lv)
                {
                    lv.SelectedItem = null;  // 選択状態だとList画面に帰ってきた時、同アイテムを選択してもイベント発行されないため、意図的に選択を解除した状態にしておく
                }

                await Navigation.PushAsync(new NoteEntryPage
                {
                    BindingContext = item,
                });
            }
        }

        private void log(string str)
        {
            if (str == null)
            {
                StatusBar.Text = "tSecret (c)2019 Manabu Tonosaki";
            }
            else
            {
                StatusBar.Text = str;
            }
        }

        private async void OnCloudSyncByListViewRefresh(object sender, EventArgs e)
        {
            try
            {
                listView.IsRefreshing = true;
                await syncProc();
                listView.IsRefreshing = false;
            }
            catch (Exception ex)
            {
                await DisplayAlert("ERROR:tSecret", $"Cloud communication exception.\r\n{ex.Message}", "Cancel");
                log(null);
                listView.IsRefreshing = false;
            }
        }

        private async void OnCloudSync(object sender, EventArgs e)
        {
            try
            {
                KurukuruIn();
                await syncProc();
                KurukuruOut();
                await DisplayAlert("tSecret", $"The tSecret data have been saved into cloud server.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("ERROR:tSecret", $"Cloud communication exception.\r\n{ex.Message}", "Cancel");
                log(null);
                KurukuruOut();
            }
        }

        private async System.Threading.Tasks.Task syncProc()
        {
            log("Cloud sync...");
            await Persister.Sync();
            Refresh();
            log(null);
        }

        private async void OnCloudUpload(object sender, EventArgs e)
        {
            try
            {
                KurukuruIn();
                log("Uploading to cloud...");
                await Persister.Upload();
                log(null);
                KurukuruOut();
                await DisplayAlert("tSecret", $"The tSecret data have been saved into cloud server.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("ERROR:tSecret", $"Cloud communication exception.\r\n{ex.Message}", "Cancel");
                log(null);
                KurukuruOut();
            }
        }

        private async void OnCloudDownload(object sender, EventArgs e)
        {
            try
            {
                KurukuruIn();
                log("Downloading from cloud...");
                await Persister.Download();
                Refresh();
                log(null);
                KurukuruOut();
                await DisplayAlert("tSecret", $"Load cloud data successfully.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("ERROR:tSecret", $"Cloud communication exception.\r\n{ex.Message}", "Cancel");
                log(null);
                KurukuruOut();
            }
        }

        private bool IsShowAll { get; set; }

        private void OnShowAllClicked(object sender, EventArgs e)
        {
            if (IsShowAll)
            {
                IsShowAll = false;
                ShowAllButton.Text = "Active";
                if (Device.RuntimePlatform == Device.UWP)
                {
                    ShowAllButton.IconImageSource = ImageSource.FromFile("cc_btn_checked0.png");
                }
            }
            else
            {
                IsShowAll = true;
                ShowAllButton.Text = "All";
                if (Device.RuntimePlatform == Device.UWP)
                {
                    ShowAllButton.IconImageSource = ImageSource.FromFile("cc_btn_checked1.png");
                }
            }
            Refresh();
        }

        /// <summary>
        /// レコード削除（未使用）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDeleteRecordClicked(object sender, EventArgs e)
        {
            if (sender is MenuItem mi && mi.BindingContext is Note note)
            {
                note.IsDeleted = true;
                Persister.Add(note);
                Refresh();
            }
        }

        private void OnCopyIdClicked(object sender, EventArgs e)
        {
            if (sender is MenuItem mi && mi.BindingContext is Note note)
            {
                CrossClipboard.Current.SetText(note.AccountID);
                log($"Copy Account ID : {note.AccountID}");
            }
        }

        private void OnCopyPwClicked(object sender, EventArgs e)
        {
            if (sender is MenuItem mi && mi.BindingContext is Note note)
            {
                CrossClipboard.Current.SetText(note.Password);
                log($"Copy Password : {StrUtil.Repeat("●", note.Password.Length)}");
            }
        }

        private async void OnLogoffButton(object sender, EventArgs e)
        {
            if (Auth.IsAuthenticated)
            {
                var res = await DisplayAlert($"tSecret", "Do you LOG-OUT now?", "LOG-OUT", "Cancel");
                if (res)
                {
                    LogoffButton.IsEnabled = false;
                    Persister.SaveFile();

                    await Auth.LogoutAsync(() => new StoryNode { CTS = new CancellationTokenSource(5000), });
                    var success = Auth.IsAuthenticated == false;

                    if (success)
                    {
                        Application.Current.MainPage.Title = "";
                        await Navigation.PushAsync(new AuthPage(), true);   // give-up to control task scheduling.
                                                                            // give-up to use Navigation.PopToRootAsync (not working)
                    }
                    else
                    {
                        LogoffButton.IsEnabled = true;
                    }
                }
            }
            else
            {
                await DisplayAlert($"Your have not logged in yet (LOCAL Mode)", "tSecret", "Cancel");
            }
        }

        private void OnClearClipboard(object sender, EventArgs e)
        {
            CrossClipboard.Current.SetText("");
            log($"Clear Clipboard text.");
        }

        private void OnExitButton(object sender, EventArgs e)
        {
            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    Thread.CurrentThread.Abort();
                    break;
                case Device.UWP:
                    System.Environment.Exit(0);
                    break;
                case Device.Android:
                    System.Environment.Exit(0);
                    break;
            }
        }
    }
}