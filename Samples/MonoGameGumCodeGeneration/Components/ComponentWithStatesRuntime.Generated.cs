//Code for ComponentWithStates (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using MonoGameGum.GueDeriving;
using MonoGameGumCodeGeneration.Components;
using System.Linq;
namespace MonoGameGumCodeGeneration.Components
{
    public partial class ComponentWithStatesRuntime
    {
        public enum ColorCategory
        {
            RedState,
            GreenState,
            BlueState,
        }

        public ColorCategory ColorCategoryState
        {
            set
            {
                if(Categories.ContainsKey("ColorCategory"))
                {
                    var category = Categories["ColorCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "ColorCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
        public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }

        public ComponentWithStatesRuntime()
        {


        }
        public override void AfterFullCreation()
        {
            ColoredRectangleInstance = this.GetGraphicalUiElementByName("ColoredRectangleInstance") as ColoredRectangleRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
