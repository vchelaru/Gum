//Code for Controls/FancyListBoxItem (Container)
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
    public partial class FancyListBoxItemRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/FancyListBoxItem", typeof(FancyListBoxItemRuntime));
        }
        public MonoGameGum.Forms.Controls.ListBoxItem FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.ListBoxItem;
        public enum ListBoxItemCategory
        {
            Enabled,
            Highlighted,
            Selected,
            Focused,
        }

        public ListBoxItemCategory ListBoxItemCategoryState
        {
            set
            {
                if(Categories.ContainsKey("ListBoxItemCategory"))
                {
                    var category = Categories["ListBoxItemCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "ListBoxItemCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }
        public RectangleRuntime RectangleInstance { get; protected set; }

        public string ListItemDisplayText
        {
            get => TextInstance.Text;
            set => TextInstance.Text = value;
        }

        public FancyListBoxItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
                var element = ObjectFinder.Self.GetElementSave("Controls/FancyListBoxItem");
                element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            }



        }
        public override void AfterFullCreation()
        {
            if (FormsControl == null)
            {
                FormsControlAsObject = new MonoGameGum.Forms.Controls.ListBoxItem(this);
            }
            Background = this.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
            TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as NineSliceRuntime;
            RectangleInstance = this.GetGraphicalUiElementByName("RectangleInstance") as RectangleRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
