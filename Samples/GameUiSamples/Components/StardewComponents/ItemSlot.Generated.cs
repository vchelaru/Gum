//Code for StardewComponents/ItemSlot (Container)
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
partial class ItemSlot : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("StardewComponents/ItemSlot");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named StardewComponents/ItemSlot - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ItemSlot(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ItemSlot)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("StardewComponents/ItemSlot", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public ItemIcon ItemIconInstance { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public NineSliceRuntime HighlightNineSlice { get; protected set; }

    public bool IsHighlighted
    {
        get => HighlightNineSlice.Visible;
        set => HighlightNineSlice.Visible = value;
    }

    public string HotkeyText
    {
        get => TextInstance.Text;
        set => TextInstance.Text = value;
    }

    public ItemSlot(InteractiveGue visual) : base(visual)
    {
    }
    public ItemSlot()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        NineSliceInstance = this.Visual?.GetGraphicalUiElementByName("NineSliceInstance") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        ItemIconInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ItemIcon>(this.Visual,"ItemIconInstance");
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        HighlightNineSlice = this.Visual?.GetGraphicalUiElementByName("HighlightNineSlice") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
