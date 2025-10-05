//Code for Elements/VerticalLines (Container)
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
partial class VerticalLinesRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Elements/VerticalLines", typeof(VerticalLinesRuntime));
    }
    public SpriteRuntime LinesSprite { get; protected set; }

    public int LineAlpha
    {
        get => LinesSprite.Alpha;
        set => LinesSprite.Alpha = value;
    }

    public string LineColor
    {
        set => LinesSprite.SetProperty("ColorCategoryState", value?.ToString());
    }

    public VerticalLinesRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Elements/VerticalLines");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        LinesSprite = this.GetGraphicalUiElementByName("LinesSprite") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
