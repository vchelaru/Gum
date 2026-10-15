using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Messages;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.Undo;
using Gum.Wireframe;
using GumRuntime;
using Gum.Services;
using System;
using System.Linq;
using System.Globalization;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <inheritdoc/>
public class VariableReferenceConflictFixer : IVariableReferenceConflictFixer
{
    private readonly IDialogService _dialogService;
    private readonly IUndoManager _undoManager;
    private readonly IPluginManager _pluginManager;
    private readonly IVariableReferenceLogic _variableReferenceLogic;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IMessenger _messenger;

    public VariableReferenceConflictFixer(
        IDialogService dialogService,
        IUndoManager undoManager,
        IPluginManager pluginManager,
        IVariableReferenceLogic variableReferenceLogic,
        IWireframeObjectManager wireframeObjectManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IMessenger messenger)
    {
        _dialogService = dialogService;
        _undoManager = undoManager;
        _pluginManager = pluginManager;
        _variableReferenceLogic = variableReferenceLogic;
        _wireframeObjectManager = wireframeObjectManager;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _messenger = messenger;
    }

    /// <inheritdoc/>
    public void Fix(VariableReferenceConflict conflict)
    {
        ElementSave element = conflict.Element;
        StateSave state = conflict.State;
        InstanceSave? instance = conflict.InstanceName == null ? null : element.GetInstance(conflict.InstanceName);

        ///////////////////Early Out///////////////////
        if (!element.AllStates.Contains(state) || (conflict.InstanceName != null && instance == null))
        {
            // An undo or a delete replaced what the row described.
            return;
        }
        /////////////////End Early Out/////////////////

        string oldText = FormatValue(conflict.OldValue);
        string newText = FormatValue(conflict.NewValue);
        string message =
            $"{state.Name}: \"{conflict.VariableName}\" is {oldText}, but its reference sets it to {newText}.\n\n" +
            $"{oldText} becomes {newText}. The reference stays.";
        if (!_dialogService.ShowYesNoMessage(message, "Apply Reference?"))
        {
            return;
        }

        string unqualifiedName = conflict.InstanceName == null
            ? conflict.VariableName
            : conflict.VariableName.Substring(conflict.InstanceName.Length + 1);

        // Requested before the change so the history entry covers it, and held through the plugin
        // reaction so what plugins change in response undoes with it.
        using UndoLock undoLock = _undoManager.RequestLock(element);

        object? oldValue = state.GetValue(conflict.VariableName);
        if (conflict.ReferenceLine != null)
        {
            // The same per-row step ApplyVariableReferences runs, limited to the row the user confirmed.
            ElementSaveExtensions.ApplyVariableReferencesOnSpecificOwner(
                instance, conflict.ReferenceLine, state, _wireframeObjectManager.GetRepresentation(element));
        }
        else
        {
            // The reference lives on another element's state, which this state never re-applies.
            state.SetValue(conflict.VariableName, conflict.NewValue, instance);
        }

        _pluginManager.VariableSet(element, instance, unqualifiedName, oldValue);
        _variableReferenceLogic.ApplyReferencesToElement(element, trySave: true);
        _guiCommands.RefreshVariableValues();
        _fileCommands.TryAutoSaveElement(element);
        _messenger.Send(new RequestErrorRefreshMessage());
    }

    private static string FormatValue(object? value) =>
        Convert.ToString(value, CultureInfo.InvariantCulture) ?? "(null)";
}
