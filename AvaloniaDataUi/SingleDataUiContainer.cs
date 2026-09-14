using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace AvaloniaDataUi;

/// <summary>
/// The Avalonia row host: creates (or reuses) the displayer for its <see cref="InstanceMember"/>
/// DataContext, choosing the control through the grid's <see cref="DisplayerRegistry"/>, applies
/// <see cref="InstanceMember.PropertiesToSetOnDisplayer"/>, and raises <see cref="InstanceMember.UiCreated"/>.
/// </summary>
public class SingleDataUiContainer : ContentControl
{
    private readonly DataUiGrid? _grid;
    private readonly DisplayerRegistry _registry;
    private InstanceMember? _member;

    /// <summary>Creates a row host owned by <paramref name="grid"/>.</summary>
    public SingleDataUiContainer(DataUiGrid grid) : this(grid.Displayers, grid)
    {
    }

    /// <summary>Creates a row host that resolves displayers through <paramref name="registry"/>.</summary>
    public SingleDataUiContainer(DisplayerRegistry registry, DataUiGrid? grid = null)
    {
        _registry = registry;
        _grid = grid;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
    }

    /// <summary>The displayer currently hosted, if any.</summary>
    public Control? Displayer { get; private set; }

    /// <summary>The member the displayer edits.</summary>
    public InstanceMember? Member => _member;

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_member != null)
        {
            _member.PropertyChanged -= HandleMemberPropertyChanged;
        }
        _member = DataContext as InstanceMember;

        if (_member == null)
        {
            Content = null;
            return;
        }

        _member.PropertyChanged += HandleMemberPropertyChanged;
        Displayer = CreateDisplayer(_member);
        Content = Displayer;
        RefreshToolTip();
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _grid?.RegisterContainer(this);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _grid?.UnregisterContainer(this);
    }

    private Control CreateDisplayer(InstanceMember member)
    {
        Type controlType = _registry.SelectControlType(member);

        Control control;
        // Reuse the existing control for an explicitly preferred displayer, keeping its focus.
        if (member.PreferredDisplayer != null && Displayer != null && Displayer.GetType() == controlType)
        {
            control = Displayer;
        }
        else
        {
            control = (Control)Activator.CreateInstance(controlType)!;
        }

        foreach (var kvp in member.PropertiesToSetOnDisplayer)
        {
            controlType.GetProperty(kvp.Key)?.SetValue(control, kvp.Value, null);
        }

        if (control is not IDataUi display)
        {
            throw new InvalidOperationException(
                $"The object of type {controlType} must implement the IDataUi interface to be used in a DataUiGrid");
        }

        display.InstanceMember = member;
        member.CallUiCreated(control);
        return control;
    }

    private void HandleMemberPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(InstanceMember.DetailText):
                (Displayer as IDataUi)?.Refresh();
                break;
            case nameof(InstanceMember.ToolTipText):
                RefreshToolTip();
                break;
        }
    }

    private void RefreshToolTip()
    {
        ToolTip.SetTip(this, string.IsNullOrEmpty(_member?.ToolTipText) ? null : _member.ToolTipText);
    }
}
