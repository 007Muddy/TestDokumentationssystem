using System.Text.Json;
using System.Text;
using System.Net.Http;
using Microsoft.Maui.Controls;

namespace Dokumentationssystem.Views
{
    public partial class RegistrationPage : ContentPage
    {
        // Define the base address and registration URL based on the platform
        public static string BaseAddress =
            DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5119" : "http://localhost:5119";
        public static string RegisterUrl = $"{BaseAddress}/api/auth/register";

        public RegistrationPage()
        {
            InitializeComponent();
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

            // Prepare the data to send to the API
            var registerModel = new
            {
                Username = username,
                Email = email,
                Password = password
            };

            // Create a new HttpClient to make the request
            var httpClient = new HttpClient();

            // Serialize the model into JSON format
            var json = JsonSerializer.Serialize(registerModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                // Use the platform-specific RegisterUrl for the API call
                var response = await httpClient.PostAsync(RegisterUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    // Display success message
                    await DisplayAlert("Success", "User registered successfully!", "OK");
                    await Navigation.PushAsync(new LoginPage());
                }
                else
                {
                    // Handle errors (show the error message returned from the API)
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Registration failed: {errorMessage}", "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (like network issues)
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }
}
