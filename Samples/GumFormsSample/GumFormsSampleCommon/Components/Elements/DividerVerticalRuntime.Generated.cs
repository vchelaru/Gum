//Code for Elements/DividerVertical (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Components
{
    public partial class DividerVerticalRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Elements/DividerVertical", typeof(DividerVerticalRuntime));
        }
        public SpriteRuntime AccentTop { get; protected set; }
        public SpriteRuntime Line { get; protected set; }
        public SpriteRuntime AccentRight { get; protected set; }

        public DividerVerticalRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
                var element = ObjectFinder.Self.GetElementSave("Elements/DividerVertical");
                element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            }



        }
        public override void AfterFullCreation()
        {
            AccentTop = this.GetGraphicalUiElementByName("AccentTop") as SpriteRuntime;
            Line = this.GetGraphicalUiElementByName("Line") as SpriteRuntime;
            AccentRight = this.GetGraphicalUiElementByName("AccentRight") as SpriteRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
