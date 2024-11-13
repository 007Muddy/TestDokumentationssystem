using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using System.Text.Json.Serialization;

namespace Dokumentationssystem.Views
{
    public partial class AdminViewPage : ContentPage
    {
        public AdminViewPage()
        {
            InitializeComponent();
        }
        private async Task AnimateButton(Button button)
        {
            await button.TranslateTo(0, 10, 100, Easing.CubicInOut);

            await button.TranslateTo(0, 0, 100, Easing.CubicInOut);
        }
        // View all users functionality (click event handler with 'void' return type)
        private void OnViewUsersClicked(object sender, EventArgs e)
        {
            // Call the async method without awaiting
            LoadUsersAsync();
        }

        // Async method to load users
        private async Task LoadUsersAsync()
        {
            var users = await FetchAllUsers();
            if (users != null && users.Count > 0)
            {
                // Assign the fetched data to the ListView
                UsersListView.ItemsSource = users;
            }
            else
            {
                await DisplayAlert("Notice", "No users found or data could not be fetched.", "OK");
            }
        }

        // Register a new user functionality
        private async void OnRegisterUserClicked(object sender, EventArgs e)
        {
            await AnimateButton((Button)sender);

            string username = UsernameEntry.Text;
            string email = EmailEntry.Text;
            string password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Please fill in all fields", "OK");
                return;
            }

            var registerModel = new
            {
                Username = username,
                Email = email,
                Password = password
            };

            var token = Preferences.Get("JwtToken", string.Empty);
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(registerModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("https://localhost:8585/api/auth/register", content);
                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success", "User registered successfully", "OK");
                    UsernameEntry.Text = string.Empty;
                    EmailEntry.Text = string.Empty;
                    PasswordEntry.Text = string.Empty;
                    await LoadUsersAsync(); // Refresh user list
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"User registration failed: {errorMessage}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        // Delete user functionality
        private async void OnDeleteUserClicked(object sender, EventArgs e)
        {
            await AnimateButton((Button)sender);

            var username = (string)((Button)sender).CommandParameter;

            var token = Preferences.Get("JwtToken", string.Empty);
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await httpClient.DeleteAsync($"https://localhost:8585/api/auth/delete/{username}");
                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success", "User deleted successfully", "OK");
                    await LoadUsersAsync(); // Refresh user list
                }
                else
                {
                    await DisplayAlert("Error", "Failed to delete user", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        // Fetch all users from the API
        private async Task<List<User>> FetchAllUsers()

        {
            var httpClient = new HttpClient();
            var token = Preferences.Get("JwtToken", string.Empty);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await httpClient.GetAsync("https://localhost:8585/api/auth/users");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<User>>(jsonResponse);

                    // Log each user to verify deserialization
                    foreach (var user in users)
                    {
                        Console.WriteLine($"UserName: {user.UserName}, Email: {user.Email}");
                    }

                    return users;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Failed to fetch users. Status Code: {response.StatusCode}, Error: {errorMessage}", "OK");
                    return null;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
                return null;
            }
        }


        // This class represents the User model (for demonstration purposes)
        public class User
        {
            [JsonPropertyName("userName")]
            public string UserName { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; }

        }
    }
}
