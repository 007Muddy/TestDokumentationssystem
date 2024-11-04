using Dokumentationssystem.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.Maui.Storage;

namespace Dokumentationssystem.Views;

public partial class EditPhotoPage : ContentPage
{
    // Define base address and endpoint URLs based on the platform
    public static string BaseAddress =
        DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5119" : "http://localhost:5119";
    public static string DeletePhotoUrl(int inspectionId, int photoId) => $"{BaseAddress}/api/inspections/{inspectionId}/photos/{photoId}";
    public static string UpdatePhotoUrl(int inspectionId, int photoId) => $"{BaseAddress}/api/inspections/{inspectionId}/photos/{photoId}";

    private Photo _selectedPhoto;
    private byte[] _newPhotoData;
    private InspectionDetailsPage _parentPage;
    private int _selectedRating;

    public EditPhotoPage(Photo selectedPhoto, InspectionDetailsPage parentPage)
    {
        InitializeComponent();
        _selectedPhoto = selectedPhoto;
        _parentPage = parentPage;

        // Set the initial values
        if (_selectedPhoto.PhotoData != null)
        {
            PhotoImage.Source = ImageSource.FromStream(() => new MemoryStream(_selectedPhoto.PhotoData));
        }
        PhotoNameEntry.Text = _selectedPhoto.PhotoName;
        DescriptionEditor.Text = _selectedPhoto.Description;
        _selectedRating = _selectedPhoto.Rating;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
    }
    private void OnRatingButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button && int.TryParse(button.CommandParameter.ToString(), out int rating))
        {
            _selectedRating = rating;
            DisplayAlert("Selected Rating", $"You selected rating: {_selectedRating}", "OK");
        }
    }

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
                Rating = _selectedRating,
                PhotoData = _newPhotoData ?? _selectedPhoto.PhotoData
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(updateData), System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PutAsync(UpdatePhotoUrl(_selectedPhoto.InspectionId, _selectedPhoto.Id), jsonContent);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Success", "Photo updated successfully!", "OK");

                // Refresh the photo list on the parent page
                _parentPage.LoadExistingPhotos();

                await Navigation.PopAsync();
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

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
