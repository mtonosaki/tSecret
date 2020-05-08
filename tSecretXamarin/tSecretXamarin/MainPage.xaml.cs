using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace tSecretXamarin
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            var scopes = new[] { "user.read" };
            AuthenticationResult authResult = null;

            var accounts = await App.AuthenticationClient.GetAccountsAsync();
            try
            {
                authResult = await App.AuthenticationClient
                    .AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                try
                {
                    authResult = await App.AuthenticationClient
                        .AcquireTokenInteractive(scopes)
                        .WithParentActivityOrWindow(App.ParentWindow)
                        .ExecuteAsync();
                }
                catch (Exception ex2)
                {
                    await DisplayAlert("Acquire token interactive failed. See exception message for details: ", ex2.Message, "Dismiss");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Acquire token interactive failed. See exception message for details: ", ex.Message, "Dismiss");
            }
            base.OnAppearing();
        }
    }
}
