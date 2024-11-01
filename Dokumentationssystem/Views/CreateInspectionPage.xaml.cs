using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace Dokumentationssystem.Views
{
    public partial class CreateInspectionPage : ContentPage
    {
        // Define base address and endpoint URL for creating inspection
        public static string BaseAddress =
            DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5119" : "http://localhost:5119";
        public static string CreateInspectionUrl = $"{BaseAddress}/api/inspections/createinspection";

        public CreateInspectionPage()
        {
            InitializeComponent();
            LoadUserInfo(); // Load the CreatedBy (username) value when the page loads
        }

        // Method to load the username from the JWT token
        private void LoadUserInfo()
        {
            var jwtToken = Preferences.Get("JwtToken", string.Empty);
            var userInfo = GetUserNameFromToken(jwtToken);
            CreatedByEntry.Text = userInfo ?? "Unknown User";
        }

        // Decoding the JWT token to extract the username
        private string GetUserNameFromToken(string jwtToken)
        {
            if (string.IsNullOrEmpty(jwtToken))
            {
                return null;
            }

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;
            var userName = jsonToken?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

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

                // Use the platform-specific CreateInspectionUrl for the API call
                var response = await httpClient.PostAsync(CreateInspectionUrl, formData);

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
            await Navigation.PopAsync();
        }
    }
}
