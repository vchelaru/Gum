//Code for Controls/Slider (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class SliderRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/Slider", typeof(SliderRuntime));
    }
    public MonoGameGum.Forms.Controls.Slider FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.Slider;
    public enum SliderCategory
    {
        Enabled,
        Focused,
        Highlighted,
        HighlightedFocused,
    }

    public SliderCategory SliderCategoryState
    {
        set
        {
            if(Categories.ContainsKey("SliderCategory"))
            {
                var category = Categories["SliderCategory"];
                var state = category.States.Find(item => item.Name == value.ToString());
                this.ApplyState(state);
            }
            else
            {
                var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "SliderCategory");
                var state = category.States.Find(item => item.Name == value.ToString());
                this.ApplyState(state);
            }
        }
    }
    public ContainerRuntime TrackInstance { get; protected set; }
    public NineSliceRuntime NineSliceInstance { get; protected set; }
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
            FormsControlAsObject = new MonoGameGum.Forms.Controls.Slider(this);
        }
        TrackInstance = this.GetGraphicalUiElementByName("TrackInstance") as ContainerRuntime;
        NineSliceInstance = this.GetGraphicalUiElementByName("NineSliceInstance") as NineSliceRuntime;
        ThumbInstance = this.GetGraphicalUiElementByName("ThumbInstance") as ButtonStandardRuntime;
        FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
