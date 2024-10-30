using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.Maui.Storage;

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
            var inspectionName = InspectionNameEntry.Text;
            var address = AddressEntry.Text;
            var inspectionDate = InspectionDatePicker.Date;
            var createdBy = CreatedByEntry.Text;

            if (string.IsNullOrEmpty(inspectionName) || string.IsNullOrEmpty(address))
            {
                await DisplayAlert("Error", "Please fill in all fields", "OK");
                return;
            }

            var jwtToken = Preferences.Get("JwtToken", string.Empty);

            if (string.IsNullOrEmpty(jwtToken))
            {
                await DisplayAlert("Error", "User is not authenticated. Please log in.", "OK");
                return;
            }

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(inspectionName), "InspectionName");
                formData.Add(new StringContent(address), "Address");
                formData.Add(new StringContent(inspectionDate.ToString("yyyy-MM-dd")), "Date");
                formData.Add(new StringContent(createdBy), "CreatedBy");

             
                var response = await httpClient.PostAsync("https://localhost:7250/api/inspections/createinspection", formData);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success", "Inspection created successfully!", "OK");
                    await Navigation.PushAsync(new InspectionListPage());
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Failed to create inspection: {errorMessage}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync(); // Navigates back to the previous page in the navigation stack
        }


    }
}
