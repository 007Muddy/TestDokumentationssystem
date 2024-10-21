using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.Maui.Storage; // Import this for using Preferences

namespace Dokumentationssystem.Views
{
    public partial class CreateInspectionPage : ContentPage
    {
        public CreateInspectionPage()
        {
            InitializeComponent();

            // Load the CreatedBy (username) value when the page loads
            LoadUserInfo();
        }

        // Model to represent an inspection
        public class InspectionModel
        {
            public string InspectionName { get; set; }
            public string Address { get; set; }
            public DateTime Date { get; set; }
            public string CreatedBy { get; set; }  // Will store username
        }

        // Method to load the username from the JWT token
        private void LoadUserInfo()
        {
            // Get the JWT token from Preferences
            var jwtToken = Preferences.Get("JwtToken", string.Empty);

            // Extract username from the JWT token
            var userInfo = GetUserNameFromToken(jwtToken);

            // Set the CreatedBy field to the extracted username
            CreatedByEntry.Text = userInfo ?? "Unknown User";
        }

        // Decoding the JWT token to extract the username
        private string GetUserNameFromToken(string jwtToken)
        {
            // Check if the token is not empty
            if (string.IsNullOrEmpty(jwtToken))
            {
                return null;
            }

            // Decode the JWT token
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            // Extract the claim (UserName)
            var userName = jsonToken?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            // Return the username
            return userName;
        }

        private async void OnCreateInspectionClicked(object sender, EventArgs e)
        {
            // Get input from form
            var inspectionName = InspectionNameEntry.Text;
            var address = AddressEntry.Text;
            var inspectionDate = InspectionDatePicker.Date;
            var createdBy = CreatedByEntry.Text;  // This should be the username

            // Validate input
            if (string.IsNullOrEmpty(inspectionName) || string.IsNullOrEmpty(address))
            {
                await DisplayAlert("Error", "Please fill in all fields", "OK");
                return;
            }

            // Get the JWT token from Preferences
            var jwtToken = Preferences.Get("JwtToken", string.Empty);
            Console.WriteLine($"JWT Token: {jwtToken}");  // Log token for debugging

            // Check if token exists
            if (string.IsNullOrEmpty(jwtToken))
            {
                await DisplayAlert("Error", "User is not authenticated. Please log in.", "OK");
                return;
            }

            // Create the inspection model
            var inspectionModel = new InspectionModel
            {
                InspectionName = inspectionName,
                Address = address,
                Date = inspectionDate,
                CreatedBy = createdBy  // Set the CreatedBy to the username
            };

            // Initialize HttpClient and set JWT in Authorization header
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                // Serialize the inspection model to JSON
                var json = JsonSerializer.Serialize(inspectionModel);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Post the request to the API
                var response = await httpClient.PostAsync("https://localhost:7250/api/inspections/createinspection", content);

                // Check response status and handle accordingly
                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success", "Inspection created successfully!", "OK");
                    // Navigate to the inspection list page
                    await Navigation.PushAsync(new InspectionListPage());
                }
                else
                {
                    // Handle error response
                    var statusCode = response.StatusCode;
                    var errorContentType = response.Content.Headers.ContentType?.MediaType;
                    var errorMessage = await response.Content.ReadAsStringAsync();

                    // Display error message with detailed information
                    await DisplayAlert("Error", $"Failed to create inspection. Status Code: {statusCode}, Content Type: {errorContentType}, Error Message: {errorMessage}", "OK");
                }
            }
            catch (HttpRequestException httpRequestEx)
            {
                // Handle network-related errors
                await DisplayAlert("Error", $"Network error: {httpRequestEx.Message}", "OK");
            }
            catch (JsonException jsonEx)
            {
                // Handle JSON serialization/deserialization errors
                await DisplayAlert("Error", $"Serialization error: {jsonEx.Message}", "OK");
            }
            catch (Exception ex)
            {
                // General error handling
                await DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
            }
        }
    }
}
