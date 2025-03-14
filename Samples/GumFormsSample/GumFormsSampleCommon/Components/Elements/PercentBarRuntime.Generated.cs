//Code for Elements/PercentBar (Container)
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
    public partial class PercentBarRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Elements/PercentBar", typeof(PercentBarRuntime));
        }
        public enum BarDecorCategory
        {
            None,
            CautionLines,
            VerticalLines,
        }

        BarDecorCategory mBarDecorCategoryState;
        public BarDecorCategory BarDecorCategoryState
        {
            get => mBarDecorCategoryState;
            set
            {
                mBarDecorCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case BarDecorCategory.None:
                            this.CautionLinesInstance.Visible = false;
                            this.VerticalLinesInstance.Visible = false;
                            break;
                        case BarDecorCategory.CautionLines:
                            this.CautionLinesInstance.Visible = true;
                            this.VerticalLinesInstance.Visible = false;
                            break;
                        case BarDecorCategory.VerticalLines:
                            this.CautionLinesInstance.Visible = false;
                            this.VerticalLinesInstance.Visible = true;
                            break;
                    }
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public NineSliceRuntime BarContainer { get; protected set; }
        public NineSliceRuntime Bar { get; protected set; }
        public CautionLinesRuntime CautionLinesInstance { get; protected set; }
        public VerticalLinesRuntime VerticalLinesInstance { get; protected set; }

        public string BarColor
        {
            set => Bar.SetProperty("ColorCategoryState", value?.ToString());
        }

        public float BarPercent
        {
            get => Bar.Width;
            set => Bar.Width = value;
        }

        public PercentBarRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 16f;
             
            this.Width = 128f;

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
            BarContainer = new NineSliceRuntime();
            BarContainer.Name = "BarContainer";
            Bar = new NineSliceRuntime();
            Bar.Name = "Bar";
            CautionLinesInstance = new CautionLinesRuntime();
            CautionLinesInstance.Name = "CautionLinesInstance";
            VerticalLinesInstance = new VerticalLinesRuntime();
            VerticalLinesInstance.Name = "VerticalLinesInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            this.Children.Add(BarContainer);
            BarContainer.Children.Add(Bar);
            Bar.Children.Add(CautionLinesInstance);
            Bar.Children.Add(VerticalLinesInstance);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("ColorCategoryState", "DarkGray");
Background.SetProperty("StyleCategoryState", "Bordered");

BarContainer.SetProperty("ColorCategoryState", "Black");
BarContainer.SetProperty("StyleCategoryState", "Solid");
            this.BarContainer.Height = -4f;
            this.BarContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.BarContainer.Width = -4f;
            this.BarContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.BarContainer.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.BarContainer.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.BarContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.BarContainer.YUnits = GeneralUnitType.PixelsFromMiddle;

Bar.SetProperty("ColorCategoryState", "Primary");
Bar.SetProperty("StyleCategoryState", "Solid");
            this.Bar.Width = 25f;
            this.Bar.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Bar.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.Bar.XUnits = GeneralUnitType.PixelsFromSmall;

CautionLinesInstance.SetProperty("LineColor", "Black");
            this.CautionLinesInstance.Height = 0f;
            this.CautionLinesInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.CautionLinesInstance.LineAlpha = 50;
            this.CautionLinesInstance.Visible = false;
            this.CautionLinesInstance.Width = 0f;
            this.CautionLinesInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

VerticalLinesInstance.SetProperty("LineColor", "Black");
            this.VerticalLinesInstance.Height = 0f;
            this.VerticalLinesInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.VerticalLinesInstance.LineAlpha = 50;
            this.VerticalLinesInstance.Visible = false;
            this.VerticalLinesInstance.Width = 0f;
            this.VerticalLinesInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

        }
        partial void CustomInitialize();
    }
}
