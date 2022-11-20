// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Tono;
using Tono.Gui.Uwp;
using tSecretCommon;
using tSecretCommon.Models;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace tSecretUwp {
    public sealed partial class NoteListPage: Page {
        private AuthAzureAD Auth => ((App)Application.Current).Auth;
        private NotePersister Persister => ((App)Application.Current).Persister;

        public NoteListPage() {
            InitializeComponent();
        }

        private bool IsShowAll {
            get => ShowDeleted.IsChecked ?? false;
            set => ShowDeleted.IsChecked = value;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            log(null);
            Refresh();
            if (e.NavigationMode == NavigationMode.New) {
                ConfigUtil.Set("LoginUtc", DateTime.UtcNow.ToString());
            }
            if (e.NavigationMode == NavigationMode.Back) {
                string prevID = ConfigUtil.Get("PreviousNoteID", "--");
                Note note = lvMain.Items.Select(a => (Note)a).Where(a => a.ID.ToString() == prevID).FirstOrDefault();
                if (note != default) {
                    DelayUtil.Start(TimeSpan.FromMilliseconds(100), () => {
                        lvMain.ScrollIntoView(note);
                        lvMain.SelectedItem = note;
                    });
                }
            }
            base.OnNavigatedTo(e);
        }
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            Logout.IsEnabled = Auth.IsAuthenticated;

            if (e.Parameter is Note note) {
                ConfigUtil.Set("PreviousNoteID", note.ID.ToString());
            } else {
                if (e.SourcePageType != typeof(AuthPage)) {
                    e.Cancel = true;
                }
            }
            base.OnNavigatingFrom(e);
        }

        private App App => (App)Application.Current;

        private void Refresh() {
            // Delete empty instance
            List<Note> dels = new List<Note>();
            foreach (Note note in App.Persister) {
                if (string.IsNullOrEmpty(note.AccountID?.Trim()) &&
                    string.IsNullOrEmpty(note.Caption?.Trim()) &&
                    string.IsNullOrEmpty(note.Password?.Trim())) {
                    dels.Add(note);
                }
            }
            foreach (Note note in dels) {
                App.Persister.Remove(note);
            }

            ObservableCollection<Note> dat = new ObservableCollection<Note>();
            foreach (Note note in App.Persister
                                    .Where(rec => IsShowAll || rec.IsDeleted == false)
                                    .OrderBy(rec => $"{rec.CaptionRubi1}--{rec.CaptionRubi}")
            ) {
                dat.Add(note);
            }
            lvMain.ItemsSource = dat;

            // Make index buttons
            IndexButtons.Children.Clear();

            foreach (string rubi1 in App.Persister
                                    .Select(a => a.CaptionRubi1)
                                    .Where(a => a != "@")
                                    .Distinct()
                                    .OrderBy(a => a[0])) {
                Button btn;
                IndexButtons.Children.Add(btn = new Button {
                    Content = rubi1,
                    FontSize = 12,
                    Height = 28,
                    Margin = new Thickness { Left = -6, Top = -6, Right = -6, Bottom = -6 },
                    Foreground = new SolidColorBrush(Colors.Magenta),
                    Background = new SolidColorBrush(Colors.Transparent),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    ClickMode = ClickMode.Press,
                    KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Auto,
                });
                btn.Click += Index_Click;
                if (Enum.TryParse<VirtualKey>(rubi1, out VirtualKey key)) {
                    btn.KeyboardAccelerators.Add(new KeyboardAccelerator {
                        Key = key,
                    });
                }
            }
        }

        private void Index_Click(object sender, RoutedEventArgs e) {
            if (sender is Button btn && btn.Content is string caption) {
                for (int i = 0; i < lvMain.Items.Count; i++) {
                    Note item = lvMain.Items[i] as Note;
                    if (item?.CaptionRubi1 == caption) {
                        lvMain.ScrollIntoView(item, ScrollIntoViewAlignment.Leading);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Inplementation of Sync event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Button_Sync_Click(object sender, RoutedEventArgs e) {
            try {
                KurukuruIn();
                await syncProc();
                KurukuruOut();
                _ = await new MessageDialog($"Downloaded from Cloud", "tSecret").ShowAsync();
            } catch (Exception ex) {
                _ = await new MessageDialog($"Cloud sync error\r\n{ex.Message}", "tSecret").ShowAsync();
                log(null);
                KurukuruOut();
            }
        }

        /// <summary>
        /// Data syncronize between Local PC and Cloud storage
        /// </summary>
        /// <returns></returns>
        private async System.Threading.Tasks.Task syncProc() {
            log("Download from cloud...");
            _ = await App.Persister.Sync(false);
            Refresh();
            log(null);
        }

        private void KurukuruIn() {
            Kurukuru.Visibility = Visibility.Visible;
        }

        private void KurukuruOut() {
            Kurukuru.Visibility = Visibility.Collapsed;
        }

        private void log(string str) {
            StatusBar.Text = str ?? "tSecret (c)2019-2020 Manabu Tonosaki Allrights reserved.";
        }

        /// <summary>
        /// move to detail page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvMain_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
            _ = Frame.Navigate(typeof(NoteEntryPage), lvMain.SelectedItem, new SlideNavigationTransitionInfo {
                Effect = SlideNavigationTransitionEffect.FromRight,
            });
        }

        private void ShowDeleted_Click(object sender, RoutedEventArgs e) {
            Refresh();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) {
            if (Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        private void NewButton_Click(object sender, RoutedEventArgs e) {
            Note note = new Note {
                ID = Guid.NewGuid(),
                CreatedDateTime = DateTime.Now,
            };
            App.Persister.Add(note);
            _ = Frame.Navigate(typeof(NoteEntryPage), note, new SlideNavigationTransitionInfo {
                Effect = SlideNavigationTransitionEffect.FromRight,
            });
        }

        private async void Logout_Click(object sender, RoutedEventArgs e) {
            if (Auth.IsAuthenticated) {
                Logout.IsEnabled = false;
                Persister.SaveFile();

                _ = await Auth.LogoutAsync(() => new StoryNode { CTS = new CancellationTokenSource(5000), });

                if (Auth.IsAuthenticated == false) {
                    ApplicationView.GetForCurrentView().Title = "";

                    if (Frame.CanGoBack) {
                        Frame.GoBack();
                    } else {
                        _ = Frame.Navigate(typeof(AuthPage));
                    }
                } else {
                    Logout.IsEnabled = Auth.IsAuthenticated;
                }
            } else {
                _ = await new MessageDialog($"Your have not logged in yet (LOCAL Mode)", "tSecret").ShowAsync();
            }
        }

        private void ClearClipBoard_Click(object sender, RoutedEventArgs e) {
            ClipboardUtil.Current.Set("");
            log("The clipboard text has been erased.");
        }

        private void ContextMenu_Copy_Click(object sender, RoutedEventArgs e) {
            MenuFlyoutItem mi = (MenuFlyoutItem)sender;
            string val = mi.Tag?.ToString();
            if (string.IsNullOrEmpty(val) == false) {
                ClipboardUtil.Current.Set(val);
                if (mi.Text.Contains("Pass", StringComparison.CurrentCultureIgnoreCase)) {
                    log($"{mi.Text} : {StrUtil.Repeat("●", val.Length)}");
                } else {
                    log($"{mi.Text} : {val}");
                }
            } else {
                log($"Warning : Cancelled to {mi.Text} (no value)");
            }
        }
    }
}
