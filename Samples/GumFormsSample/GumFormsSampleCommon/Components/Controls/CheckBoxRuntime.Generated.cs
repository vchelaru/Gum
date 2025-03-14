//Code for Controls/CheckBox (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class CheckBoxRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/CheckBox", typeof(CheckBoxRuntime));
    }
    public MonoGameGum.Forms.Controls.CheckBox FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.CheckBox;
    public enum CheckBoxCategory
    {
        EnabledOn,
        EnabledOff,
        DisabledOn,
        DisabledOff,
        HighlightedOn,
        HighlightedOff,
        PushedOn,
        PushedOff,
        FocusedOn,
        FocusedOff,
        HighlightedFocusedOn,
        HighlightedFocusedOff,
        DisabledFocusedOn,
        DisabledFocusedOff,
    }

    public CheckBoxCategory CheckBoxCategoryState
    {
        set
        {
            if(Categories.ContainsKey("CheckBoxCategory"))
            {
                var category = Categories["CheckBoxCategory"];
                var state = category.States.Find(item => item.Name == value.ToString());
                this.ApplyState(state);
            }
            else
            {
                var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "CheckBoxCategory");
                var state = category.States.Find(item => item.Name == value.ToString());
                this.ApplyState(state);
            }
        }
    }
    public NineSliceRuntime CheckboxBackground { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public IconRuntime Check { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public string CheckboxDisplayText
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
            FormsControlAsObject = new MonoGameGum.Forms.Controls.CheckBox(this);
        }
        CheckboxBackground = this.GetGraphicalUiElementByName("CheckboxBackground") as NineSliceRuntime;
        TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
        Check = this.GetGraphicalUiElementByName("Check") as IconRuntime;
        FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
