namespace Dokumentationssystem.Views;

public partial class InspectionListPage : ContentPage
{
	public InspectionListPage()
	{
		InitializeComponent();
	}
    private async void OnCreateInspectionClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CreateInspectionPage());
    }

}