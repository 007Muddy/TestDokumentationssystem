using CommunityToolkit.Maui.Views;
using Dokumentationssystem.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace Dokumentationssystem.Views
{
    public partial class DownloadInspectionsPage : ContentPage
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private List<Inspection> selectedInspections;

        public DownloadInspectionsPage(List<Inspection> selectedInspections)
        {
            InitializeComponent();
            BindingContext = this;
            this.selectedInspections = selectedInspections;
            _httpClient.Timeout = TimeSpan.FromMinutes(5); // Set timeout for long-running requests
        }

        private async void OnPdfButtonClicked(object sender, EventArgs e)
        {
            await DownloadSelectedInspections("PDF");
        }

        private async void OnWordButtonClicked(object sender, EventArgs e)
        {
            await DownloadSelectedInspections("Word");
        }

        // Method to download all selected inspections as either a single PDF or Word document
        private async Task DownloadSelectedInspections(string format)
        {
            try
            {
                var jwtToken = Preferences.Get("JwtToken", string.Empty);
                if (string.IsNullOrEmpty(jwtToken))
                {
                    await DisplayAlert("Error", "User is not authenticated. Please log in.", "OK");
                    return;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

                // Construct the API endpoint URL
                var url = format == "PDF"
                    ? $"{InspectionListPage.InspectionsUrl}/download-inspections-pdf"
                    : $"{InspectionListPage.InspectionsUrl}/download-inspections-word";

                // Collect selected inspection IDs
                var inspectionIds = selectedInspections.Select(i => i.Id).ToList();
                var payload = new StringContent(JsonSerializer.Serialize(inspectionIds));
                payload.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                // Send POST request to the API
                var response = await _httpClient.PostAsync(url, payload);

                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    string fileName = format == "PDF" ? "Selected_Inspections.pdf" : "Selected_Inspections.docx";

                    // Prompt the user to choose a save location
                    var saveFilePath = await PickSaveFileAsync(fileName, format == "PDF" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

                    if (!string.IsNullOrEmpty(saveFilePath))
                    {
                        File.WriteAllBytes(saveFilePath, fileBytes);
                        await DisplayAlert("Success", $"{format} document saved to: {saveFilePath}", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Cancelled", "Download cancelled by user.", "OK");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Failed to download inspections as {format}. Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}, Error: {errorContent}", "OK");
                    Console.WriteLine($"Failed to download {format} - Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}, Error: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
                Console.WriteLine($"Unexpected error during {format} download: {ex}");
            }
        }

        // Helper method to display a save dialog and select the download location
        private async Task<string> PickSaveFileAsync(string suggestedFileName, string mimeType)
        {
#if WINDOWS
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.SuggestedFileName = suggestedFileName;
            savePicker.FileTypeChoices.Add(
                mimeType == "application/pdf" ? "PDF Document" : "Word Document",
                new List<string> { mimeType == "application/pdf" ? ".pdf" : ".docx" }
            );

            var hwnd = ((MauiWinUIWindow)App.Current.Windows[0].Handler.PlatformView).WindowHandle;
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            var file = await savePicker.PickSaveFileAsync();
            return file?.Path;

#elif ANDROID
            var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permission Denied", "Cannot save file without storage permission.", "OK");
                return null;
            }

            var downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
            var filePath = Path.Combine(downloadsPath, suggestedFileName);
            return filePath;

#elif IOS
            return Path.Combine(FileSystem.AppDataDirectory, suggestedFileName);

#else
            return null;
#endif
        }
    }
}
