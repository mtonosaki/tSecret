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
            try
            {
                var accounts = await App.AuthenticationClient.GetAccountsAsync();
                var result = await App.AuthenticationClient
                    .AcquireTokenSilent(new[] { "user.read" }, accounts.FirstOrDefault())
                    .ExecuteAsync();

                //await Navigation.PushAsync(new LogoutPage(result));
            }
            catch
            {
                // Do nothing - the user isn't logged in
            }
            base.OnAppearing();
        }
    }
}
