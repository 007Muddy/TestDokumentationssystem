using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Maui.Storage;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Dokumentationssystem.Views
{
    public partial class CreateInspectionPage : ContentPage
    {
        private const string GooglePlacesApiKey = "AIzaSyCKFcUhDoSnYywizaP0HsYDdmxMcx6JDvg";
        private const string GooglePlacesApiUrl = "https://maps.googleapis.com/maps/api/place/autocomplete/json?components=country:dk";

        public static string BaseAddress = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5119" : "https://dokumentationssystem.onrender.com";
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
            var jwtToken = Preferences.Get("JwtToken", string.Empty);
            var userInfo = GetUserNameFromToken(jwtToken);
            CreatedByEntry.Text = userInfo ?? "Unknown User";
        }

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
