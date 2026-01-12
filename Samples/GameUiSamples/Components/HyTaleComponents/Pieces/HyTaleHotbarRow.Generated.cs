//Code for HyTaleComponents/Pieces/HyTaleHotbarRow (Container)
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
partial class HyTaleHotbarRow : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("HyTaleComponents/Pieces/HyTaleHotbarRow");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named HyTaleComponents/Pieces/HyTaleHotbarRow - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new HyTaleHotbarRow(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(HyTaleHotbarRow)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("HyTaleComponents/Pieces/HyTaleHotbarRow", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public StackPanel HotBarRowContainer { get; protected set; }
    public HyTaleItemSlot HotBarItem1 { get; protected set; }
    public HyTaleItemSlot HotBarItem2 { get; protected set; }
    public HyTaleItemSlot HotBarItem3 { get; protected set; }
    public HyTaleItemSlot HotBarItem4 { get; protected set; }
    public HyTaleItemSlot HotBarItem5 { get; protected set; }
    public HyTaleItemSlot HotBarItem6 { get; protected set; }
    public HyTaleItemSlot HotBarItem7 { get; protected set; }
    public HyTaleItemSlot HotBarItem8 { get; protected set; }
    public HyTaleItemSlot HotBarItem9 { get; protected set; }

    public HyTaleHotbarRow(InteractiveGue visual) : base(visual)
    {
    }
    public HyTaleHotbarRow()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        HotBarRowContainer = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<StackPanel>(this.Visual,"HotBarRowContainer");
        HotBarItem1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HotBarItem1");
        HotBarItem2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HotBarItem2");
        HotBarItem3 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HotBarItem3");
        HotBarItem4 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HotBarItem4");
        HotBarItem5 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HotBarItem5");
        HotBarItem6 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HotBarItem6");
        HotBarItem7 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HotBarItem7");
        HotBarItem8 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HotBarItem8");
        HotBarItem9 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HyTaleItemSlot>(this.Visual,"HotBarItem9");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
