using Gum.Wireframe;
using System;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif


#if !FRB
namespace Gum.Forms.Controls;
#endif

public class Label : FrameworkElement
{
    protected GraphicalUiElement textComponent;
    protected global::RenderingLibrary.Graphics.IText coreTextObject;

    public GraphicalUiElement TextComponent => textComponent;

    public string Text
    {
        get
        {
#if DEBUG
            ReportMissingTextInstance();
#endif
            return coreTextObject.RawText;
        }
        set
        {
            if(value != Text)
            {

#if DEBUG
                ReportMissingTextInstance();
#endif
                // go through the component instead of the core text object to force a layout refresh if necessary
                textComponent.SetProperty("Text", value);

                PushValueToViewModel();
            }


        }
    }

    public Label() : base() { }

    public Label(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        RefreshInternalVisualReferences();
        base.ReactToVisualChanged();
    }

    protected virtual void RefreshInternalVisualReferences()
    {
        if (base.Visual?.Name == "TextInstance")
        {
            textComponent = base.Visual;
        }
        else
        {
            textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");
        }

#if DEBUG
        // If we do this, we prevent Label controls from instantiating and later setting their visuals...
        //ReportMissingTextInstance();
#endif

        coreTextObject = (global::RenderingLibrary.Graphics.IText)textComponent?.RenderableComponent;
    }

#if DEBUG
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
