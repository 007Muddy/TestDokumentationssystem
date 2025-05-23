using System.Net.Http.Headers;
using System.Text.Json;
using Dokumentationssystem.Models.Dokumentationssystem.Models;
using Microsoft.Maui.Storage;

namespace Dokumentationssystem.Views
{
    public partial class InspectionListPage : ContentPage
    {
        public InspectionListPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var httpClient = new HttpClient();

            // Retrieve the stored JWT token
            var token = Preferences.Get("JwtToken", string.Empty);

            if (string.IsNullOrEmpty(token))
            {
                await DisplayAlert("Error", "User is not authenticated. Please log in.", "OK");
                await Navigation.PopToRootAsync(); // Navigate back to login page
                return;
            }

            // Set the Authorization header with the Bearer token
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.GetAsync("https://localhost:7250/api/inspections");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var inspections = JsonSerializer.Deserialize<List<Inspection>>(json);

                InspectionListView.ItemsSource = inspections; // Set the ListView's data source

            }
            else
            {
                await DisplayAlert("Error", "Failed to load inspections. Please log in again.", "OK");
            }
        }

        private async void OnInspectionSelected(object sender, ItemTappedEventArgs e)
        {
            var selectedInspection = (Inspection)e.Item;
            // Navigate to inspection details page or handle selection logic
        }
    }
}
