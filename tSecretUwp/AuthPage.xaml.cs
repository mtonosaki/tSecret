using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Tono.Gui.Uwp;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Credentials.UI;
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
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class AuthPage : Page
    {
        public string FirstErrorMessage { get; set; } = string.Empty;

        public AuthPage()
        {
            this.InitializeComponent();

            DelayUtil.Start(TimeSpan.FromMilliseconds(97), () =>
            {
                var dmy = AuthenticateAsync();
            });
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var dmy = AuthenticateAsync();
        }

        private async Task<bool> AuthenticateAsync()
        {
            ErrorMessage.Visibility = Visibility.Collapsed;
            FirstErrorMessage = string.Empty;

            try
            {
                var res = await UserConsentVerifier.RequestVerificationAsync("tSecret");
                switch (res)
                {
                    case UserConsentVerificationResult.Verified:
                        ConfigUtil.Set("LoginUtc", DateTime.UtcNow.ToString());
                        this.Frame.Navigate(typeof(NoteListPage));
                        return true;
                    default:
                        ErrorMessage.Text = "Authentication error";
                        ErrorMessage.Visibility = Visibility.Visible;
                        return false;
                }
            }
            catch (Exception)
            {
                ErrorMessage.Text = "ERROR: Cannot use tSecret on this platform";
                ErrorMessage.Visibility = Visibility.Visible;
                StartButton.IsEnabled = false;
                return false;
            }
        }
    }
}
