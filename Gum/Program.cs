using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.IO;
using System.Windows.Forms;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gum
{
    static class Program
    {



        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[]? args)
        {
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            IHost host = GumBuilder.CreateHostBuilder(args).ConfigureServices(services =>
            {
                services.AddSingleton<MainWindow>();
            }).Build();
            
            Locator.Register(host.Services);
            host.Start();

            ILogger logger = host.Services.GetRequiredService<ILogger<Application>>();
            MainWindow? mainWindow;

            try
            {
                mainWindow = host.Services.GetRequiredService<MainWindow>();
            }
            catch(FileNotFoundException e)
            {
                logger.LogError(e, "XNA not installed");
                return RunResponseCodes.XnaNotInstalled;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unknown failure");
                return RunResponseCodes.UnknownFailure;
            }

            Application.Run(mainWindow);
            
            return RunResponseCodes.Success;
        }

    }
    
    static class RunResponseCodes
    {
        public const int Success = 0;
        public const int UnknownFailure = 1;
        public const int XnaNotInstalled = 2;
    }

    public record CloseMainWindowMessage;
}
