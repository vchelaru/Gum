using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.IO;
using System.Windows.Forms;

namespace Gum
{
    static class Program
    {



        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main()
        {
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            MainWindow mainWindow = null;

            // testing:
            return RunResponseCodes.XnaNotInstalled;

            try
            {
                mainWindow = new MainWindow();
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
