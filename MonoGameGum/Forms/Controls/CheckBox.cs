using Gum.Wireframe;
using System;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif


#if !FRB
namespace Gum.Forms.Controls;

#endif

public class CheckBox : ToggleButton
{
    #region Fields/Properties

    private GraphicalUiElement? textComponent;

    private global::RenderingLibrary.Graphics.IText? coreTextObject;

    public string? Text
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


    #endregion

    #region Initialize Methods

    public CheckBox() : base() { }

    public CheckBox(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        RefreshInternalVisualReferences();

        base.ReactToVisualChanged();

        // In case the check is visible - the checkbox starts in a IsChecked = false state:
        UpdateState();
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
        if (Visual == null) //don't try to update the UI when the UI is not set yet, mmmmkay?
            return;

        const string category = "CheckBoxCategoryState";

        var state = GetDesiredStateWithChecked(IsChecked);

        Visual.SetProperty(category, state);
    }

    #endregion

    #region Utilities

#if FULL_DIAGNOSTICS
    private void ReportMissingTextInstance()
    {
        if (textComponent == null)
        {
            throw new Exception(
                "This button was created with a Gum component that does not have an instance called 'TextInstance'. A 'TextInstance' instance must be added to modify the radio button's Text property.");
        }
    }
#endif

    #endregion



}
