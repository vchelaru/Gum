//Code for HyTaleComponents/HyTaleInventory (Container)
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
partial class HyTaleInventory : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("HyTaleComponents/HyTaleInventory");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named HyTaleComponents/HyTaleInventory - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new HyTaleInventory(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(HyTaleInventory)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("HyTaleComponents/HyTaleInventory", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public StackPanel InventoryRow1 { get; protected set; }
    public StackPanel InventoryRow2 { get; protected set; }
    public StackPanel InventoryRow3 { get; protected set; }
    public StackPanel InventoryRow4 { get; protected set; }
    public HyTaleHotbarRow HyTaleHotbarRowInstance { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance1 { get; protected set; }
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public PanelStandard MainGrid { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance2 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance3 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance4 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance5 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance6 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance7 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance8 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance9 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance10 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance11 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance12 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance13 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance14 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance15 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance16 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance17 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance18 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance28 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance29 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance30 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance31 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance32 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance33 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance34 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance35 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance36 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance19 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance20 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance21 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance22 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance23 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance24 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance25 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance26 { get; protected set; }
    public HyTaleItemSlot HyTaleItemSlotInstance27 { get; protected set; }
    public HyTaleInventoryHeaderRow HyTaleInventoryHeaderRowInstance { get; protected set; }
    public ContainerRuntime ContainerInstance { get; protected set; }

    public HyTaleInventory(InteractiveGue visual) : base(visual)
    {
    }
    public HyTaleInventory()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        InventoryRow1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<StackPanel>(this.Visual,"InventoryRow1");
        InventoryRow2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<StackPanel>(this.Visual,"InventoryRow2");
        InventoryRow3 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<StackPanel>(this.Visual,"InventoryRow3");
        InventoryRow4 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<StackPanel>(this.Visual,"InventoryRow4");
        HyTaleHotbarRowInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleHotbarRow>(this.Visual,"HyTaleHotbarRowInstance");
        HyTaleItemSlotInstance1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance1");
        NineSliceInstance = this.Visual?.GetGraphicalUiElementByName("NineSliceInstance") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        MainGrid = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<PanelStandard>(this.Visual,"MainGrid");
        HyTaleItemSlotInstance2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance2");
        HyTaleItemSlotInstance3 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance3");
        HyTaleItemSlotInstance4 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance4");
        HyTaleItemSlotInstance5 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance5");
        HyTaleItemSlotInstance6 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance6");
        HyTaleItemSlotInstance7 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance7");
        HyTaleItemSlotInstance8 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance8");
        HyTaleItemSlotInstance9 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance9");
        HyTaleItemSlotInstance10 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance10");
        HyTaleItemSlotInstance11 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance11");
        HyTaleItemSlotInstance12 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance12");
        HyTaleItemSlotInstance13 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance13");
        HyTaleItemSlotInstance14 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance14");
        HyTaleItemSlotInstance15 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance15");
        HyTaleItemSlotInstance16 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance16");
        HyTaleItemSlotInstance17 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance17");
        HyTaleItemSlotInstance18 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance18");
        HyTaleItemSlotInstance28 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance28");
        HyTaleItemSlotInstance29 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance29");
        HyTaleItemSlotInstance30 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance30");
        HyTaleItemSlotInstance31 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance31");
        HyTaleItemSlotInstance32 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance32");
        HyTaleItemSlotInstance33 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance33");
        HyTaleItemSlotInstance34 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance34");
        HyTaleItemSlotInstance35 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance35");
        HyTaleItemSlotInstance36 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance36");
        HyTaleItemSlotInstance19 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance19");
        HyTaleItemSlotInstance20 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance20");
        HyTaleItemSlotInstance21 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance21");
        HyTaleItemSlotInstance22 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance22");
        HyTaleItemSlotInstance23 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance23");
        HyTaleItemSlotInstance24 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance24");
        HyTaleItemSlotInstance25 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance25");
        HyTaleItemSlotInstance26 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance26");
        HyTaleItemSlotInstance27 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HyTaleItemSlotInstance27");
        HyTaleInventoryHeaderRowInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleInventoryHeaderRow>(this.Visual,"HyTaleInventoryHeaderRowInstance");
        ContainerInstance = this.Visual?.GetGraphicalUiElementByName("ContainerInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
