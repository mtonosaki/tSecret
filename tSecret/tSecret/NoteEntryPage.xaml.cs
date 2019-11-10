using Plugin.Clipboard;
using System;
using System.Collections.Generic;
using System.Linq;
using tSecret.Models;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace tSecret
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NoteEntryPage : ContentPage
    {
        private Note Note => (Note)BindingContext;

        public NoteEntryPage()
        {
            InitializeComponent();
            NavigationPage.SetHasBackButton(this, true);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Application.Current.Properties["LoginUtc"] = DateTime.UtcNow;

            Note.ValueChanged += (s, e) =>
            {
                if (NavigationPage.GetHasBackButton(this))
                {
                    NavigationPage.SetHasBackButton(this, false); // Hide back button because of that users want to back with this button even if not saved
                }
            };
        }
        private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            NotePersister.Current.Add((Note)BindingContext);
            await Navigation.PopAsync();
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnHistoryClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new NoteHistoryPage
            {
                BindingContext = BindingContext,
            });
        }
        private void OnTogglePasswordClicked(object sender, EventArgs e)
        {
            Note.IsHidePassword = !Note.IsHidePassword;
        }

        private void showCopiedText(Label tar)
        {
            IEnumerable<Label> ls = new[] { CopiedAccount, CopiedPassword, CopiedEmail };   // Enum labeled "Copied password"
            foreach (Label l in ls.Where(a => ReferenceEquals(a, tar) == false))
            {
                l.IsVisible = false;
            }
            tar.IsVisible = true;
            Device.StartTimer(TimeSpan.FromSeconds(1), () => tar.IsVisible = false);
        }

        private void OnAccountIDClicked(object sender, EventArgs e)
        {
            CrossClipboard.Current.SetText(Note.AccountID);
            showCopiedText(CopiedAccount);
        }

        private void OnCopyPasswordClicked(object sender, EventArgs e)
        {
            CrossClipboard.Current.SetText(Note.Password);
            showCopiedText(CopiedPassword);
        }

        private void OnCopyEmailClicked(object sender, EventArgs e)
        {
            CrossClipboard.Current.SetText(Note.Email);
            showCopiedText(CopiedEmail);
        }
    }
}