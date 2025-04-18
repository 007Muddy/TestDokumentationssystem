using System.Text.Json;
using System.Text;
using Microsoft.Maui.Storage;
using System.Text.Json.Serialization; // For using Preferences to store the JWT token

namespace Dokumentationssystem.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent(); // This links the XAML elements to the code-behind
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
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
                // API URL must match your actual server endpoint
                var response = await httpClient.PostAsync("https://localhost:7250/api/auth/login", content);

                // After successful login
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content
                    var resultJson = await response.Content.ReadAsStringAsync();

                    // Debug to verify the JSON response structure
                    await DisplayAlert("Debug", $"Response JSON: {resultJson}", "OK");

                    // Deserialize the response to extract the token
                    var result = JsonSerializer.Deserialize<LoginResponse>(resultJson);

                    // Check if the token was received
                    if (result != null && !string.IsNullOrEmpty(result.Token))
                    {
                        // Save the JWT token
                        Preferences.Set("JwtToken", result.Token);

                        // Show success message
                        await DisplayAlert("Success", "Login successful!", "OK");

                        // Redirect to the CreateInspectionPage after successful login
                        await Navigation.PushAsync(new CreateInspectionPage());
                    }
                    else
                    {
                        await DisplayAlert("Error", "Login failed: No token received.", "OK");
                    }
                }
                else
                {
                    // Capture and display the API error message
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Login failed: {errorMessage}", "OK");
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions and show an error message
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }

    // Class to map the login response (ensure this matches your API response)
    public class LoginResponse
    {
        [JsonPropertyName("token")]  // Match the JSON field name to the property
        public string Token { get; set; }  // The token field returned by the API
    }
}
