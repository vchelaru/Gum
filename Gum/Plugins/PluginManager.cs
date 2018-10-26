using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Reflection;
using System.ComponentModel.Composition.Hosting;
using System.CodeDom.Compiler;
using System.IO;
using System.Windows.Forms;
using Gum.Plugins.BaseClasses;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Gui.Plugins;
using Gum.Gui.Forms;
using ToolsUtilities;
using Gum.DataTypes.Behaviors;

namespace Gum.Plugins
{
    #region PluginCategories enum

    internal enum PluginCategories
    {
        Global = 1,
        ProjectSpecific = 2,
        All = Global | ProjectSpecific
    }

    #endregion

    public class PluginManager
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
        private bool mError = false;
        
        static PluginManager mGlobalInstance;
        static PluginManager mProjectInstance;
        static List<PluginManager> mInstances = new List<PluginManager>();
        private bool mGlobal;

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


        #endregion

        #region Exported objects

        public static PluginManager GetGlobal()
        {
            return mGlobalInstance;
        }

        public static PluginManager GetProject()
        {
            return mProjectInstance;
        }


        public static List<PluginManager> GetInstances()
        {
            return mInstances;
        }

        #endregion

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

        #region Methods


        public void Initialize(MainWindow mainWindow)
        {
            LoadPluginSettings();
            LoadPlugins(this, mainWindow);
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


        private static void LoadPlugins(PluginManager instance, MainWindow mainWindow)
        {
            #region Get the Catalog

            instance.mError = false;

            ResolveEventHandler reh = new ResolveEventHandler(instance.currentDomain_AssemblyResolve);

            try
            {
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += reh;
                AggregateCatalog catalog = instance.CreateCatalog();



                var container = new CompositionContainer(catalog);
                container.ComposeParts(instance);
            }
            catch (Exception e)
            {
                string error = "Error loading plugins";
                if (e is ReflectionTypeLoadException)
                {
                    error += "Error is a reflection type load exception\n";
                    var loaderExceptions = (e as ReflectionTypeLoadException).LoaderExceptions;

                    foreach(var loaderException in loaderExceptions)
                    {
                        error += "\n" + loaderException.ToString();
                    }
                }
                else
                {
                    error += "\n" + e.Message;

                    if(e.InnerException != null)
                    {
                        error += "\n Inner Exception:\n" + e.InnerException.Message;
                    }
                }
                MessageBox.Show(error);

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

                // We used to do this all in an assign references method,
                // but we now do it here so that the Startup function can have
                // access to these references.
                if (plugin is MainWindowPlugin)
                {
                    ((MainWindowPlugin)plugin).MainWindow = mainWindow;
                }

                plugin.MenuStrip = mainWindow.MainMenuStrip;

                StartupPlugin(plugin, instance);
            }
            
            #endregion
        }

        private Assembly currentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (Assembly item in mExternalAssemblies)
            {
                if (item.FullName == args.Name)
                {
                    return item;
                }
            }

            MessageBox.Show("Couldn't find assembly: " + args.Name + " for " + args.RequestingAssembly);

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
                    MessageBox.Show("Plugin failed to start up:\n\n" + e.ToString());
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

                dllFiles.Add(executablePath + "Gum.exe");
                foreach (string dll in dllFiles)
                {
                    Assembly loadedAssembly = Assembly.LoadFrom(dll);

                    returnValue.Catalogs.Add(new AssemblyCatalog(loadedAssembly));
                }
            }

            if (mGlobal)
            {
                returnValue.Catalogs.Add(new AssemblyCatalog(System.Reflection.Assembly.GetExecutingAssembly()));
            }

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


        private static bool ShouldProcessPluginManager(PluginCategories pluginCategories, PluginManager pluginManager)
        {
            return (pluginManager.mGlobal && (pluginCategories & PluginCategories.Global) == PluginCategories.Global) ||
                                (!pluginManager.mGlobal && (pluginCategories & PluginCategories.ProjectSpecific) == PluginCategories.ProjectSpecific);
        }

        
        public static bool ShutDownPlugin(IPlugin pluginToShutDown)
        {
            return ShutDownPlugin(pluginToShutDown, PluginShutDownReason.PluginInitiated);
        }

        internal static bool ShutDownPlugin(IPlugin pluginToShutDown,
            PluginShutDownReason shutDownReason)
        {
            bool doesPluginWantToShutDown = true;
            PluginContainer container;

            if (mGlobalInstance.mPluginContainers.ContainsKey(pluginToShutDown))
            {
                container = mGlobalInstance.mPluginContainers[pluginToShutDown];
            }
            else
            {
                container = mProjectInstance.mPluginContainers[pluginToShutDown];
            }

            try
            {
                doesPluginWantToShutDown =
                    container.Plugin.ShutDown(shutDownReason);
            }
            catch (Exception)
            {
                doesPluginWantToShutDown = true;
            }


            if (doesPluginWantToShutDown)
            {
                container.IsEnabled = false;
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

        #region Methods called by Gum when certain events happen


        void CallMethodOnPlugin(Action<PluginBase> methodToCall, string methodName)
        {

#if !TEST

            foreach (var plugin in this.Plugins)
            {
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
                        MessageBox.Show("Error in plugin " + plugin.FriendlyName + ":\n\n" + e.ToString());
#endif
                        container.Fail(e, "Failed in " + methodName);
                    }
                }
            }      

#endif
        }

        internal void BeforeElementSave(ElementSave savedElement)
        {
            foreach (PluginBase plugin in this.Plugins)
            {
                PluginContainer container = this.mPluginContainers[plugin];

                if (container.IsEnabled)
                {
                    try
                    {
                        plugin.CallBeforeElementSave(savedElement);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        MessageBox.Show("Error in plugin " + plugin.FriendlyName + ":\r\n" + e.ToString());
#endif
                        container.Fail(e, "Failed in BeforeElementSave");
                    }
                }
            }
        }

        internal void AfterElementSave(ElementSave savedElement)
        {
            foreach (PluginBase plugin in this.Plugins)
            {
                PluginContainer container = this.mPluginContainers[plugin];

                if (container.IsEnabled)
                {
                    try
                    {
                        plugin.CallAfterElementSave(savedElement);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        MessageBox.Show("Error in plugin " + plugin.FriendlyName + ":\r\n" + e.ToString());
#endif
                        container.Fail(e, "Failed in ElementSave");
                    }
                }
            }
        }

        internal void BeforeProjectSave(GumProjectSave savedProject)
        {
            foreach (var plugin in this.Plugins)
            {
                PluginContainer container = this.PluginContainers[plugin];

                if (container.IsEnabled)
                {
                    try
                    {
                        plugin.CallBeforeProjectSave(savedProject);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        MessageBox.Show("Error in plugin " + plugin.FriendlyName + ":\n\n" + e.ToString());
#endif
                        container.Fail(e, "Failed in BeforeProjectSave");
                    }
                }
            }
        }

        internal void Export(ElementSave elementToExport)
        {
            foreach (PluginBase plugin in this.Plugins)
            {
                PluginContainer container = this.mPluginContainers[plugin];

                if (container.IsEnabled)
                {
                    try
                    {
                        plugin.CallExport(elementToExport);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        MessageBox.Show("Error in plugin " + plugin.FriendlyName + ":\n\n" + e.ToString());
#endif
                        container.Fail(e, "Failed in ReactToRightClick");
                    }
                }
            }
        }

        internal void ModifyDefaultStandardState(string type, StateSave stateSave)
        {
            CallMethodOnPlugin(
                delegate(PluginBase plugin)
                {
                    plugin.CallAddAndRemoveVariablesForType(type, stateSave);

                },
                "ModifyDefaultStandardState"
                );
        }

        internal void ShowDeleteDialog(DeleteOptionsWindow window, object objectToDelete)
        {
            CallMethodOnPlugin(
                delegate(PluginBase plugin)
                {
                    plugin.CallDeleteOptionsWindowShow(window, objectToDelete);
                    
                },
                "ShowDeleteDialog"
                );
        }

        internal void DeleteConfirm(DeleteOptionsWindow window, object objectToDelete)
        {
            CallMethodOnPlugin(
                delegate(PluginBase plugin)
                {
                    plugin.CallDeleteConfirm(window, objectToDelete);
                },
                "ConfirmDelete"
                );
        }

        internal void ElementRename(ElementSave elementSave, string oldName)
        {
            CallMethodOnPlugin(
                delegate(PluginBase plugin)
                {
                    plugin.CallElementRename(elementSave, oldName);
                },
                "ElementRename"
                );
        }

        internal void StateRename(StateSave stateSave, string oldName)
        {
            CallMethodOnPlugin(
                delegate (PluginBase plugin)
                {
                    plugin.CallStateRename(stateSave, oldName);
                },
                "StateRename"
                );
        }

        internal void CategoryRename(StateSaveCategory category, string oldName)
        {
            CallMethodOnPlugin(
                delegate (PluginBase plugin)
                {
                    plugin.CallStateCategoryRename(category, oldName);
                },
                "CategoryRename"
                );
        }

        internal void InstanceRename(InstanceSave instanceSave, string oldName)
        {
            CallMethodOnPlugin(
                delegate (PluginBase plugin)
                {
                    plugin.CallInstanceRename(instanceSave, oldName);
                },
                "InstanceRename"
                );

        }

        internal void GuidesChanged()
        {
            CallMethodOnPlugin(
                delegate(PluginBase plugin)
                {
                    plugin.CallGuidesChanged();
                },
                "GuidesChanged"
            );
        }

        internal void ProjectLoad(GumProjectSave newlyLoadedProject)
        {

            foreach (var plugin in this.Plugins)
            {
                PluginContainer container = this.PluginContainers[plugin];

                if (container.IsEnabled)
                {
                    try
                    {
                        plugin.CallProjectLoad(newlyLoadedProject);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        MessageBox.Show("Error in plugin " + plugin.FriendlyName + ":\n\n" + e.ToString());
#endif
                        container.Fail(e, "Failed in ProjectLoad");
                    }
                }
            }
        }

        internal void ProjectSave(GumProjectSave savedProject)
        {
            foreach (var plugin in this.Plugins)
            {
                PluginContainer container = this.PluginContainers[plugin];

                if (container.IsEnabled)
                {
                    try
                    {
                        plugin.CallProjectSave(savedProject);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        MessageBox.Show("Error in plugin " + plugin.FriendlyName + ":\n\n" + e.ToString());
#endif
                        container.Fail(e, "Failed in ProjectSave");
                    }
                }
            }
        }

        internal List<Attribute> GetAttributesFor(VariableSave variableSave)
        {
            List<Attribute> listToFill = new List<Attribute>();

            CallMethodOnPlugin(
                delegate(PluginBase plugin)
                {
                    plugin.CallFillVariableAttributes(variableSave, listToFill);
                },
                "GetAttributesFor"
            );

            return listToFill;
        }

        internal void VariableSet(ElementSave parentElement, InstanceSave instance, string changedMember, object oldValue)
        {
            CallMethodOnPlugin(
                delegate(PluginBase plugin)
                {
                    plugin.CallVariableSet(parentElement, instance, changedMember, oldValue);
                },
                nameof(VariableSet)
            );

        }

        internal void ElementSelected(ElementSave elementSave)
        {
            CallMethodOnPlugin(
                (plugin) => plugin.CallElementSelected(elementSave),
                nameof(ElementSelected)
            );

        }

        internal void TreeNodeSelected(TreeNode treeNode)
        {
            CallMethodOnPlugin(
                (plugin) => plugin.CallTreeNodeSelected(treeNode),
                nameof(TreeNodeSelected)
                );
        }

        internal void BehaviorSelected(BehaviorSave behaviorSave)
        {
            CallMethodOnPlugin(
                delegate (PluginBase plugin)
                {
                    plugin.CallBehaviorSelected(behaviorSave);
                },
                nameof(BehaviorSelected)
            );

        }

        internal void InstanceSelected(ElementSave elementSave, InstanceSave instance)
        {
            CallMethodOnPlugin(
                delegate(PluginBase plugin)
                {
                    plugin.CallInstanceSelected(elementSave, instance);
                },
                "InstanceSelected"
            );

        }

        internal void InstanceAdd(ElementSave elementSave, InstanceSave instance)
        {

            CallMethodOnPlugin(
                (plugin) => plugin.CallInstanceAdd(elementSave, instance),
                "InstanceAdd"
            );
        }

        internal void InstanceDelete(ElementSave elementSave, InstanceSave instance)
        {
            CallMethodOnPlugin(
                (plugin) => plugin.CallInstanceDelete(elementSave, instance),
                nameof(InstanceDelete)
            );
        }

        internal void BehaviorReferencesChanged(ElementSave elementSave)
        {
            CallMethodOnPlugin(
                (plugin) => plugin.CallBehaviorReferencesChanged(elementSave),
                nameof(BehaviorReferencesChanged)
            );
        }

        internal void WireframeRefreshed()
        {
            CallMethodOnPlugin(
                (plugin) => plugin.CallWireframeRefreshed(),
                nameof(WireframeRefreshed)
                );
        }

        #endregion

        internal bool ShouldExclude(VariableSave defaultVariable, RecursiveVariableFinder rvf)
        {
            bool shouldExclude = false;
            foreach (var plugin in this.Plugins.Where(item=>this.PluginContainers[item].IsEnabled))
            {
                PluginContainer container = this.PluginContainers[plugin];

                if (container.Plugin is PluginBase)
                {

                    try
                    {
                        shouldExclude |= ((PluginBase)container.Plugin).GetIfVariableIsExcluded(defaultVariable, rvf);
                    }
                    catch (Exception e)
                    {
                        container.Fail(e, "Failed in GetIfVariableIsExcluded");
                    }
                }
            }
            return shouldExclude;
        }
    }
}
