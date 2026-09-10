using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using Gum.CommandLine;
using Gum.DataTypes;
using Gum.Diagnostics;
using Gum.Dialogs;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Reflection;
using Gum.Services;
using Gum.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gum
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            StartupTiming.Mark("Main entry");

            // Surface exceptions that would otherwise vanish silently - e.g. from a WinForms-hosted
            // plugin control (EditorTabPlugin_XNA's KNI viewport) failing on its own message-pump
            // thread, which doesn't route through any try/catch Gum itself owns. Without these, a
            // failure here just looks like a blank, unresponsive editor with no diagnostic trail.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Console.Error.WriteLine("Unhandled exception: " + e.ExceptionObject);
            System.Windows.Forms.Application.ThreadException += (s, e) =>
                Console.Error.WriteLine("WinForms thread exception: " + e.Exception);
            TaskScheduler.UnobservedTaskException += (s, e) =>
                Console.Error.WriteLine("Unobserved task exception: " + e.Exception);
            System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException);
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

#if DEBUG
            // This suppresses annoying, useless output from WPF, as explained here:
            // http://weblogs.asp.net/akjoshi/resolving-un-harmful-binding-errors-in-wpf
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level =
                System.Diagnostics.SourceLevels.Critical;
#endif

            try
            {
                return MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                System.Diagnostics.Debug.WriteLine(e);
                // look at the Output window for details
                Debugger.Break();
                return RunResponseCodes.UnexpectedFailure;
            }
        }

        private static async Task<int> MainAsync(string[] args)
        {
            using IHost host = GumBuilder.CreateHostBuilder(args).Build();
            StartupTiming.Mark("Host built");
            Locator.Register(host.Services);

            await host.StartAsync().ConfigureAwait(true);
            StartupTiming.Mark("Host started");
            IMessenger messenger = host.Services.GetRequiredService<IMessenger>();

            App app = new();
            app.InitializeComponent();
            StartupTiming.Mark("App.InitializeComponent");

            // See the AppDomain.UnhandledException comment in Main - same rationale, but for
            // exceptions on the WPF dispatcher thread specifically.
            app.DispatcherUnhandledException += (s, e) =>
                Console.Error.WriteLine("WPF dispatcher unhandled exception: " + e.Exception);

            app.Startup += (_, _) => messenger.Send<ApplicationStartupMessage>();
            app.Exit += (_, _) =>
            {
                List<Action> teardownActions = [];
                messenger.Send(new ApplicationTeardownMessage(teardownActions));
                foreach (Action action in teardownActions)
                {
                    action();
                }
            };

            app.MainWindow = host.Services.GetRequiredService<MainWindow>();
            StartupTiming.Mark("MainWindow resolved");

            if (StartupTiming.IsEnabled)
            {
                app.MainWindow.ContentRendered += (_, _) =>
                {
                    StartupTiming.MarkOnce("MainWindow first paint");
                    StartupTiming.Log(
                        "=== TOTAL cold start, process start -> first paint: " +
                        $"{StartupTiming.MillisecondsSinceProcessStart:0} ms ===");
                };
            }

            app.MainWindow.Visibility = Visibility.Visible;
            StartupTiming.Mark("MainWindow visible");

            await new GumStartupSequence(host.Services, new WpfHeadStartup(host.Services))
                .RunAsync().ConfigureAwait(true);
            StartupTiming.Mark("InitializeGum complete");

            if (host.Services.GetRequiredService<ICommandLineManager>().ShouldExitImmediately)
            {
                await host.StopAsync().ConfigureAwait(true);
                return RunResponseCodes.Success;
            }

            app.Run();

            await host.StopAsync().ConfigureAwait(true);
            return RunResponseCodes.Success;
        }

    }


    static class RunResponseCodes
    {
        public const int Success = 0;
        public const int UnexpectedFailure = 1;
    }
}
