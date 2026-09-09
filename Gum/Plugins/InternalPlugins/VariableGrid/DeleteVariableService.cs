using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

public class DeleteVariableService : IDeleteVariableService
{
    private readonly IUndoManager _undoManager;
    private readonly IFileCommands _fileCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly IRenameLogic _renameLogic;
    private readonly IDialogService _dialogService;
    private readonly IPluginManager _pluginManager;

    public DeleteVariableService(IUndoManager undoManager,
        IFileCommands fileCommands,
        IGuiCommands guiCommands,
        IRenameLogic renameLogic,
        IDialogService dialogService,
        IPluginManager pluginManager)
    {
        _undoManager = undoManager;
        _fileCommands = fileCommands;
        _guiCommands = guiCommands;
        _renameLogic = renameLogic;
        _dialogService = dialogService;
        _pluginManager = pluginManager;
    }

    public bool CanDeleteVariable(VariableSave variable)
    {
        return variable.IsCustomVariable;
    }

    public void DeleteVariable(VariableSave variable, IStateContainer stateContainer)
    {
        var response = GetIfCanDeleteVariable(variable, stateContainer, out var cascadingInstanceOverrides);

        if(response.Succeeded == false)
        {
            _dialogService.ShowMessage(response.Message);
            return;
        }

        var elementSave = stateContainer as ElementSave;
        var crossElementRemovals = new List<CrossElementVariableChange>();

        using (_undoManager.RequestLock())
        {
            if(elementSave != null)
            {
                elementSave.DefaultState.Variables.Remove(variable);
                _fileCommands.TryAutoSaveElement(elementSave);
            }
            else if(stateContainer is BehaviorSave behavior)
            {
                behavior.RequiredVariables.Variables.Remove(variable);
                _fileCommands.TryAutoSaveObject(behavior);
            }

            // Instance-level value overrides elsewhere in the project are cascaded (removed, and
            // recorded so undo/redo can restore/re-remove them). See ADR 0016.
            foreach (var change in cascadingInstanceOverrides)
            {
                if (change.Container is ElementSave changeElement &&
                    changeElement.GetInstance(change.Variable.SourceObject) is { } instance)
                {
                    change.State.Variables.Remove(change.Variable);
                    _fileCommands.TryAutoSaveElement(changeElement);
                    _pluginManager.VariableSet(changeElement, instance, change.Variable.GetRootName(), null);

                    crossElementRemovals.Add(new CrossElementVariableChange
                    {
                        Container = changeElement,
                        Instance = instance,
                        State = change.State,
                        Variable = change.Variable
                    });
                }
            }
        }

        if (crossElementRemovals.Count > 0)
        {
            _undoManager.AttachCrossElementVariableRemovals(crossElementRemovals);
        }

        _guiCommands.RefreshVariables(force: true);

        _pluginManager.VariableDelete(elementSave, variable.Name);
    }

    /// <summary>
    /// Plain instance-level value overrides on other elements are cascaded rather than blocking -
    /// they're returned via <paramref name="cascadingInstanceOverrides"/> for the caller to remove and
    /// record for undo. Anything else that reuses the same rename-impact lookup - a VariableReferences
    /// binding, or an inheriting element's own exposed-name entry - still blocks: those aren't a simple
    /// "restore this value" case, so silently deleting through them would leave a dangling reference or
    /// destroy an unrelated exposed-variable declaration. See ADR 0016.
    ///
    /// The discriminator is <see cref="VariableSave.ExposedAsName"/>, not <see cref="VariableSave.SourceObject"/>:
    /// an exposed variable's own <c>Name</c> is itself instance-qualified (e.g. "InnerButton.X" exposed
    /// as "Variable1"), so it has a non-null SourceObject too - SourceObject alone can't tell a plain
    /// override apart from an exposed-name entry. Confirmed by
    /// ReferenceFinderTests.GetReferencesToVariable_ExposedNameOnInheritingElement_IsDetected_AndHasNonNullSourceObject.
    /// </summary>
    private GeneralResponse GetIfCanDeleteVariable(VariableSave variable, IStateContainer stateContainer,
        out List<VariableChange> cascadingInstanceOverrides)
    {
        cascadingInstanceOverrides = new List<VariableChange>();

        var isVariableContained = false;

        if (stateContainer is ElementSave elementSave)
        {
            isVariableContained =
                elementSave.DefaultState.Variables.Contains(variable);
        }
        else if (stateContainer is BehaviorSave behaviorSave)
        {
            isVariableContained = behaviorSave.RequiredVariables.Variables.Contains(variable);
        }

        if(!isVariableContained)
        {
            return GeneralResponse.UnsuccessfulWith($"The variable {variable} is not contained in {stateContainer}");
        }

        var renames = _renameLogic.GetChangesForRenamedVariable(stateContainer, variable.Name, variable.GetRootName());

        var blockingChanges = renames.VariableChanges.Where(c => !string.IsNullOrEmpty(c.Variable.ExposedAsName)).ToList();

        if (blockingChanges.Count > 0 || renames.VariableReferenceChanges.Count > 0)
        {
            var blockingResponse = new VariableChangeResponse();
            blockingResponse.VariableChanges.AddRange(blockingChanges);
            blockingResponse.VariableReferenceChanges.AddRange(renames.VariableReferenceChanges);

            return GeneralResponse.UnsuccessfulWith(
                $"Cannot delete variable {variable.Name} because it is referenced by other elements.\n\n{blockingResponse.GetChangesDetails()}");
        }

        cascadingInstanceOverrides = renames.VariableChanges.Where(c => string.IsNullOrEmpty(c.Variable.ExposedAsName)).ToList();

        return GeneralResponse.SuccessfulResponse;
    }
}