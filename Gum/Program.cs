using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Gum
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            InitializeAppCenter();

            Application.Run(new MainWindow());
        }

        private static void InitializeAppCenter()
        {
            Application.ThreadException += (sender, args) =>
            {
                Crashes.TrackError(args.Exception);
            };

            AppCenter.Start("ba71b882-7cee-4dff-90a0-3cbbb179bec0",
                   typeof(Analytics), typeof(Crashes));
        }
    }
}
