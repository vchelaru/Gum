//Code for StardewComponents/Hotbar (Container)
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
partial class Hotbar : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("StardewComponents/Hotbar");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named StardewComponents/Hotbar - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Hotbar(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Hotbar)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("StardewComponents/Hotbar", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public ItemSlot ItemSlotInstance1 { get; protected set; }
    public ItemSlot ItemSlotInstance2 { get; protected set; }
    public ItemSlot ItemSlotInstance3 { get; protected set; }
    public ItemSlot ItemSlotInstance4 { get; protected set; }
    public ItemSlot ItemSlotInstance5 { get; protected set; }
    public ItemSlot ItemSlotInstance6 { get; protected set; }
    public ItemSlot ItemSlotInstance7 { get; protected set; }
    public ItemSlot ItemSlotInstance8 { get; protected set; }
    public ItemSlot ItemSlotInstance9 { get; protected set; }
    public StackPanel ItemSlotContainer { get; protected set; }

    public Hotbar(InteractiveGue visual) : base(visual)
    {
    }
    public Hotbar()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        NineSliceInstance = this.Visual?.GetGraphicalUiElementByName("NineSliceInstance") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        ItemSlotInstance1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance1");
        ItemSlotInstance2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance2");
        ItemSlotInstance3 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance3");
        ItemSlotInstance4 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance4");
        ItemSlotInstance5 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance5");
        ItemSlotInstance6 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance6");
        ItemSlotInstance7 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance7");
        ItemSlotInstance8 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance8");
        ItemSlotInstance9 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemSlot>(this.Visual,"ItemSlotInstance9");
        ItemSlotContainer = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<StackPanel>(this.Visual,"ItemSlotContainer");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
