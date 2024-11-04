using System.Text.Json;
using System.Text;
using Microsoft.Maui.Storage;
using System.Text.Json.Serialization;

namespace Dokumentationssystem.Views
{
    public partial class LoginPage : ContentPage
    {
        // Define base address based on platform
        public static string BaseAddress =
            DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5119" : "http://localhost:5119";
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
                    // Read the response content
                    var resultJson = await response.Content.ReadAsStringAsync();

                    // Deserialize the response to extract the token
                    var result = JsonSerializer.Deserialize<LoginResponse>(resultJson);

                    // Check if the token was received
                    if (result != null && !string.IsNullOrEmpty(result.Token))
                    {
                        // Save the JWT token
                        Preferences.Set("JwtToken", result.Token);

                        // Show success message
                        await DisplayAlert("Success", "Login successful! welcome to our system", "OK");

                        // Redirect to the CreateInspectionPage after successful login
                        await Navigation.PushAsync(new SeeOrCreateNewInspection());
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
                    await DisplayAlert("Error", $"Login failed: username or password is incorrect", "OK");
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
