using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using System.Linq;

namespace Gum.Logic;

public class InheritanceLogic
{
    private readonly IFileCommands _fileCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly StandardElementsManagerGumTool _standardElementsManagerGumTool;

    public InheritanceLogic(IFileCommands fileCommands, IGuiCommands guiCommands,
        StandardElementsManagerGumTool standardElementsManagerGumTool)
    {
        _fileCommands = fileCommands;
        _guiCommands = guiCommands;
        _standardElementsManagerGumTool = standardElementsManagerGumTool;
    }

    public void HandleInstanceAdded(ElementSave container, InstanceSave instance)
    {
        var elementsInheritingFromContainer =
            ObjectFinder.Self.GetElementsInheritingFrom(container);

        foreach (var inheritingElement in elementsInheritingFromContainer)
        {
            var existingInInheriting = inheritingElement.GetInstance(instance.Name);
            if (existingInInheriting == null)
            {
                var clone = instance.Clone();
                clone.DefinedByBase = true;
                clone.ParentContainer = inheritingElement;
                inheritingElement.Instances.Add(clone);

                var directBase = ObjectFinder.Self.GetElementSave(inheritingElement.BaseType);

                AdjustInstance(directBase, inheritingElement, clone.Name);

                _fileCommands.TryAutoSaveElement(inheritingElement);
                _guiCommands.RefreshElementTreeView(inheritingElement);
            }
        }
    }

    public void HandleInstanceDeleted(ElementSave? container, InstanceSave instance)
    {
        if (container == null) return;

        var elementsInheritingFromContainer =
            ObjectFinder.Self.GetElementsInheritingFrom(container);

        foreach (var inheritingElement in elementsInheritingFromContainer)
        {
            inheritingElement.Instances.RemoveAll(item => item.Name == instance.Name);

            _fileCommands.TryAutoSaveElement(inheritingElement);
            _guiCommands.RefreshElementTreeView(inheritingElement);
        }
    }

    public void HandleInstanceRenamed(ElementSave? container, InstanceSave instance, string oldName)
    {
        if (container == null) return;

        var elementsInheritingFromContainer =
            ObjectFinder.Self.GetElementsInheritingFrom(container);

        foreach (var inheritingElement in elementsInheritingFromContainer)
        {
            var toRename = inheritingElement.GetInstance(oldName);

            if (toRename != null)
            {
                toRename.Name = instance.Name;
                _fileCommands.TryAutoSaveElement(inheritingElement);
                _guiCommands.RefreshElementTreeView(inheritingElement);
            }
        }
    }

    public void HandleInstanceReordered(InstanceSave instance)
    {
        var baseElement = instance.ParentContainer;

        if (baseElement != null)
        {
            var elementsInheritingFromContainer =
                ObjectFinder.Self.GetElementsInheritingFrom(baseElement);

            foreach (var inheritingElement in elementsInheritingFromContainer)
            {
                var didShiftIndex =
                    AdjustInstance(baseElement, inheritingElement, instance.Name);

                if (didShiftIndex)
                {
                    _fileCommands.TryAutoSaveElement(inheritingElement);
                    _guiCommands.RefreshElementTreeView(inheritingElement);
                }
            }
        }
    }

    public void HandleInstanceVariableBaseTypeSet(ElementSave container, InstanceSave instance)
    {
        var elementsInheritingFromContainer =
            ObjectFinder.Self.GetElementsInheritingFrom(container);

        foreach (var inheritingElement in elementsInheritingFromContainer)
        {
            var toChange = inheritingElement.GetInstance(instance.Name);

            if (toChange != null)
            {
                toChange.BaseType = instance.BaseType;
                _fileCommands.TryAutoSaveElement(inheritingElement);
            }
        }
    }

    public void HandleElementBaseType(ElementSave asElementSave)
    {
        string newValue = asElementSave.BaseType;

        asElementSave.Instances.RemoveAll(item => item.DefinedByBase);

        if (!string.IsNullOrEmpty(newValue))
        {
            if (StandardElementsManager.Self.IsDefaultType(newValue))
            {
                // Intentionally empty - see comment in original plugin code.
            }
            else
            {
                var baseElement = ObjectFinder.Self.GetElementSave(asElementSave.BaseType);

                StateSave stateSave = new StateSave();
                if (baseElement != null)
                {
                    foreach (var instance in baseElement.Instances)
                    {
                        var instanceName = instance.Name;
                        var alreadyExists = asElementSave.Instances.FirstOrDefault(item => item.Name == instanceName);
                        if (alreadyExists != null)
                        {
                            alreadyExists.DefinedByBase = true;
                        }
                        else
                        {
                            var derivedInstance = instance.Clone();
                            derivedInstance.DefinedByBase = true;
                            asElementSave.Instances.Add(derivedInstance);
                        }
                    }
                    asElementSave.Initialize(stateSave);
                    _standardElementsManagerGumTool.FixCustomTypeConverters(asElementSave);
                }
            }
        }

        const bool fullRefresh = true;
        _guiCommands.RefreshElementTreeView(asElementSave);
        _guiCommands.RefreshVariables(fullRefresh);
        _guiCommands.RefreshStateTreeView();
    }

    private bool AdjustInstance(ElementSave baseElement, ElementSave derivedElement, string instanceName)
    {
        var instanceInBase = baseElement.GetInstance(instanceName);
        var instanceInDerived = derivedElement.GetInstance(instanceName);

        var indexInBase = baseElement.Instances.IndexOf(instanceInBase);
        string? nameOfObjectBefore = null;
        if (indexInBase > 0)
        {
            nameOfObjectBefore = baseElement.Instances[indexInBase - 1].Name;
        }

        string? nameOfObjectAfter = null;
        if (indexInBase < baseElement.Instances.Count - 1)
        {
            nameOfObjectAfter = baseElement.Instances[indexInBase + 1].Name;
        }

        int exclusiveLowerIndexInDerived = -1;
        if (nameOfObjectBefore != null)
        {
            var instanceBefore = derivedElement.GetInstance(nameOfObjectBefore);
            exclusiveLowerIndexInDerived = derivedElement.Instances.IndexOf(instanceBefore);
        }

        int exclusiveUpperIndexInDerived = derivedElement.Instances.Count;

        if (nameOfObjectAfter != null)
        {
            var instanceAfter = derivedElement.GetInstance(nameOfObjectAfter);
            exclusiveUpperIndexInDerived = derivedElement.Instances.IndexOf(instanceAfter);
        }

        int currentDerivedIndex = derivedElement.Instances.IndexOf(instanceInDerived);

        var desiredIndex = System.Math.Min(currentDerivedIndex, exclusiveUpperIndexInDerived - 1);
        desiredIndex = System.Math.Max(desiredIndex, exclusiveLowerIndexInDerived + 1);

        bool didAdjust = false;
        if (desiredIndex != currentDerivedIndex)
        {
            didAdjust = true;
            derivedElement.Instances.Remove(instanceInDerived);
            if (currentDerivedIndex < desiredIndex)
            {
                derivedElement.Instances.Insert(desiredIndex - 1, instanceInDerived);
            }
            else
            {
                derivedElement.Instances.Insert(desiredIndex, instanceInDerived);
            }
        }

        return didAdjust;
    }
}
