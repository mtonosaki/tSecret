namespace tSecret.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
            tSecret.App app = new tSecret.App();
            LoadApplication(app);
        }
    }
}
