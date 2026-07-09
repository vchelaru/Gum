//Code for Controls/Slider (Container)
using CodeGenProject.Components.Controls;
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGenProject.Components.Controls;
partial class Slider : global::Gum.Forms.Controls.Slider
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/Slider") ?? throw new System.InvalidOperationException("Could not find an element named Controls/Slider - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Slider(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Slider)] = template;
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.Slider)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/Slider", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum SliderCategory
    {
        Enabled,
        Disabled,
        DisabledFocused,
        Focused,
        Highlighted,
        HighlightedFocused,
        Pushed,
    }

    SliderCategory? _sliderCategoryState;
    public SliderCategory? SliderCategoryState
    {
        get => _sliderCategoryState;
        set
        {
            _sliderCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("SliderCategory"))
                {
                    var category = Visual.Categories["SliderCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "SliderCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public ContainerRuntime TrackInstance { get; protected set; }
    public NineSliceRuntime TrackBackground { get; protected set; }
    public ButtonStandard ThumbInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public float SliderPercent
    {
        get => ThumbInstance.Visual.X;
        set => ThumbInstance.Visual.X = value;
    }

    public Slider(InteractiveGue visual) : base(visual)
    {
    }
    public Slider()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        TrackInstance = this.Visual?.GetGraphicalUiElementByName("TrackInstance") as global::Gum.GueDeriving.ContainerRuntime;
        TrackBackground = this.Visual?.GetGraphicalUiElementByName("TrackBackground") as global::Gum.GueDeriving.NineSliceRuntime;
        ThumbInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonStandard>(this.Visual,"ThumbInstance");
        FocusedIndicator = this.Visual?.GetGraphicalUiElementByName("FocusedIndicator") as global::Gum.GueDeriving.NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
