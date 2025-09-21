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

    GraphicalUiElement textComponent;

    global::RenderingLibrary.Graphics.IText coreTextObject;

    /// <summary>
    /// Text displayed by the button. This property requires that the TextInstance instance be present in the Gum component.
    /// If the TextInstance instance is not present, an exception will be thrown in DEBUG mode
    /// </summary>
    public virtual string Text
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
#if DEBUG
            ReportMissingTextInstance();
#endif
            // go through the component instead of the core text object to force a layout refresh if necessary
            textComponent?.SetProperty("Text", value);
        }
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

    #endregion

    #region Utilities

#if DEBUG
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
