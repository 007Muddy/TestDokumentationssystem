using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Dokumentationssystem.Models;
using System.IO;
using System.Windows.Input;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Dokumentationssystem.Views
{
    public partial class InspectionDetailsPage : ContentPage
    {
        // ObservableCollection to store photos and bind to CollectionView
        private ObservableCollection<Photo> _photoData = new ObservableCollection<Photo>();
        private List<Photo> _initialPhotoData = new List<Photo>(); // Track photos loaded from server
        private readonly int _inspectionId;  // Store the inspection ID

        // Add a selectedPhoto for storing the currently selected photo for edit
        private Photo _selectedPhoto;

        public ICommand TapImageCommand { get; }
        public ICommand EditPhotoCommand { get; }

        public InspectionDetailsPage(Inspection selectedInspection)
        {
            InitializeComponent();

            _inspectionId = selectedInspection.Id;  // Assign the inspection ID
            BindingContext = this;  // Ensure the BindingContext is set to the current page

            // Initialize TapImageCommand for image tapping functionality
            TapImageCommand = new Command<Photo>(OnPhotoTapped);
            EditPhotoCommand = new Command<Photo>(OnEditButtonClicked);

            // Load existing photos from the server
            LoadExistingPhotos();
        }

        // Load existing photos from the server
        public async void LoadExistingPhotos()
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
                var response = await httpClient.GetAsync($"https://localhost:7250/api/inspections/{_inspectionId}/photos");

                if (response.IsSuccessStatusCode)
                {
                    var photosJson = await response.Content.ReadAsStringAsync();
                    var photoList = JsonConvert.DeserializeObject<List<Photo>>(photosJson);

                    _photoData.Clear();
                    foreach (var photo in photoList)
                    {
                        photo.InspectionId = _inspectionId;
                        _photoData.Add(photo);
                    }

                    PhotoCollectionView.ItemsSource = _photoData;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Failed to load photos: {errorMessage}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }


        // Handle photo selection for tapping on a photo to view details
        private async void OnPhotoTapped(Photo selectedPhoto)
        {
            if (selectedPhoto?.PhotoData != null)
            {
                await Navigation.PushAsync(new FullImagePage(selectedPhoto.PhotoData));
            }
        }


        // Handle editing an existing photo
        private async void OnEditButtonClicked(Photo selectedPhoto)
        {
            if (selectedPhoto == null || selectedPhoto.Id == 0)
            {
                await DisplayAlert("Error", "Selected photo is invalid or missing an ID.", "OK");
                return;
            }

            await Navigation.PushAsync(new EditPhotoPage(selectedPhoto, this));
        }




        // Handle photo picking for adding a new photo
        private async void OnPickPhotosClicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.PickPhotoAsync();
                if (photo != null)
                {
                    using (var stream = await photo.OpenReadAsync())
                    {
                        var memoryStream = new MemoryStream();
                        await stream.CopyToAsync(memoryStream);
                        var photoBytes = memoryStream.ToArray();

                        if (photoBytes != null && photoBytes.Length > 0)
                        {
                            // Prompt user for name and description
                            string newPhotoName = await DisplayPromptAsync("Photo Name", "Enter a name for the new photo:");
                            string newDescription = await DisplayPromptAsync("Description", "Enter a description for the new photo:");

                            if (string.IsNullOrEmpty(newPhotoName))
                            {
                                await DisplayAlert("Error", "Photo name cannot be empty.", "OK");
                                return;
                            }

                            if (string.IsNullOrEmpty(newDescription))
                            {
                                await DisplayAlert("Error", "Description cannot be empty.", "OK");
                                return;
                            }

                            // Add a new photo model to the ObservableCollection
                            var photoModel = new Photo
                            {
                                Id = 0,  // Mark as new photo with a temporary ID
                                InspectionId = _inspectionId,  // Use current inspection ID
                                PhotoData = photoBytes,
                                PhotoName = newPhotoName,  // Set the name from user input
                                Description = newDescription  // Set the description from user input
                            };

                            _photoData.Add(photoModel);  // Add to collection

                            // Set the new photo as the selected photo
                            _selectedPhoto = photoModel;

                            // Refresh CollectionView after adding new photo
                            PhotoCollectionView.ItemsSource = null;
                            PhotoCollectionView.ItemsSource = _photoData;
                        }
                        else
                        {
                            await DisplayAlert("Error", "Failed to read the photo.", "OK");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while picking the photo: {ex.Message}", "OK");
            }
        }



        // Handle the Save button click for saving metadata of the edited photo
        private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            if (_selectedPhoto == null)
            {
                await DisplayAlert("Error", "No photo selected.", "OK");
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
                if (_selectedPhoto.Id == 0)  // New photo (Id is 0 or null)
                {
                    // New photo logic (POST request)
                    var content = new MultipartFormDataContent();
                    var photoContent = new ByteArrayContent(_selectedPhoto.PhotoData);
                    photoContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                    content.Add(photoContent, "photos", "photo.jpg");

                    content.Add(new StringContent(_selectedPhoto.PhotoName), "photoNames");
                    content.Add(new StringContent(_selectedPhoto.Description), "descriptions");

                    var response = await httpClient.PostAsync($"https://localhost:7250/api/inspections/{_inspectionId}/photos", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();
                        var uploadedPhoto = JsonConvert.DeserializeObject<Photo>(responseData);
                        _selectedPhoto.Id = uploadedPhoto.Id;

                        await DisplayAlert("Success", "New photo added successfully!", "OK");
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        await DisplayAlert("Error", $"Failed to upload new photo: {errorMessage}", "OK");
                    }
                }
                else  // Existing photo (edit)
                {
                    // Editing existing photo (PUT request)
                    var updateData = new
                    {
                        PhotoName = _selectedPhoto.PhotoName,
                        Description = _selectedPhoto.Description
                    };

                    var jsonContent = new StringContent(JsonConvert.SerializeObject(updateData), System.Text.Encoding.UTF8, "application/json");

                    // Send PUT request to update the photo metadata
                    var response = await httpClient.PutAsync($"https://localhost:7250/api/inspections/{_inspectionId}/photos/{_selectedPhoto.Id}", jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Success", "Photo metadata updated successfully!", "OK");
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        await DisplayAlert("Error", $"Failed to update photo metadata: {errorMessage}", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }


      


    }
}
