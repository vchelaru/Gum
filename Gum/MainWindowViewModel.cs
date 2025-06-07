using System;
using System.ComponentModel;
using System.Windows.Forms;
using Gum.Controls;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.MenuStripPlugin;
using Gum.Reflection;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;

namespace Gum;

public class MainWindowViewModel
{
    private GumCommands GumCommands { get; }
    private TypeManager TypeManager { get; }
    private ProjectManager ProjectManager { get; }
    private ElementTreeViewManager ElementTreeViewManager { get; }
    private PropertyGridManager PropertyGridManager { get; }
    private PluginManager PluginManager { get; }
    private StandardElementsManager StandardElementsManager { get; }
    private WireframeObjectManager WireframeObjectManager { get; }
    private ProjectState ProjectState { get; }
    private FileWatchManager FileWatchManager { get; }
    private StandardElementsManagerGumTool StandardElementsManagerGumTool { get; }
    
    public MainWindowViewModel(GumCommands gumCommands,
        TypeManager typeManager,
        ProjectManager projectManager,
        ElementTreeViewManager treeViewManager,
        PropertyGridManager propertyGridManager,
        PluginManager pluginManager,
        StandardElementsManager elementsManager,
        WireframeObjectManager wireframeObjectManager,
        ProjectState projectState,
        FileWatchManager fileWatchManager,
        StandardElementsManagerGumTool standardElementsManagerGumTool)
    {
        GumCommands = gumCommands;
        TypeManager = typeManager;
        ProjectManager = projectManager;
        ElementTreeViewManager = treeViewManager;
        PropertyGridManager = propertyGridManager;
        PluginManager = pluginManager;
        StandardElementsManager = elementsManager;
        WireframeObjectManager = wireframeObjectManager;
        ProjectState = projectState;
        FileWatchManager = fileWatchManager;
        StandardElementsManagerGumTool = standardElementsManagerGumTool;
    }

    private void Initialize()
    {

    }

    public void Initialize(MainWindow mainWindow, MainPanelControl mainPanelControl, IContainer container, ImageList imageList)
    {
        // Initialize before the StateView is created...
        GumCommands.Initialize(mainWindow, mainPanelControl);
        TypeManager.Initialize();
        
        // This has to happen before plugins are loaded since they may depend on settings...
        ProjectManager.LoadSettings();
        
        GumCommands.GuiCommands.AddCursor = LoadAddCursor();

        // Vic says - I tried
        // to instantiate the ElementTreeImages
        // in the ElementTreeViewManager. I move 
        // the code there and it works, but then at
        // some point it stops working and it breaks. Not 
        // sure why, Winforms editor must be doing something
        // beyond the generation of code which isn't working when
        // I move it to custom code. Oh well, maybe one day I'll move
        // to a wpf window and can get rid of this
        ElementTreeViewManager.Initialize(container, imageList);
        
        PropertyGridManager.InitializeEarly();
        MainMenuStripPlugin.InitializeMenuStrip();
        PluginManager.Initialize(mainWindow);
        
        StandardElementsManager.Initialize();
        StandardElementsManager.CustomGetDefaultState = PluginManager.GetDefaultStateFor;
        ElementSaveExtensions.VariableChangedThroughReference += PluginManager.VariableSet;
        StandardElementsManagerGumTool.Initialize();
        VariableSaveExtensionMethods.CustomFixEnumerations = VariableSaveExtensionMethodsGumTool.FixEnumerationsWithReflection;
        
        PluginManager.Self.XnaInitialized();
        InitializeFileWatcher(container);
        
        Cursor LoadAddCursor()
        {
            try
            {
                Cursor cursor = new(typeof(MainWindow), "Content.Cursors.AddCursor.cur");
                return cursor;
            }
            catch
            {
                // Vic got this to crash on Sean's machine. Not sure why, but let's tolerate it since it's not breaking
                return Cursor.Current;
            }
        }
    }

    private void InitializeFileWatcher(IContainer container)
    {
        Timer timer = new(container);
        timer.Enabled = true;
        timer.Interval = 1000;
        timer.Tick += Handle;
        void Handle(object? sender, EventArgs e)
        {
            if (ProjectState.GumProjectSave is { } save && !string.IsNullOrEmpty(save.FullFileName))
            {
                FileWatchManager.Flush();
            }
        }
    }
    
    
}