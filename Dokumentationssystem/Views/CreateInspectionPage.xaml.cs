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

        private List<FileResult> _selectedPhotos = new List<FileResult>();
        private List<string> _photoPaths = new List<string>();

        private async void OnPickPhotosClicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.PickPhotoAsync();

                if (photo != null)
                {
                    using (var stream = await photo.OpenReadAsync())
                    {
                        // Convert the photo to a byte array
                        using (var memoryStream = new MemoryStream())
                        {
                            await stream.CopyToAsync(memoryStream);
                            var imageBytes = memoryStream.ToArray();

                            // Convert the byte array to a Base64 string
                            var base64String = Convert.ToBase64String(imageBytes);

                            // Add the Base64 string to the list
                            _photoPaths.Add(base64String);

                            // Update the CollectionView with the new photo path
                            PhotoCollectionView.ItemsSource = null; // Clear previous items
                            PhotoCollectionView.ItemsSource = _photoPaths; // Set new items
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while picking photos: {ex.Message}", "OK");
            }
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

                // Add Base64-encoded photos to the form data
                foreach (var base64Photo in _photoPaths)
                {
                    formData.Add(new StringContent(base64Photo), "photos");
                }

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


    }
}
