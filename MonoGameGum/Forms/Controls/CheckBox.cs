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
    #region Initialize Methods

    public CheckBox() : base() { }

#if !FRB
    /// <summary>
    /// Creates a new CheckBox using the default visual registered for <paramref name="service"/>, without
    /// having to make that service current first. The service that was current is restored afterward.
    /// </summary>
    public CheckBox(global::RenderingLibrary.IGumService service) : base(service) { }
#endif

    public CheckBox(InteractiveGue visual) : base(visual) { }

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
}
