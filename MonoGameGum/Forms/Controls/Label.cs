using Gum.Wireframe;
using System;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif


#if !FRB
namespace Gum.Forms.Controls;
#endif

public class Label :
#if FRB
    FrameworkElement
#else
    Gum.Forms.Controls.FrameworkElement
#endif
{
    protected GraphicalUiElement textComponent;
    protected global::RenderingLibrary.Graphics.IText coreTextObject;

    public GraphicalUiElement TextComponent => textComponent;

    /// <summary>
    /// Gets or sets the label text. Setting this property applies localization
    /// if a <see cref="Gum.Localization.LocalizationService"/> is registered.
    /// To bypass localization, use <see cref="SetTextNoTranslate"/>.
    /// </summary>
    public string? Text
    {
        get
        {
#if FULL_DIAGNOSTICS
            ReportMissingTextInstance();
#endif
            return coreTextObject.RawText;
        }
        set
        {
            if(value != Text)
            {

#if FULL_DIAGNOSTICS
                ReportMissingTextInstance();
#endif
                // go through the component instead of the core text object to force a layout refresh if necessary
                textComponent.SetProperty("Text", value);

                PushValueToViewModel();
            }


        }
    }

    /// <summary>
    /// Sets the label text without applying localization/translation.
    /// </summary>
    /// <remarks>
    /// This is a method rather than a property because the "no translate" state is not preserved on
    /// the underlying text renderable — only the final string is stored.
    /// Use this for text that should not be localized.
    /// </remarks>
    public void SetTextNoTranslate(string? value)
    {
        if (value != Text)
        {
            textComponent.SetProperty("TextNoTranslate", value);
            PushValueToViewModel();
        }
    }

    public Label() : base() { }

    public Label(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        RefreshInternalVisualReferences();
        base.ReactToVisualChanged();
    }

    protected override void RefreshInternalVisualReferences()
    {
        if (base.Visual?.Name == "TextInstance")
        {
            textComponent = base.Visual;
        }
        else if(base.Visual?.RenderableComponent is global::RenderingLibrary.Graphics.IText)
        {
            textComponent = base.Visual;
        }
        else
        {
            textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");
        }

#if FULL_DIAGNOSTICS
        // If we do this, we prevent Label controls from instantiating and later setting their visuals...
        //ReportMissingTextInstance();
#endif

        coreTextObject = (global::RenderingLibrary.Graphics.IText)textComponent?.RenderableComponent;
    }

#if FULL_DIAGNOSTICS
    private void ReportMissingTextInstance()
    {
        if(Visual == null)
        {
            throw new Exception("Cannot set the Text on this label because it doesn't have a Visual assigned"); 
        }
        if (textComponent == null)
        {
            throw new Exception(
                $"This label was created with a Gum component ({Visual?.ElementSave}) " +
                "that does not have an instance called 'TextInstance'. A 'TextInstance' instance must be added to modify the button's Text property.");
        }
    }
#endif
}
