//Code for StardewComponents/InventoryGrid (Container)
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
partial class InventoryGrid : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("StardewComponents/InventoryGrid");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named StardewComponents/InventoryGrid - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new InventoryGrid(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(InventoryGrid)] = template;
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

    public InventoryGrid(InteractiveGue visual) : base(visual)
    {
    }
    public InventoryGrid()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        MainGrid = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<PanelStandard>(this.Visual,"MainGrid");
        ItemSlotInstance1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance1");
        ItemSlotInstance2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance2");
        ItemSlotInstance3 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance3");
        ItemSlotInstance4 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance4");
        ItemSlotInstance5 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance5");
        ItemSlotInstance6 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance6");
        ItemSlotInstance7 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance7");
        ItemSlotInstance8 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance8");
        ItemSlotInstance9 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance9");
        ItemSlotInstance10 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance10");
        ItemSlotInstance11 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance11");
        ItemSlotInstance12 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance12");
        ItemSlotInstance13 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance13");
        ItemSlotInstance14 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance14");
        ItemSlotInstance15 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance15");
        ItemSlotInstance16 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance16");
        ItemSlotInstance17 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance17");
        ItemSlotInstance18 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance18");
        ItemSlotInstance19 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance19");
        ItemSlotInstance20 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance20");
        ItemSlotInstance21 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance21");
        ItemSlotInstance22 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance22");
        ItemSlotInstance23 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance23");
        ItemSlotInstance24 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance24");
        ItemSlotInstance25 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance25");
        ItemSlotInstance26 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance26");
        ItemSlotInstance27 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance27");
        ItemSlotInstance28 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance28");
        ItemSlotInstance29 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance29");
        ItemSlotInstance30 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance30");
        ItemSlotInstance31 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance31");
        ItemSlotInstance32 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance32");
        ItemSlotInstance33 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance33");
        ItemSlotInstance34 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance34");
        ItemSlotInstance35 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance35");
        ItemSlotInstance36 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance36");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
