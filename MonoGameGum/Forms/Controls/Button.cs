using Gum.Wireframe;
using System;

#if FRB
using FlatRedBall.Forms.Controls.Primitives;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif


#if !FRB
using Gum.Forms.Controls.Primitives;
namespace Gum.Forms.Controls;
#endif

public class Button : ButtonBase
{
    /// <summary>
    /// The name of the Category containing visual states for the Button object.
    /// </summary>
    public const string ButtonCategoryName = "ButtonCategory";

    #region Fields/Properties

    GraphicalUiElement? textComponent;

    /// <summary>
    /// Set on the thumb of a Slider or ScrollBar. A drag handle keeps its Pushed state until the
    /// primary button is released, even if the cursor is dragged off it.
    /// </summary>
    internal bool IsDragHandle { get; set; }

    internal override bool KeepsPushedStateWhenCursorLeaves => IsDragHandle;

    global::RenderingLibrary.Graphics.IText? coreTextObject;

    /// <summary>
    /// Text displayed by the button. This property requires that the TextInstance instance be present in the Gum component.
    /// If the TextInstance instance is not present, an exception will be thrown in DEBUG mode.
    /// Setting this property applies localization if a <see cref="Gum.Localization.LocalizationService"/> is registered.
    /// To bypass localization, use <see cref="SetTextNoTranslate"/>.
    /// </summary>
    public virtual string? Text
    {
        get
        {
#if FULL_DIAGNOSTICS
            ReportMissingTextInstance();
#endif
            return coreTextObject?.RawText;
        }
        set
        {
#if FULL_DIAGNOSTICS
            ReportMissingTextInstance();
#endif
            // go through the component instead of the core text object to force a layout refresh if necessary
            textComponent?.SetProperty("Text", value);
        }
    }

    /// <summary>
    /// Sets the button text without applying localization/translation.
    /// </summary>
    /// <remarks>
    /// This is a method rather than a property because the "no translate" state is not preserved on
    /// the underlying text renderable — only the final string is stored.
    /// Use this for text that should not be localized.
    /// </remarks>
    public void SetTextNoTranslate(string? value)
    {
        textComponent?.SetProperty("TextNoTranslate", value);
    }


    #endregion

    #region Initialize Methods

    public Button() : base() { }

    public Button(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        RefreshInternalVisualReferences();
        base.ReactToVisualChanged();
    }

    protected override void RefreshInternalVisualReferences()
    {
        // text component is optional:
        textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");
        coreTextObject = textComponent?.RenderableComponent as global::RenderingLibrary.Graphics.IText;
    }


    #endregion

    #region UpdateTo Methods

    public override void UpdateState()
    {
        var state = base.GetDesiredState();

        Visual.SetProperty(ButtonCategoryName + "State", state);
    }

    // Manual save/restore rather than RegisterRuntimeProperty because
    // ReactToVisualChanged can fire multiple times (visual reassignment),
    // which would accumulate duplicate registrations.
    private string? _savedText;
    private bool _hasSavedText;

    /// <inheritdoc/>
    public override void SaveRuntimeProperties()
    {
        _savedText = coreTextObject?.RawText;
        _hasSavedText = coreTextObject != null;
        base.SaveRuntimeProperties();
    }

    /// <inheritdoc/>
    public override void ApplyRuntimeProperties()
    {
        if (_hasSavedText && textComponent != null)
        {
            textComponent.SetProperty("Text", _savedText);
        }
        _savedText = null;
        _hasSavedText = false;
        base.ApplyRuntimeProperties();
    }

    #endregion

    #region Utilities

#if FULL_DIAGNOSTICS
    private void ReportMissingTextInstance()
    {
        if (textComponent == null)
        {
            throw new Exception(
                $"This button was created with a Gum component ({Visual?.ElementSave}) " +
                "that does not have an instance called 'TextInstance'. " +
                "A 'TextInstance' instance must be added to modify the button's Text property.");
        }
    }
#endif

    #endregion
}

/// <summary>
/// The draggable thumb of a <see cref="Slider"/> or <see cref="ScrollBar"/>. It behaves exactly like a
/// <see cref="Button"/> and looks like one by default: until a template is registered for
/// <c>typeof(Thumb)</c> in <see cref="FrameworkElement.DefaultFormsTemplates"/>, a thumb uses the
/// Button template. Registering one restyles thumbs without restyling every button.
/// </summary>
public class Thumb : Button
{
    public Thumb() : base() { }

    public Thumb(InteractiveGue visual) : base(visual) { }
}

/// <summary>
/// The up or down arrow button of a <see cref="ScrollBar"/>. It behaves exactly like a
/// <see cref="Button"/> and looks like one by default: until a template is registered for
/// <c>typeof(ScrollBarButton)</c> in <see cref="FrameworkElement.DefaultFormsTemplates"/>, an arrow uses
/// the Button template. Registering one restyles the arrows without restyling every button.
/// </summary>
public class ScrollBarButton : Button
{
    public ScrollBarButton() : base() { }

    public ScrollBarButton(InteractiveGue visual) : base(visual) { }
}
