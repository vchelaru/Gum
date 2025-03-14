//Code for Elements/DividerVertical (Container)
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
    public partial class DividerVerticalRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Elements/DividerVertical", typeof(DividerVerticalRuntime));
        }
        public SpriteRuntime AccentTop { get; protected set; }
        public SpriteRuntime Line { get; protected set; }
        public SpriteRuntime AccentRight { get; protected set; }

        public DividerVerticalRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 128f;
             
            this.Width = 0f;
            this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

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
            AccentTop = new SpriteRuntime();
            AccentTop.Name = "AccentTop";
            Line = new SpriteRuntime();
            Line.Name = "Line";
            AccentRight = new SpriteRuntime();
            AccentRight.Name = "AccentRight";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(AccentTop);
            this.Children.Add(Line);
            this.Children.Add(AccentRight);
        }
        private void ApplyDefaultVariables()
        {
AccentTop.SetProperty("ColorCategoryState", "Gray");
            this.AccentTop.Height = 100f;
            this.AccentTop.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            this.AccentTop.SourceFileName = @"UISpriteSheet.png";
            this.AccentTop.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
            this.AccentTop.TextureHeight = 3;
            this.AccentTop.TextureLeft = 281;
            this.AccentTop.TextureTop = 0;
            this.AccentTop.TextureWidth = 3;
            this.AccentTop.Width = 100f;
            this.AccentTop.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.AccentTop.XUnits = GeneralUnitType.PixelsFromMiddle;

Line.SetProperty("ColorCategoryState", "Gray");
            this.Line.Height = -8f;
            this.Line.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Line.SourceFileName = @"UISpriteSheet.png";
            this.Line.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
            this.Line.TextureHeight = 1;
            this.Line.TextureLeft = 281;
            this.Line.TextureTop = 1;
            this.Line.TextureWidth = 3;
            this.Line.Width = 1f;
            this.Line.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.Line.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.Line.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.Line.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Line.YUnits = GeneralUnitType.PixelsFromMiddle;

AccentRight.SetProperty("ColorCategoryState", "Gray");
            this.AccentRight.Height = 100f;
            this.AccentRight.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            this.AccentRight.SourceFileName = @"UISpriteSheet.png";
            this.AccentRight.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
            this.AccentRight.TextureHeight = 3;
            this.AccentRight.TextureLeft = 281;
            this.AccentRight.TextureTop = 0;
            this.AccentRight.TextureWidth = 3;
            this.AccentRight.Width = 100f;
            this.AccentRight.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.AccentRight.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.AccentRight.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
            this.AccentRight.YUnits = GeneralUnitType.PixelsFromLarge;

        }
        partial void CustomInitialize();
    }
}
