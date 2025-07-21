//Code for Controls/CheckBox (Container)
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using GameUiSamples.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components;
partial class CheckBox : MonoGameGum.Forms.Controls.CheckBox
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/CheckBox");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new CheckBox(visual);
            return visual;
        });
        MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(CheckBox)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/CheckBox", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
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

    CheckBoxCategory? _checkBoxCategoryState;
    public CheckBoxCategory? CheckBoxCategoryState
    {
        get => _checkBoxCategoryState;
        set
        {
            _checkBoxCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("CheckBoxCategory"))
                {
                    var category = Visual.Categories["CheckBoxCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "CheckBoxCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime CheckboxBackground { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public Icon Check { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public string CheckboxDisplayText
    {
        get => TextInstance.Text;
        set => TextInstance.Text = value;
    }

    public CheckBox(InteractiveGue visual) : base(visual) { }
    public CheckBox()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        CheckboxBackground = this.Visual?.GetGraphicalUiElementByName("CheckboxBackground") as NineSliceRuntime;
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
        Check = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"Check");
        FocusedIndicator = this.Visual?.GetGraphicalUiElementByName("FocusedIndicator") as NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
