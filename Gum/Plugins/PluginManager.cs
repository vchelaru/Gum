using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using System.Reflection;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Windows.Forms;
using Gum.Plugins.BaseClasses;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Gui.Plugins;
using Gum.Gui.Windows;
using ToolsUtilities;
using Gum.DataTypes.Behaviors;
using RenderingLibrary.Graphics;
using Gum.Responses;
using System.Runtime.CompilerServices;
using Gum.Wireframe;
using Gum.ToolStates;
using Gum.Managers;
using Gum.Services;
using RenderingLibrary;
using System.Numerics;
using Gum.Commands;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Services.Dialogs;
using Gum.Undo;
using Gum.Localization;

namespace Gum.Plugins;

#region PluginCategories enum

internal enum PluginCategories
{
    Global = 1,
    ProjectSpecific = 2,
    All = Global | ProjectSpecific
}

#endregion

public class PluginManager : IPluginManager
{
    #region Fields

    static PluginSettingsSave mPluginSettingsSave = new PluginSettingsSave();

    private List<Assembly> mExternalAssemblies = new List<Assembly>();
    private List<string> mReferenceListInternal = new List<string>();
    private List<string> mReferenceListLoaded = new List<string>();
    private List<string> mReferenceListExternal = new List<string>();

    private Dictionary<IPlugin, PluginContainer> mPluginContainers = new Dictionary<IPlugin, PluginContainer>();

    private const String ReferenceFileName = "References.txt";
    private const String CompatibilityFileName = "Compatibility.txt";
    
    static PluginManager mGlobalInstance;
    static List<PluginManager> mInstances = new List<PluginManager>();

    private IGuiCommands _guiCommands;
    private IMessenger _messenger;
    private IDialogService _dialogService;

    public static string PluginFolder
    {
        get
        {
            return FileManager.GetDirectory(Application.ExecutablePath) + "Plugins\\";
        }
    }

    #endregion

    #region Interface Lists


    [ImportMany(AllowRecomposition = true)]
    public IEnumerable<PluginBase> Plugins { get; set; }

    

    #endregion

    #region Properties

    public static PluginManager Self
    {
        get
        {
            if (mGlobalInstance == null)
            {
                mGlobalInstance = new PluginManager();
            }
            return mGlobalInstance;
        }
    }

    static string PluginSettingsSaveFileName
    {
        get
        {
            return FileManager.UserApplicationDataForThisApplication + "GumPluginSettings.xml";
        }
    }

    [Export("LocalizationService")]
    public LocalizationService LocalizationService => Locator.GetRequiredService<LocalizationService>();


    internal static List<PluginContainer> AllPluginContainers
    {
        get
        {
            List<PluginContainer> returnList = new List<PluginContainer>();

            foreach (PluginManager pluginManager in mInstances)
            {
                returnList.AddRange(pluginManager.mPluginContainers.Values);
            }

            return returnList;
        }
    }

    public Dictionary<IPlugin, PluginContainer> PluginContainers
    {
        get { return mPluginContainers; }
    }

    public bool IsInitialized => this.Plugins != null;
    #endregion

    #region Exported objects


    public static List<PluginManager> GetInstances()
    {
        return mInstances;
    }

    #endregion

    #region >>>Methods called by Gum when certain events happen<<<


    void CallMethodOnPlugin(Action<PluginBase> methodToCall, [CallerMemberName]string methodName = null)
    {
        if(this.Plugins == null)
        {
            throw new InvalidOperationException("Plugins haven't yet been initialized");
        }
#if !TEST
        // let internal plugins handle changes first before external plugins.
        var sortedPlugins = this.Plugins.OrderBy(item => !(item is InternalPlugin)).ToArray();
        foreach (var plugin in sortedPlugins)
        {
            if (this.PluginContainers.ContainsKey(plugin) == false)
            {
                throw new KeyNotFoundException("Could not find a plugin container for the plugin " + plugin);
            }

            PluginContainer container = this.PluginContainers[plugin];

            if (container.IsEnabled)
            {
                try
                {
                    methodToCall(plugin);
                }
                catch (Exception e)
                {
#if DEBUG
                    _dialogService.ShowMessage("Error in plugin " + plugin.FriendlyName + ":\n\n" + e.ToString());
#endif
                    container.Fail(e, "Failed in " + methodName);
                }
            }
        }      

#endif
    }

    public void BeforeSavingElementSave(ElementSave savedElement) => 
        CallMethodOnPlugin(plugin => plugin.CallBeforeElementSave(savedElement));

    public void AfterSavingElementSave(ElementSave savedElement) =>
        CallMethodOnPlugin(plugin => plugin.CallAfterElementSave(savedElement));

    public void BeforeSavingProjectSave(GumProjectSave savedProject) =>
        CallMethodOnPlugin(plugin => plugin.CallBeforeProjectSave(savedProject));

    public void ProjectLoad(GumProjectSave newlyLoadedProject) =>
        CallMethodOnPlugin(plugin => plugin.CallProjectLoad(newlyLoadedProject));

    public void ProjectPropertySet(string propertyName) =>
        CallMethodOnPlugin(plugin => plugin.CallProjectPropertySet(propertyName));

    public void ProjectSave(GumProjectSave savedProject) =>
        CallMethodOnPlugin(plugin => plugin.CallProjectSave(savedProject));

    public GraphicalUiElement CreateGraphicalUiElement(ElementSave elementSave)
    {
        GraphicalUiElement toReturn = null;
        CallMethodOnPlugin(plugin =>
        {
            var internalGue = plugin.CallCreateGraphicalUiElement(elementSave);

            if(internalGue != null)
            {
                toReturn = internalGue;
            }
        });
        return toReturn;
    }

    public void ProjectLocationSet(FilePath filePath) =>
        CallMethodOnPlugin(plugin => plugin.CallProjectLocationSet(filePath));

    public void Export(ElementSave elementToExport) =>
        CallMethodOnPlugin(plugin => plugin.CallExport(elementToExport));

    public void ModifyDefaultStandardState(string type, StateSave stateSave) =>
        CallMethodOnPlugin(plugin => plugin.CallAddAndRemoveVariablesForType(type, stateSave));

    public bool TryHandleDelete()
    {
        bool toReturn = false;

#if !TEST
        // let internal plugins handle changes first before external plugins.
        var sortedPlugins = this.Plugins.OrderBy(item => !(item is InternalPlugin)).ToArray();
        foreach (var plugin in sortedPlugins)
        {
            PluginContainer container = this.PluginContainers[plugin];

            if (container.IsEnabled)
            {
                try
                {
                    if(plugin.CallTryHandleDelete())
                    {
                        toReturn = true;
                        break;
                    }
                }
                catch (Exception e)
                {
#if DEBUG
                    _dialogService.ShowMessage("Error in plugin " + plugin.FriendlyName + ":\n\n" + e.ToString());
#endif
                    container.Fail(e, "Failed in " + TryHandleDelete);
                }
            }
        }
#endif
        return toReturn;
    }

    /// <summary>
    /// Allows all plugins to adjust the DeleteOptionsWindow whenever any object is deleted, including
    /// elements, instances, and behaviors.
    /// </summary>
    /// <param name="window">The window to modify.</param>
    /// <param name="objectsToDelete">An array of objects that may be deleted, which could be any Gum type.</param>
    public void ShowDeleteDialog(DeleteOptionsWindow window, Array objectsToDelete) =>
        CallMethodOnPlugin(plugin => plugin.CallDeleteOptionsWindowShow(window, objectsToDelete));

    public void DeleteConfirm(DeleteOptionsWindow window, Array objectsToDelete) => 
        CallMethodOnPlugin(plugin => plugin.CallDeleteConfirm(window, objectsToDelete));

    public void ElementRename(ElementSave elementSave, string oldName) =>
        CallMethodOnPlugin(plugin => plugin.CallElementRename(elementSave, oldName));

    public void ElementAdd(ElementSave element) =>
        CallMethodOnPlugin(plugin => plugin.CallElementAdd(element));

    public void ElementDelete(ElementSave element) =>
        CallMethodOnPlugin(plugin => plugin.CallElementDelete(element));

    public void ElementDuplicate(ElementSave oldElement, ElementSave newElement) =>
        CallMethodOnPlugin(plugin => plugin.CallElementDuplicate(oldElement, newElement));

    public void ElementReloaded(ElementSave element) =>
        CallMethodOnPlugin(plugin => plugin.CallElementReloaded(element));

    public void StateRename(StateSave stateSave, string oldName) => 
        CallMethodOnPlugin(plugin => plugin.CallStateRename(stateSave, oldName));

    public void StateAdd(StateSave stateSave) =>
        CallMethodOnPlugin((plugin) => plugin.CallStateAdd(stateSave));

    public void StateMovedToCategory(StateSave stateSave, StateSaveCategory newCategory, StateSaveCategory oldCategory) =>
        CallMethodOnPlugin(plugin => plugin.CallStateMovedToCategory(stateSave, newCategory, oldCategory));

    public void StateDelete(StateSave stateSave) =>
        CallMethodOnPlugin((plugin) => plugin.CallStateDelete(stateSave));

    public void ReactToStateSaveSelected(StateSave? stateSave) =>
        CallMethodOnPlugin((plugin) => plugin.CallReactToStateSaveSelected(stateSave));

    public void ReactToCustomStateSaveSelected(StateSave stateSave) =>
        CallMethodOnPlugin((plugin) => plugin.CallReactToCustomStateSaveSelected(stateSave));

    public void RefreshStateTreeView() =>
        CallMethodOnPlugin(plugin => plugin.CallRefreshStateTreeView());

    public void RefreshElementTreeView(IInstanceContainer? instanceContainer = null) =>
        CallMethodOnPlugin(plugin => plugin.CallRefreshElementTreeView(instanceContainer));


    public void CategoryRename(StateSaveCategory category, string oldName) =>
        CallMethodOnPlugin((plugin) => plugin.CallStateCategoryRename(category, oldName));

    public void CategoryAdd(StateSaveCategory category) =>
        CallMethodOnPlugin((plugin) => plugin.CallStateCategoryAdd(category));

    public void CategoryDelete(StateSaveCategory category) =>
        CallMethodOnPlugin((plugin) => plugin.CallStateCategoryDelete(category));

    public void ReactToStateSaveCategorySelected(StateSaveCategory? category) =>
        CallMethodOnPlugin((plugin) => plugin.CallReactToStateSaveCategorySelected(category));

    public void VariableAdd(ElementSave elementSave, string variableName) =>
        CallMethodOnPlugin((plugin) => plugin.CallVariableAdd(elementSave, variableName));

    public void VariableDelete(ElementSave elementSave, string variableName) =>
        CallMethodOnPlugin(plugin => plugin.CallVariableDelete(elementSave, variableName));

    public void VariableSet(ElementSave parentElement, InstanceSave? instance, string unqualifiedChangedMemberName, object? oldValue)
    {
        CallMethodOnPlugin(plugin => plugin.CallVariableSet(parentElement, instance, unqualifiedChangedMemberName, oldValue));
        CallMethodOnPlugin(plugin => plugin.CallVariableSetLate(parentElement, instance, unqualifiedChangedMemberName, oldValue), "VariableSet (Late)");
    }

    public void VariableSelected(IStateContainer container, VariableSave variable) =>
        CallMethodOnPlugin(plugin => plugin.CallVariableSelected(container, variable));

    public void VariableRemovedFromCategory(string variableName, StateSaveCategory category) =>
        CallMethodOnPlugin(plugin => plugin.CallVariableRemovedFromCategory(variableName, category));

    public void InstanceRename(ElementSave element, InstanceSave instanceSave, string oldName) =>
        CallMethodOnPlugin(plugin => plugin.CallInstanceRename(element, instanceSave, oldName));
            
    public void AfterUndo() =>
        CallMethodOnPlugin(plugin => plugin.CallAfterUndo());

    public List<Attribute> GetAttributesFor(VariableSave variableSave)
    {
        var listToFill = new List<Attribute>();
        CallMethodOnPlugin(plugin => plugin.CallFillVariableAttributes(variableSave, listToFill));
        return listToFill;
    }


    public void ElementSelected(ElementSave? elementSave) =>
        CallMethodOnPlugin(plugin => plugin.CallElementSelected(elementSave));

    internal void TreeNodeSelected(TreeNode? treeNode) =>
        CallMethodOnPlugin(plugin => plugin.CallTreeNodeSelected(treeNode));

    internal void StateWindowTreeNodeSelected(TreeNode treeNode) =>
        CallMethodOnPlugin(plugin => plugin.CallStateWindowTreeNodeSelected(treeNode));

    public ITreeNode? GetTreeNodeOver()
    {
        ITreeNode? treeNodeOver = null;

        CallMethodOnPlugin(plugin =>
        {
            var internalTreeNode = plugin.CallGetTreeNodeOver();

            if (internalTreeNode != null)
            {
                treeNodeOver = internalTreeNode;
            }
        });

        return treeNodeOver;
    }

    public IEnumerable<ITreeNode> GetSelectedNodes()
    {
        IEnumerable<ITreeNode>? toReturn = null;
        CallMethodOnPlugin(plugin =>
        {
            var internalResult = plugin.CallGetSelectedNodes();
            if (internalResult != null)
            {
                toReturn = internalResult;
            }
        });
        return toReturn ?? Enumerable.Empty<ITreeNode>();
    }

    public void BehaviorSelected(BehaviorSave? behaviorSave) =>
        CallMethodOnPlugin(plugin => plugin.CallBehaviorSelected(behaviorSave));

    public void BehaviorReferenceSelected(ElementBehaviorReference behaviorReference, ElementSave elementSave) =>
        CallMethodOnPlugin(plugin => plugin.CallBehaviorReferenceSelected(behaviorReference, elementSave));

    public void BehaviorVariableSelected(VariableSave variable) =>
        CallMethodOnPlugin(plugin => plugin.CallBehaviorVariableSelected(variable));
    public void BehaviorCreated(BehaviorSave behavior) =>
        CallMethodOnPlugin(plugin => plugin.CallBehaviorCreated(behavior));

    public void BehaviorDeleted(BehaviorSave behavior) =>
        CallMethodOnPlugin(plugin => plugin.CallBehaviorDeleted(behavior));

    public void InstanceSelected(ElementSave elementSave, InstanceSave instance) =>
        CallMethodOnPlugin(plugin => plugin.CallInstanceSelected(elementSave, instance));

    public virtual void InstanceAdd(ElementSave elementSave, InstanceSave instance) =>
        CallMethodOnPlugin(plugin => plugin.CallInstanceAdd(elementSave, instance));


    public virtual void InstanceDelete(ElementSave elementSave, InstanceSave instance) =>
        CallMethodOnPlugin(plugin => plugin.CallInstanceDelete(elementSave, instance));

    public virtual void InstancesDelete(ElementSave elementSave, InstanceSave[] instances) =>
        CallMethodOnPlugin(plugin => plugin.CallInstancesDelete(elementSave, instances));

    public void BehaviorInstanceAdd(BehaviorSave behavior, BehaviorInstanceSave instance) =>
        CallMethodOnPlugin(plugin => plugin.CallBehaviorInstanceAdd(behavior, instance));

    public StateSave? GetDefaultStateFor(string type)
    {
        StateSave? toReturn = null;
        CallMethodOnPlugin(plugin => toReturn = plugin.CallGetDefaultStateFor(type) ?? toReturn);
        return toReturn;
    }

    public void InstanceReordered(InstanceSave instance) =>
        CallMethodOnPlugin(plugin => plugin.CallInstanceReordered(instance));
    

    public bool GetIfExtensionIsValid(string extension, ElementSave parentElement, InstanceSave instance, string changedMember)
    {
        bool toReturn = false;
        CallMethodOnPlugin(plugin =>
        {
            var result = plugin.CallIsExtensionValid(extension, parentElement, instance, changedMember);
            if(result)
            {
                toReturn = true;
            }
        });

        return toReturn;
    }

    public void RefreshBehaviorView(ElementSave elementSave) =>
        CallMethodOnPlugin(plugin => plugin.CallRefreshBehaviorUi());

    internal void RefreshVariableView(bool force) =>
        CallMethodOnPlugin(plugin => plugin.CallRefreshVariableView(force));

    internal void BehaviorReferencesChanged(ElementSave elementSave) => 
        CallMethodOnPlugin(plugin => plugin.CallBehaviorReferencesChanged(elementSave));

    public void WireframeRefreshed() =>
        CallMethodOnPlugin(
            plugin => plugin.CallWireframeRefreshed());

    internal void WireframePropertyChanged(string propertyName) =>
        CallMethodOnPlugin(plugin => plugin.CallWireframePropertyChanged(propertyName));

    internal IRenderableIpso CreateRenderableForType(string type)
    {
        IRenderableIpso toReturn = null;


        CallMethodOnPlugin(
            plugin =>
            {
                var innerToReturn = plugin.CallCreateRenderableForType(type);

                if (innerToReturn != null)
                {
                    toReturn = innerToReturn;
                }

            },
            nameof(CreateRenderableForType));

        return toReturn;
    }

    public DeleteResponse GetDeleteStateResponse(StateSave stateSave, IStateContainer element)
    {
        DeleteResponse response = new DeleteResponse();
        response.ShouldDelete = true;
        response.ShouldShowMessage = false;

#if !TEST
        // let internal plugins handle changes first before external plugins.
        var sortedPlugins = this.Plugins.OrderBy(item => !(item is InternalPlugin)).ToArray();
        foreach (var plugin in sortedPlugins)
        {
            PluginContainer container = this.PluginContainers[plugin];

            if (container.IsEnabled && plugin.GetDeleteStateResponse != null)
            {
                try
                {
                    var responseInternal = plugin.GetDeleteStateResponse(stateSave, element);

                    if(responseInternal.ShouldDelete == false)
                    {
                        response = responseInternal;
                        break;
                    }
                }
                catch (Exception e)
                {
#if DEBUG
                    _dialogService.ShowMessage("Error in plugin " + plugin.FriendlyName + ":\n\n" + e.ToString());
#endif
                    container.Fail(e, $"Failed in {nameof(GetDeleteStateResponse)}");
                }
            }
        }

#endif
        return response;
    }

    public DeleteResponse GetDeleteStateCategoryResponse(StateSaveCategory stateSaveCategory, IStateContainer element)
    {
        DeleteResponse response = new DeleteResponse();
        response.ShouldDelete = true;
        response.ShouldShowMessage = false;

#if !TEST
        // let internal plugins handle changes first before external plugins.
        var sortedPlugins = this.Plugins.OrderBy(item => !(item is InternalPlugin)).ToArray();
        foreach (var plugin in sortedPlugins)
        {
            PluginContainer container = this.PluginContainers[plugin];

            if (container.IsEnabled && plugin.GetDeleteStateCategoryResponse != null)
            {
                try
                {
                    var responseInternal = plugin.GetDeleteStateCategoryResponse(stateSaveCategory, element);

                    if (responseInternal.ShouldDelete == false)
                    {
                        response = responseInternal;
                        break;
                    }
                }
                catch (Exception e)
                {
#if DEBUG
                    _dialogService.ShowMessage("Error in plugin " + plugin.FriendlyName + ":\n\n" + e.ToString());
#endif
                    container.Fail(e, $"Failed in {nameof(GetDeleteStateCategoryResponse)}");
                }
            }
        }

#endif
        return response;
    }

    internal void XnaInitialized() =>
        CallMethodOnPlugin(plugin => plugin.CallXnaInitialized());

    public void HandleWireframeResized() =>
        CallMethodOnPlugin(plugin => plugin.CallWireframeResized());

    public void CameraChanged() =>
        CallMethodOnPlugin(plugin => plugin.CallCameraChanged());

    public void BeforeRender() =>
        CallMethodOnPlugin(plugin => plugin.CallBeforeRender());

    public void AfterRender() =>
        CallMethodOnPlugin(plugin => plugin.CallAfterRender());

    internal void ReactToFileChanged(FilePath filePath) =>
        CallMethodOnPlugin(plugin => plugin.CallReactToFileChanged(filePath));
    

    public void SetHighlightedIpso(IPositionedSizedObject? positionedSizedObject) =>
        CallMethodOnPlugin(plugin => plugin.CallSetHighlightedIpso(positionedSizedObject));

    public void HighlightTreeNode(IPositionedSizedObject? positionedSizedObject) =>
        CallMethodOnPlugin(plugin => plugin.CallHighlightTreeNode(positionedSizedObject));

    public void IpsoSelected(IPositionedSizedObject? positionedSizedObject) =>
        CallMethodOnPlugin(plugin => plugin.CallIpsoSelected(positionedSizedObject));

    public IEnumerable<IPositionedSizedObject>? GetSelectedIpsos()
    {
        IEnumerable<IPositionedSizedObject>? toReturn = null;
        CallMethodOnPlugin(plugin =>
        {
            var innerResult = plugin.CallGetSelectedIpsos();
            if (innerResult != null)
            {
                toReturn = innerResult;
            }
        });
        return toReturn;
    }

    public System.Numerics.Vector2? GetWorldCursorPosition(InputLibrary.Cursor cursor)
    {
        Vector2? toReturn = null;
        CallMethodOnPlugin(plugin =>
        {
            var innerResult = plugin.CallGetWorldCursorPosition(cursor);

            if(innerResult != null)
            {
                toReturn = innerResult;
            }
        });

        return toReturn;
    }

    public void FillWithErrors(List<ErrorViewModel> errors, PluginBase? plugin = null)
    {
        if(plugin != null)
        {
            var internalErrors = plugin.CallGetAllErrors();
            if(internalErrors != null)
            {
                errors.AddRange(internalErrors);
            }
        }
        else
        {
            CallMethodOnPlugin(plugin =>
            {
                var internalErrors = plugin.CallGetAllErrors();
                if (internalErrors != null)
                {
                    errors.AddRange(internalErrors);
                }
            });
        }

    }

    /// <summary>
    /// Returns whether any plugins are asking un-highlighting to be suppressed when moving over the editor.
    /// If true, then the editor will not un-highlight the currently-highlighted object. This allows other plugins
    /// to force a highlight such as the tree view on hover.
    /// </summary>
    /// <returns>Whether to suppress the unhighlighting.</returns>
    public bool GetIfShouldSuppressRemoveEditorHighlight()
    {
        bool toReturn = false;
        CallMethodOnPlugin(plugin =>
        {
            if (plugin.CallGetIfShouldSuppressRemoveEditorHighlight() == true)
            {
                toReturn = true;
            }
        });
        return toReturn;
    }

    public void FocusSearch() =>
        CallMethodOnPlugin(plugin => plugin.CallFocusSearch());

    public bool ShouldExclude(VariableSave defaultVariable, RecursiveVariableFinder rvf)
    {
        bool shouldExclude = false;
        foreach (var plugin in this.Plugins.Where(item=>this.PluginContainers[item].IsEnabled))
        {
            PluginContainer container = this.PluginContainers[plugin];

            if (container.Plugin is PluginBase pluginBase)
            {

                try
                {
                    shouldExclude |= pluginBase.GetIfVariableIsExcluded(defaultVariable, rvf);
                }
                catch (Exception e)
                {
                    container.Fail(e, "Failed in GetIfVariableIsExcluded");
                }
            }
        }
        return shouldExclude;
    }
    #endregion


    #region Additional Methods

    public PluginManager()
    {

    }


    public void Initialize()
    {
        _guiCommands = Locator.GetRequiredService<IGuiCommands>();
        _messenger = Locator.GetRequiredService<IMessenger>();
        _dialogService = Locator.GetRequiredService<IDialogService>();

        _messenger.Register<AfterUndoMessage>(this, (_, _) => AfterUndo());
        LoadPluginSettings();
        LoadPlugins(this);
        mInstances.Add(this);
    }

    private void LoadPluginSettings()
    {

        if (System.IO.File.Exists(PluginSettingsSaveFileName))
        {
            mPluginSettingsSave = PluginSettingsSave.Load(PluginSettingsSaveFileName);
        }
        else
        {
            mPluginSettingsSave = new PluginSettingsSave();
        }
    }

    public void SavePluginSettings()
    {
        FileManager.XmlSerialize(mPluginSettingsSave, PluginSettingsSaveFileName);
    }

    private void LoadReferenceLists()
    {
        // We use absolute paths for some of the .dlls and .exes
        // because if we don't, then Glue looks for them in the Startup
        // path, which could depend on whether Glue is launched from a shortcut
        // or not - this is really common for released versions.
        string executablePath = FileManager.GetDirectory(System.Windows.Forms.Application.ExecutablePath);

        //Load Internal List
        mReferenceListInternal.Add(executablePath + "Ionic.Zip.dll");
        mReferenceListExternal.Add(executablePath + "CsvLibrary.dll");
        mReferenceListExternal.Add(executablePath + "RenderingLibrary.dll");

        mReferenceListExternal.Add(executablePath + "ToolsUtilities.dll");
        mReferenceListExternal.Add(executablePath + "Gum.exe");

        mReferenceListInternal.Add("Microsoft.CSharp.dll");
        mReferenceListInternal.Add("System.dll");
        mReferenceListInternal.Add("System.ComponentModel.Composition.dll");
        mReferenceListInternal.Add("System.Core.dll");
        mReferenceListInternal.Add("System.Data.dll");
        mReferenceListInternal.Add("System.Data.DataSetExtensions.dll");
        mReferenceListInternal.Add("System.Drawing.dll");
        mReferenceListInternal.Add("System.Windows.Forms.dll");
        mReferenceListInternal.Add("System.Xml.dll");
        mReferenceListInternal.Add("System.Xml.Linq.dll");
    }

    private void LoadExternalReferenceList(string filePath)
    {
        string ReferenceFilePath = filePath + "\\" + ReferenceFileName;
        mReferenceListExternal = new List<string>();

        if (File.Exists(ReferenceFilePath))
        {
            using (StreamReader file = new StreamReader(ReferenceFilePath))
            {
                string line;

                while ((line = file.ReadLine()) != null)
                {
                    if (!String.IsNullOrEmpty(line) &&
                       !String.IsNullOrEmpty(line.Trim()))
                    {
                        if (FileManager.FileExists(line.Trim()))
                        {
                            string absolute = FileManager.MakeAbsolute(line.Trim());

                            if (!mReferenceListInternal.Contains(absolute))
                            {
                                mReferenceListExternal.Add(absolute);
                                mExternalAssemblies.Add(Assembly.LoadFrom(absolute));
                            }
                        }
                    }
                }
            }
        }
    }


    private void LoadPlugins(PluginManager instance)
    {
        #region Get the Catalog


        ResolveEventHandler reh = new ResolveEventHandler(instance.currentDomain_AssemblyResolve);

        try
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += reh;
            AggregateCatalog catalog = instance.CreateCatalog();

            var batch = new CompositionBatch();
            batch.AddExportedValue<ISelectedState>(Locator.GetRequiredService<ISelectedState>());
            

            var container = new CompositionContainer(catalog);

            container.Compose(batch);

            container.ComposeParts(instance);
        }
        catch (Exception e)
        {
            string error = "Error loading plugins\n";
            if (e is ReflectionTypeLoadException)
            {
                error += "Error is a reflection type load exception\n";
                var loaderExceptions = (e as ReflectionTypeLoadException).LoaderExceptions;

                foreach (var loaderException in loaderExceptions)
                {
                    error += "\n" + loaderException.ToString();
                }
            }
            else
            {
                error += "\n" + e.Message;

                if (e.InnerException != null)
                {
                    error += "\n Inner Exception:\n" + e.InnerException.Message;
                }
            }
            _dialogService.ShowMessage(error);

            instance.Plugins = new List<PluginBase>();

            return;
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= reh;
        }

        #endregion

        #region Start all plugins

        foreach (PluginBase plugin in instance.Plugins)
        {
            StartupPlugin(plugin, instance);
        }

        #endregion
    }



    private Assembly currentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
    {
        foreach (Assembly item in mExternalAssemblies)
        {
            if (item.FullName == args.Name)
            {
                return item;
            }
        }

        //MessageBox.Show("Couldn't find assembly: " + args.Name + " for " + args.RequestingAssembly);
        if(args.RequestingAssembly != null)
        {
            _guiCommands.PrintOutput("Couldn't find assembly: " + args.Name + " for " + args.RequestingAssembly);
        }

        return null;
    }

    //internal static void Initialize()
    //{
    //    if (mGlobalInstance == null)
    //    {
    //        mGlobalInstance = new PluginManager(true);
    //        LoadPlugins(mGlobalInstance);
    //    }

    //    if (mProjectInstance != null)
    //    {
    //        foreach (IPlugin plugin in mProjectInstance.mPluginContainers.Keys)
    //        {
    //            ShutDownPlugin(plugin, PluginShutDownReason.GlueShutDown);
    //        }
    //    }

    //    mProjectInstance = new PluginManager(false);

    //    mInstances.Clear();
    //    mInstances.Add(mGlobalInstance);
    //    mInstances.Add(mProjectInstance);


    //    LoadPlugins(mProjectInstance);
    //}

    internal static void StartupPlugin(IPlugin plugin, PluginManager instance)
    {

        // See if the plugin already exists - it may implement multiple interfaces
        if (!instance.mPluginContainers.ContainsKey(plugin))
        {
            PluginContainer pluginContainer = new PluginContainer(plugin);
            instance.mPluginContainers.Add(plugin, pluginContainer);

            try
            {
                plugin.UniqueId = plugin.GetType().FullName;


                if (!mPluginSettingsSave.DisabledPlugins.Contains(plugin.UniqueId))
                {

                    plugin.StartUp();
                }
                else
                {
                    pluginContainer.IsEnabled = false;
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Locator.GetRequiredService<IDialogService>().ShowMessage("Plugin failed to start up:\n\n" + e.ToString());
#endif
                pluginContainer.Fail(e, "Plugin failed in StartUp");
            }
        }
    }

    private AggregateCatalog CreateCatalog()
    {
        mExternalAssemblies.Clear();
        LoadReferenceLists();

        var returnValue = new AggregateCatalog();

        var pluginDirectories = new List<string>();

        pluginDirectories.Add(PluginFolder);

        foreach (var directory in pluginDirectories)
        {
            List<string> dllFiles = FileManager.GetAllFilesInDirectory(directory, "dll");
            string executablePath = FileManager.GetDirectory(System.Windows.Forms.Application.ExecutablePath);

            //dllFiles.Add(executablePath + "Gum.exe");
            foreach (string dll in dllFiles)
            {
                try
                {
                    Assembly loadedAssembly = Assembly.LoadFrom(dll);

                    returnValue.Catalogs.Add(new AssemblyCatalog(loadedAssembly));

                }
                catch
                {
                    // todo - report the error
                }
            }
        }

        returnValue.Catalogs.Add(new AssemblyCatalog(System.Reflection.Assembly.GetExecutingAssembly()));
        
        return returnValue;
    }

    // Eventually we may add support for this but not on the first pass
    //private CompilerResults CompilePlugin(string filepath)
    //{
    //    using (new ZipFile()) { }

    //    texture.ToString();// We do this to eliminate "is never used" warnings

    //    if (IsCompatible(filepath))
    //    {
    //        LoadExternalReferenceList(filepath);

    //        return PluginCompiler.Compiler.CompilePlugin(filepath, mReferenceListInternal, mReferenceListLoaded, mReferenceListExternal);
    //    }

    //    return null;
    //}

    private static bool IsCompatible(string filepath)
    {
        var compatibilityFilePath = filepath + @"\" + CompatibilityFileName;

        //Check for compatibility file
        if (File.Exists(compatibilityFilePath))
        {
            string value;

            //Get compatibility timestamp
            using (var file = new StreamReader(compatibilityFilePath))
            {
                value = file.ReadToEnd();
            }

            DateTime compatibilityTime;

            if (DateTime.TryParse(value, out compatibilityTime))
            {
                //If compatibility timestamp is newer than current Glue's timestamp, then don't compile plugin
                if (new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime < compatibilityTime)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void ExportFile(ElementSave elementSave)
    {
        PluginManager pluginManager = mGlobalInstance;

        foreach (PluginBase plugin in pluginManager.Plugins)
        {
            PluginContainer container = pluginManager.mPluginContainers[plugin];

            if (container.IsEnabled)
            {
                try
                {
                    plugin.CallExport(elementSave);
                }
                catch (Exception e)
                {
                    container.Fail(e, "Failed in ReactToRightClick");
                }
            }
        }
    }

    public static bool ShutDownPlugin(IPlugin pluginToShutDown)
    {
        return ShutDownPlugin(pluginToShutDown, PluginShutDownReason.PluginInitiated);
    }

    internal static bool ShutDownPlugin(IPlugin pluginToShutDown,
        PluginShutDownReason shutDownReason)
    {
        bool doesPluginWantToShutDown = true;
        PluginContainer? container = null;

        if (mGlobalInstance.mPluginContainers.ContainsKey(pluginToShutDown))
        {
            container = mGlobalInstance.mPluginContainers[pluginToShutDown];
        }

        try
        {
            doesPluginWantToShutDown =
                container?.Plugin.ShutDown(shutDownReason) ?? false;
        }
        catch (Exception)
        {
            doesPluginWantToShutDown = true;
        }


        if (doesPluginWantToShutDown)
        {
            container!.IsEnabled = false;
        }

        if (shutDownReason == PluginShutDownReason.UserDisabled)
        {
            if (!mPluginSettingsSave.DisabledPlugins.Contains(pluginToShutDown.UniqueId))
            {
                mPluginSettingsSave.DisabledPlugins.Add(pluginToShutDown.UniqueId);
                mPluginSettingsSave.Save(PluginSettingsSaveFileName);
            }
        }

        return doesPluginWantToShutDown;
    }

    internal static void ReenablePlugin(IPlugin pluginToReenable)
    {
        if (mPluginSettingsSave.DisabledPlugins.Remove(pluginToReenable.UniqueId))
            mPluginSettingsSave.Save(PluginSettingsSaveFileName);
    }

    internal static void AddInternalPlugins()
    {
    }


    #endregion

}
