using System;
using System.Globalization;
using System.IO;
using Microsoft.Maui.Controls;

namespace Dokumentationssystem.Converters
{
    public class ByteArrayToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is a byte array
            if (value is byte[] imageBytes && imageBytes.Length > 0)
            {
                // Convert the byte array to an ImageSource
                return ImageSource.FromStream(() => new MemoryStream(imageBytes));
            }

            return null; // Return null if the value is not valid
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }



}


