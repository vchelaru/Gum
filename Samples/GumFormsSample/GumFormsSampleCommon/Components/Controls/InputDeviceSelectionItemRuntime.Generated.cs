//Code for Controls/InputDeviceSelectionItem (Container)
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
    public partial class InputDeviceSelectionItemRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/InputDeviceSelectionItem", typeof(InputDeviceSelectionItemRuntime));
        }
        public enum JoinedCategory
        {
            NoInputDevice,
            HasInputDevice,
        }

        public JoinedCategory JoinedCategoryState
        {
            set
            {
                if(Categories.ContainsKey("JoinedCategory"))
                {
                    var category = Categories["JoinedCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "JoinedCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public IconRuntime IconInstance { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }
        public ButtonCloseRuntime RemoveDeviceButtonInstance { get; protected set; }

        public InputDeviceSelectionItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
                var element = ObjectFinder.Self.GetElementSave("Controls/InputDeviceSelectionItem");
                element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            }



        }
        public override void AfterFullCreation()
        {
            Background = this.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
            IconInstance = this.GetGraphicalUiElementByName("IconInstance") as IconRuntime;
            TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            RemoveDeviceButtonInstance = this.GetGraphicalUiElementByName("RemoveDeviceButtonInstance") as ButtonCloseRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
