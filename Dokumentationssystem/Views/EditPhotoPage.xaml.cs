using Dokumentationssystem.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace Dokumentationssystem.Views;

public partial class EditPhotoPage : ContentPage
{
    private Photo _selectedPhoto;
    private byte[] _newPhotoData;
    private InspectionDetailsPage _parentPage;

    public EditPhotoPage(Photo selectedPhoto, InspectionDetailsPage parentPage)
    {
        InitializeComponent();
        _selectedPhoto = selectedPhoto;
        _parentPage = parentPage;


        if (_selectedPhoto.PhotoData != null)
        {
            PhotoImage.Source = ImageSource.FromStream(() => new MemoryStream(_selectedPhoto.PhotoData));
        }
        PhotoNameEntry.Text = _selectedPhoto.PhotoName;
        DescriptionEditor.Text = _selectedPhoto.Description;
    }

    // Handle selecting a new photo
    private async void OnPickPhotoClicked(object sender, EventArgs e)
    {
        var photo = await MediaPicker.PickPhotoAsync();
        if (photo != null)
        {
            using (var stream = await photo.OpenReadAsync())
            {
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                _newPhotoData = memoryStream.ToArray();

                PhotoImage.Source = ImageSource.FromStream(() => new MemoryStream(_newPhotoData));
            }
        }
    }
    private async void OnDeleteClicked(object sender, EventArgs e)
{
    var jwtToken = Preferences.Get("JwtToken", string.Empty);
    if (string.IsNullOrEmpty(jwtToken))
    {
        await DisplayAlert("Error", "User is not authenticated. Please log in.", "OK");
        return;
    }

    var confirm = await DisplayAlert("Confirm Delete", "Are you sure you want to delete this photo?", "Yes", "No");
    if (!confirm)
    {
        return;
    }

    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

    try
    {

        var response = await httpClient.DeleteAsync($"https://localhost:7250/api/inspections/{_selectedPhoto.InspectionId}/photos/{_selectedPhoto.Id}");

        if (response.IsSuccessStatusCode)
        {
            await DisplayAlert("Success", "Photo deleted successfully!", "OK");

            // Refresh the photo list on the parent page
            _parentPage.LoadExistingPhotos();

            await Navigation.PopAsync();  // Return to the previous page
        }
        else
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            await DisplayAlert("Error", $"Failed to delete photo: {errorMessage}", "OK");
        }
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
    }
}

    // Handle saving changes to the photo
    private async void OnSaveClicked(object sender, EventArgs e)
    {
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

            var updateData = new
            {
                PhotoName = PhotoNameEntry.Text,
                Description = DescriptionEditor.Text,
                PhotoData = _newPhotoData ?? _selectedPhoto.PhotoData
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(updateData), System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PutAsync($"https://localhost:7250/api/inspections/{_selectedPhoto.InspectionId}/photos/{_selectedPhoto.Id}", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Success", "Photo updated successfully!", "OK");

                // Refresh the photo list on the parent page
                _parentPage.LoadExistingPhotos();

                await Navigation.PopAsync();  // Return to the previous page
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"Failed to update photo: {errorMessage}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }
}
