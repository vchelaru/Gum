//Code for Controls/RadioButton (Container)
using GumRuntime;
using System.Linq;
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
partial class RadioButton : global::Gum.Forms.Controls.RadioButton
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/RadioButton");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/RadioButton - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new RadioButton(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(RadioButton)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/RadioButton", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum RadioButtonCategory
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

    RadioButtonCategory? _radioButtonCategoryState;
    public RadioButtonCategory? RadioButtonCategoryState
    {
        get => _radioButtonCategoryState;
        set
        {
            _radioButtonCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("RadioButtonCategory"))
                {
                    var category = Visual.Categories["RadioButtonCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "RadioButtonCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime RadioBackground { get; protected set; }
    public Icon Radio { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public string RadioDisplayText
    {
        get => TextInstance.Text;
        set => TextInstance.Text = value;
    }

    public RadioButton(InteractiveGue visual) : base(visual)
    {
    }
    public RadioButton()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        RadioBackground = this.Visual?.GetGraphicalUiElementByName("RadioBackground") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        Radio = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"Radio");
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        FocusedIndicator = this.Visual?.GetGraphicalUiElementByName("FocusedIndicator") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
