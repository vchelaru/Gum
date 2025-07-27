using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using System.ComponentModel.Composition;
using System.Linq;

namespace Gum.Plugins.ParentPlugin
{
    [Export(typeof(PluginBase))]
    public class MainParentPlugin : InternalPlugin
    {
        private readonly ISelectedState _selectedState;

        public MainParentPlugin()
        {
            _selectedState = Locator.GetRequiredService<ISelectedState>();
        }
        
        public override void StartUp()
        {
            this.VariableSet += HandleVariableSet;
        }

        private void HandleVariableSet(ElementSave container, InstanceSave instance, string variableName, object oldValue)
        {
            ///////////////////////Early Out//////////////////
            if (variableName != "Parent" || instance == null)
            {
                return;
            }
            /////////////////////End Early Out////////////////

            var currentState = _selectedState.SelectedStateSave ?? 
                // This can happen if the user drag+drops one item on another without anything selected:
                container.DefaultState;
            var newParentName = currentState.GetValueOrDefault<string>($"{instance.Name}.Parent");
            InstanceSave newParent = null;
            if (!string.IsNullOrEmpty(newParentName))
            {
                newParent = container.GetInstance(newParentName);
            }

            if (newParent != null)
            {
                var typeRestriction = currentState.GetValueOrDefault<string>($"{newParent.Name}.ContainedType");

                if (!string.IsNullOrEmpty(typeRestriction))
                {
                    // this is allowed only if the child inherits from this type
                    var doTypesMatchExactly = typeRestriction == instance.BaseType;
                    var doTypesMatchConsideringInheritance = false;
                    if (!doTypesMatchExactly)
                    {
                        var element = ObjectFinder.Self.GetElementSave(typeRestriction);
                        if (element != null)
                        {
                            var typesInheritingFromRestriction = ObjectFinder.Self.GetElementsInheritingFrom(element);

                            doTypesMatchConsideringInheritance = typesInheritingFromRestriction.Any(item => item.Name == typeRestriction);
                        }
                    }

                    var shouldRevert = doTypesMatchExactly == false && doTypesMatchConsideringInheritance == false;

                    if(shouldRevert)
                    {
                        // This container can't support this value
                        currentState.SetValue($"{instance.Name}.Parent", oldValue, "string");

                        _guiCommands.PrintOutput(
                            $"The instance {newParent.Name} has a type restriction of {typeRestriction} so {instance.Name} cannot be added as a child.");
                    }
                }
            }
        }
    }
}
