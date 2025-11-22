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
using Gum.Dialogs;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Reflection;
using Gum.Services;
using Gum.Settings;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Extensions.Configuration;
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
            Locator.Register(host.Services);

            await host.StartAsync().ConfigureAwait(true);
            IMessenger messenger = host.Services.GetRequiredService<IMessenger>();

            App app = new();
            app.InitializeComponent();

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
            app.MainWindow.Visibility = Visibility.Visible;
                
            await InitializeGum(host.Services).ConfigureAwait(true);

            if (CommandLineManager.Self.ShouldExitImmediately)
            {
                await host.StopAsync().ConfigureAwait(true);
                return RunResponseCodes.Success;
            }

            app.Run();

            await host.StopAsync().ConfigureAwait(true);
            return RunResponseCodes.Success;
        }

        private static async Task InitializeGum(IServiceProvider services)
        {
            TypeManager.Self.Initialize();

            // This has to happen before plugins are loaded since they may depend on settings...
            ProjectManager.Self.LoadSettings();

            MigrateAppSettings(services, ProjectManager.Self.GeneralSettingsFile);
            services.GetRequiredService<IThemingService>().ApplyInitialTheme();

            ElementTreeViewManager.Self.Initialize();

            WireframeObjectManager.Self.Initialize();
            // This has to be initialized very early because other things depend on it.

            // ProperGridManager before MenuStripManager. Why does it need to be initialized before MainMenuStripPlugin?
            // Is htere a way to move this to a plugin?
            PropertyGridManager.Self.InitializeEarly();

            PluginManager.Self.Initialize();

            StandardElementsManager.Self.Initialize();
            StandardElementsManager.Self.CustomGetDefaultState =
                PluginManager.Self.GetDefaultStateFor;

            ElementSaveExtensions.VariableChangedThroughReference +=
                Gum.Plugins.PluginManager.Self.VariableSet;

            StandardElementsManagerGumTool.Self.Initialize();

            VariableSaveExtensionMethods.CustomFixEnumerations = VariableSaveExtensionMethodsGumTool.FixEnumerationsWithReflection;


            // ProjectManager.Initialize used to happen here, but I 
            // moved it down because it may load a project, and if it
            // does, then we need to make sure that the wireframe controls
            // are set up properly before that happens.
            // XnaInitialize is where wireframe controls are initialized.
            PluginManager.Self.XnaInitialized();

            await ProjectManager.Self.Initialize();

            PeriodicUiTimer fileWatchTimer = services.GetRequiredService<PeriodicUiTimer>();

            var fileWatchManager = Locator.GetRequiredService<FileWatchManager>(); 

            fileWatchTimer.Tick += () =>
            {
                GumProjectSave? gumProject = ProjectState.Self.GumProjectSave;
                if (gumProject != null && !string.IsNullOrEmpty(gumProject.FullFileName))
                {
                    fileWatchManager.Flush();
                }
            };

            fileWatchTimer.Start(TimeSpan.FromSeconds(2));
        }

        private static void MigrateAppSettings(IServiceProvider services, GeneralSettingsFile legacySettings)
        {
            IConfiguration config = services.GetRequiredService<IConfiguration>();

            ApplyIfNotExists<ThemeSettings>(x => ThemeSettings.MigrateExplicitLegacyColors(legacySettings, x));
            ApplyIfNotExists<LayoutSettings>(x => LayoutSettings.MigrateLegacyLayout(legacySettings, x));

            void ApplyIfNotExists<T>(Action<T> applyAction) where T : class, new()
            {
                if (config.GetSection(typeof(T).Name) is { } section &&
                    section.Exists())
                {
                    return;
                }
                services.GetRequiredService<IWritableOptions<T>>().Update(applyAction);
            }
        }
    }


    static class RunResponseCodes
    {
        public const int Success = 0;
        public const int UnexpectedFailure = 1;
    }

    public record ApplicationStartupMessage;

    public class ApplicationTeardownMessage(List<Action> teardownList)
    {
        public void OnTearDown(Action action) => teardownList.Add(action);
    }
}
