// (c) 2020 Manabu Tonosaki
// Licensed under the MIT license.

namespace tSecretXamarin.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();

            LoadApplication(new tSecretXamarin.App());
        }
    }
}
