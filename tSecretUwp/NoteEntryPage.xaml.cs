// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

using System;
using System.Linq;
using Tono.Gui.Uwp;
using tSecretCommon.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace tSecretUwp
{
    /// <summary>
    /// Note detail editor
    /// </summary>
    public sealed partial class NoteEntryPage : Page
    {
        public Note Note { get; private set; }

        public NoteEntryPage()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Note = e.Parameter as Note;
            base.OnNavigatedTo(e);
        }

        private void ClearClipBoard_Click(object sender, RoutedEventArgs e)
        {
            ClipboardUtil.Current.Set("");
            showCopiedText(ClearClipboard);
        }

        private void CopyAccount_Click(object sender, RoutedEventArgs e)
        {
            ClipboardUtil.Current.Set(Note.AccountID);
            showCopiedText(CopiedAccount);
        }

        private void CopyPassword_Click(object sender, RoutedEventArgs e)
        {
            ClipboardUtil.Current.Set(Note.Password);
            showCopiedText(CopiedPassword);
        }

        private void CopyEmail_Click(object sender, RoutedEventArgs e)
        {
            ClipboardUtil.Current.Set(Note.Email);
            showCopiedText(CopiedEmail);
        }

        private void showCopiedText(TextBlock tar)
        {
            var ls = new[] { CopiedAccount, CopiedPassword, CopiedEmail, ClearClipboard };   // Enum labeled "Copied password"
            foreach (var l in ls.Where(a => ReferenceEquals(a, tar) == false))
            {
                l.Visibility = Visibility.Collapsed;
            }
            tar.Visibility = Visibility.Visible;
            DelayUtil.Start(TimeSpan.FromSeconds(1.5), () => tar.Visibility = Visibility.Collapsed);
        }


        private void ShowPassword_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordShow.Visibility == Visibility.Collapsed)
            {
                PasswordHide.Visibility = Visibility.Collapsed;
                PasswordShow.Visibility = Visibility.Visible;
                PasswordVisibleMode.Glyph = "\xe1f7";
            }
            else
            {
                PasswordShow.Visibility = Visibility.Collapsed;
                PasswordHide.Visibility = Visibility.Visible;
                PasswordVisibleMode.Glyph = "\xe1f6";
            }
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(NoteHistoryPage), Note, new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight,
            });
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
            else
            {
                Frame.Navigate(typeof(NoteListPage));
            }
        }
    }
}
