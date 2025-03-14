//Code for Elements/Label (Container)
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
    public partial class LabelRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Elements/Label", typeof(LabelRuntime));
        }
        public MonoGameGum.Forms.Controls.Label FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.Label;
        public TextRuntime TextInstance { get; protected set; }

        public string LabelColor
        {
            set => TextInstance.SetProperty("ColorCategoryState", value?.ToString());
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get => TextInstance.HorizontalAlignment;
            set => TextInstance.HorizontalAlignment = value;
        }

        public string Style
        {
            set => TextInstance.SetProperty("StyleCategoryState", value?.ToString());
        }

        public string LabelText
        {
            get => TextInstance.Text;
            set => TextInstance.Text = value;
        }

        public VerticalAlignment VerticalAlignment
        {
            get => TextInstance.VerticalAlignment;
            set => TextInstance.VerticalAlignment = value;
        }

        public LabelRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 0f;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
             
            this.Width = 128f;

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            if(tryCreateFormsObject)
            {
                if (FormsControl == null)
                {
                    FormsControlAsObject = new MonoGameGum.Forms.Controls.Label(this);
                }
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(TextInstance);
        }
        private void ApplyDefaultVariables()
        {
TextInstance.SetProperty("ColorCategoryState", "White");
TextInstance.SetProperty("StyleCategoryState", "Strong");
            this.TextInstance.Text = @"Item Label";
            this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

        }
        partial void CustomInitialize();
    }
}
