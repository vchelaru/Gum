//Code for Controls/ScrollViewer (Container)
using GumRuntime;
using GumFormsSample.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Components
{
    public partial class ScrollViewerRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/ScrollViewer", typeof(ScrollViewerRuntime));
        }
        public MonoGameGum.Forms.Controls.ScrollViewer FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.ScrollViewer;
        public enum ScrollBarVisibility
        {
            NoScrollBar,
            VerticalScrollVisible,
        }

        public ScrollBarVisibility ScrollBarVisibilityState
        {
            set
            {
                if(Categories.ContainsKey("ScrollBarVisibility"))
                {
                    var category = Categories["ScrollBarVisibility"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "ScrollBarVisibility");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public ScrollBarRuntime VerticalScrollBarInstance { get; protected set; }
        public ContainerRuntime ClipContainerInstance { get; protected set; }
        public ContainerRuntime InnerPanelInstance { get; protected set; }
        public ListBoxItemRuntime ListBoxItemInstance { get; protected set; }

        public ScrollViewerRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
                var element = ObjectFinder.Self.GetElementSave("Controls/ScrollViewer");
                element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            }



        }
        public override void AfterFullCreation()
        {
            if (FormsControl == null)
            {
                FormsControlAsObject = new MonoGameGum.Forms.Controls.ScrollViewer(this);
            }
            Background = this.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
            VerticalScrollBarInstance = this.GetGraphicalUiElementByName("VerticalScrollBarInstance") as ScrollBarRuntime;
            ClipContainerInstance = this.GetGraphicalUiElementByName("ClipContainerInstance") as ContainerRuntime;
            InnerPanelInstance = this.GetGraphicalUiElementByName("InnerPanelInstance") as ContainerRuntime;
            ListBoxItemInstance = this.GetGraphicalUiElementByName("ListBoxItemInstance") as ListBoxItemRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
