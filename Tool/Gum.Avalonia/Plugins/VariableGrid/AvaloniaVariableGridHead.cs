using System;
using System.Runtime.CompilerServices;
using Avalonia.Input;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Services;
using Gum.Input;
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

    /// <summary>Creates the head with the standard and Gum displayers registered.</summary>
    public AvaloniaVariableGridHead()
    {
        _referenceHandlers = new ConditionalWeakTable<StringListTextBoxDisplay, EventHandler<KeyEventArgs>>();
        Displayers = DataUiGrid.CreateStandardRegistry();
        RegisterDisplayers(Displayers);
    }

    /// <summary>The displayers the Variables tab's grids use.</summary>
    public DisplayerRegistry Displayers { get; }

    /// <inheritdoc/>
    public IVariablesTabView CreateVariablesTabView(MainControlViewModel viewModel)
    {
        return new VariablesTabView(Displayers) { DataContext = viewModel };
    }

    /// <summary>Registers this head's editors for the Gum-specific displayer keys.</summary>
    public void RegisterDisplayers(DisplayerRegistry registry)
    {
        registry.Register(typeof(GumDisplayers.Color), typeof(ColorDisplay));
        registry.Register(typeof(GumDisplayers.CornerRadius), typeof(CornerRadiusDisplay));
        registry.Register(typeof(GumDisplayers.RemoveButton), typeof(VariableRemoveButton));
        registry.Register(typeof(GumDisplayers.TextOverflowVerticalMode), typeof(TextOverflowVerticalModeDisplay));
        registry.Register(typeof(GumDisplayers.TextOverflowHorizontalMode), typeof(TextOverflowHorizontalModeDisplay));
        registry.Register(typeof(GumDisplayers.ChildrenLayout), typeof(ChildrenLayoutDisplay));
        registry.Register(typeof(GumDisplayers.WidthUnits), typeof(WidthUnitsDisplay));
        registry.Register(typeof(GumDisplayers.HeightUnits), typeof(HeightUnitsDisplay));
        registry.Register(typeof(GumDisplayers.XUnits), typeof(XUnitsDisplay));
        registry.Register(typeof(GumDisplayers.YUnits), typeof(YUnitsDisplay));
        registry.Register(typeof(GumDisplayers.TextVerticalAlignment), typeof(TextVerticalAlignmentDisplay));
        registry.Register(typeof(GumDisplayers.YOrigin), typeof(YOriginDisplay));
        registry.Register(typeof(GumDisplayers.TextHorizontalAlignment), typeof(TextHorizontalAlignmentDisplay));
        registry.Register(typeof(GumDisplayers.XOrigin), typeof(XOriginDisplay));
    }

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
