using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gum.Commands;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gum
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string [] args)
        {
            return MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task<int> MainAsync(string[] args)
        {
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddGum();
                })
                .Build();
                
            Locator.Register(host.Services);
            
            await host.StartAsync().ConfigureAwait(true);
            
            MainWindow? mainWindow = null;

            try
            {
                mainWindow = host.Services.GetRequiredService<MainWindow>();
            }
            catch(FileNotFoundException)
            {
                return RunResponseCodes.XnaNotInstalled;
            }
            catch (Exception e)
            {
                return RunResponseCodes.UnknownFailure;
            }
            
            Application.Run(mainWindow);
            await host.StopAsync().ConfigureAwait(true);
            return RunResponseCodes.Success;
        }
    }

    static class RunResponseCodes
    {
        public const int Success = 0;
        public const int UnknownFailure = 1;
        public const int XnaNotInstalled = 2;
    }
}
