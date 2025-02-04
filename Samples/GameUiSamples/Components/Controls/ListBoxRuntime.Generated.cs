//Code for Controls/ListBox (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components
{
    public partial class ListBoxRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/ListBox", typeof(ListBoxRuntime));
        }
        public enum ListBoxCategory
        {
            Enabled,
            Disabled,
            Focused,
            DisabledFocused,
        }

        public ListBoxCategory ListBoxCategoryState
        {
            set
            {
                if(Categories.ContainsKey("ListBoxCategory"))
                {
                    var category = Categories["ListBoxCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "ListBoxCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public ScrollBarRuntime VerticalScrollBarInstance { get; protected set; }
        public ContainerRuntime ClipContainerInstance { get; protected set; }
        public ContainerRuntime InnerPanelInstance { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }

        public ListBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            Background = this.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
            VerticalScrollBarInstance = this.GetGraphicalUiElementByName("VerticalScrollBarInstance") as ScrollBarRuntime;
            ClipContainerInstance = this.GetGraphicalUiElementByName("ClipContainerInstance") as ContainerRuntime;
            InnerPanelInstance = this.GetGraphicalUiElementByName("InnerPanelInstance") as ContainerRuntime;
            FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as NineSliceRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
