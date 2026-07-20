using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary;
using System.Collections.Generic;
using System.Linq;

namespace Gum.ToolStates;

/// <summary>
/// An in-memory, non-singleton <see cref="ISelectedState"/> implementation used to describe a
/// forced/scoped selection (e.g. a paste target) without touching the real app-wide
/// <c>SelectedState</c> singleton, which would have side effects app-wide. Public (rather than the
/// original internal) because its only two consumers — the real <c>SelectedState</c> class and
/// <c>DragDropManager</c> — now live in different assemblies (Gum.csproj and Gum.Presentation,
/// respectively).
/// </summary>
public class SelectedStateSnapshot : ISelectedState
{
    public ScreenSave? SelectedScreen
    {
        get => SelectedElement as ScreenSave;
        set => SelectedElement = value;
    }
    public ElementSave? SelectedElement
    {
        get => selectedElements.FirstOrDefault();
        set
        {
            selectedElements.Clear();
            if(value != null)
            {
                selectedElements.Add(value);
            }
        }
    }

    List<ElementSave> selectedElements = new List<ElementSave>();
    public IEnumerable<ElementSave> SelectedElements
    {
        get => selectedElements;
        set
        {
            selectedElements.Clear();
            if(value?.Count() > 0)
            {
                selectedElements.AddRange(value);
            }
        }
    }

    public IStateContainer SelectedStateContainer
    {
        get => (IStateContainer)SelectedElement ?? SelectedBehavior;
        set
        {
            if (value is ElementSave elementSave)
            {
                SelectedElement = elementSave;
            }
            else if (value is BehaviorSave behaviorSave)
            {
                SelectedBehavior = behaviorSave;
            }
            else
            {
                SelectedElement = null;
                SelectedBehavior = null;
            }
        }
    }

    public BehaviorSave? SelectedBehavior
    {
        get => selectedBehaviors.FirstOrDefault();
        set
        {
            selectedBehaviors.Clear();
            if (value != null)
            {
                selectedBehaviors.Add(value);
            }
        }
    }

    List<BehaviorSave> selectedBehaviors = new List<BehaviorSave>();
    public IEnumerable<BehaviorSave> SelectedBehaviors
    {
        get => selectedBehaviors;
        set
        {
            selectedBehaviors.Clear();
            if (value?.Count() > 0)
            {
                selectedBehaviors.AddRange(value);
            }

            var behavirs = value == null ? "null" : value.Count().ToString();

            System.Diagnostics.Debug.WriteLine($"Selected {behavirs} behaviors");
        }
    }


    public ElementBehaviorReference? SelectedBehaviorReference { get; set; }

    public StateSave? CustomCurrentStateSave { get; set; }
    public StateSave? SelectedStateSave { get; set; }

    public StateSave? SelectedStateSaveOrDefault { get; set; }

    public StateSaveCategory? SelectedStateCategorySave { get; set; }
    public ComponentSave SelectedComponent
    {
        get => SelectedElement as ComponentSave;
        set
        {
            SelectedElement = value;
        }
    }
    //public InstanceSave SelectedInstance { get; set; }
    public InstanceSave? SelectedInstance
    {
        get => SelectedInstances.FirstOrDefault();
        set
        {
            selectedInstances.Clear();
            if(value != null)
            {
                selectedInstances.Add(value);
            }
        }
    }
    public IPositionedSizedObject? SelectedIpso { get; }

    List<InstanceSave> selectedInstances = new List<InstanceSave>();
    public IEnumerable<InstanceSave> SelectedInstances
    {
        get => selectedInstances;
        set
        {
            selectedInstances.Clear();
            if (value?.Count() > 0)
            {
                selectedInstances.AddRange(value);
            }
        }
    }

    public string? SelectedVariableName { get; set; }

    public StandardElementSave? SelectedStandardElement
    {
        get => SelectedElement as StandardElementSave;
        set
        {
            SelectedElement = value;
        }
    }
    public VariableSave? SelectedVariableSave { get; set; }
    public VariableSave? SelectedBehaviorVariable { get; set; }

    public ITreeNode? SelectedTreeNode { get; set; }

    public IEnumerable<ITreeNode> SelectedTreeNodes { get; set; }

}
