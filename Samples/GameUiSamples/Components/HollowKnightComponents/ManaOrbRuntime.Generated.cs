//Code for HollowKnightComponents/ManaOrb (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using GameUiSamples.Components;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components
{
    public partial class ManaOrbRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("HollowKnightComponents/ManaOrb", typeof(ManaOrbRuntime));
        }
        public enum FullEmptyCategory
        {
            Full,
            Empty,
        }

        public FullEmptyCategory FullEmptyCategoryState
        {
            set
            {
                if(Categories.ContainsKey("FullEmptyCategory"))
                {
                    var category = Categories["FullEmptyCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "FullEmptyCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
        public SpriteRuntime OrbBackground { get; protected set; }
        public ContainerRuntime RenderTargetContainer { get; protected set; }
        public SpriteRuntime WaveTop { get; protected set; }
        public SpriteRuntime WaveMaskSprite { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }

        public ManaOrbRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            OrbBackground = this.GetGraphicalUiElementByName("OrbBackground") as SpriteRuntime;
            RenderTargetContainer = this.GetGraphicalUiElementByName("RenderTargetContainer") as ContainerRuntime;
            WaveTop = this.GetGraphicalUiElementByName("WaveTop") as SpriteRuntime;
            WaveMaskSprite = this.GetGraphicalUiElementByName("WaveMaskSprite") as SpriteRuntime;
            ColoredRectangleInstance = this.GetGraphicalUiElementByName("ColoredRectangleInstance") as ColoredRectangleRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
