using CommunityToolkit.Maui.Views;
using Dokumentationssystem.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows.Input;
using System.Text;
using System.IO;
using Microsoft.Maui.Storage;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Dokumentationssystem.Views
{
    public partial class CreateInspectionPage : ContentPage
    {
        private const string GooglePlacesApiKey = "AIzaSyCKFcUhDoSnYywizaP0HsYDdmxMcx6JDvg";
        private const string GooglePlacesApiUrl = "https://maps.googleapis.com/maps/api/place/autocomplete/json?components=country:dk";

        public static string BaseAddress = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5119" : "https://mnbstrcut.onrender.com";
        public static string CreateInspectionUrl = $"{BaseAddress}/api/inspections/createinspection";

        public CreateInspectionPage()
        {
            InitializeComponent();
            LoadUserInfo();
        }

        private async Task AnimateButton(Button button)
        {
            // Move the button down slightly
            await button.TranslateTo(0, 10, 100, Easing.CubicInOut);

            // Move the button back to its original position
            await button.TranslateTo(0, 0, 100, Easing.CubicInOut);
        }

        private void LoadUserInfo()
        {
            // Load the user ID securely (example approach, adjust as needed)
            CreatedByEntry.Text = Preferences.Get("UserId", "Unknown User");
        }

        private async void OnAddressTextChanged(object sender, TextChangedEventArgs e)
        {
            AddressSuggestionsList.IsVisible = false; // Hide suggestions at the start
            AddressSuggestionsList.ItemsSource = null; // Clear previous suggestions

            var query = e.NewTextValue;
            if (!string.IsNullOrWhiteSpace(query))
            {
                await FetchAddressSuggestions(query);
            }
        }

        private async Task FetchAddressSuggestions(string input)
        {
            using (var httpClient = new HttpClient()) // Using a new instance of HttpClient
            {
                var url = $"{GooglePlacesApiUrl}&input={input}&key={GooglePlacesApiKey}";

                try
                {
                    var response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var places = JsonSerializer.Deserialize<GooglePlacesResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (places?.Predictions != null && places.Predictions.Any())
                        {
                            var suggestions = places.Predictions.Select(p => p.Description).ToList();
                            AddressSuggestionsList.ItemsSource = suggestions;
                            AddressSuggestionsList.IsVisible = true;
                        }
                        else
                        {
                            AddressSuggestionsList.ItemsSource = null;
                            AddressSuggestionsList.IsVisible = false;
                        }
                    }
                    else
                    {
                        await DisplayAlert("Error", $"Failed to fetch address suggestions: {response.ReasonPhrase}", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
                }
            }
        }

        private void ClearAddressSuggestions()
        {
            AddressSuggestionsList.ItemsSource = null;
            AddressSuggestionsList.IsVisible = false;
        }

        private void OnAddressSuggestionSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
            {
                AddressEntry.Text = e.SelectedItem.ToString();
                AddressSuggestionsList.IsVisible = false;
                AddressSuggestionsList.SelectedItem = null;
            }
        }

        private async void OnCreateInspectionClicked(object sender, EventArgs e)
        {
            // Animate the button
            await AnimateButton((Button)sender);

            ClearAddressSuggestions();

            var inspectionName = InspectionNameEntry.Text;
            var address = AddressEntry.Text;
            var inspectionDate = InspectionDatePicker.Date;
            var createdBy = CreatedByEntry.Text;

            if (string.IsNullOrEmpty(inspectionName) || string.IsNullOrEmpty(address) || string.IsNullOrEmpty(createdBy))
            {
                await DisplayAlert("Error", "Please fill in all fields", "OK");
                return;
            }

            var httpClient = new HttpClient();

            try
            {
                // Set Authorization header with JWT token
                var jwtToken = Preferences.Get("JwtToken", string.Empty);
                if (string.IsNullOrEmpty(jwtToken))
                {
                    await DisplayAlert("Error", "User is not authenticated. Please log in again.", "OK");
                    return;
                }

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

                // Prepare form data
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(inspectionName), "InspectionName");
                formData.Add(new StringContent(address), "Address");
                formData.Add(new StringContent(inspectionDate.ToString("yyyy-MM-dd")), "Date");
                formData.Add(new StringContent(createdBy), "CreatedBy");

                // Send request to create inspection
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
            // Animate the button
            await AnimateButton((Button)sender);

            // Navigate back
            await Navigation.PopAsync();
        }

        //remove back from top bar
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
            ClearAddressSuggestions(); // Clear previous address suggestions on page load
        }
    }

    public class GooglePlacesResponse
    {
        public List<Prediction> Predictions { get; set; }
    }

    public class Prediction
    {
        public string Description { get; set; }
        public string PlaceId { get; set; }
    }
}