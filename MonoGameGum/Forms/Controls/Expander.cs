using Gum.Wireframe;
using System;

namespace Gum.Forms.Controls;

/// <summary>
/// A control that has a header and a collapsible content area.
/// Clicking the header toggles the content visibility.
/// Follows the WPF Expander pattern but inherits from FrameworkElement
/// since Gum does not have a HeaderedContentControl base.
/// </summary>
public class Expander : FrameworkElement
{
    #region Fields/Properties

    public const string ExpanderCategoryName = "ExpanderCategoryState";

    private GraphicalUiElement? headerContainer;
    private GraphicalUiElement? contentContainer;
    private GraphicalUiElement? textComponent;
    private global::RenderingLibrary.Graphics.IText? coreTextObject;

    private bool isExpanded;

    /// <summary>
    /// Gets or sets whether the content area is visible.
    /// </summary>
    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            if (isExpanded != value)
            {
                isExpanded = value;

                if (contentContainer != null)
                {
                    contentContainer.Visible = isExpanded;
                }

                if (isExpanded)
                {
                    Expanded?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Collapsed?.Invoke(this, EventArgs.Empty);
                }

                UpdateState();
                PushValueToViewModel();
            }
        }
    }

    /// <summary>
    /// Gets or sets the header text. Setting this property applies localization
    /// if a localization service is registered.
    /// </summary>
    public string? Header
    {
        get => coreTextObject?.RawText;
        set
        {
            _codeText.Record(value, translate: true);
            textComponent?.SetProperty("Text", value);
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when IsExpanded changes to true.
    /// </summary>
    public event EventHandler? Expanded;

    /// <summary>
    /// Raised when IsExpanded changes to false.
    /// </summary>
    public event EventHandler? Collapsed;

    #endregion

    #region Initialize

    public Expander() : base() { }

    public Expander(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        RefreshInternalVisualReferences();

        if (headerContainer is InteractiveGue interactiveHeader)
        {
            interactiveHeader.Click += HandleHeaderClick;
        }

        base.ReactToVisualChanged();

        UpdateState();
    }

    /// <inheritdoc/>
    protected override void ReactToVisualRemoved()
    {
        base.ReactToVisualRemoved();

        if (headerContainer is InteractiveGue interactiveHeader)
        {
            interactiveHeader.Click -= HandleHeaderClick;
        }
    }

    protected override void RefreshInternalVisualReferences()
    {
        // The new visual's text is authoritative; text set on the previous visual does not carry over.
        _codeText.Clear();

        headerContainer = Visual.GetGraphicalUiElementByName("HeaderContainer");
        contentContainer = Visual.GetGraphicalUiElementByName("ContentContainer");
        textComponent = Visual.GetGraphicalUiElementByName("TextInstance");
        coreTextObject = textComponent?.RenderableComponent as global::RenderingLibrary.Graphics.IText;
    }

    #endregion

    #region Event Handlers

    private void HandleHeaderClick(object? sender, EventArgs args)
    {
        IsExpanded = !IsExpanded;
    }

    #endregion

    #region UpdateTo Methods

    // Manual restore rather than RegisterRuntimeProperty because
    // ReactToVisualChanged can fire multiple times (visual reassignment),
    // which would accumulate duplicate registrations. Only text assigned through
    // the property is restored; text the Visual authored is re-applied from the
    // Visual's state instead.
    private CodeAssignedText _codeText;

    /// <inheritdoc/>
    public override void ApplyRuntimeProperties()
    {
        _codeText.Restore(textComponent);
        base.ApplyRuntimeProperties();
    }

    public override void UpdateState()
    {
        if (Visual == null)
            return;

        var state = IsExpanded ? "Expanded" : "Collapsed";

        if (!IsEnabled)
        {
            state = "Disabled" + state;
        }

        Visual.SetProperty(ExpanderCategoryName, state);

        if (contentContainer != null)
        {
            contentContainer.Visible = isExpanded;
        }
    }

    #endregion

    /// <summary>
    /// Adds a child element to the expander's content area.
    /// </summary>
    public void AddContent(FrameworkElement child)
    {
        if (contentContainer != null)
        {
            contentContainer.Children.Add(child.Visual);
        }
    }

    public void AddContent(GraphicalUiElement child)
    {
        if (contentContainer != null)
        {
            contentContainer.Children.Add(child);
        }

    }
}
