using Dokumentationssystem.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Dokumentationssystem.Views
{
    public partial class InspectionListPage : ContentPage
    {
        public InspectionListPage()
        {
            InitializeComponent();
            LoadInspections();
        }

      

        private async void LoadInspections()
        {
            try
            {
                // Get the JWT token from Preferences
                var jwtToken = Preferences.Get("JwtToken", string.Empty);

                // Initialize HttpClient and set JWT in Authorization header
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

                // Send GET request to fetch inspections
                var response = await httpClient.GetAsync("https://localhost:7250/api/inspections");

                // Check if request was successful
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var inspections = JsonSerializer.Deserialize<List<Inspection>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Ensure dates are set correctly and update binding
                    foreach (var inspection in inspections)
                    {
                        if (inspection.Date == DateTime.MinValue)
                        {
                            inspection.Date = DateTime.Now;  // Set a default date or handle as needed
                        }
                    }

                    // Bind the inspections to the CollectionView
                    InspectionsCollectionView.ItemsSource = inspections;
                }
                else
                {
                    await DisplayAlert("Error", "Failed to load inspections.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle any errors
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }

        }
        private async void OnInspectionSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedInspection = (Inspection)e.CurrentSelection.FirstOrDefault();
            if (selectedInspection != null)
            {
                // Navigate to the details page with the selected inspection
                await Navigation.PushAsync(new InspectionDetailsPage(selectedInspection));

                // Clear the selection to allow re-selection of the same item
                InspectionsCollectionView.SelectedItem = null;
            }
        }




    }
}
