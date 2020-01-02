using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Tono.Gui.Uwp;
using tSecretCommon.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

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
            IEnumerable<TextBlock> ls = new[] { CopiedAccount, CopiedPassword, CopiedEmail };   // Enum labeled "Copied password"
            foreach (var l in ls.Where(a => ReferenceEquals(a, tar) == false))
            {
                l.Visibility = Visibility.Collapsed;
            }
            tar.Visibility = Visibility.Visible;
            DelayUtil.Start(TimeSpan.FromSeconds(1), () => tar.Visibility = Visibility.Collapsed);
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

        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(NoteListPage));
        }
    }
}
