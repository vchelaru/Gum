//Code for ComponentWithExposedVariables (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGenProject.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGenProject.Components;
partial class ComponentWithExposedVariables : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("ComponentWithExposedVariables");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named ComponentWithExposedVariables - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ComponentWithExposedVariables(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ComponentWithExposedVariables)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("ComponentWithExposedVariables", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public SpriteComponent SpriteComponentInstance { get; protected set; }

    public string SpriteComponentInstanceSpriteInstanceSourceFile
    {
        set => SpriteComponentInstance.SpriteInstanceSourceFile = value;
    }

    public TextureAddress SpriteComponentInstanceSpriteInstanceTextureAddress
    {
        get => SpriteComponentInstance.SpriteInstanceTextureAddress;
        set => SpriteComponentInstance.SpriteInstanceTextureAddress = value;
    }

    public float SpriteComponentInstanceSpriteInstanceX
    {
        get => SpriteComponentInstance.SpriteInstanceX;
        set => SpriteComponentInstance.SpriteInstanceX = value;
    }

    public ComponentWithExposedVariables(InteractiveGue visual) : base(visual)
    {
    }
    public ComponentWithExposedVariables()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        SpriteComponentInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<SpriteComponent>(this.Visual,"SpriteComponentInstance");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
