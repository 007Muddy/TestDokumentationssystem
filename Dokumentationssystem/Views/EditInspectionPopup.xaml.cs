using Dokumentationssystem.Models;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.Maui.Storage;

namespace Dokumentationssystem.Views
{
    public partial class EditInspectionPopup : CommunityToolkit.Maui.Views.Popup
    {
        // Define base address and URL for updating inspections based on the platform
        public static string BaseAddress =
            DeviceInfo.Platform == DevicePlatform.Android ? "https://struct.onrender.com" : "https://struct.onrender.com";
        public static string UpdateInspectionUrl(int inspectionId) => $"{BaseAddress}/api/inspections/{inspectionId}";

        private Inspection _inspection;
        private readonly Action _refreshInspections;

        public EditInspectionPopup(Inspection inspection, Action refreshInspections)
        {
            InitializeComponent();
            _inspection = inspection;
            _refreshInspections = refreshInspections;

            // Pre-fill fields with existing data
            InspectionNameEntry.Text = _inspection.InspectionName;
            AddressEntry.Text = _inspection.Address;
            DateEntry.Date = _inspection.Date;
        }
        private async Task AnimateButton(Button button)
        {
            // Move the button down slightly
            await button.TranslateTo(0, 10, 100, Easing.CubicInOut);

            // Move the button back to its original position
            await button.TranslateTo(0, 0, 100, Easing.CubicInOut);
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            await AnimateButton((Button)sender);

            _inspection.InspectionName = InspectionNameEntry.Text?.Trim();
            _inspection.Address = AddressEntry.Text?.Trim();

            // Convert the selected date to UTC
            _inspection.Date = DateEntry.Date.ToUniversalTime();

            if (string.IsNullOrEmpty(_inspection.InspectionName) || string.IsNullOrEmpty(_inspection.Address))
            {
                await Application.Current.MainPage.DisplayAlert("Validation Error", "Inspection Name and Address cannot be empty.", "OK");
                return;
            }

            var jwtToken = Preferences.Get("JwtToken", string.Empty);
            if (string.IsNullOrEmpty(jwtToken))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "User is not authenticated. Please log in.", "OK");
                return;
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

                var jsonContent = new StringContent(JsonConvert.SerializeObject(new
                {
                    _inspection.InspectionName,
                    _inspection.Address,
                    _inspection.Date
                }), System.Text.Encoding.UTF8, "application/json");

                try
                {
                    var response = await httpClient.PutAsync(UpdateInspectionUrl(_inspection.Id), jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        await Application.Current.MainPage.DisplayAlert("Success", "Inspection updated successfully!", "OK");
                        _refreshInspections?.Invoke();
                        Close();
                    }
                    else
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        await Application.Current.MainPage.DisplayAlert("Error", $"Failed to update inspection: {errorMessage}", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
                }
            }
        }

    }
}
