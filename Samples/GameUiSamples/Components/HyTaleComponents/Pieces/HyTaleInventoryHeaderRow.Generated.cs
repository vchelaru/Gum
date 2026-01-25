//Code for HyTaleComponents/Pieces/HyTaleInventoryHeaderRow (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using GameUiSamples.Components;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components;
partial class HyTaleInventoryHeaderRow : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("HyTaleComponents/Pieces/HyTaleInventoryHeaderRow");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named HyTaleComponents/Pieces/HyTaleInventoryHeaderRow - did you forget to load a Gum project?");
#endif
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
        TitleContainer = this.Visual?.GetGraphicalUiElementByName("TitleContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        FilterIconWeapons = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleInventoryTitleItem>(this.Visual,"FilterIconWeapons");
        FilterIconArmor = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleInventoryTitleItem>(this.Visual,"FilterIconArmor");
        FilterIconIngots = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleInventoryTitleItem>(this.Visual,"FilterIconIngots");
        TitleContainer1 = this.Visual?.GetGraphicalUiElementByName("TitleContainer1") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        TitleContainer2 = this.Visual?.GetGraphicalUiElementByName("TitleContainer2") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        NineSliceInstance = this.Visual?.GetGraphicalUiElementByName("NineSliceInstance") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        OutsideBorder = this.Visual?.GetGraphicalUiElementByName("OutsideBorder") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        StackContainer = this.Visual?.GetGraphicalUiElementByName("StackContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        NineSliceInstance1 = this.Visual?.GetGraphicalUiElementByName("NineSliceInstance1") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TextInstance1 = this.Visual?.GetGraphicalUiElementByName("TextInstance1") as global::MonoGameGum.GueDeriving.TextRuntime;
        NineSliceInstance2 = this.Visual?.GetGraphicalUiElementByName("NineSliceInstance2") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        IconSprite = this.Visual?.GetGraphicalUiElementByName("IconSprite") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
