//Code for Controls/CheckBox (Container)
using CodeGen_MonoGameForms_Localization_ByReference.Components.Elements;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_Localization_ByReference.Components.Controls;
partial class CheckBox : global::Gum.Forms.Controls.CheckBox
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/CheckBox");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/CheckBox - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new CheckBox(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(CheckBox)] = template;
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.CheckBox)] = template;
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
                if(Visual.Categories.ContainsKey("CheckBoxCategory"))
                {
                    var category = Visual.Categories["CheckBoxCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "CheckBoxCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime CheckBoxBackground { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public Icon InnerCheck { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public string Text
    {
        get => TextInstance.Text;
        set => TextInstance.Text = value;
    }

    public CheckBox(InteractiveGue visual) : base(visual)
    {
    }
    public CheckBox()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        CheckBoxBackground = this.Visual?.GetGraphicalUiElementByName("CheckBoxBackground") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        InnerCheck = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"InnerCheck");
        FocusedIndicator = this.Visual?.GetGraphicalUiElementByName("FocusedIndicator") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    public void ApplyLocalization()
    {
    }
    partial void CustomInitialize();
}
