using System;
using System.Globalization;
using System.IO;
using Microsoft.Maui.Controls;

namespace Dokumentationssystem.Converters
{
    public class Base64ToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is string base64String && !string.IsNullOrEmpty(base64String))
            {
                byte[] imageBytes = System.Convert.FromBase64String(base64String);
                MemoryStream stream = new MemoryStream(imageBytes);
                return ImageSource.FromStream(() => stream);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
