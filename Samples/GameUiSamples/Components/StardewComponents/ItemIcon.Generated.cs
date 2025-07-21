//Code for StardewComponents/ItemIcon (Container)
using GumRuntime;
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
partial class ItemIcon : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("StardewComponents/ItemIcon");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ItemIcon(visual);
            return visual;
        });
        MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ItemIcon)] = template;
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

    public ItemIcon(InteractiveGue visual) : base(visual) { }
    public ItemIcon()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        SpriteInstance = this.Visual?.GetGraphicalUiElementByName("SpriteInstance") as SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
