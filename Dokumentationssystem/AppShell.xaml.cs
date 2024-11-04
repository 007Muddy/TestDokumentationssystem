namespace Dokumentationssystem
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Set the back button visibility for all navigation items to false
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
        }
    }

}
