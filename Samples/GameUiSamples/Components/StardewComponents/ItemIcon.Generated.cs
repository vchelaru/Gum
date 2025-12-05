//Code for StardewComponents/ItemIcon (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components;
partial class ItemIcon : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("StardewComponents/ItemIcon");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named StardewComponents/ItemIcon - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ItemIcon(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ItemIcon)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("StardewComponents/ItemIcon", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public SpriteRuntime SpriteInstance { get; protected set; }

    public int TextureLeft
    {
        get => SpriteInstance.TextureLeft;
        set => SpriteInstance.TextureLeft = value;
    }

    public int TextureTop
    {
        get => SpriteInstance.TextureTop;
        set => SpriteInstance.TextureTop = value;
    }

    public ItemIcon(InteractiveGue visual) : base(visual)
    {
    }
    public ItemIcon()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        SpriteInstance = this.Visual?.GetGraphicalUiElementByName("SpriteInstance") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
