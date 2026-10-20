//Code for HyTaleComponents/Pieces/HyTaleInventoryTitleItem (Container)
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace GameUiSamples.Components;
partial class HyTaleInventoryTitleItem : global::Gum.Forms.Controls.FrameworkElement
{
    #if UNITY_5_3_OR_NEWER
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    #else
    [System.Runtime.CompilerServices.ModuleInitializer]
    #endif
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("HyTaleComponents/Pieces/HyTaleInventoryTitleItem") ?? throw new System.InvalidOperationException("Could not find an element named HyTaleComponents/Pieces/HyTaleInventoryTitleItem - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new HyTaleInventoryTitleItem(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(HyTaleInventoryTitleItem)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("HyTaleComponents/Pieces/HyTaleInventoryTitleItem", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public ContainerRuntime TitleContainer { get; protected set; }
    public NineSliceRuntime Background { get; protected set; }
    public SpriteRuntime IconSprite { get; protected set; }

    public int StartX
    {
        get => IconSprite.TextureLeft;
        set => IconSprite.TextureLeft = value;
    }

    public int StartY
    {
        get => IconSprite.TextureTop;
        set => IconSprite.TextureTop = value;
    }

    public HyTaleInventoryTitleItem(InteractiveGue visual) : base(visual)
    {
    }
    public HyTaleInventoryTitleItem()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        TitleContainer = this.Visual?.GetGraphicalUiElementByName("TitleContainer") as global::Gum.GueDeriving.ContainerRuntime;
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as global::Gum.GueDeriving.NineSliceRuntime;
        IconSprite = this.Visual?.GetGraphicalUiElementByName("IconSprite") as global::Gum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
