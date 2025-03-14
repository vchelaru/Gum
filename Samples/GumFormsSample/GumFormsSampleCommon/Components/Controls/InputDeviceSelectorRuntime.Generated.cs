//Code for Controls/InputDeviceSelector (Container)
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
    public partial class InputDeviceSelectorRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/InputDeviceSelector", typeof(InputDeviceSelectorRuntime));
        }
        public NineSliceRuntime Background { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }
        public TextRuntime TextInstance1 { get; protected set; }
        public ContainerRuntime ContainerInstance1 { get; protected set; }
        public ContainerRuntime InputDeviceContainerInstance { get; protected set; }
        public ContainerRuntime ContainerInstance2 { get; protected set; }
        public InputDeviceSelectionItemRuntime InputDeviceSelectionItemInstance { get; protected set; }
        public InputDeviceSelectionItemRuntime InputDeviceSelectionItemInstance1 { get; protected set; }
        public InputDeviceSelectionItemRuntime InputDeviceSelectionItemInstance2 { get; protected set; }
        public InputDeviceSelectionItemRuntime InputDeviceSelectionItemInstance3 { get; protected set; }

        public InputDeviceSelectorRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 6f;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
             
            this.Width = 60f;
            this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.X = 0f;
            this.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.Y = 0f;
            this.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.YUnits = GeneralUnitType.PixelsFromMiddle;

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            if(tryCreateFormsObject)
            {
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            Background = new NineSliceRuntime();
            Background.Name = "Background";
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            TextInstance1 = new TextRuntime();
            TextInstance1.Name = "TextInstance1";
            ContainerInstance1 = new ContainerRuntime();
            ContainerInstance1.Name = "ContainerInstance1";
            InputDeviceContainerInstance = new ContainerRuntime();
            InputDeviceContainerInstance.Name = "InputDeviceContainerInstance";
            ContainerInstance2 = new ContainerRuntime();
            ContainerInstance2.Name = "ContainerInstance2";
            InputDeviceSelectionItemInstance = new InputDeviceSelectionItemRuntime();
            InputDeviceSelectionItemInstance.Name = "InputDeviceSelectionItemInstance";
            InputDeviceSelectionItemInstance1 = new InputDeviceSelectionItemRuntime();
            InputDeviceSelectionItemInstance1.Name = "InputDeviceSelectionItemInstance1";
            InputDeviceSelectionItemInstance2 = new InputDeviceSelectionItemRuntime();
            InputDeviceSelectionItemInstance2.Name = "InputDeviceSelectionItemInstance2";
            InputDeviceSelectionItemInstance3 = new InputDeviceSelectionItemRuntime();
            InputDeviceSelectionItemInstance3.Name = "InputDeviceSelectionItemInstance3";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            ContainerInstance1.Children.Add(TextInstance);
            ContainerInstance2.Children.Add(TextInstance1);
            this.Children.Add(ContainerInstance1);
            this.Children.Add(InputDeviceContainerInstance);
            this.Children.Add(ContainerInstance2);
            InputDeviceContainerInstance.Children.Add(InputDeviceSelectionItemInstance);
            InputDeviceContainerInstance.Children.Add(InputDeviceSelectionItemInstance1);
            InputDeviceContainerInstance.Children.Add(InputDeviceSelectionItemInstance2);
            InputDeviceContainerInstance.Children.Add(InputDeviceSelectionItemInstance3);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("ColorCategoryState", "Primary");
Background.SetProperty("StyleCategoryState", "Panel");

TextInstance.SetProperty("ColorCategoryState", "White");
TextInstance.SetProperty("StyleCategoryState", "H1");
            this.TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextInstance.Text = @"Press A / Space to Join";
            this.TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.X = 0f;
            this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
            this.TextInstance.Y = 0f;
            this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

TextInstance1.SetProperty("ColorCategoryState", "White");
TextInstance1.SetProperty("StyleCategoryState", "H1");
            this.TextInstance1.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextInstance1.Text = @"Press Start / Enter to Continue";
            this.TextInstance1.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance1.X = 0f;
            this.TextInstance1.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextInstance1.XUnits = GeneralUnitType.PixelsFromSmall;
            this.TextInstance1.Y = 0f;
            this.TextInstance1.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance1.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.ContainerInstance1.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            this.ContainerInstance1.Height = 31f;
            this.ContainerInstance1.Width = 0f;
            this.ContainerInstance1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.ContainerInstance1.X = 0f;
            this.ContainerInstance1.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.ContainerInstance1.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.ContainerInstance1.Y = 27f;
            this.ContainerInstance1.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.ContainerInstance1.YUnits = GeneralUnitType.PixelsFromSmall;

            this.InputDeviceContainerInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            this.InputDeviceContainerInstance.Height = 20f;
            this.InputDeviceContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.InputDeviceContainerInstance.StackSpacing = 4f;
            this.InputDeviceContainerInstance.Width = 0f;
            this.InputDeviceContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.InputDeviceContainerInstance.X = 0f;
            this.InputDeviceContainerInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.InputDeviceContainerInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.InputDeviceContainerInstance.Y = 89f;
            this.InputDeviceContainerInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.InputDeviceContainerInstance.YUnits = GeneralUnitType.PixelsFromSmall;

            this.ContainerInstance2.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            this.ContainerInstance2.Height = 31f;
            this.ContainerInstance2.Width = 0f;
            this.ContainerInstance2.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.ContainerInstance2.X = 0f;
            this.ContainerInstance2.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.ContainerInstance2.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.ContainerInstance2.Y = 228f;
            this.ContainerInstance2.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.ContainerInstance2.YUnits = GeneralUnitType.PixelsFromSmall;





        }
        partial void CustomInitialize();
    }
}
