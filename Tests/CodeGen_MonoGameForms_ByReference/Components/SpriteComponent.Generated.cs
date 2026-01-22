//Code for SpriteComponent (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGenProject.Components;
partial class SpriteComponent : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("SpriteComponent");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named SpriteComponent - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new SpriteComponent(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(SpriteComponent)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("SpriteComponent", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public SpriteRuntime SpriteInstance { get; protected set; }

    public string SpriteInstanceSourceFile
    {
        set => SpriteInstance.SourceFileName = value;
    }

    public TextureAddress SpriteInstanceTextureAddress
    {
        get => SpriteInstance.TextureAddress;
        set => SpriteInstance.TextureAddress = value;
    }

    public int SpriteInstanceTextureHeight
    {
        get => SpriteInstance.TextureHeight;
        set => SpriteInstance.TextureHeight = value;
    }

    public int SpriteInstanceTextureLeft
    {
        get => SpriteInstance.TextureLeft;
        set => SpriteInstance.TextureLeft = value;
    }

    public int SpriteInstanceTextureTop
    {
        get => SpriteInstance.TextureTop;
        set => SpriteInstance.TextureTop = value;
    }

    public int SpriteInstanceTextureWidth
    {
        get => SpriteInstance.TextureWidth;
        set => SpriteInstance.TextureWidth = value;
    }

    public float SpriteInstanceX
    {
        get => SpriteInstance.X;
        set => SpriteInstance.X = value;
    }

    public SpriteComponent(InteractiveGue visual) : base(visual)
    {
    }
    public SpriteComponent()
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
