using System;
using System.Collections.Generic;
using Gum.Wireframe;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif


#if !FRB
namespace Gum.Forms.Controls;

#endif

public class RadioButton : ToggleButton
{
    #region Fields/Properties

    /// <summary>
    /// No longer populated. Radio button groups are the radio buttons sharing a parent visual and
    /// <see cref="GroupName"/>, found by scanning the parent's children, so there is no registry to keep
    /// in sync or to leak a removed screen's visual tree.
    /// </summary>
    [Obsolete("Radio button groups are no longer tracked in a static dictionary, so this is always empty.")]
    public static Dictionary<object, Dictionary<string, List<RadioButton>>> RadioButtonDictionary = new Dictionary<object, Dictionary<string, List<RadioButton>>>();

    private string _groupName = "";

    /// <summary>
    /// The name of the group this radio button belongs to. Checking a radio button unchecks the other radio
    /// buttons with the same GroupName that share its parent.
    /// </summary>
    public string GroupName
    {
        get
        {
            return _groupName;
        }
        set
        {
            _groupName = value ?? "";
        }
    }

    /// <summary>
    /// No longer needed. Groups are no longer stored anywhere, so there is nothing to clear on screen clean-up.
    /// </summary>
    [Obsolete("Radio button groups are no longer tracked in a static dictionary, so there is nothing to clear.")]
    public static void ClearDictionary()
    {
    }

    #endregion

    #region Initialize Methods

    public RadioButton(string groupName = "") : base()
    {
        GroupName = groupName;
        IsChecked = false;
    }

    public RadioButton(InteractiveGue visual, string groupName = "") : base(visual)
    {
        GroupName = groupName;
        IsChecked = false;
    }

    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();

        UpdateState();
    }

    #endregion

    #region UpdateTo Methods
    private void SetThisAsOnlyCheckedInGroup()
    {
        GraphicalUiElement? parent = Visual?.Parent;

        // Don't set all RadioButtons to FALSE just because they don't have a parent yet!
        if (parent?.Children != null)
        {
            // Index rather than foreach: unchecking raises events whose handlers may change the children.
            for (int i = 0; i < parent.Children.Count; i++)
            {
                GraphicalUiElement sibling = parent.Children[i];
                if ((sibling as InteractiveGue)?.FormsControlAsObject is RadioButton radio
                    && radio != this
                    && radio.GroupName == GroupName)
                {
                    radio.IsChecked = false;
                }
            }
        }

        IsChecked = true;
    }

    public override void UpdateState()
    {
        if (Visual == null) //don't try to update the UI when the UI is not set yet, mmmmkay?
            return;

        const string category = "RadioButtonCategoryState";

        var state = GetDesiredStateWithChecked(IsChecked);

        Visual.SetProperty(category, state);
    }


    #endregion

    #region Event Handlers

    protected override void OnClick()
    {
        SetThisAsOnlyCheckedInGroup();
    }

    #endregion

    protected override void OnChecked()
    {
        base.OnChecked();

        // This will set IsChecked to true, but it's already set to true so that
        // won't repeat indefinitely.
        SetThisAsOnlyCheckedInGroup();
    }
}
