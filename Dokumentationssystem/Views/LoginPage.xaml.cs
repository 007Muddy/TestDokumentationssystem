using System.Text.Json;
using System.Text;
using Microsoft.Maui.Storage;

namespace Dokumentationssystem.Views
{
    public partial class LoginPage : ContentPage
    {
        // Define base address based on platform
        public static string BaseAddress =
            DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5119" : "https://dokumentationssystem.onrender.com";
        public static string LoginUrl = $"{BaseAddress}/api/auth/login";

        public LoginPage()
        {
            InitializeComponent(); // This links the XAML elements to the code-behind
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            await AnimateButton((Button)sender);

            var username = UsernameEntry.Text; // UsernameEntry from XAML
            var password = PasswordEntry.Text; // PasswordEntry from XAML

            // Ensure both fields are filled
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Please fill in both fields", "OK");
                return;
            }

            var loginModel = new
            {
                Username = username,
                Password = password
            };

            var httpClient = new HttpClient();
            var json = JsonSerializer.Serialize(loginModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                // Use the platform-specific LoginUrl for the API call
                var response = await httpClient.PostAsync(LoginUrl, content);

                // After successful login
                if (response.IsSuccessStatusCode)
                {
                    // Show success message
                    await DisplayAlert("Success", "Login successful! Welcome to our system", "OK");

                    // Redirect to the CreateInspectionPage after successful login
                    await Navigation.PushAsync(new SeeOrCreateNewInspection());
                }
                else
                {
                    // Capture and display the API error message
                    await DisplayAlert("Error", "Login failed: username or password is incorrect", "OK");
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions and show an error message
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async Task AnimateButton(Button button)
        {
            // Move the button down slightly
            await button.TranslateTo(0, 10, 100, Easing.CubicInOut);

            // Move the button back to its original position
            await button.TranslateTo(0, 0, 100, Easing.CubicInOut);
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            // Animate the button
            await AnimateButton((Button)sender);

            // Navigate back
            await Navigation.PopAsync();
        }
    }
}
