using Gum.Wireframe;
using System;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#else
namespace MonoGameGum.Forms.Controls;
#endif

public class Label : FrameworkElement
{
    protected GraphicalUiElement textComponent;
    protected RenderingLibrary.Graphics.Text coreTextObject;

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
        textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");
        coreTextObject = textComponent.RenderableComponent as RenderingLibrary.Graphics.Text;
        base.ReactToVisualChanged();
    }

#if DEBUG
    private void ReportMissingTextInstance()
    {
        if (textComponent == null)
        {
            throw new Exception(
                $"This label was created with a Gum component ({Visual?.ElementSave}) " +
                "that does not have an instance called 'text'. A 'text' instance must be added to modify the button's Text property.");
        }
    }
#endif
}
