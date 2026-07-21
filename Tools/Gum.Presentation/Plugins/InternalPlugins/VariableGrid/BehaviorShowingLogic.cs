using Gum.Commands;
using Gum.DataTypes.Behaviors;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Plugins.VariableGrid;

/// <summary>
/// Relocated from Gum.csproj into headless Gum.Presentation (ADR-0005 Phase 3, part of #3860):
/// builds the Behavior data grid's "Behavior Properties" category. Converted from a static class
/// resolving its dependencies via <c>Locator</c> to an ordinary constructor-injected instance class
/// (both dependencies - <see cref="IFileCommands"/>/<see cref="IProjectState"/> - already live in
/// Gum.Presentation). Returns <see cref="SyntheticCategoryDescriptor"/> (wrapping
/// <see cref="SyntheticVariableRow"/>) instead of WpfDataUi's <c>MemberCategory</c>/
/// <c>InstanceMember</c> - a WPF-side mapper materializes the real WPF types from these descriptors.
/// </summary>
public class BehaviorShowingLogic
{
    private readonly IFileCommands _fileCommands;
    private readonly IProjectState _projectState;

    public BehaviorShowingLogic(IFileCommands fileCommands, IProjectState projectState)
    {
        _fileCommands = fileCommands;
        _projectState = projectState;
    }

    public List<SyntheticCategoryDescriptor> GetCategoriesFor(BehaviorSave behavior)
    {
        var category = new SyntheticCategoryDescriptor("Behavior Properties");

        var componentsImplementingBehavior = _projectState.GumProjectSave!.Components
            .Where(item => item.Behaviors.Any(behaviorSave => behaviorSave.BehaviorName == behavior.Name));

        var options = componentsImplementingBehavior
            .Select(item => (object)item.Name).ToList();
        options.Insert(0, null!);

        category.Members.Add(new SyntheticVariableRow
        {
            Name = nameof(BehaviorSave.DefaultImplementation),
            ValueType = typeof(string),
            Get = () => behavior.DefaultImplementation,
            Set = (newValue) =>
            {
                behavior.DefaultImplementation = (string)newValue!;
                _fileCommands.TryAutoSaveBehavior(behavior);
            },
            DetailText = "Code generation is required for this to work at runtime",
            CustomOptions = options
        });

        return new List<SyntheticCategoryDescriptor> { category };
    }
}
