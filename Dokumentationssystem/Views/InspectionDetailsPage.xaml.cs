using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Dokumentationssystem.Models;
using System.IO;
using System.Windows.Input;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Storage;

namespace Dokumentationssystem.Views
{
    public partial class InspectionDetailsPage : ContentPage
    {
        // Define base address and API URLs based on platform
        public static string BaseAddress =
            DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5119" : "http://localhost:5119";
        public static string PhotosUrl(int inspectionId) => $"{BaseAddress}/api/inspections/{inspectionId}/photos";
        public static string DeletePhotoUrl(int inspectionId, int photoId) => $"{BaseAddress}/api/inspections/{inspectionId}/photos/{photoId}";
        public static string AddPhotoUrl(int inspectionId) => $"{BaseAddress}/api/inspections/{inspectionId}/photos";

        private List<Photo> _initialPhotoData = new List<Photo>(); // Track photos loaded from server

        private ObservableCollection<Photo> _photoData = new ObservableCollection<Photo>();
        private readonly int _inspectionId;
        private Photo _selectedPhoto;

        public ICommand TapImageCommand { get; }
        public ICommand EditPhotoCommand { get; }
        public ICommand DeletePhotoCommand { get; }

        public InspectionDetailsPage(Inspection selectedInspection)
        {
            InitializeComponent();

            _inspectionId = selectedInspection.Id;
            BindingContext = this;

            TapImageCommand = new Command<Photo>(OnPhotoTapped);
            EditPhotoCommand = new Command<Photo>(OnEditButtonClicked);
            DeletePhotoCommand = new Command<Photo>(OnDeletePhoto);

            LoadExistingPhotos();
        }

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
                var response = await httpClient.GetAsync(PhotosUrl(_inspectionId));

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

        private async void OnPhotoTapped(Photo selectedPhoto)
        {
            if (selectedPhoto?.PhotoData != null)
            {
                await Navigation.PushAsync(new FullImagePage(selectedPhoto.PhotoData));
            }
        }

        private async void OnEditButtonClicked(Photo selectedPhoto)
        {
            if (selectedPhoto == null || selectedPhoto.Id == 0)
            {
                await DisplayAlert("Error", "Selected photo is not saved yet. Please save it first.", "OK");
                return;
            }

            await Navigation.PushAsync(new EditPhotoPage(selectedPhoto, this));
        }

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
                            string newPhotoName = await DisplayPromptAsync("Photo Name", "Enter a name for the new photo:");
                            string newDescription = await DisplayPromptAsync("Description", "Enter a description for the new photo:");

                            if (string.IsNullOrEmpty(newPhotoName) || string.IsNullOrEmpty(newDescription))
                            {
                                await DisplayAlert("Error", "Photo name and description cannot be empty.", "OK");
                                return;
                            }

                            int rating = await ShowRatingSelectionPopup();

                            var photoModel = new Photo
                            {
                                Id = 0,
                                InspectionId = _inspectionId,
                                PhotoData = photoBytes,
                                PhotoName = newPhotoName,
                                Description = newDescription,
                                Rating = rating
                            };

                            _photoData.Add(photoModel);
                            _selectedPhoto = photoModel;

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

        private async Task<int> ShowRatingSelectionPopup()
        {
            var tcs = new TaskCompletionSource<int>();
            bool isResultSet = false; // Flag to prevent multiple calls to SetResult

            var popupLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Spacing = 10
            };

            // Add rating buttons from 1 to 10
            for (int i = 1; i <= 10; i++)
            {
                var button = new Button
                {
                    Text = i.ToString(),
                    BackgroundColor = i <= 3 ? Colors.Green : i <= 7 ? Colors.Orange : Colors.Red,
                    TextColor = Colors.White,
                    CornerRadius = 20,
                    WidthRequest = 40,
                    HeightRequest = 40,
                    CommandParameter = i
                };

                // Define what happens when a button is clicked
                button.Clicked += async (sender, e) =>
                {
                    if (!isResultSet) // Check if result has already been set
                    {
                        if (sender is Button selectedButton && int.TryParse(selectedButton.CommandParameter.ToString(), out int selectedRating))
                        {
                            isResultSet = true; // Set flag to true to prevent further calls
                            tcs.SetResult(selectedRating);

                            // Immediately remove the popup after selection
                            if (Application.Current.MainPage.Navigation.ModalStack.Count > 0)
                            {
                                await Application.Current.MainPage.Navigation.PopModalAsync();
                            }
                        }
                    }
                };

                popupLayout.Children.Add(button);
            }

            // Display the popup
            var overlay = new AbsoluteLayout
            {
                BackgroundColor = Colors.Black.WithAlpha(0.5f) // Semi-transparent overlay
            };
            AbsoluteLayout.SetLayoutBounds(popupLayout, new Rect(0.5, 0.5, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
            AbsoluteLayout.SetLayoutFlags(popupLayout, AbsoluteLayoutFlags.PositionProportional);
            overlay.Children.Add(popupLayout);

            var modalPage = new ContentPage
            {
                Content = overlay,
                BackgroundColor = Colors.Transparent
            };

            await Application.Current.MainPage.Navigation.PushModalAsync(modalPage);

            return await tcs.Task;
        }
   

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
                if (_selectedPhoto.Id == 0)
                {
                    var content = new MultipartFormDataContent();
                    var photoContent = new ByteArrayContent(_selectedPhoto.PhotoData);
                    photoContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                    content.Add(photoContent, "photos", "photo.jpg");

                    content.Add(new StringContent(_selectedPhoto.PhotoName), "photoNames");
                    content.Add(new StringContent(_selectedPhoto.Description), "descriptions");
                    content.Add(new StringContent(_selectedPhoto.Rating.ToString()), "ratings");

                    var response = await httpClient.PostAsync(AddPhotoUrl(_inspectionId), content);

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
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async void OnDeletePhoto(Photo selectedPhoto)
        {
            if (selectedPhoto == null)
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

            var confirm = await DisplayAlert("Confirm Delete", "Are you sure you want to delete this photo?", "Yes", "No");
            if (!confirm)
            {
                return;
            }

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            try
            {
                var response = await httpClient.DeleteAsync(DeletePhotoUrl(_inspectionId, selectedPhoto.Id));

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success", "Photo deleted successfully!", "OK");

                    _photoData.Remove(selectedPhoto);
                    PhotoCollectionView.ItemsSource = null;
                    PhotoCollectionView.ItemsSource = _photoData;
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

        private void OnDeleteButtonClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Photo selectedPhoto)
            {
                OnDeletePhoto(selectedPhoto);
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
