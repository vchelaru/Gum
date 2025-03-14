//Code for Controls/DialogBox (Container)
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
    public partial class DialogBoxRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/DialogBox", typeof(DialogBoxRuntime));
        }
        public NineSliceRuntime NineSliceInstance { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }
        public IconRuntime ContinueIndicatorInstance { get; protected set; }

        public DialogBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

             
            this.Height = 128f;
             
            this.Width = 256f;

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
            NineSliceInstance = new NineSliceRuntime();
            NineSliceInstance.Name = "NineSliceInstance";
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            ContinueIndicatorInstance = new IconRuntime();
            ContinueIndicatorInstance.Name = "ContinueIndicatorInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(NineSliceInstance);
            this.Children.Add(TextInstance);
            this.Children.Add(ContinueIndicatorInstance);
        }
        private void ApplyDefaultVariables()
        {
NineSliceInstance.SetProperty("ColorCategoryState", "Primary");
NineSliceInstance.SetProperty("StyleCategoryState", "Panel");

TextInstance.SetProperty("ColorCategoryState", "White");
TextInstance.SetProperty("StyleCategoryState", "Normal");
            this.TextInstance.Height = -32f;
            this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.Text = @"This is a dialog box. This text will be displayed one character at a time. Typically a dialog box is added to a Screen such as the GameScreen, but it defaults to being invisible.";
            this.TextInstance.TextOverflowVerticalMode = global::RenderingLibrary.Graphics.TextOverflowVerticalMode.TruncateLine;
            this.TextInstance.Width = -16f;
            this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TextInstance.Y = 8f;

this.ContinueIndicatorInstance.IconCategoryState = IconRuntime.IconCategory.Arrow2;
            this.ContinueIndicatorInstance.Height = 24f;
            this.ContinueIndicatorInstance.Rotation = -90f;
            this.ContinueIndicatorInstance.Width = 24f;
            this.ContinueIndicatorInstance.X = -8f;
            this.ContinueIndicatorInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.ContinueIndicatorInstance.XUnits = GeneralUnitType.PixelsFromLarge;
            this.ContinueIndicatorInstance.Y = -8f;
            this.ContinueIndicatorInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.ContinueIndicatorInstance.YUnits = GeneralUnitType.PixelsFromLarge;

        }
        partial void CustomInitialize();
    }
}
