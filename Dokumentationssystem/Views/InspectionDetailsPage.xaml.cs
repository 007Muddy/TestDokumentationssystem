using Dokumentationssystem.Models.Dokumentationssystem.Models;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace Dokumentationssystem.Views
{
    public partial class InspectionDetailsPage : ContentPage
    {
        public ICommand TapImageCommand { get; }

        public InspectionDetailsPage(Inspection selectedInspection)
        {
            InitializeComponent();

            // Bind the selected inspection to the page
            BindingContext = selectedInspection;

            // Set the photos in the CollectionView
            PhotoCollectionView.ItemsSource = selectedInspection.PhotoPaths ?? new List<string>();

            // Initialize the TapImageCommand
            TapImageCommand = new Command<string>(OnPhotoTapped);
        }

        // Method to handle the tap event on the photo
        private async void OnPhotoTapped(string base64Image)
        {
            if (!string.IsNullOrEmpty(base64Image))
            {
                // Navigate to a new page to display the full image
                await Navigation.PushAsync(new FullImagePage(base64Image));
            }
        }
    }
}
