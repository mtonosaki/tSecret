// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

using System.Linq;
using Tono;
using tSecretCommon.Models;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace tSecretUwp {
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class NoteHistoryPage: Page {
        private Note Note { get; set; }

        public NoteHistoryPage() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            Note = e.Parameter as Note;

            Captions.Children.Clear();
            foreach (NoteHistRecord rec in Note.CaptionHistory.OrderByDescending(a => a.DT)) {
                AddHistRecord(rec, Captions.Children);
            }

            Accounts.Children.Clear();
            foreach (NoteHistRecord rec in Note.AccountIDHistory.OrderByDescending(a => a.DT)) {
                AddHistRecord(rec, Accounts.Children);
            }

            Passwords.Children.Clear();
            foreach (NoteHistRecord rec in Note.PasswordHistory.OrderByDescending(a => a.DT)) {
                AddHistRecord(rec, Passwords.Children);
            }

            Emails.Children.Clear();
            foreach (NoteHistRecord rec in Note.EmailHistory.OrderByDescending(a => a.DT)) {
                AddHistRecord(rec, Emails.Children);
            }

            base.OnNavigatedTo(e);
        }

        private void AddHistRecord(NoteHistRecord rec, UIElementCollection children) {
            Grid grid;
            children.Add(grid = new Grid());
            grid.Children.Add(new TextBox {
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = rec.Value,
                BorderThickness = new Thickness(0, 0, 0, 0),
                IsReadOnly = true,
                IsTextPredictionEnabled = false,
            });
            grid.Children.Add(new TextBlock {
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = new SolidColorBrush(Colors.Gray),
                Text = rec.DT.ToString(TimeUtil.FormatYMDHMS),
            });
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) {
            if (Frame.CanGoBack) {
                Frame.GoBack();
            }
        }
    }
}
