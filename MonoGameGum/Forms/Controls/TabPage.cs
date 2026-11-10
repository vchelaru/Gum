using Gum.Wireframe;

namespace Gum.Forms.Controls;

/// <summary>
/// The content of one tab in a <see cref="TabControl"/>, paired with its <see cref="TabItem"/> header
/// by position. The page's own Selected/Unselected state shows or hides it.
/// </summary>
public class TabPage : FrameworkElement
{
    /// <summary>The state category this control sets: Selected or Unselected.</summary>
    public const string TabPageCategoryName = "TabPageCategory";

    bool _isSelected;

    /// <summary>Whether this page belongs to the selected tab. Set by the owning TabControl.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        internal set
        {
            if (value != _isSelected)
            {
                _isSelected = value;
                UpdateState();
            }
        }
    }

    /// <summary>
    /// The container that content is added to: the visual's child named InnerPanelInstance, or the
    /// visual itself when it has none.
    /// </summary>
    public GraphicalUiElement ContentContainer =>
        Visual.GetGraphicalUiElementByName("InnerPanelInstance") ?? Visual;

    public TabPage() : base() { }

    public TabPage(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        UpdateState();
        base.ReactToVisualChanged();
    }

    public override void UpdateState()
    {
        if (Visual == null)
        {
            return;
        }
        Visual.SetProperty(TabPageCategoryName + "State",
            IsSelected ? SelectedStateName : TabItem.UnselectedStateName);
    }
}
