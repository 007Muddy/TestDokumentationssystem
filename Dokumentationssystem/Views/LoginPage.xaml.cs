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
            DeviceInfo.Platform == DevicePlatform.Android ? "https://struct.onrender.com" : "https://localhost:8585";
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

            var username = UsernameEntry.Text;
            var password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Please fill in both fields", "OK");
                return;
            }

            var loginModel = new { Username = username, Password = password };

            var httpClient = new HttpClient();
            var json = JsonSerializer.Serialize(loginModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(LoginUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<LoginResponse>(resultJson);

                    if (result != null && !string.IsNullOrEmpty(result.Token))
                    {
                        Preferences.Set("JwtToken", result.Token);

                        await DisplayAlert("Success", $"Login successful! Role: {result.Role}", "OK"); // Debugging line to show the role

                        if (result.Role == "Admin")
                        {
                            await Navigation.PushAsync(new AdminViewPage());
                        }
                        else
                        {
                            await Navigation.PushAsync(new SeeOrCreateNewInspection());
                        }
                    }
                    else
                    {
                        await DisplayAlert("Error", "Login failed: No token received.", "OK");
                    }
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", "Login failed: Username or password is incorrect", "OK");
                }
            }
            catch (Exception ex)
            {
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

    // Class to map the login response (ensure this matches your API response)
    public class LoginResponse
    {
        [JsonPropertyName("token")]  // Match the JSON field name to the property
        public string Token { get; set; }  // The token field returned by the API

        [JsonPropertyName("role")]  // Assume the API response includes a role field
        public string Role { get; set; }  // The role of the user, e.g., "Admin" or "User"
    }
}
