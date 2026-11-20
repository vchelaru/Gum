using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;

namespace Gum.ProjectServices.Editing;

// The rename operations. Each runs the data change the tool's rename runs (ProjectRenamer) and records the files
// that follow the renamed object in PendingFileChanges, which the caller commits after the project saves.
public partial class ProjectEditService
{
    private PendingFileChanges _pendingFiles = null!;
    private IProjectRenamer _renamer = null!;

    private void StartRenames(GumProjectSave project)
    {
        _pendingFiles = new PendingFileChanges(project);
        _renamer = new ProjectRenamer(_referenceFinder, _inheritanceSync, new RenameReferenceApplier(), _pendingFiles);
    }

    private static readonly string[] AnimationOps = { "addAnimation", "removeAnimation", "setAnimationLoops", "addKeyframe", "removeKeyframe" };
    private static readonly string[] SidecarRenameOps = { "renameElement", "renameInstance", "renameState", "renameCategory" };

    // Both edit the element's animation file in memory, and neither sees the other's copy or the file move, so an
    // animation op after a rename would look in the wrong file and one before it would be overwritten.
    private static string? GetWhyCannotMixRenamesAndAnimations(IEnumerable<string> ops)
    {
        List<string> all = ops.ToList();
        return all.Any(AnimationOps.Contains) && all.Any(SidecarRenameOps.Contains)
            ? "An operations file cannot mix animation operations with renameElement, renameInstance, renameState or renameCategory, which also change the animation file. Split it into two runs."
            : null;
    }

    private void RenameElement(GumProjectSave project, JsonElement operation)
    {
        string name = GetString(operation, "name");
        string newName = GetString(operation, "newName");
        ElementSave element = (ElementSave?)project.Screens.FirstOrDefault(item => item.Name == name)
            ?? project.Components.FirstOrDefault(item => item.Name == name)
            ?? throw (project.StandardElements.Any(item => item.Name == name)
                ? new EditException($"{name} is a standard element, which cannot be renamed.")
                : new EditException($"The project has no Screen or Component named {name}."));

        if (newName == name)
        {
            throw new EditException($"{name} is already named {newName}.");
        }
        if (string.Equals(newName, name, StringComparison.OrdinalIgnoreCase))
        {
            throw new EditException($"{newName} differs from {name} only in letter case, which file systems that ignore case cannot rename in one step. Rename it to a different name first.");
        }

        int slash = newName.LastIndexOf('/');
        string? folder = slash < 0 ? null : newName.Substring(0, slash);
        if (!_nameValidator.IsElementNameValid(newName.Substring(slash + 1), folder, element, out string? whyNotValid))
        {
            throw new EditException(whyNotValid);
        }

        _renamer.RenameElement(project, element, newName);
    }

    private void RenameInstance(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string name = GetString(operation, "name");
        string newName = GetString(operation, "newName");
        InstanceSave instance = element.GetInstance(name)
            ?? throw new EditException($"{element.Name} has no instance named {name}.");

        if (instance.DefinedByBase)
        {
            throw new EditException($"{name} is defined by {element.BaseType}. Rename it there and {element.Name} follows.");
        }
        if (newName == name)
        {
            throw new EditException($"{name} is already named {newName}.");
        }
        if (!_nameValidator.IsInstanceNameValid(newName, instance, element, out string? whyNotValid))
        {
            throw new EditException(whyNotValid);
        }

        _renamer.RenameInstance(element, instance, newName);
    }

    private void RenameState(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string name = GetString(operation, "name");
        string newName = GetString(operation, "newName");
        StateSaveCategory? category = GetOptionalString(operation, "category") is { } categoryName
            ? GetCategory(element, categoryName)
            : null;
        StateSave state = (category?.States ?? element.States).FirstOrDefault(item => item.Name == name)
            ?? throw new EditException(category == null
                ? $"{element.Name} has no uncategorized state named {name}."
                : $"{element.Name}'s category {category.Name} has no state named {name}.");

        if (state == element.DefaultState)
        {
            throw new EditException($"{name} is the default state of {element.Name}, which cannot be renamed.");
        }
        if (newName == name)
        {
            throw new EditException($"{name} is already named {newName}.");
        }
        if (!_nameValidator.IsStateNameValid(newName, category, state, out string? whyNotValid))
        {
            throw new EditException(whyNotValid);
        }

        _renamer.RenameState(element, category, state, newName);
    }

    private void RenameCategory(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string name = GetString(operation, "name");
        string newName = GetString(operation, "newName");
        StateSaveCategory category = GetCategory(element, name);

        if (newName == name)
        {
            throw new EditException($"{name} is already named {newName}.");
        }
        if (ProjectRenamer.GetWhyCategoryCannotBeRenamed(category, _stateRemoval.GetBehaviorsNeedingCategory(category, element as ComponentSave)) is { } whyNot)
        {
            throw new EditException(whyNot);
        }
        if (!_nameValidator.IsCategoryNameValid(newName, element, out string? whyNotValid, category))
        {
            throw new EditException(whyNotValid);
        }

        _renamer.RenameCategory(element, category, newName);
    }

    // A custom variable gets a new name everywhere it is set; any other variable can only have its exposed name
    // renamed, since its own name is the one its type defines.
    private void RenameVariable(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string name = GetString(operation, "name");
        string newName = GetString(operation, "newName");
        List<VariableSave> variables = element.GetDefaultStateOrThrow().Variables;

        VariableSave? custom = variables.FirstOrDefault(item => item.Name == name && item.IsCustomVariable);
        VariableSave? exposed = custom == null ? variables.FirstOrDefault(item => item.ExposedAsName == name) : null;
        VariableSave variable = custom ?? exposed
            ?? throw new EditException($"{element.Name} has no custom variable or exposed variable named {name}.");

        if (newName == name)
        {
            throw new EditException($"{name} is already named {newName}.");
        }
        if (!_nameValidator.IsVariableNameValid(newName, element, variable, out string? whyNotValid))
        {
            throw new EditException(whyNotValid);
        }

        _renamer.RenameVariable(element, variable, renameExposedName: custom == null, newName);
    }

    private void RenameBehavior(GumProjectSave project, JsonElement operation)
    {
        string name = GetString(operation, "name");
        string newName = GetString(operation, "newName");
        BehaviorSave behavior = project.Behaviors.FirstOrDefault(item => item.Name == name)
            ?? throw new EditException($"The project has no behavior named {name}.");

        if (StandardFormsBehaviorNames.All.Contains(name))
        {
            throw new EditException($"{name} is a standard Gum Forms behavior, which the Forms runtime finds by its exact name. Rename it in the Gum tool if you must.");
        }
        BehaviorReference? reference = project.BehaviorReferences.FirstOrDefault(item => item.Name == name);
        if (behavior.IsSourceFileMissing || !string.IsNullOrEmpty(reference?.SourcePath))
        {
            throw new EditException($"{name} is stored outside the project's Behaviors folder or its file is missing, so renaming it would move a file gumcli does not own.");
        }
        if (newName == name)
        {
            throw new EditException($"{name} is already named {newName}.");
        }
        if (!new NameRules().IsBehaviorNameValid(newName, behavior, project.Behaviors, out string? whyNotValid))
        {
            throw new EditException(whyNotValid);
        }

        _renamer.RenameBehavior(project, behavior, newName);
    }
}
