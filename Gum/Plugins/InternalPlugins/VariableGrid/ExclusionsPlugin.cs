using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using System.ComponentModel.Composition;

namespace Gum.Plugins.InternalPlugins.VariableGrid
{
    [Export(typeof(PluginBase))]
    public class ExclusionsPlugin : InternalPlugin
    {
        public override void StartUp()
        {
            this.VariableExcluded += HandleGetIfVariableIsExcluded;
            this.VariableSet += HandleVariableSet;
        }

        private void HandleVariableSet(ElementSave save1, InstanceSave save2, string variableName, object oldValue)
        {
            if(variableName == "Children Layout")
            {
                // Changing children layout can result in different values being shown in the property grid
                GumCommands.Self.GuiCommands.RefreshPropertyGrid(force:true);
            }
        }

        private bool HandleGetIfVariableIsExcluded(VariableSave variable, RecursiveVariableFinder finder)
        {

            if(variable.Name == "AutoGridHorizontalCells" || variable.Name == "AutoGridVerticalCells")
            {
                var childrenLayoutVariable = finder.GetVariable("Children Layout");

                var isAuto = false;
                if(childrenLayoutVariable?.Value is ChildrenLayout childrenLayout)
                { 
                    isAuto = childrenLayout == ChildrenLayout.AutoGridHorizontal || childrenLayout == ChildrenLayout.AutoGridVertical;
                }
                return !isAuto;
            }
            else if(variable.Name == "StackSpacing" || variable.Name == "Wraps Children")
            {
                var childrenLayoutVariable = finder.GetVariable("Children Layout");
                var isStack = false;
                if(childrenLayoutVariable?.Value is ChildrenLayout childrenLayout)
                {
                    isStack = childrenLayout == ChildrenLayout.LeftToRightStack || childrenLayout == ChildrenLayout.TopToBottomStack;
                }
                return !isStack;
            }


            return false;
        }
    }
}
