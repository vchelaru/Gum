//Code for Controls/PasswordBox (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Components
{
    public partial class PasswordBoxRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/PasswordBox", typeof(PasswordBoxRuntime));
        }
        public MonoGameGum.Forms.Controls.PasswordBox FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.PasswordBox;
        public enum PasswordBoxCategory
        {
            Enabled,
            Disabled,
            Highlighted,
            Selected,
        }

        public PasswordBoxCategory PasswordBoxCategoryState
        {
            set
            {
                if(Categories.ContainsKey("PasswordBoxCategory"))
                {
                    var category = Categories["PasswordBoxCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "PasswordBoxCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public NineSliceRuntime SelectionInstance { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }
        public TextRuntime PlaceholderTextInstance { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }
        public SpriteRuntime CaretInstance { get; protected set; }

        public string PlaceholderText
        {
            get => PlaceholderTextInstance.Text;
            set => PlaceholderTextInstance.Text = value;
        }

        public PasswordBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
                var element = ObjectFinder.Self.GetElementSave("Controls/PasswordBox");
                element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            }



        }
        public override void AfterFullCreation()
        {
            if (FormsControl == null)
            {
                FormsControlAsObject = new MonoGameGum.Forms.Controls.PasswordBox(this);
            }
            Background = this.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
            SelectionInstance = this.GetGraphicalUiElementByName("SelectionInstance") as NineSliceRuntime;
            TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            PlaceholderTextInstance = this.GetGraphicalUiElementByName("PlaceholderTextInstance") as TextRuntime;
            FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as NineSliceRuntime;
            CaretInstance = this.GetGraphicalUiElementByName("CaretInstance") as SpriteRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
