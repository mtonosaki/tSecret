﻿// (c) 2020 Manabu Tonosaki
// Licensed under the MIT license.

using Plugin.Clipboard;
using System;
using System.Collections.Generic;
using System.Linq;
using tSecretCommon;
using tSecretCommon.Models;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace tSecretXamarin
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NoteEntryPage : ContentPage
    {
        protected NotePersister Persister => ((App)Application.Current).Persister;
        protected AuthAzureAD Auth => ((App)Application.Current).Auth;
        protected SettingPersister Setting => ((App)Application.Current).Setting;

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
            Persister.Add((Note)BindingContext);
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