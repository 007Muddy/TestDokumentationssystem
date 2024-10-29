using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Dokumentationssystem.Models;
using System.IO;
using System.Windows.Input;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.Maui.Layouts;

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

                            if (string.IsNullOrEmpty(newPhotoName) || string.IsNullOrEmpty(newDescription))
                            {
                                await DisplayAlert("Error", "Photo name and description cannot be empty.", "OK");
                                return;
                            }

                            // Show a rating selection popup with circular buttons
                            int rating = await ShowRatingSelectionPopup();

                            // Add a new photo model to the ObservableCollection
                            var photoModel = new Photo
                            {
                                Id = 0, // New photo ID set to 0
                                InspectionId = _inspectionId,
                                PhotoData = photoBytes,
                                PhotoName = newPhotoName,
                                Description = newDescription,
                                Rating = rating
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

        // Popup for rating selection
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
                    content.Add(new StringContent(_selectedPhoto.Rating.ToString()), "ratings"); // Include rating

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
             
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }


      


    }
}
