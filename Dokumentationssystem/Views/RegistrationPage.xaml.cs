using System.Text.Json;
using System.Text;
using System.Net.Http;
using Microsoft.Maui.Controls;

namespace Dokumentationssystem.Views
{
    public partial class RegistrationPage : ContentPage
    {
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
                // Call your API endpoint (replace the URL with your actual API URL)
                var response = await httpClient.PostAsync("https://localhost:7250/api/auth/register", content);

                if (response.IsSuccessStatusCode)
                {
                    // Display success message
                    await DisplayAlert("Success", "User registered successfully!", "OK");
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
