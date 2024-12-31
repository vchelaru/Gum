//Code for Popup (Container)
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
    public partial class PopupRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Popup", typeof(PopupRuntime));
        }
        public NineSliceRuntime NineSliceInstance { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }

        public PopupRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {

                this.Height = 176f;
                 
                this.Width = 272f;
                this.X = 0f;
                this.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                this.XUnits = GeneralUnitType.PixelsFromMiddle;
                this.Y = 0f;
                this.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                this.YUnits = GeneralUnitType.PixelsFromMiddle;

                InitializeInstances();

                ApplyDefaultVariables();
                AssignParents();
                CustomInitialize();
            }
        }
        protected virtual void InitializeInstances()
        {
            NineSliceInstance = new NineSliceRuntime();
            NineSliceInstance.Name = "NineSliceInstance";
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(NineSliceInstance);
            NineSliceInstance.Children.Add(TextInstance);
        }
        private void ApplyDefaultVariables()
        {
            this.NineSliceInstance.Blue = 137;
            this.NineSliceInstance.Green = 17;
            this.NineSliceInstance.Height = 0f;
            this.NineSliceInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.NineSliceInstance.Red = 0;
            this.NineSliceInstance.SourceFileName = "examplespriteframe.png";
            this.NineSliceInstance.Width = 0f;
            this.NineSliceInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.NineSliceInstance.X = 0f;
            this.NineSliceInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.NineSliceInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.NineSliceInstance.Y = 0f;
            this.NineSliceInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.NineSliceInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.TextInstance.Height = 0f;
            this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextInstance.Text = "This is text inside of a popup. This text wraps automatically.";
            this.TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.TextInstance.Width = -16f;
            this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.X = 0f;
            this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TextInstance.Y = 8f;
            this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.TextInstance.YUnits = GeneralUnitType.PixelsFromSmall;

        }
        partial void CustomInitialize();
    }
}
