//Code for HyTaleComponents/Pieces/HyTaleItemIcon (Container)
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
partial class HyTaleItemIcon : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("HyTaleComponents/Pieces/HyTaleItemIcon");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named HyTaleComponents/Pieces/HyTaleItemIcon - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new HyTaleItemIcon(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(HyTaleItemIcon)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("HyTaleComponents/Pieces/HyTaleItemIcon", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public SpriteRuntime ItemSprite { get; protected set; }

    public int ItemStartX
    {
        get => ItemSprite.TextureLeft;
        set => ItemSprite.TextureLeft = value;
    }

    public int ItemStartY
    {
        get => ItemSprite.TextureTop;
        set => ItemSprite.TextureTop = value;
    }

    public HyTaleItemIcon(InteractiveGue visual) : base(visual)
    {
    }
    public HyTaleItemIcon()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        ItemSprite = this.Visual?.GetGraphicalUiElementByName("ItemSprite") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
