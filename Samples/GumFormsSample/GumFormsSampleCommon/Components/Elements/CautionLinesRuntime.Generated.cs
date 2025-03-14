//Code for Elements/CautionLines (Container)
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
    public partial class CautionLinesRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Elements/CautionLines", typeof(CautionLinesRuntime));
        }
        public SpriteRuntime LinesSprite { get; protected set; }

        public int LineAlpha
        {
            get => LinesSprite.Alpha;
            set => LinesSprite.Alpha = value;
        }

        public string LineColor
        {
            set => LinesSprite.SetProperty("ColorCategoryState", value?.ToString());
        }

        public CautionLinesRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.ClipsChildren = true;
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
            LinesSprite = new SpriteRuntime();
            LinesSprite.Name = "LinesSprite";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(LinesSprite);
        }
        private void ApplyDefaultVariables()
        {
            this.LinesSprite.SourceFileName = @"UISpriteSheet.png";
            this.LinesSprite.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
            this.LinesSprite.TextureHeight = 32;
            this.LinesSprite.TextureLeft = 0;
            this.LinesSprite.TextureTop = 992;
            this.LinesSprite.TextureWidth = 1024;
            this.LinesSprite.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.LinesSprite.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.LinesSprite.YUnits = GeneralUnitType.PixelsFromMiddle;

        }
        partial void CustomInitialize();
    }
}
