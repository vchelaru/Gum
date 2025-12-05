//Code for Controls/Slider (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGame_ByReference.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGame_ByReference.Components.Controls;
partial class SliderRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/Slider", typeof(SliderRuntime));
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof(global::Gum.Forms.Controls.Slider)] = typeof(SliderRuntime);
    }
    public global::Gum.Forms.Controls.Slider FormsControl => FormsControlAsObject as global::Gum.Forms.Controls.Slider;
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
                if(Categories.ContainsKey("SliderCategory"))
                {
                    var category = Categories["SliderCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "SliderCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
    }
    public ContainerRuntime TrackInstance { get; protected set; }
    public NineSliceRuntime TrackBackground { get; protected set; }
    public ButtonStandardRuntime ThumbInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public float SliderPercent
    {
        get => ThumbInstance.X;
        set => ThumbInstance.X = value;
    }

    public SliderRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/Slider");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null)
        {
            FormsControlAsObject = new global::Gum.Forms.Controls.Slider(this);
        }
        TrackInstance = this.GetGraphicalUiElementByName("TrackInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        TrackBackground = this.GetGraphicalUiElementByName("TrackBackground") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        ThumbInstance = this.GetGraphicalUiElementByName("ThumbInstance") as CodeGen_MonoGame_ByReference.Components.Controls.ButtonStandardRuntime;
        FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
