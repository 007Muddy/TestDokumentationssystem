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
    public partial class InspectionListPage : ContentPage
    {
        // Define base address and endpoint URLs based on the platform
        public static string BaseAddress =
            DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5119" : "http://localhost:5119";
        public static string InspectionsUrl = $"{BaseAddress}/api/inspections";
        public static string DeleteInspectionUrl = $"{InspectionsUrl}/"; // Inspection ID will be appended dynamically
        public static string PhotosUrl = $"{InspectionsUrl}/"; // Use with inspection ID: /{inspection.Id}/photos

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

                var response = await httpClient.GetAsync($"{PhotosUrl}{inspection.Id}/photos");

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Error", "Failed to load inspection photos.", "OK");
                    return;
                }

                var photosJson = await response.Content.ReadAsStringAsync();
                var photos = JsonSerializer.Deserialize<List<Photo>>(photosJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                using (var stream = new MemoryStream())
                {
                    var doc = new Document();
                    PdfWriter.GetInstance(doc, stream);
                    doc.Open();

                    doc.Add(new Paragraph($"Inspection Name: {inspection.InspectionName}"));
                    doc.Add(new Paragraph($"Address: {inspection.Address}"));
                    doc.Add(new Paragraph($"Date: {inspection.Date.ToShortDateString()}"));
                    doc.Add(new Paragraph(" ")); // Spacer

                    foreach (var photo in photos)
                    {
                        doc.Add(new Paragraph($"Photo Name: {photo.PhotoName}"));
                        doc.Add(new Paragraph($"Description: {photo.Description}"));
                        doc.Add(new Paragraph($"Rating: {photo.Rating}"));

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
                                doc.Add(new Paragraph("Error loading image"));
                            }
                        }

                        doc.Add(new Paragraph(" ")); // Spacer
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

            var confirm = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete inspection '{inspection.InspectionName}'?", "Yes", "No");
            if (!confirm)
            {
                return;
            }

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                var response = await httpClient.DeleteAsync($"{DeleteInspectionUrl}{inspection.Id}");

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success", "Inspection deleted successfully!", "OK");

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
                var jwtToken = Preferences.Get("JwtToken", string.Empty);

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

                var response = await httpClient.GetAsync(InspectionsUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var inspections = JsonSerializer.Deserialize<List<Inspection>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    foreach (var inspection in inspections)
                    {
                        if (inspection.Date == DateTime.MinValue)
                        {
                            inspection.Date = DateTime.Now;
                        }
                    }

                    InspectionsCollectionView.ItemsSource = inspections;
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

        private async void OnInspectionSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedInspection = (Inspection)e.CurrentSelection.FirstOrDefault();
            if (selectedInspection != null)
            {
                await Navigation.PushAsync(new InspectionDetailsPage(selectedInspection));
                InspectionsCollectionView.SelectedItem = null;
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
