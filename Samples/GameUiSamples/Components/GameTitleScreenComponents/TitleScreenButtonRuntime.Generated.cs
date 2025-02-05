//Code for GameTitleScreenComponents/TitleScreenButton (Container)
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
    public partial class TitleScreenButtonRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("GameTitleScreenComponents/TitleScreenButton", typeof(TitleScreenButtonRuntime));
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
        public TextRuntime TextInstance { get; protected set; }
        public TextRuntime FocusIndicator { get; protected set; }

        public string ButtonDisplayText
        {
            get => TextInstance.Text;
            set => TextInstance.Text = value;
        }

        public TitleScreenButtonRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            if (FormsControl == null)
            {
                FormsControlAsObject = new MonoGameGum.Forms.Controls.Button(this);
            }
            TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            FocusIndicator = this.GetGraphicalUiElementByName("FocusIndicator") as TextRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
