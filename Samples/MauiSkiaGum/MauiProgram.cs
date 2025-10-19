using Microsoft.Extensions.Logging;
using RenderingLibrary;
using SkiaGum.Content;
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

            SkiaResourceManager.CustomResourceAssembly = typeof(MauiProgram).Assembly;
            SkiaResourceManager.AdjustContentName = (contentName) =>
            {
                return "MauiSkiaGum.GumProject." + contentName;
            };

            return builder.Build();
        }
    }
}
