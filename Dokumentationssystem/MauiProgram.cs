using Dokumentationssystem.Views;
using Microsoft.Extensions.Logging;

namespace Dokumentationssystem
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            //views
            builder.Services.AddSingleton<RegistrationPage>();
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddSingleton<StartPage>();
            builder.Services.AddSingleton<CreateInspectionPage>();
            builder.Services.AddSingleton<InspectionListPage>();
            builder.Services.AddSingleton<PhotoRatingPage>();





#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
