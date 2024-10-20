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
        }

        private async void OnCreateInspectionClicked(object sender, EventArgs e)
        {
            var inspectionName = InspectionNameEntry.Text;
            var address = AddressEntry.Text;
            var inspectionDate = InspectionDatePicker.Date;

            // Validate input fields
            if (string.IsNullOrEmpty(inspectionName) || string.IsNullOrEmpty(address))
            {
                await DisplayAlert("Error", "Please fill in all fields", "OK");
                return;
            }

            // Retrieve the stored JWT token
            var jwtToken = Preferences.Get("JwtToken", string.Empty);
            if (string.IsNullOrEmpty(jwtToken))
            {
                await DisplayAlert("Error", "User is not authenticated. Please log in.", "OK");
                return;
            }

            // Create the inspection model without the CreatedBy field, which will be added on the server side
            var inspectionModel = new
            {
                InspectionName = inspectionName,  // Match this with the server-side model
                Address = address,
                Date = inspectionDate
            };

            // Prepare HTTP client and add Authorization header with the JWT token
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);



            try
            {
                // Serialize the inspection model to JSON
                var json = JsonSerializer.Serialize(inspectionModel);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Make the POST request to the API to create the inspection
                var response = await httpClient.PostAsync("https://localhost:7250/api/inspections/createinspection", content);

                // Check the response status
                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success", "Inspection created successfully!", "OK");
                   
                    await Navigation.PushAsync(new InspectionListPage());
                }
                else
                {
                    // Get more details from the server response
                    var statusCode = response.StatusCode;
                    var errorContentType = response.Content.Headers.ContentType?.MediaType;
                    var errorMessage = await response.Content.ReadAsStringAsync();

                    // Log or display detailed error information
                    await DisplayAlert("Error",
                        $"Failed to create inspection. Status Code: {statusCode}, Content Type: {errorContentType}, Error Message: {errorMessage}",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle any unexpected errors
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }

        }
    }
}
