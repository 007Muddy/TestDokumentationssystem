using Microsoft.Maui.Controls;
using System.IO;

namespace Dokumentationssystem.Views
{
    public partial class FullImagePage : ContentPage
    {
        public FullImagePage(byte[] imageData)
        {
            InitializeComponent();

            if (imageData != null && imageData.Length > 0)
            {
                // Set the Image Source directly from the byte array
                FullImageView.Source = ImageSource.FromStream(() => new MemoryStream(imageData));
            }
            else
            {
                DisplayAlert("Error", "Image data is invalid or empty.", "OK");
            }
        }
    }
}


