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

    private GraphicalUiElement textComponent;

    private global::RenderingLibrary.Graphics.IText coreTextObject;

    //<radio button parent, <group name, radio button list>>
    public static Dictionary<object, Dictionary<string, List<RadioButton>>> RadioButtonDictionary = new Dictionary<object, Dictionary<string, List<RadioButton>>>();

    private static readonly object FakeRoot = new object();    //will act as fake root to enable root level radio buttons to be added to the dictionary

    private string _groupName;

    private object GetParent()
    {
        object parent;
        if (Visual == null)
            parent = null;
        else if (Visual.Parent != null)
            parent = Visual.Parent;
        else
            parent = FakeRoot;

        return parent;
    }

    public string GroupName
    {
        get
        {
            return _groupName;
        }
        set
        {
            RemoveFromDictionary();
            _groupName = value;
            AddToDictionary();
        }
    }

    private void RemoveFromDictionary()
    {
        var parent = GetParent();

        if (parent == null) //parent will be null when visual is null. groupname dictionary updates are meaningless until we have set the scope for the radio button!
            return;

        if (RadioButtonDictionary.ContainsKey(parent)
            && GroupName != null
            && RadioButtonDictionary[parent].ContainsKey(GroupName)
            && RadioButtonDictionary[parent][GroupName].Contains(this))
        {
            RadioButtonDictionary[parent][GroupName].Remove(this);
        }
    }

    //Only use this on screen clean-up!!
    public static void ClearDictionary()
    {
        foreach (var parent in RadioButtonDictionary)
        {
            foreach (var child in parent.Value)
            {
                child.Value.Clear();
            }
            parent.Value.Clear();
        }
        RadioButtonDictionary.Clear();
    }

    private void AddToDictionary()
    {
        // early out
        if (Visual == null)
        {
            return;
        }
        // end early out

        var parent = GetParent();

        if (RadioButtonDictionary.ContainsKey(parent) == false)
            RadioButtonDictionary.Add(parent, new Dictionary<string, List<RadioButton>>());

        if (RadioButtonDictionary[parent].ContainsKey(GroupName) == false)
            RadioButtonDictionary[parent].Add(GroupName, new List<RadioButton>());

        RadioButtonDictionary[parent][GroupName].Add(this);
    }



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
#if DEBUG
            ReportMissingTextInstance();
#endif
            // go through the component instead of the core text object to force a layout refresh if necessary
            textComponent.SetProperty("Text", value);
        }
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

    /*
    This method will assign the group of the radio box according to the parent of the assigned Visual. 
    This method assumes that the visual is already attached to its parent
    and that the parent will not change after this method has been called. If these assumptions cause problems in the future we may have to revisit this
     */
    protected override void ReactToVisualChanged()
    {
        RefreshInternalVisualReferences();

        base.ReactToVisualChanged();

        Visual.ParentChanged += HandleParentChanged;

        UpdateState();
    }

    protected override void RefreshInternalVisualReferences()
    {
        // text component is optional:
        textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");

        if (textComponent != null)
            coreTextObject = (global::RenderingLibrary.Graphics.IText)textComponent.RenderableComponent;

        if (GroupName == null)
        {
            GroupName = ""; //this will force the dictionary to be updated for the current <group name, visual> pair
        }
        else
        {
            GroupName = GroupName; //this will force the dictionary to be updated for the current <group name, visual> pair
        }
    }
    #endregion

    #region UpdateTo Methods
    private void SetThisAsOnlyCheckedInGroup()
    {
        var parent = GetParent();

        // Don't set all RadioButtons to FALSE just because they don't have a parent yet!
        if (parent != FakeRoot)
        {
            foreach (var radio in RadioButtonDictionary[parent][GroupName])
            {
                if (radio != this)
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

    private void HandleParentChanged(object sender, GraphicalUiElement.ParentChangedEventArgs e)
    {
        // setting GroupName refreshes grouping
        GroupName = GroupName;
    }

    protected override void OnClick()
    {
        SetThisAsOnlyCheckedInGroup();
    }

    #endregion

    #region Utilities

#if DEBUG
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

    protected override void OnChecked()
    {
        base.OnChecked();

        // This will set IsChecked to true, but it's already set to true so that
        // won't repeat indefinitely.
        SetThisAsOnlyCheckedInGroup();
    }
}
