//Code for Controls/Menu (Container)
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
    public partial class MenuRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/Menu", typeof(MenuRuntime));
        }
        public MonoGameGum.Forms.Controls.Menu FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.Menu;
        public NineSliceRuntime Background { get; protected set; }
        public ContainerRuntime InnerPanelInstance { get; protected set; }

        public MenuRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 24f;
             
            this.Width = 0f;
            this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.X = 0f;
            this.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.Y = 0f;
            this.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.YUnits = GeneralUnitType.PixelsFromSmall;

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            if(tryCreateFormsObject)
            {
                if (FormsControl == null)
                {
                    FormsControlAsObject = new MonoGameGum.Forms.Controls.Menu(this);
                }
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            Background = new NineSliceRuntime();
            Background.Name = "Background";
            InnerPanelInstance = new ContainerRuntime();
            InnerPanelInstance.Name = "InnerPanelInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            this.Children.Add(InnerPanelInstance);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("ColorCategoryState", "DarkGray");
            this.Background.Height = 0f;
            this.Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Background.Width = 0f;
            this.Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Background.X = 0f;
            this.Background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.Background.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.Background.Y = 0f;
            this.Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Background.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.InnerPanelInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            this.InnerPanelInstance.Height = -8f;
            this.InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.InnerPanelInstance.StackSpacing = 2f;
            this.InnerPanelInstance.Width = 0f;
            this.InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.InnerPanelInstance.X = 0f;
            this.InnerPanelInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.InnerPanelInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.InnerPanelInstance.Y = 0f;
            this.InnerPanelInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.InnerPanelInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

        }
        partial void CustomInitialize();
    }
}
