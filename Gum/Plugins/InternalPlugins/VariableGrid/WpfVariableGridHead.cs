using System;
using Gum.Controls;
using Gum.Controls.DataUi;
using Gum.Input;
using Gum.Plugins.VariableGrid;
using WpfDataUi;
using WpfDataUi.Controls;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// The WPF head's Variables tab: <see cref="Gum.MainPropertyGrid"/> as the view, and the Gum WPF
/// controls behind each <see cref="GumDisplayers"/> key.
/// </summary>
public class WpfVariableGridHead : IVariableGridHead
{
    /// <inheritdoc/>
    public IVariablesTabView CreateVariablesTabView(MainControlViewModel viewModel)
    {
        RegisterDisplayers(SingleDataUiContainer.DisplayerRegistry);

        MainPropertyGrid view = new MainPropertyGrid();
        view.DataContext = viewModel;
        return view;
    }

    /// <summary>Registers the WPF controls for the Gum-specific displayer keys.</summary>
    public void RegisterDisplayers(DisplayerRegistry registry)
    {
        registry.Register(typeof(GumDisplayers.Color), typeof(ColorDisplay));
        registry.Register(typeof(GumDisplayers.CornerRadius), typeof(CornerRadiusDisplay));
        registry.Register(typeof(GumDisplayers.RemoveButton), typeof(VariableRemoveButton));
        registry.Register(typeof(GumDisplayers.TextOverflowVerticalMode), typeof(TextOverflowVerticalModeControl));
        registry.Register(typeof(GumDisplayers.TextOverflowHorizontalMode), typeof(TextOverflowHorizontalModeControl));
        registry.Register(typeof(GumDisplayers.ChildrenLayout), typeof(ChildrenLayoutControl));
        registry.Register(typeof(GumDisplayers.WidthUnits), typeof(WidthUnitsControl));
        registry.Register(typeof(GumDisplayers.HeightUnits), typeof(HeightUnitsControl));
        registry.Register(typeof(GumDisplayers.XUnits), typeof(XUnitsControl));
        registry.Register(typeof(GumDisplayers.YUnits), typeof(YUnitsControl));
        registry.Register(typeof(GumDisplayers.TextVerticalAlignment), typeof(TextVerticalAlignmentControl));
        registry.Register(typeof(GumDisplayers.YOrigin), typeof(YOriginControl));
        registry.Register(typeof(GumDisplayers.TextHorizontalAlignment), typeof(TextHorizontalAlignmentControl));
        registry.Register(typeof(GumDisplayers.XOrigin), typeof(XOriginControl));
    }

    /// <inheritdoc/>
    public void AttachReferenceTextEditor(object displayer, Action<GumKeyEventArgs, string> handleKeyDown)
    {
        if (displayer is StringListTextBoxDisplay asTextBox)
        {
            asTextBox.KeyDown += (_, e) => handleKeyDown(e.ToGumKeyEventArgs(), asTextBox.GetCurrentLineText());
        }
    }
}
