using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using System.ComponentModel.Composition;

namespace Gum.Plugins.Inheritance;

[Export(typeof(PluginBase))]
public class MainInheritancePlugin : InternalPlugin
{
    private readonly InheritanceLogic _inheritanceLogic;

    public MainInheritancePlugin()
    {
        _inheritanceLogic = Locator.GetRequiredService<InheritanceLogic>();
    }

    public override void StartUp()
    {
        this.InstanceAdd += HandleInstanceAdded;
        this.InstanceDelete += HandleInstanceDeleted;
        this.InstanceRename += HandleInstanceRenamed;
        this.InstanceReordered += HandleInstanceReordered;
        this.VariableSet += HandleVariableSet;
    }

    private void HandleInstanceAdded(ElementSave container, InstanceSave instance) =>
        _inheritanceLogic.HandleInstanceAdded(container, instance);

    private void HandleInstanceDeleted(ElementSave container, InstanceSave instance) =>
        _inheritanceLogic.HandleInstanceDeleted(container, instance);

    private void HandleInstanceRenamed(ElementSave container, InstanceSave instance, string oldName) =>
        _inheritanceLogic.HandleInstanceRenamed(container, instance, oldName);

    private void HandleInstanceReordered(InstanceSave instance) =>
        _inheritanceLogic.HandleInstanceReordered(instance);

    private void HandleVariableSet(ElementSave container, InstanceSave? instance,
        string variableName, object? oldValue)
    {
        if (variableName == "BaseType" && container != null)
        {
            if (instance != null)
            {
                _inheritanceLogic.HandleInstanceVariableBaseTypeSet(container, instance);
            }
            else
            {
                _inheritanceLogic.HandleElementBaseType(container);
            }
        }
    }
}
