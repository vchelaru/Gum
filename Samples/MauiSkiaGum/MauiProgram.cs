using Microsoft.Extensions.Logging;
using RenderingLibrary;
using RenderingLibrary.Content;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace MauiSkiaGum
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            ContentLoader.CustomResourceAssembly = typeof(MauiProgram).Assembly;
            ContentLoader.AdjustContentName = (contentName) =>
            {
                return "MauiSkiaGum.GumProject." + contentName;
            };

            return builder.Build();
        }
    }
}
