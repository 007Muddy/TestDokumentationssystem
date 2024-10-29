using Microsoft.Maui.Controls;
using System.IO;

namespace Dokumentationssystem.Views
{
    public partial class FullImagePage : ContentPage
    {
        private double _currentScale = 1;
        private double _startScale = 1;
        private double _xOffset = 0;
        private double _yOffset = 0;

        public FullImagePage(byte[] photoData)
        {
            InitializeComponent();

            if (photoData != null && photoData.Length > 0)
            {
                PhotoImage.Source = ImageSource.FromStream(() => new MemoryStream(photoData));
            }
            else
            {
                DisplayAlert("Error", "Failed to load image.", "OK");
            }
        }

        private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            if (e.Status == GestureStatus.Started)
            {
                _startScale = PhotoImage.Scale;
                PhotoImage.AnchorX = 0;
                PhotoImage.AnchorY = 0;
            }
            else if (e.Status == GestureStatus.Running)
            {
                // Calculate the scale factor to be applied.
                _currentScale += (e.Scale - 1) * _startScale;
                _currentScale = Math.Max(1, _currentScale); // Restrict to original size or larger

                // Apply scale
                PhotoImage.Scale = _currentScale;

                // Translate image to stay centered during zoom
                double renderedX = PhotoImage.X + _xOffset;
                double deltaX = renderedX / Width;
                double deltaWidth = Width / (PhotoImage.Width * _startScale);
                double originX = (e.ScaleOrigin.X - deltaX) * deltaWidth;

                double renderedY = PhotoImage.Y + _yOffset;
                double deltaY = renderedY / Height;
                double deltaHeight = Height / (PhotoImage.Height * _startScale);
                double originY = (e.ScaleOrigin.Y - deltaY) * deltaHeight;

                _xOffset = originX * (PhotoImage.Width * _currentScale - Width);
                _yOffset = originY * (PhotoImage.Height * _currentScale - Height);

                PhotoImage.TranslationX = _xOffset;
                PhotoImage.TranslationY = _yOffset;
            }
            else if (e.Status == GestureStatus.Completed)
            {
                // Store the current translation applied during the zoom gesture
                _xOffset = PhotoImage.TranslationX;
                _yOffset = PhotoImage.TranslationY;
            }
        }
    }
}
