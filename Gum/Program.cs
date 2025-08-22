using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Gum.CommandLine;
using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Reflection;
using Gum.Services;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
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
                catch(Exception e)
                {
                    Debugger.Break();
                    Console.Error.WriteLine(e);
                    return RunResponseCodes.UnexpectedFailure;
                }
            }

            private static async Task<int> MainAsync(string[] args)
            {
                using IHost host = GumBuilder.CreateHostBuilder(args).Build();
                Locator.Register(host.Services);
                
                await host.StartAsync().ConfigureAwait(true);

                App app = new()
                {
                    MainWindow = host.Services.GetRequiredService<MainWindow>()
                };
                app.MainWindow.Visibility = Visibility.Visible;
                
                
                await Initialize(host.Services).ConfigureAwait(true);

                if (CommandLineManager.Self.ShouldExitImmediately)
                {
                    return RunResponseCodes.Success;           
                }
                
                app.Run();
                
                await host.StopAsync().ConfigureAwait(true);
                return RunResponseCodes.Success;
            }

            private static async Task Initialize(IServiceProvider services)
            {
                TypeManager.Self.Initialize();

                // This has to happen before plugins are loaded since they may depend on settings...
                ProjectManager.Self.LoadSettings();
                
                ElementTreeViewManager.Self.Initialize();

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
                
                WireframeObjectManager.Self.Initialize();

                PluginManager.Self.XnaInitialized();
                
                await ProjectManager.Self.Initialize();

                PeriodicUiTimer fileWatchTimer = services.GetRequiredService<PeriodicUiTimer>();
                fileWatchTimer.Tick += static () =>
                {
                    GumProjectSave? gumProject = ProjectState.Self.GumProjectSave;
                    if (gumProject != null && !string.IsNullOrEmpty(gumProject.FullFileName))
                    {
                        FileWatchManager.Self.Flush();
                    }
                };
                
                fileWatchTimer.Start(TimeSpan.FromSeconds(2));
            }
    }

    static class RunResponseCodes
    {
        public const int Success = 0;
        public const int UnexpectedFailure = 1;
    }
}
