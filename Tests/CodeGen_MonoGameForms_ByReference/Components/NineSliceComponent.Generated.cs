//Code for NineSliceComponent (Container)
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

namespace CodeGenProject.Components;
partial class NineSliceComponent : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("NineSliceComponent");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named NineSliceComponent - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new NineSliceComponent(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(NineSliceComponent)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("NineSliceComponent", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public NineSliceRuntime NineSliceInstance { get; protected set; }

    public int NineSliceInstanceTextureHeight
    {
        get => NineSliceInstance.TextureHeight;
        set => NineSliceInstance.TextureHeight = value;
    }

    public int NineSliceInstanceTextureLeft
    {
        get => NineSliceInstance.TextureLeft;
        set => NineSliceInstance.TextureLeft = value;
    }

    public int NineSliceInstanceTextureTop
    {
        get => NineSliceInstance.TextureTop;
        set => NineSliceInstance.TextureTop = value;
    }

    public int NineSliceInstanceTextureWidth
    {
        get => NineSliceInstance.TextureWidth;
        set => NineSliceInstance.TextureWidth = value;
    }

    public NineSliceComponent(InteractiveGue visual) : base(visual)
    {
    }
    public NineSliceComponent()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        NineSliceInstance = this.Visual?.GetGraphicalUiElementByName("NineSliceInstance") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
