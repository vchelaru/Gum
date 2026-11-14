using System;
using System.Runtime.CompilerServices;
using Avalonia.Input;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Services;
using Gum.Avalonia.Themes;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.VariableGrid;
using WpfDataUi;

namespace Gum.Avalonia.Plugins.VariableGrid;

/// <summary>
/// The Avalonia head's Variables tab: <see cref="VariablesTabView"/> as the view, the standard
/// AvaloniaDataUi editors, and this head's editors for the <see cref="GumDisplayers"/> keys.
/// </summary>
public class AvaloniaVariableGridHead : IVariableGridHead
{
    // One key handler per reference editor, so a re-bound editor forwards to its current row only.
    private readonly ConditionalWeakTable<StringListTextBoxDisplay, EventHandler<KeyEventArgs>> _referenceHandlers;
    private readonly ConditionalWeakTable<StringListTextBoxDisplay, ReferenceSuggestionPopup> _suggestionPopups;
    private readonly IVariableReferenceSuggestions _referenceSuggestions;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly IKeyCombinationFormatter _keyFormatter;

    /// <summary>Creates the head with the standard and Gum displayers registered.</summary>
    public AvaloniaVariableGridHead(IHotkeyManager hotkeyManager, IKeyCombinationFormatter keyFormatter)
    {
        _hotkeyManager = hotkeyManager;
        _keyFormatter = keyFormatter;
        _referenceHandlers = new ConditionalWeakTable<StringListTextBoxDisplay, EventHandler<KeyEventArgs>>();
        _suggestionPopups = new ConditionalWeakTable<StringListTextBoxDisplay, ReferenceSuggestionPopup>();
        _referenceSuggestions = new VariableReferenceSuggestions();
        Displayers = DataUiGrid.CreateStandardRegistry();
        RegisterDisplayers(Displayers);
        // The same icon the category remove button uses.
        DataUiDisplayBase.DetailIconFactory = name =>
            name == "Delete" ? GumFluentIcons.Create(FluentIcons.Common.Icon.Delete, 12) : null;
    }

    /// <summary>The displayers the Variables tab's grids use.</summary>
    public DisplayerRegistry Displayers { get; }

    /// <inheritdoc/>
    public IVariablesTabView CreateVariablesTabView(MainControlViewModel viewModel)
    {
        return new VariablesTabView(Displayers, _keyFormatter.Format(_hotkeyManager.FocusVariableFilter)) { DataContext = viewModel };
    }

    /// <summary>Registers this head's editors for the Gum-specific displayer keys.</summary>
    public void RegisterDisplayers(DisplayerRegistry registry)
    {
        registry.Register(typeof(GumDisplayers.Color), typeof(ColorDisplay));
        registry.Register(typeof(GumDisplayers.FontRanges), typeof(Gum.Avalonia.Settings.FontRangesDisplay));
        registry.Register(typeof(GumDisplayers.CornerRadius), typeof(CornerRadiusDisplay));
        registry.Register(typeof(GumDisplayers.BorderScale), typeof(BorderScaleDisplay));
        registry.Register(typeof(GumDisplayers.ShaderVector), typeof(ShaderVectorDisplay));
        registry.Register(typeof(GumDisplayers.RemoveButton), typeof(VariableRemoveButton));
        registry.Register(typeof(GumDisplayers.TextOverflowVerticalMode), typeof(TextOverflowVerticalModeDisplay));
        registry.Register(typeof(GumDisplayers.TextOverflowHorizontalMode), typeof(TextOverflowHorizontalModeDisplay));
        registry.Register(typeof(GumDisplayers.ChildrenLayout), typeof(ChildrenLayoutDisplay));
        registry.Register(typeof(GumDisplayers.ColorOperation), typeof(ColorOperationDisplay));
        registry.Register(typeof(GumDisplayers.WidthUnits), typeof(WidthUnitsDisplay));
        registry.Register(typeof(GumDisplayers.HeightUnits), typeof(HeightUnitsDisplay));
        registry.Register(typeof(GumDisplayers.GradientRadiusUnits), typeof(GradientRadiusUnitsDisplay));
        registry.Register(typeof(GumDisplayers.GradientYUnits), typeof(GradientYUnitsDisplay));
        registry.Register(typeof(GumDisplayers.XUnits), typeof(XUnitsDisplay));
        registry.Register(typeof(GumDisplayers.YUnits), typeof(YUnitsDisplay));
        registry.Register(typeof(GumDisplayers.TextVerticalAlignment), typeof(TextVerticalAlignmentDisplay));
        registry.Register(typeof(GumDisplayers.YOrigin), typeof(YOriginDisplay));
        registry.Register(typeof(GumDisplayers.TextHorizontalAlignment), typeof(TextHorizontalAlignmentDisplay));
        registry.Register(typeof(GumDisplayers.XOrigin), typeof(XOriginDisplay));
    }

    /// <inheritdoc/>
    public void AttachReferenceSuggestions(object displayer, Func<Gum.DataTypes.ElementSave?> getOwner, Func<string?> getOwningInstanceName)
    {
        if (displayer is not StringListTextBoxDisplay editor)
        {
            return;
        }

        if (_suggestionPopups.TryGetValue(editor, out ReferenceSuggestionPopup? existing))
        {
            existing.SetOwner(getOwner, getOwningInstanceName);
            return;
        }
        _suggestionPopups.Add(editor, new ReferenceSuggestionPopup(editor, _referenceSuggestions, getOwner, getOwningInstanceName));
    }

    /// <summary>The suggestion list attached to <paramref name="editor"/>, or null; for tests.</summary>
    public ReferenceSuggestionPopup? GetSuggestionPopup(StringListTextBoxDisplay editor) =>
        _suggestionPopups.TryGetValue(editor, out ReferenceSuggestionPopup? popup) ? popup : null;

    /// <inheritdoc/>
    public void AttachReferenceTextEditor(object displayer, Action<GumKeyEventArgs, string> handleKeyDown)
    {
        if (displayer is not StringListTextBoxDisplay editor)
        {
            return;
        }

        if (_referenceHandlers.TryGetValue(editor, out EventHandler<KeyEventArgs>? previous))
        {
            editor.EditorTextBox.KeyDown -= previous;
            _referenceHandlers.Remove(editor);
        }

        EventHandler<KeyEventArgs> handler = (_, e) => handleKeyDown(e.ToGumKeyEventArgs(), editor.GetCurrentLineText());
        editor.EditorTextBox.KeyDown += handler;
        _referenceHandlers.Add(editor, handler);
    }
}
