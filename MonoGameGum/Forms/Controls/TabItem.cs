using Gum.Forms.Controls.Primitives;
using Gum.Wireframe;
using System;

namespace Gum.Forms.Controls;

/// <summary>
/// The header of one tab in a <see cref="TabControl"/>. The tab's content is a separate
/// <see cref="TabPage"/> that the TabControl pairs with this header by position.
/// </summary>
public class TabItem : FrameworkElement, ISelectableRow
{
    /// <summary>The state category this control sets: Selected or Unselected.</summary>
    internal const string TabItemCategoryName = "TabItemCategory";

    /// <summary>The state name used when the tab is not the selected one.</summary>
    internal const string UnselectedStateName = "Unselected";

    GraphicalUiElement? _text;

    bool _isSelected;

    /// <summary>
    /// Whether this tab is the TabControl's selected tab. Setting it to true selects this tab.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (value != _isSelected)
            {
                _isSelected = value;
                UpdateState();
                IsSelectedChangedInternal?.Invoke(this);
                if (value)
                {
                    Selected?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    object? _header;

    /// <summary>
    /// The value shown in the tab's header text. A data item's header is its ToString unless a
    /// VisualTemplate is set.
    /// </summary>
    public object? Header
    {
        get => _header;
        set
        {
            _header = value;
            UpdateHeaderText();
        }
    }

    /// <summary>
    /// The content shown in this tab's page when the tab is added to a TabControl directly. A
    /// FrameworkElement or GraphicalUiElement is placed inside the tab's page; a TabPage is used as
    /// the page itself. Set it before adding the tab to <see cref="ItemsControl.Items"/>.
    /// </summary>
    public object? Content { get; set; }

    /// <summary>The page paired with this tab, or null until the tab is in a TabControl.</summary>
    internal TabPage? Page { get; set; }

    /// <summary>
    /// The object in the owning TabControl's Items that this tab represents: the data item, or the
    /// tab itself when the tab was added directly.
    /// </summary>
    internal object? DataObject { get; set; }

    bool ISelectableRow.IsSelected
    {
        get => IsSelected;
        set => IsSelected = value;
    }

    bool ISelectableRow.IsEnabled => IsEnabled;

    bool ISelectableRow.IsFocused => IsFocused;

    object? ISelectableRow.DataObject => DataObject;

    // Lets the owning TabControl follow a tab selected by click or by code.
    internal event Action<TabItem>? IsSelectedChangedInternal;

    /// <summary>Raised when the tab becomes the selected tab.</summary>
    public event EventHandler? Selected;

    /// <summary>Raised when the tab is pushed by the cursor.</summary>
    public event EventHandler? Pushed;

    public TabItem() : base() { }

    public TabItem(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        Visual.Push += HandlePush;
        RefreshInternalVisualReferences();
        UpdateHeaderText();
        UpdateState();
        base.ReactToVisualChanged();
    }

    /// <inheritdoc/>
    protected override void ReactToVisualRemoved()
    {
        base.ReactToVisualRemoved();
        Visual.Push -= HandlePush;
    }

    protected override void RefreshInternalVisualReferences()
    {
        _text = Visual.GetGraphicalUiElementByName("TextInstance");
    }

    private void HandlePush(object? sender, EventArgs args)
    {
        if (IsEnabled)
        {
            IsSelected = true;
        }
        Pushed?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateHeaderText()
    {
        if (_text?.RenderableComponent is global::RenderingLibrary.Graphics.IText coreText && _header != null)
        {
            coreText.RawText = _header.ToString();
        }
    }

    public override void UpdateState()
    {
        if (Visual == null)
        {
            return;
        }
        Visual.SetProperty(TabItemCategoryName + "State", IsSelected ? SelectedStateName : UnselectedStateName);
    }

    public override string ToString() => _header?.ToString() ?? nameof(TabItem);
}
