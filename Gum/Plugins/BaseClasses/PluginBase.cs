using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.Gui.Windows;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using Gum.DataTypes.Behaviors;
using RenderingLibrary.Graphics;

namespace Gum.Plugins.BaseClasses
{
    public abstract class PluginBase : IPlugin
    {
        #region Events

        public event Action<GumProjectSave> ProjectLoad;
        public event Action<GumProjectSave> AfterProjectSave;
        public event Action<GumProjectSave> BeforeProjectSave;
        public event Action<ElementSave> BeforeElementSave;
        public event Action<ElementSave> AfterElementSave;
        public event Action GuidesChanged;
        public event Action<ElementSave> Export;
        public event Action<DeleteOptionsWindow, object> DeleteOptionsWindowShow;
        public event Action<DeleteOptionsWindow, object> DeleteConfirm;

        public event Action<ElementSave> ElementAdd;
        public event Action<ElementSave> ElementDelete;

        /// <summary>
        /// Event raised when the element is renamed.
        /// </summary>
        /// <remarks>
        /// ElementSave is the element that was renamed
        /// string is the old name
        /// </remarks>
        public event Action<ElementSave, string> ElementRename;
        /// <summary>
        /// Action raised when an event is renamed. String parameter is the State's old name.
        /// </summary>
        public event Action<StateSave, string> StateRename;
        public event Action<StateSaveCategory, string> CategoryRename;

        public event Action<VariableSave, List<Attribute>> FillVariableAttributes;
        public event Action<string, StateSave> AddAndRemoveVariablesForType;
        public event Func<VariableSave, RecursiveVariableFinder, bool> VariableExcluded;
        public event Action WireframeRefreshed;

        /// <summary>
        /// Event which is raised when an ElementSave's variable is set.
        /// ElementSave - current ElementSave (like the Screen)
        /// InstanceSave - current InstanceSave (like a sprite in a Screen). This may be null
        /// string - name of the variable set
        /// object - OLD value of the variable.  New value must be obtained through the InstanceSave
        /// </summary>
        public event Action<ElementSave, InstanceSave, string, object> VariableSet;
        public event Action<ElementSave> ElementSelected;
        public event Action<TreeNode> TreeNodeSelected;
        public event Action<TreeNode> StateWindowTreeNodeSelected;
        public event Action<BehaviorSave> BehaviorSelected;
        public event Action<ElementSave, InstanceSave> InstanceSelected;
        public event Action<ElementSave, InstanceSave> InstanceAdd;
        public event Action<ElementSave, InstanceSave> InstanceDelete;
        /// <summary>
        /// Event raised whenever an instance is renamed. Third parameter is the old name.
        /// </summary>
        public event Action<ElementSave, InstanceSave, string> InstanceRename;
        public event Action<InstanceSave> InstanceReordered;

        public event Action<ElementSave> BehaviorReferencesChanged;

        /// <summary>
        /// Method which allows a plugin to provide a default StateSave for a given type. This can be used
        /// to return a set of variables and their defaults for a completely custom StandardElementSave instead
        /// of relying on StandardElementsManager
        /// </summary>
        public event Func<string, StateSave> GetDefaultStateForType;


        public event Func<string, IRenderableIpso> CreateRenderableForType;

        #endregion

        public string UniqueId
        {
            get;
            set;
        }
        public MenuStrip MenuStrip { get; set; }

        public abstract string FriendlyName { get; }

        public abstract Version Version { get; }

        public abstract void StartUp();
        public abstract bool ShutDown(PluginShutDownReason shutDownReason);

        /// <summary>
        /// Adds a menu item using the path specified by the menuAndSubmenus. 
        /// </summary>
        /// <param name="menuAndSubmenus">The menu path. The first item may specify an existing menu to add to.
        /// For example, to add a Properties item to the existing Edit item, the following
        /// parameter could be used:
        /// new List<string> { "Edit", "Properties" }
        /// </param>
        /// <returns>The newly-created menu item.</returns>
        public ToolStripMenuItem AddMenuItem(IEnumerable<string> menuAndSubmenus)
        {
            string menuName = menuAndSubmenus.Last();

            ToolStripMenuItem menuItem = new ToolStripMenuItem(menuName);

            string menuNameToAddTo = menuAndSubmenus.First();

            var menuToAddTo =
                MenuStrip.Items.Cast<ToolStripMenuItem>().FirstOrDefault(
                    item=>item.Text == menuNameToAddTo);
                //true);

            if (menuToAddTo == null)
            {
                menuToAddTo = new ToolStripMenuItem(menuNameToAddTo);

                // Don't call Add - this will put the menu item after the "Help" menu item, which should be last
                //MenuStrip.Items.Add(menuToAddTo);

                int indexToInsertAt = MenuStrip.Items.Count - 1;
                MenuStrip.Items.Insert(indexToInsertAt, menuToAddTo);
            }


            menuToAddTo.DropDownItems.Add(menuItem);
            return menuItem;

        }

        public ToolStripMenuItem AddMenuItem(params string[] menuAndSubmenus)
        {
            return AddMenuItem((IEnumerable<string>)menuAndSubmenus);
        }

        #region Event calling

        public void CallProjectLoad(GumProjectSave newlyLoadedProject)
        {
            if (ProjectLoad != null)
            {
                ProjectLoad(newlyLoadedProject);
            }
        }

        public void CallProjectSave(GumProjectSave savedProject)
        {
            if (AfterProjectSave != null)
            {
                AfterProjectSave(savedProject);
            }
        }

        public void CallGuidesChanged()
        {
            if (GuidesChanged != null)
            {
                GuidesChanged();
            }
        }

        public void CallExport(ElementSave elementSave)
        {
            if (Export != null)
            {
                Export(elementSave);
            }
        }

        public void CallDeleteOptionsWindowShow(DeleteOptionsWindow optionsWindow, object objectToDelete)
        {
            if (DeleteOptionsWindowShow != null)
            {
                DeleteOptionsWindowShow(optionsWindow, objectToDelete);
            }
        }

        public void CallDeleteConfirm(DeleteOptionsWindow optionsWindow, object deletedObject)
        {
            if (DeleteConfirm != null)
            {
                DeleteConfirm(optionsWindow, deletedObject);
            }
        }

        public void CallElementAdd(ElementSave element)
        {
            ElementAdd?.Invoke(element);
        }

        public void CallElementDelete(ElementSave element)
        {
            ElementDelete?.Invoke(element);
        }

        public void CallElementRename(ElementSave elementSave, string oldName)
        {
            if (ElementRename != null)
            {
                ElementRename(elementSave, oldName);
            }
        }

        public void CallStateRename(StateSave stateSave, string oldName)
        {

            if (StateRename != null)
            {
                StateRename(stateSave, oldName);
            }
        }

        public void CallStateCategoryRename(StateSaveCategory category, string oldName)
        {
            if(CategoryRename != null)
            {
                CategoryRename(category, oldName);
            }
        }

        public void CallInstanceRename(ElementSave parentElement, InstanceSave instanceSave, string oldName)
        {
            InstanceRename?.Invoke(parentElement, instanceSave, oldName);
        }

        public void CallFillVariableAttributes(VariableSave variableSave, List<Attribute> listToFill)
        {
            if (FillVariableAttributes != null)
            {
                FillVariableAttributes(variableSave, listToFill);

            }
        }

        public void CallVariableSet(ElementSave parentElement, InstanceSave instance, string changedMember, object oldValue)
        {
            if (VariableSet != null)
            {
                VariableSet(parentElement, instance, changedMember, oldValue);
            }
        }

        public void CallAddAndRemoveVariablesForType(string type, StateSave standardDefaultStateSave)
        {
            if (AddAndRemoveVariablesForType != null)
            {
                AddAndRemoveVariablesForType(type, standardDefaultStateSave);
            }

        }

        public void CallElementSelected(ElementSave element)
        {
            if (ElementSelected != null)
            {
                ElementSelected(element);
            }
        }

        public void CallTreeNodeSelected(TreeNode treeNode)
        {
            TreeNodeSelected?.Invoke(treeNode);
        }

        public void CallStateWindowTreeNodeSelected(TreeNode treeNode)
        {
            StateWindowTreeNodeSelected?.Invoke(treeNode);
        }

        public void CallBehaviorSelected(BehaviorSave behavior)
        {
            BehaviorSelected?.Invoke(behavior);
        }

        public void CallInstanceSelected(ElementSave elementSave, InstanceSave instance)
        {
            if (InstanceSelected != null)
            {
                InstanceSelected(elementSave, instance);
            }
        }

        public void CallInstanceAdd(ElementSave elementSave, InstanceSave instance)
        {
            if (InstanceAdd != null)
            {
                InstanceAdd(elementSave, instance);
            }
        }

        public void CallBehaviorReferencesChanged(ElementSave element)
        {
            BehaviorReferencesChanged?.Invoke(element);
        }

        public void CallInstanceDelete(ElementSave elementSave, InstanceSave instance)
        {
            if (InstanceDelete != null)
            {
                InstanceDelete(elementSave, instance);
            }
        }

        public void CallInstanceReordered(InstanceSave instance)
        {
            InstanceReordered?.Invoke(instance);
        }

        public void CallBeforeElementSave(ElementSave elementSave)
        {
            if (BeforeElementSave != null)
            {
                BeforeElementSave(elementSave);
            }
        }

        public void CallAfterElementSave(ElementSave elementSave)
        {
            if (AfterElementSave != null)
            {
                AfterElementSave(elementSave);
            }
        }

        public void CallBeforeProjectSave(GumProjectSave savedProject)
        {
            BeforeProjectSave?.Invoke(savedProject);
        }

        public void CallWireframeRefreshed()
        {
            WireframeRefreshed?.Invoke();
        }

        public StateSave CallGetDefaultStateFor(string type)
        {
            return GetDefaultStateForType?.Invoke(type);
        }

        public IRenderableIpso CallCreateRenderableForType(string type)
        {
            return CreateRenderableForType?.Invoke(type);
        }

        internal bool GetIfVariableIsExcluded(VariableSave defaultVariable, RecursiveVariableFinder rvf)
        {
            if (VariableExcluded == null)
            {
                return false;
            }
            else
            {
                return VariableExcluded(defaultVariable, rvf);
            }
        }
        
        #endregion
    }
}
