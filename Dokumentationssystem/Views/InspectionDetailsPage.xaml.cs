using Dokumentationssystem.Models.Dokumentationssystem.Models;

namespace Dokumentationssystem.Views
{
    public partial class InspectionDetailsPage : ContentPage
    {
        public InspectionDetailsPage(Inspection selectedInspection)
        {
            InitializeComponent();

            // Bind the selected inspection to the page
            BindingContext = selectedInspection;

            // Check if PhotoPaths exists and bind to CollectionView
            if (selectedInspection.PhotoPaths != null && selectedInspection.PhotoPaths.Count > 0)
            {
                PhotoCollectionView.ItemsSource = selectedInspection.PhotoPaths;
            }
            else
            {
                PhotoCollectionView.ItemsSource = new List<string>(); // Empty if no photos available
            }
        }

    }
}
