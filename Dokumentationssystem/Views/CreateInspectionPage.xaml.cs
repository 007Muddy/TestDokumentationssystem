namespace Dokumentationssystem.Views;

public partial class CreateInspectionPage : ContentPage
{
	public CreateInspectionPage()
	{
		InitializeComponent();
	}
    private async void OnSaveInspectionClicked(object sender, EventArgs e)
    {
        var inspectionName = InspectionNameEntry.Text;
        var address = AddressEntry.Text;
        var inspectionDate = InspectionDatePicker.Date;

        // Save to the backend (API call to save the inspection)

        await DisplayAlert("Success", "Inspection saved successfully!", "OK");
        await Navigation.PopAsync(); // Go back to the list of inspections
    }

}