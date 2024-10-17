namespace Dokumentationssystem.Views;

public partial class PhotoRatingPage : ContentPage
{
	public PhotoRatingPage()
	{
		InitializeComponent();
	}
    private async void OnSaveRatingClicked(object sender, EventArgs e)
    {
        var selectedRating = RatingPicker.SelectedItem.ToString();
        var ratingDescription = RatingDescriptionEditor.Text;

        // Save photo and rating to the backend (API call)

        await DisplayAlert("Success", "Photo and rating saved successfully!", "OK");
    }

}