using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.Gui.Forms;
using Gum.DataTypes.Variables;
using System.Windows.Forms;

namespace Gum.Plugins.BaseClasses
{
    public abstract class PluginBase : IPlugin
    {
        #region Events

        public event Action<GumProjectSave> ProjectLoad;
        public event Action<GumProjectSave> ProjectSave;

        public event Action GuidesChanged;
        public event Action<ElementSave> Export;
        public event Action<DeleteOptionsWindow, object> DeleteOptionsWindowShow;
        public event Action<DeleteOptionsWindow, object> DeleteConfirm;
        public event Action<ElementSave, string> ElementRename;
        public event Action<VariableSave, List<Attribute>> FillVariableAttributes;
        public event Action<string, StateSave> AddAndRemoveVariablesForType;
        public event Func<VariableSave, RecursiveVariableFinder, bool> VariableExcluded;


        /// <summary>
        /// Event which is raised when an ElementSave's variable is set.
        /// ElementSave is the current ElementSave (like the Screen)
        /// string is the name of the variable set
        /// object is the OLD value of the variable.  New value must be obtained through the InstanceSave
        /// </summary>
        public event Action<ElementSave, InstanceSave, string, object> VariableSet;
        public event Action<ElementSave> ElementSelected;
        public event Action<ElementSave, InstanceSave> InstanceSelected;
        public event Action<ElementSave, InstanceSave> InstanceAdd;
        public event Action<ElementSave, InstanceSave> InstanceDelete;

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
                MenuStrip.Items.Add(menuToAddTo);
            }


            menuToAddTo.DropDownItems.Add(menuItem);
            return menuItem;

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
            if (ProjectSave != null)
            {
                ProjectSave(savedProject);
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

        public void CallElementRename(ElementSave elementSave, string oldName)
        {
            if (ElementRename != null)
            {
                ElementRename(elementSave, oldName);
            }
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

        public void CallInstanceDelete(ElementSave elementSave, InstanceSave instance)
        {
            if (InstanceDelete != null)
            {
                InstanceDelete(elementSave, instance);
            }
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
