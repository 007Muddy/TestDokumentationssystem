using System.Text.Json;
using System.Text;

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

            // Change this to the actual API endpoint
            var response = await httpClient.PostAsync("https://localhost:7250/api/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Success", "Login successful!", "OK");
                // Navigate to the next page after successful login
                await Navigation.PushAsync(new InspectionListPage()); // Inspection page
            }

            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"Login failed: {errorMessage}", "OK");
            }
        }
    }
}
