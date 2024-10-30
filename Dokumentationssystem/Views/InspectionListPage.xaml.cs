using CommunityToolkit.Maui.Views;
using Dokumentationssystem.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows.Input;


namespace Dokumentationssystem.Views
{
    public partial class InspectionListPage : ContentPage
    {
        public ICommand DeleteInspectionCommand { get; }
        public ICommand DownloadInspectionCommand { get; }
        public ICommand EditInspectionCommand { get; }

        public InspectionListPage()
        {
            InitializeComponent();
            BindingContext = this; // Ensure BindingContext is set
            LoadInspections();
            DownloadInspectionCommand = new Command<Inspection>(async (inspection) => await DownloadInspection(inspection));
    EditInspectionCommand = new Command<Inspection>(async (inspection) => await ShowEditInspectionPopup(inspection));

            DeleteInspectionCommand = new Command<Inspection>(async (inspection) => await DeleteInspection(inspection));

        }
        private async Task ShowEditInspectionPopup(Inspection inspection)
        {
            var popup = new EditInspectionPopup(inspection, LoadInspections);
            await this.ShowPopupAsync(popup);
        }


        private async Task DownloadInspection(Inspection inspection)
        {
            try
            {
                var jwtToken = Preferences.Get("JwtToken", string.Empty);
                if (string.IsNullOrEmpty(jwtToken))
                {
                    await DisplayAlert("Error", "User is not authenticated. Please log in.", "OK");
                    return;
                }

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

                // Fetch the photos for this inspection
                var response = await httpClient.GetAsync($"https://localhost:7250/api/inspections/{inspection.Id}/photos");

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Error", "Failed to load inspection photos.", "OK");
                    return;
                }

                var photosJson = await response.Content.ReadAsStringAsync();
                var photos = JsonSerializer.Deserialize<List<Photo>>(photosJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                using (var stream = new MemoryStream())
                {
                    var doc = new iTextSharp.text.Document();
                    iTextSharp.text.pdf.PdfWriter.GetInstance(doc, stream);
                    doc.Open();

                    // Add inspection details
                    doc.Add(new iTextSharp.text.Paragraph($"Inspection Name: {inspection.InspectionName}"));
                    doc.Add(new iTextSharp.text.Paragraph($"Address: {inspection.Address}"));
                    doc.Add(new iTextSharp.text.Paragraph($"Date: {inspection.Date.ToShortDateString()}"));
                    doc.Add(new iTextSharp.text.Paragraph(" ")); // Spacer

                    foreach (var photo in photos)
                    {
                        // Display photo details
                        doc.Add(new iTextSharp.text.Paragraph($"Photo Name: {photo.PhotoName}"));
                        doc.Add(new iTextSharp.text.Paragraph($"Description: {photo.Description}"));
                        doc.Add(new iTextSharp.text.Paragraph($"Rating: {photo.Rating}"));

                        // Convert PhotoData to image if present
                        if (photo.PhotoData != null && photo.PhotoData.Length > 0)
                        {
                            try
                            {
                                var img = iTextSharp.text.Image.GetInstance(photo.PhotoData); // Directly use PhotoData if it's a byte array
                                img.ScaleToFit(500f, 500f);
                                img.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                                doc.Add(img);
                            }
                            catch (Exception ex)
                            {
                                doc.Add(new iTextSharp.text.Paragraph("Error loading image"));
                            }
                        }


                        doc.Add(new iTextSharp.text.Paragraph(" ")); // Spacer
                    }

                    doc.Close();

                    var fileName = $"{inspection.InspectionName}_Inspection.pdf";
                    var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    var filePath = Path.Combine(documentsPath, fileName);
                    File.WriteAllBytes(filePath, stream.ToArray());

                    await DisplayAlert("Success", $"PDF saved to: {filePath}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async Task DeleteInspection(Inspection inspection)
        {
            var jwtToken = Preferences.Get("JwtToken", string.Empty);
            if (string.IsNullOrEmpty(jwtToken))
            {
                await DisplayAlert("Error", "User is not authenticated. Please log in.", "OK");
                return;
            }

            // Confirm deletion
            var confirm = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete inspection '{inspection.InspectionName}'?", "Yes", "No");
            if (!confirm)
            {
                return;
            }

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                var response = await httpClient.DeleteAsync($"https://localhost:7250/api/inspections/{inspection.Id}");

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success", "Inspection deleted successfully!", "OK");

                    // Remove the deleted inspection from the collection
                    var inspections = (List<Inspection>)InspectionsCollectionView.ItemsSource;
                    inspections.Remove(inspection);
                    InspectionsCollectionView.ItemsSource = null;
                    InspectionsCollectionView.ItemsSource = inspections;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Failed to delete inspection: {errorMessage}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
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

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync(); // This will go back to the previous page in the navigation stack
        }

    }
}
