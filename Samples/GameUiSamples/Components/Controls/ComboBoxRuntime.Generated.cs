//Code for Controls/ComboBox (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components
{
    public partial class ComboBoxRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/ComboBox", typeof(ComboBoxRuntime));
        }
        public enum ComboBoxCategory
        {
            Enabled,
            Disabled,
            Highlighted,
            Pushed,
            HighlightedFocused,
            Focused,
            DisabledFocused,
        }

        public ComboBoxCategory ComboBoxCategoryState
        {
            set
            {
                if(Categories.ContainsKey("ComboBoxCategory"))
                {
                    var category = Categories["ComboBoxCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "ComboBoxCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }
        public ListBoxRuntime ListBoxInstance { get; protected set; }
        public IconRuntime IconInstance { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }

        public ComboBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            Background = this.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
            TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            ListBoxInstance = this.GetGraphicalUiElementByName("ListBoxInstance") as ListBoxRuntime;
            IconInstance = this.GetGraphicalUiElementByName("IconInstance") as IconRuntime;
            FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as NineSliceRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
