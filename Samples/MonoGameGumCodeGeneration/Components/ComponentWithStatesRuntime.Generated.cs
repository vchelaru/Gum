//Code for ComponentWithStates (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using MonoGameGum.GueDeriving;
using MonoGameGumCodeGeneration.Components;
using System.Linq;
namespace MonoGameGumCodeGeneration.Components
{
    public partial class ComponentWithStatesRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("ComponentWithStates", typeof(ComponentWithStatesRuntime));
        }
        public enum ColorCategory
        {
            RedState,
            GreenState,
            BlueState,
        }

        ColorCategory mColorCategoryState;
        public ColorCategory ColorCategoryState
        {
            get => mColorCategoryState;
            set
            {
                mColorCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case ColorCategory.RedState:
                            this.ColoredRectangleInstance.Blue = 0;
                            this.ColoredRectangleInstance.Green = 0;
                            this.ColoredRectangleInstance.Red = 255;
                            break;
                        case ColorCategory.GreenState:
                            this.ColoredRectangleInstance.Blue = 0;
                            this.ColoredRectangleInstance.Green = 255;
                            this.ColoredRectangleInstance.Red = 0;
                            break;
                        case ColorCategory.BlueState:
                            this.ColoredRectangleInstance.Blue = 255;
                            this.ColoredRectangleInstance.Green = 0;
                            this.ColoredRectangleInstance.Red = 0;
                            break;
                    }
                }
            }
        }
        public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }

        public ComponentWithStatesRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {

                 

                InitializeInstances();

                ApplyDefaultVariables();
                AssignParents();
                CustomInitialize();
            }
        }
        protected virtual void InitializeInstances()
        {
            ColoredRectangleInstance = new ColoredRectangleRuntime();
            ColoredRectangleInstance.Name = "ColoredRectangleInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(ColoredRectangleInstance);
        }
        private void ApplyDefaultVariables()
        {
            this.ColoredRectangleInstance.Height = 0f;
            this.ColoredRectangleInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ColoredRectangleInstance.Width = 0f;
            this.ColoredRectangleInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ColoredRectangleInstance.X = 0f;
            this.ColoredRectangleInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.ColoredRectangleInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.ColoredRectangleInstance.Y = 0f;
            this.ColoredRectangleInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.ColoredRectangleInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

        }
        partial void CustomInitialize();
    }
}
