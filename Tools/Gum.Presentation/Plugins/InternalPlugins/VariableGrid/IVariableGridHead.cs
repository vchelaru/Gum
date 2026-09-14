using System;
using Gum.Input;
using Gum.Plugins.VariableGrid;
using WpfDataUi;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// The Variables tab's view as <see cref="Gum.Managers.PropertyGridManager"/> drives it. Each head
/// builds one over its own grid controls; the filter box, add-variable button, and behavior-variable
/// list bind to the <see cref="MainControlViewModel"/> it is created with.
/// </summary>
public interface IVariablesTabView
{
    /// <summary>The control to put in the Variables tab.</summary>
    object Control { get; }

    /// <summary>The grid of the selected object's variables.</summary>
    IDataUiGrid VariablesGrid { get; }

    /// <summary>The grid of a selected behavior's own properties.</summary>
    IDataUiGrid BehaviorGrid { get; }

    /// <summary>Raised when the user clicks Add Variable.</summary>
    event EventHandler? AddVariableClicked;

    /// <summary>Raised when the user picks a different behavior variable in the list.</summary>
    event EventHandler? SelectedBehaviorVariableChanged;

    /// <summary>Puts the caret in the filter box with its text selected (the Ctrl+E hotkey).</summary>
    void FocusVariableFilter();
}

/// <summary>
/// What the Variables tab needs from a head: its view, the controls it uses for the Gum-specific
/// displayer keys (<see cref="GumDisplayers"/>), and key events from the variable-reference editor.
/// Registered by each head; listed in <c>GumCoreServiceCollectionExtensions.HeadProvidedContracts</c>.
/// </summary>
public interface IVariableGridHead
{
    /// <summary>
    /// Creates the Variables tab view bound to <paramref name="viewModel"/>. Called once, by
    /// <see cref="Gum.Managers.PropertyGridManager.InitializeEarly"/>; the head also registers its
    /// controls for <see cref="GumDisplayers"/> here, before any row is shown.
    /// </summary>
    IVariablesTabView CreateVariablesTabView(MainControlViewModel viewModel);

    /// <summary>
    /// Forwards key presses in a variable-reference editor to <paramref name="handleKeyDown"/> with
    /// the text of the line under the caret, when <paramref name="displayer"/> is this head's
    /// string-list editor. Other displayers are ignored.
    /// </summary>
    void AttachReferenceTextEditor(object displayer, Action<GumKeyEventArgs, string> handleKeyDown);
}
