//Code for StardewComponents/InventoryGrid (Container)
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
partial class InventoryGrid : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("StardewComponents/InventoryGrid");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new InventoryGrid(visual);
            return visual;
        });
        MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(InventoryGrid)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("StardewComponents/InventoryGrid", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public PanelStandard MainGrid { get; protected set; }
    public ItemSlot ItemSlotInstance1 { get; protected set; }
    public ItemSlot ItemSlotInstance2 { get; protected set; }
    public ItemSlot ItemSlotInstance3 { get; protected set; }
    public ItemSlot ItemSlotInstance4 { get; protected set; }
    public ItemSlot ItemSlotInstance5 { get; protected set; }
    public ItemSlot ItemSlotInstance6 { get; protected set; }
    public ItemSlot ItemSlotInstance7 { get; protected set; }
    public ItemSlot ItemSlotInstance8 { get; protected set; }
    public ItemSlot ItemSlotInstance9 { get; protected set; }
    public ItemSlot ItemSlotInstance10 { get; protected set; }
    public ItemSlot ItemSlotInstance11 { get; protected set; }
    public ItemSlot ItemSlotInstance12 { get; protected set; }
    public ItemSlot ItemSlotInstance13 { get; protected set; }
    public ItemSlot ItemSlotInstance14 { get; protected set; }
    public ItemSlot ItemSlotInstance15 { get; protected set; }
    public ItemSlot ItemSlotInstance16 { get; protected set; }
    public ItemSlot ItemSlotInstance17 { get; protected set; }
    public ItemSlot ItemSlotInstance18 { get; protected set; }
    public ItemSlot ItemSlotInstance19 { get; protected set; }
    public ItemSlot ItemSlotInstance20 { get; protected set; }
    public ItemSlot ItemSlotInstance21 { get; protected set; }
    public ItemSlot ItemSlotInstance22 { get; protected set; }
    public ItemSlot ItemSlotInstance23 { get; protected set; }
    public ItemSlot ItemSlotInstance24 { get; protected set; }
    public ItemSlot ItemSlotInstance25 { get; protected set; }
    public ItemSlot ItemSlotInstance26 { get; protected set; }
    public ItemSlot ItemSlotInstance27 { get; protected set; }
    public ItemSlot ItemSlotInstance28 { get; protected set; }
    public ItemSlot ItemSlotInstance29 { get; protected set; }
    public ItemSlot ItemSlotInstance30 { get; protected set; }
    public ItemSlot ItemSlotInstance31 { get; protected set; }
    public ItemSlot ItemSlotInstance32 { get; protected set; }
    public ItemSlot ItemSlotInstance33 { get; protected set; }
    public ItemSlot ItemSlotInstance34 { get; protected set; }
    public ItemSlot ItemSlotInstance35 { get; protected set; }
    public ItemSlot ItemSlotInstance36 { get; protected set; }

    public InventoryGrid(InteractiveGue visual) : base(visual) { }
    public InventoryGrid()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        MainGrid = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<PanelStandard>(this.Visual,"MainGrid");
        ItemSlotInstance1 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance1");
        ItemSlotInstance2 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance2");
        ItemSlotInstance3 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance3");
        ItemSlotInstance4 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance4");
        ItemSlotInstance5 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance5");
        ItemSlotInstance6 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance6");
        ItemSlotInstance7 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance7");
        ItemSlotInstance8 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance8");
        ItemSlotInstance9 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance9");
        ItemSlotInstance10 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance10");
        ItemSlotInstance11 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance11");
        ItemSlotInstance12 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance12");
        ItemSlotInstance13 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance13");
        ItemSlotInstance14 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance14");
        ItemSlotInstance15 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance15");
        ItemSlotInstance16 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance16");
        ItemSlotInstance17 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance17");
        ItemSlotInstance18 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance18");
        ItemSlotInstance19 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance19");
        ItemSlotInstance20 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance20");
        ItemSlotInstance21 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance21");
        ItemSlotInstance22 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance22");
        ItemSlotInstance23 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance23");
        ItemSlotInstance24 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance24");
        ItemSlotInstance25 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance25");
        ItemSlotInstance26 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance26");
        ItemSlotInstance27 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance27");
        ItemSlotInstance28 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance28");
        ItemSlotInstance29 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance29");
        ItemSlotInstance30 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance30");
        ItemSlotInstance31 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance31");
        ItemSlotInstance32 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance32");
        ItemSlotInstance33 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance33");
        ItemSlotInstance34 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance34");
        ItemSlotInstance35 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance35");
        ItemSlotInstance36 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance36");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
