using CommunityToolkit.Maui.Views;
using Dokumentationssystem.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Maui.Storage;

namespace Dokumentationssystem.Views
{
    public partial class InspectionListPage : ContentPage
    {
        public static string BaseAddress =
            DeviceInfo.Platform == DevicePlatform.Android ? "https://struct.onrender.com" : "https://struct.onrender.com";
        public static string InspectionsUrl = $"{BaseAddress}/api/inspections";
        public static string DeleteInspectionUrl = $"{InspectionsUrl}/";
        private List<Inspection> allInspections = new List<Inspection>();

        public ICommand DeleteInspectionCommand { get; }
        public ICommand DownloadInspectionCommand { get; }
        public ICommand EditInspectionCommand { get; }
        public ICommand InspectionSelectedCommand { get; }
        public ICommand DeselectAllCommand { get; }
      

        private bool _areCheckboxesVisible = false;
        public bool AreCheckboxesVisible
        {
            get => _areCheckboxesVisible;
            set
            {
                _areCheckboxesVisible = value;
                OnPropertyChanged(nameof(AreCheckboxesVisible));
                RefreshCollectionView();
            }
        }

        public InspectionListPage()
        {
            InitializeComponent();
            BindingContext = this;

            LoadInspections();

            EditInspectionCommand = new Command<Inspection>(async (inspection) => await ShowEditInspectionPopup(inspection));
            InspectionSelectedCommand = new Command<Inspection>(async (inspection) => await OpenInspectionDetails(inspection));
            DeselectAllCommand = new Command(DeselectAllInspections);
            
            DownloadInspectionCommand = new Command(async () => await DownloadSelectedInspections());
        }

        private void RefreshCollectionView()
        {
            InspectionsCollectionView.ItemsSource = null;
            InspectionsCollectionView.ItemsSource = allInspections;
        }

        private void DeselectAllInspections()
        {
            foreach (var inspection in allInspections)
            {
                inspection.IsSelected = false;
            }
            RefreshCollectionView();
        }

       

        private async void OnSelectOptionsClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Select Options", "Cancel", null, "Select", "Select All", "Deselect All", "Delete Selected", "Download Selected");

            switch (action)
            {
                case "Select":
                    AreCheckboxesVisible = true;
                    break;

                case "Select All":
                    AreCheckboxesVisible = true;
                    foreach (var inspection in allInspections)
                    {
                        inspection.IsSelected = true;
                    }
                    RefreshCollectionView();
                    break;

                case "Deselect All":
                    DeselectAllInspections();
                    AreCheckboxesVisible = false;
                    break;

                case "Delete Selected":
                    await DeleteSelectedInspections();
                    break;

                case "Download Selected":
                    await DownloadSelectedInspections();
                    break;

                case "Cancel":
                    AreCheckboxesVisible = false;
                    break;
            }
        }

        private async void OnDeleteButtonClicked(object sender, EventArgs e)
        {
            // Get the selected inspections
            var selectedInspections = allInspections.Where(i => i.IsSelected).ToList();

            // Check if there are selected inspections
            if (!selectedInspections.Any())
            {
                await DisplayAlert("No Selection", "Please select at least one inspection to delete.", "OK");
                return;
            }

            // Ask for confirmation once
            var confirm = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete {selectedInspections.Count} selected inspection(s)?", "Yes", "No");
            if (!confirm) return;

            // Get the JWT token
            var jwtToken = Preferences.Get("JwtToken", string.Empty);
            if (string.IsNullOrEmpty(jwtToken))
            {
                await DisplayAlert("Error", "User is not authenticated. Please log in.", "OK");
                return;
            }

            // Perform delete operations
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                foreach (var inspection in selectedInspections)
                {
                    var response = await httpClient.DeleteAsync($"{DeleteInspectionUrl}{inspection.Id}");
                    if (response.IsSuccessStatusCode)
                    {
                        allInspections.Remove(inspection);
                    }
                    else
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        await DisplayAlert("Error", $"Failed to delete inspection '{inspection.InspectionName}': {errorMessage}", "OK");
                    }
                }

                // Refresh the CollectionView after deletion
                RefreshCollectionView();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }


        private void OnDownloadButtonClicked(object sender, EventArgs e)
        {
            var selectedInspections = allInspections.Where(i => i.IsSelected).ToList();

            if (!selectedInspections.Any())
            {
                DisplayAlert("No Selection", "Please select at least one inspection to download.", "OK");
                return;
            }

            if (DownloadInspectionCommand?.CanExecute(null) == true)
            {
                DownloadInspectionCommand.Execute(null);
            }
        }

        private async Task DeleteSelectedInspections()
        {
            var selectedInspections = allInspections.Where(i => i.IsSelected).ToList();

            if (!selectedInspections.Any())
            {
                await DisplayAlert("No Selection", "Please select at least one inspection to delete.", "OK");
                return;
            }

            // Ask for confirmation once
            var confirm = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete {selectedInspections.Count} selected inspection(s)?", "Yes", "No");
            if (!confirm) return;

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
                // Delete inspections in a single loop or batch
                foreach (var inspection in selectedInspections)
                {
                    var response = await httpClient.DeleteAsync($"{DeleteInspectionUrl}{inspection.Id}");
                    if (response.IsSuccessStatusCode)
                    {
                        allInspections.Remove(inspection);
                    }
                    else
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        await DisplayAlert("Error", $"Failed to delete inspection '{inspection.InspectionName}': {errorMessage}", "OK");
                    }
                }

                RefreshCollectionView();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }



        private async Task DownloadSelectedInspections()
        {
            var selectedInspections = allInspections.Where(i => i.IsSelected).ToList();

            if (!selectedInspections.Any())
            {
                await DisplayAlert("No Selection", "Please select at least one inspection to download.", "OK");
                return;
            }

            await Navigation.PushAsync(new DownloadInspectionsPage(selectedInspections));
        }

        private async Task ShowEditInspectionPopup(Inspection inspection)
        {
            var popup = new EditInspectionPopup(inspection, LoadInspections);
            await this.ShowPopupAsync(popup);
        }

        private async void LoadInspections()
        {
            try
            {
                var jwtToken = Preferences.Get("JwtToken", string.Empty);

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

                var response = await httpClient.GetAsync(InspectionsUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    allInspections = JsonSerializer.Deserialize<List<Inspection>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    foreach (var inspection in allInspections)
                    {
                        if (inspection.Date == DateTime.MinValue)
                        {
                            inspection.Date = DateTime.Now;
                        }
                    }

                    RefreshCollectionView();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to load inspections.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? string.Empty;

            var filteredInspections = string.IsNullOrWhiteSpace(searchText)
                ? allInspections
                : allInspections.Where(i => i.InspectionName.ToLower().Contains(searchText)).ToList();

            InspectionsCollectionView.ItemsSource = filteredInspections;
        }

        private async Task OpenInspectionDetails(Inspection inspection)
        {
            if (inspection != null)
            {
                await Navigation.PushAsync(new InspectionDetailsPage(inspection));
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
