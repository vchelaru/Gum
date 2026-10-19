//Code for HyTaleComponents/Pieces/HyTaleInventoryHeaderRow (Container)
using GameUiSamples.Components;
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
partial class HyTaleInventoryHeaderRow : global::Gum.Forms.Controls.FrameworkElement
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
            var element = ObjectFinder.Self.GetElementSave("HyTaleComponents/Pieces/HyTaleInventoryHeaderRow") ?? throw new System.InvalidOperationException("Could not find an element named HyTaleComponents/Pieces/HyTaleInventoryHeaderRow - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new HyTaleInventoryHeaderRow(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(HyTaleInventoryHeaderRow)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("HyTaleComponents/Pieces/HyTaleInventoryHeaderRow", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public ContainerRuntime TitleContainer { get; protected set; }
    public HyTaleInventoryTitleItem FilterIconWeapons { get; protected set; }
    public HyTaleInventoryTitleItem FilterIconArmor { get; protected set; }
    public HyTaleInventoryTitleItem FilterIconIngots { get; protected set; }
    public ContainerRuntime TitleContainer1 { get; protected set; }
    public ContainerRuntime TitleContainer2 { get; protected set; }
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public NineSliceRuntime OutsideBorder { get; protected set; }
    public ContainerRuntime StackContainer { get; protected set; }
    public NineSliceRuntime NineSliceInstance1 { get; protected set; }
    public TextRuntime TextInstance1 { get; protected set; }
    public NineSliceRuntime NineSliceInstance2 { get; protected set; }
    public SpriteRuntime IconSprite { get; protected set; }

    public HyTaleInventoryHeaderRow(InteractiveGue visual) : base(visual)
    {
    }
    public HyTaleInventoryHeaderRow()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        TitleContainer = this.Visual?.GetGraphicalUiElementByName("TitleContainer") as global::Gum.GueDeriving.ContainerRuntime;
        FilterIconWeapons = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<HyTaleInventoryTitleItem>(this.Visual,"FilterIconWeapons");
        FilterIconArmor = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<HyTaleInventoryTitleItem>(this.Visual,"FilterIconArmor");
        FilterIconIngots = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<HyTaleInventoryTitleItem>(this.Visual,"FilterIconIngots");
        TitleContainer1 = this.Visual?.GetGraphicalUiElementByName("TitleContainer1") as global::Gum.GueDeriving.ContainerRuntime;
        TitleContainer2 = this.Visual?.GetGraphicalUiElementByName("TitleContainer2") as global::Gum.GueDeriving.ContainerRuntime;
        NineSliceInstance = this.Visual?.GetGraphicalUiElementByName("NineSliceInstance") as global::Gum.GueDeriving.NineSliceRuntime;
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as global::Gum.GueDeriving.TextRuntime;
        OutsideBorder = this.Visual?.GetGraphicalUiElementByName("OutsideBorder") as global::Gum.GueDeriving.NineSliceRuntime;
        StackContainer = this.Visual?.GetGraphicalUiElementByName("StackContainer") as global::Gum.GueDeriving.ContainerRuntime;
        NineSliceInstance1 = this.Visual?.GetGraphicalUiElementByName("NineSliceInstance1") as global::Gum.GueDeriving.NineSliceRuntime;
        TextInstance1 = this.Visual?.GetGraphicalUiElementByName("TextInstance1") as global::Gum.GueDeriving.TextRuntime;
        NineSliceInstance2 = this.Visual?.GetGraphicalUiElementByName("NineSliceInstance2") as global::Gum.GueDeriving.NineSliceRuntime;
        IconSprite = this.Visual?.GetGraphicalUiElementByName("IconSprite") as global::Gum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
