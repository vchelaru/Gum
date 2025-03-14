//Code for Controls/ButtonClose (Container)
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
    public partial class ButtonCloseRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/ButtonClose", typeof(ButtonCloseRuntime));
        }
        public MonoGameGum.Forms.Controls.Button FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.Button;
        public enum ButtonCategory
        {
            Enabled,
            Disabled,
            Highlighted,
            Pushed,
            HighlightedFocused,
            Focused,
            DisabledFocused,
        }

        public ButtonCategory ButtonCategoryState
        {
            set
            {
                if(Categories.ContainsKey("ButtonCategory"))
                {
                    var category = Categories["ButtonCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "ButtonCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public IconRuntime Icon { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }

        public ButtonCloseRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
                var element = ObjectFinder.Self.GetElementSave("Controls/ButtonClose");
                element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            }



        }
        public override void AfterFullCreation()
        {
            if (FormsControl == null)
            {
                FormsControlAsObject = new MonoGameGum.Forms.Controls.Button(this);
            }
            Background = this.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
            Icon = this.GetGraphicalUiElementByName("Icon") as IconRuntime;
            FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as NineSliceRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
