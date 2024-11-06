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
            await AnimateButton((Button)sender);

            await Navigation.PushAsync(new InspectionListPage());
        }

        private async void OnCreateNewInspectionClicked(object sender, EventArgs e)
        {
            await AnimateButton((Button)sender);

            await Navigation.PushAsync(new CreateInspectionPage());
        }
        private async Task AnimateButton(Button button)
        {
            // Move the button down slightly
            await button.TranslateTo(0, 10, 100, Easing.CubicInOut);

            // Move the button back to its original position
            await button.TranslateTo(0, 0, 100, Easing.CubicInOut);
        }
    }
}