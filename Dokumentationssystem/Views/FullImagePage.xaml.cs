using Microsoft.Maui.Controls;

namespace Dokumentationssystem.Views
{
    public partial class FullImagePage : ContentPage
    {
        public string Base64Image { get; set; }

        public FullImagePage(string base64Image)
        {
            InitializeComponent();

            Base64Image = base64Image;
            BindingContext = this;
        }
    }
}
