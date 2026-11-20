using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Renames elements, instances, states, categories, variables and behaviors, and everything that names them: the
/// data change behind the tool's rename, run by <c>gumcli edit</c>. The renamed object's file follows it through
/// <see cref="PendingFileChanges"/>. Names are checked by the caller.
/// </summary>
public interface IProjectRenamer
{
    /// <summary>Renames a Screen or Component, which may move it to another folder.</summary>
    void RenameElement(GumProjectSave project, ElementSave element, string newName);

    /// <summary>Renames an instance in its container and in the elements that inherit it.</summary>
    void RenameInstance(ElementSave container, InstanceSave instance, string newName);

    /// <summary>Renames a state, and the variables on other elements that select it.</summary>
    void RenameState(ElementSave element, StateSaveCategory? category, StateSave state, string newName);

    /// <summary>Renames a category and the <c>{Name}State</c> variables of its type.</summary>
    void RenameCategory(ElementSave element, StateSaveCategory category, string newName);

    /// <summary>Renames a custom variable, or the exposed name of a variable.</summary>
    void RenameVariable(ElementSave element, VariableSave variable, bool renameExposedName, string newName);

    /// <summary>Renames a behavior and the references to it.</summary>
    void RenameBehavior(GumProjectSave project, BehaviorSave behavior, string newName);
}

/// <inheritdoc/>
public class ProjectRenamer : IProjectRenamer
{
    private readonly IReferenceFinder _referenceFinder;
    private readonly IInheritanceSync _inheritanceSync;
    private readonly IRenameReferenceApplier _applier;
    private readonly PendingFileChanges _files;

    public ProjectRenamer(IReferenceFinder referenceFinder, IInheritanceSync inheritanceSync, IRenameReferenceApplier applier, PendingFileChanges files)
    {
        _referenceFinder = referenceFinder;
        _inheritanceSync = inheritanceSync;
        _applier = applier;
        _files = files;
    }

    /// <summary>
    /// Why a category cannot be renamed, or null when it can: a behavior of the component defines a category of
    /// that name, and a component must keep the categories its behaviors require.
    /// </summary>
    public static string? GetWhyCategoryCannotBeRenamed(StateSaveCategory category, IEnumerable<BehaviorSave> behaviorsNeedingCategory)
    {
        List<string> names = behaviorsNeedingCategory.Select(item => item.Name).ToList();
        return names.Count == 0
            ? null
            : $"The category {category.Name} cannot be renamed because it is needed by the following behavior(s):\n" + string.Join("\n", names);
    }

    /// <inheritdoc/>
    public void RenameElement(GumProjectSave project, ElementSave element, string newName)
    {
        string oldName = element.Name;
        ElementReferences references = _referenceFinder.GetReferencesToElement(element, oldName);

        element.Name = newName;
        // The project's reference to the element, and the sort order that follows from the new name.
        project.ReactToRenamed(element, null, oldName);
        project.SortElementAndBehaviors();

        _applier.ApplyElementReferences(references, element, oldName, new HashSet<ElementSave>());

        if (element is ComponentSave)
        {
            FavoriteComponentList.Rename(project, oldName, newName);
        }
        _files.ElementRenamed(element, oldName);
    }

    /// <inheritdoc/>
    public void RenameInstance(ElementSave container, InstanceSave instance, string newName)
    {
        string oldName = instance.Name;
        instance.Name = newName;

        InstanceReferences references = _referenceFinder.GetReferencesToInstance(container, instance, oldName);

        foreach (StateSave state in container.AllStates)
        {
            _applier.RenameInstanceInState(state, oldName, newName);
        }
        _applier.RenameInstanceEventsAndChildContainer(container, instance, oldName);
        _applier.ApplyInstanceReferences(references, newName, oldName, new HashSet<ElementSave>());

        _files.InstanceRenamed(container, oldName, newName);
        foreach (ElementSave inheriting in _inheritanceSync.RenameInstance(container, instance, oldName))
        {
            _files.InstanceRenamed(inheriting, oldName, newName);
        }
    }

    /// <inheritdoc/>
    public void RenameState(ElementSave element, StateSaveCategory? category, StateSave state, string newName)
    {
        string oldName = state.Name;
        StateReferences references = _referenceFinder.GetReferencesToState(state, oldName, element, category);

        state.Name = newName;
        _applier.ApplyStateReferences(references, state, new HashSet<ElementSave>());

        _files.StateRenamed(element, category?.Name, oldName, newName);
    }

    /// <inheritdoc/>
    public void RenameCategory(ElementSave element, StateSaveCategory category, string newName)
    {
        string oldName = category.Name;
        CategoryReferences references = _referenceFinder.GetReferencesToStateCategory(element, category, oldName);

        _applier.ApplyCategoryReferences(references, element, category, oldName, newName, new HashSet<ElementSave>());

        _files.CategoryRenamed(element, oldName, newName);
    }

    /// <inheritdoc/>
    public void RenameVariable(ElementSave element, VariableSave variable, bool renameExposedName, string newName)
    {
        string oldName = renameExposedName ? variable.ExposedAsName! : variable.Name;
        // A full rename finds the references by the variable's own name; an exposed rename by the exposed one.
        VariableChangeResponse references = _referenceFinder.GetReferencesToVariable(element, variable.Name, oldName);

        if (renameExposedName)
        {
            // The tool's Edit Variable for an exposed variable: the variable keeps its name, and what set it
            // through the exposed name follows.
            variable.ExposedAsName = newName;
            _applier.ApplyVariableRenameChanges(references, oldName, newName, new HashSet<ElementSave>());
            return;
        }
        _applier.RenameVariable(element, oldName, newName, variable.Type, renameExposedName: false, references, new HashSet<ElementSave>());
    }

    /// <inheritdoc/>
    public void RenameBehavior(GumProjectSave project, BehaviorSave behavior, string newName)
    {
        string oldName = behavior.Name;

        BehaviorReferences references = _referenceFinder.GetReferencesToBehavior(behavior, oldName);
        foreach ((_, ElementBehaviorReference reference) in references.ElementsWithBehaviorReference)
        {
            reference.BehaviorName = newName;
        }

        BehaviorReference? projectReference = project.BehaviorReferences.FirstOrDefault(item => item.Name == oldName);
        if (projectReference != null)
        {
            projectReference.Name = newName;
        }

        behavior.Name = newName;
        _files.BehaviorRenamed(behavior, oldName);
    }
}
