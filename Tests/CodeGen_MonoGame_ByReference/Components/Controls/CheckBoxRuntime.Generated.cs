//Code for Controls/CheckBox (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGame_ByReference.Components.Elements;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGame_ByReference.Components.Controls;
partial class CheckBoxRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/CheckBox", typeof(CheckBoxRuntime));
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof(global::MonoGameGum.Forms.Controls.CheckBox)] = typeof(CheckBoxRuntime);
    }
    public global::MonoGameGum.Forms.Controls.CheckBox FormsControl => FormsControlAsObject as global::MonoGameGum.Forms.Controls.CheckBox;
    public enum CheckBoxCategory
    {
        EnabledOn,
        EnabledOff,
        EnabledIndeterminate,
        DisabledOn,
        DisabledOff,
        DisabledIndeterminate,
        HighlightedOn,
        HighlightedOff,
        HighlightedIndeterminate,
        PushedOn,
        PushedOff,
        PushedIndeterminate,
        FocusedOn,
        FocusedOff,
        FocusedIndeterminate,
        HighlightedFocusedOn,
        HighlightedFocusedOff,
        HighlightedFocusedIndeterminate,
        DisabledFocusedOn,
        DisabledFocusedOff,
        DisabledFocusedIndeterminate,
    }

    CheckBoxCategory? _checkBoxCategoryState;
    public CheckBoxCategory? CheckBoxCategoryState
    {
        get => _checkBoxCategoryState;
        set
        {
            _checkBoxCategoryState = value;
            if(value != null)
            {
                if(Categories.ContainsKey("CheckBoxCategory"))
                {
                    var category = Categories["CheckBoxCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "CheckBoxCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime CheckboxBackground { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public IconRuntime Check { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public string Text
    {
        get => TextInstance.Text;
        set => TextInstance.Text = value;
    }

    public CheckBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/CheckBox");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null)
        {
            FormsControlAsObject = new global::MonoGameGum.Forms.Controls.CheckBox(this);
        }
        CheckboxBackground = this.GetGraphicalUiElementByName("CheckboxBackground") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TextInstance = this.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        Check = this.GetGraphicalUiElementByName("Check") as CodeGen_MonoGame_ByReference.Components.Elements.IconRuntime;
        FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
