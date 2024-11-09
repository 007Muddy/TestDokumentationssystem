using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;

namespace Dokumentationssystem.Views
{
    public partial class RegistrationPage : ContentPage
    {
        // Define the base address and registration URL based on the platform
        public static string BaseAddress =
            DeviceInfo.Platform == DevicePlatform.Android ? "https://struct.onrender.com" : "https://struct.onrender.com";
        public static string RegisterUrl = $"{BaseAddress}/api/auth/register";
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
        }
        public RegistrationPage()
        {
            InitializeComponent();
        }
        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            // Animate the button
            await AnimateButton((Button)sender);

            // Navigate back
            await Navigation.PopAsync();
        }
        private async Task AnimateButton(Button button)
        {
            // Move the button down slightly
            await button.TranslateTo(0, 10, 100, Easing.CubicInOut);

            // Move the button back to its original position
            await button.TranslateTo(0, 0, 100, Easing.CubicInOut);
        }

        // This method will be triggered when the user clicks the Register button
        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            // Get input values
            var username = UsernameEntry.Text;
            var email = EmailEntry.Text;
            var password = PasswordEntry.Text;

            // Validate inputs
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Please fill all fields", "OK");
                return;
            }

            // Validate email format
            if (!IsValidEmail(email))
            {
                await DisplayAlert("Error", "Please enter a valid email address", "OK");
                return;
            }

            // Prepare the data to send to the API
            var registerModel = new
            {
                Username = username,
                Email = email,
                Password = password
            };

            // Create a new HttpClient to make the request
            var httpClient = new HttpClient();

            var json = JsonSerializer.Serialize(registerModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(RegisterUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success", "User registered successfully!", "OK");
                    await Navigation.PushAsync(new LoginPage());
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Registration failed: {errorMessage}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
      }

        // Helper method to validate email format
        private bool IsValidEmail(string email)
        {
            var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, emailRegex);
        }
    }
}
