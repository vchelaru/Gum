//Code for Elements/DividerHorizontal (Container)
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
    public partial class DividerHorizontalRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Elements/DividerHorizontal", typeof(DividerHorizontalRuntime));
        }
        public SpriteRuntime AccentLeft { get; protected set; }
        public SpriteRuntime Line { get; protected set; }
        public SpriteRuntime AccentRight { get; protected set; }

        public DividerHorizontalRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
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
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            AccentLeft = new SpriteRuntime();
            AccentLeft.Name = "AccentLeft";
            Line = new SpriteRuntime();
            Line.Name = "Line";
            AccentRight = new SpriteRuntime();
            AccentRight.Name = "AccentRight";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(AccentLeft);
            this.Children.Add(Line);
            this.Children.Add(AccentRight);
        }
        private void ApplyDefaultVariables()
        {
AccentLeft.SetProperty("ColorCategoryState", "Gray");
            this.AccentLeft.Height = 100f;
            this.AccentLeft.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            this.AccentLeft.SourceFileName = @"UISpriteSheet.png";
            this.AccentLeft.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
            this.AccentLeft.TextureHeight = 3;
            this.AccentLeft.TextureLeft = 281;
            this.AccentLeft.TextureTop = 0;
            this.AccentLeft.TextureWidth = 3;
            this.AccentLeft.Width = 100f;

Line.SetProperty("ColorCategoryState", "Gray");
            this.Line.Height = 100f;
            this.Line.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            this.Line.SourceFileName = @"UISpriteSheet.png";
            this.Line.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
            this.Line.TextureHeight = 1;
            this.Line.TextureLeft = 281;
            this.Line.TextureTop = 1;
            this.Line.TextureWidth = 3;
            this.Line.Width = -8f;
            this.Line.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
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
            this.AccentRight.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.AccentRight.XUnits = GeneralUnitType.PixelsFromLarge;

        }
        partial void CustomInitialize();
    }
}
