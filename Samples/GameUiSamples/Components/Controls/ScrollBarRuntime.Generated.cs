//Code for Controls/ScrollBar (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components
{
    public partial class ScrollBarRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/ScrollBar", typeof(ScrollBarRuntime));
        }
        public enum ScrollBarCategory
        {
        }

        public ScrollBarCategory ScrollBarCategoryState
        {
            set
            {
                if(Categories.ContainsKey("ScrollBarCategory"))
                {
                    var category = Categories["ScrollBarCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "ScrollBarCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
        public ButtonIconRuntime UpButtonInstance { get; protected set; }
        public ButtonIconRuntime DownButtonInstance { get; protected set; }
        public ContainerRuntime TrackInstance { get; protected set; }
        public NineSliceRuntime TrackBackground { get; protected set; }
        public ButtonStandardRuntime ThumbInstance { get; protected set; }

        public ScrollBarRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            UpButtonInstance = this.GetGraphicalUiElementByName("UpButtonInstance") as ButtonIconRuntime;
            DownButtonInstance = this.GetGraphicalUiElementByName("DownButtonInstance") as ButtonIconRuntime;
            TrackInstance = this.GetGraphicalUiElementByName("TrackInstance") as ContainerRuntime;
            TrackBackground = this.GetGraphicalUiElementByName("TrackBackground") as NineSliceRuntime;
            ThumbInstance = this.GetGraphicalUiElementByName("ThumbInstance") as ButtonStandardRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
