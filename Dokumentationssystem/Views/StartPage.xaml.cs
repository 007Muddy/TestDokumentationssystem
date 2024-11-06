namespace Dokumentationssystem.Views
{
    public partial class StartPage : ContentPage
    {
        public StartPage()
        {
            InitializeComponent();
        }

        // Navigate to the LoginPage when the "Login" button is clicked
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            await AnimateButton((Button)sender);

            await Navigation.PushAsync(new LoginPage());
        }

        // Navigate to the RegistrationPage when the "Register" button is clicked
        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            await AnimateButton((Button)sender);

            await Navigation.PushAsync(new RegistrationPage());
        }
        private async Task AnimateButton(Button button)
        {
            // Move the button down slightly
            await button.TranslateTo(0, 10, 100, Easing.CubicInOut);

            // Move the button back to its original position
            await button.TranslateTo(0, 0, 100, Easing.CubicInOut);
        }
    }
}
