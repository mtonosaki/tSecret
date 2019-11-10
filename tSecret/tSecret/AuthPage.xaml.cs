using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace tSecret
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AuthPage : ContentPage
    {
        public bool IsAutoAuthentication { get; set; }
        public string FirstErrorMessage { get; set; } = string.Empty;

        public AuthPage()
        {
            InitializeComponent();
        }

        private async void OnStartClicked(object sender, EventArgs e)
        {
            await AuthenticateAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (FirstErrorMessage != string.Empty)
            {
                ErrorMessage.Text = FirstErrorMessage;
            }
            if (IsAutoAuthentication)
            {
                Device.StartTimer(TimeSpan.FromMilliseconds(97), AutoAuthenticate);
            }
        }

        private bool AutoAuthenticate()
        {
            var dmy = AuthenticateAsync();
            return false;
        }

        private async Task AuthenticateAsync()
        {
            ErrorMessage.IsVisible = false;
            FirstErrorMessage = string.Empty;

            IAuthService ti = DependencyService.Get<IAuthService>();
            if (ti == null)
            {
                ErrorMessage.Text = "ERROR: Cannot use tSecret on this platform";
                ErrorMessage.IsVisible = true;
                StartButton.IsEnabled = false;
                return;
            }
            bool ret = await ti.GetAuthentication();
            if (ret)
            {
                Application.Current.Properties["LoginUtc"] = DateTime.UtcNow;
                await Navigation.PushAsync(new NoteListPage());
            }
            else
            {
                ErrorMessage.Text = "Authentication error";
                ErrorMessage.IsVisible = true;
            }
        }
    }
}