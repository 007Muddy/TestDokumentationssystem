namespace Dokumentationssystem.Views
{
    public partial class SeeOrCreateNewInspection : ContentPage
    {
        public SeeOrCreateNewInspection()
        {
            InitializeComponent();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
        }
        private async void OnInspectoinListClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new InspectionListPage());
        }

        private async void OnCreateNewInspectionClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateInspectionPage());
        }
    }
}