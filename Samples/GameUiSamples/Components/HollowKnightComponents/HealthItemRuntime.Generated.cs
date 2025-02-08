//Code for HollowKnightComponents/HealthItem (Container)
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
    public partial class HealthItemRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("HollowKnightComponents/HealthItem", typeof(HealthItemRuntime));
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
        public SpriteRuntime SpriteInstance { get; protected set; }

        public HealthItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            SpriteInstance = this.GetGraphicalUiElementByName("SpriteInstance") as SpriteRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
