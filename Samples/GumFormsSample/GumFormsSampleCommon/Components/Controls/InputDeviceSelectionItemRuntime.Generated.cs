//Code for Controls/InputDeviceSelectionItem (Container)
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
    public partial class InputDeviceSelectionItemRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/InputDeviceSelectionItem", typeof(InputDeviceSelectionItemRuntime));
        }
        public enum JoinedCategory
        {
            NoInputDevice,
            HasInputDevice,
        }

        JoinedCategory mJoinedCategoryState;
        public JoinedCategory JoinedCategoryState
        {
            get => mJoinedCategoryState;
            set
            {
                mJoinedCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case JoinedCategory.NoInputDevice:
                            this.IconInstance.IconCategoryState = IconRuntime.IconCategory.None;
                            this.RemoveDeviceButtonInstance.Visible = false;
                            this.TextInstance.Visible = false;
                            break;
                        case JoinedCategory.HasInputDevice:
                            this.IconInstance.IconCategoryState = IconRuntime.IconCategory.Gamepad;
                            this.RemoveDeviceButtonInstance.Visible = true;
                            this.TextInstance.Visible = true;
                            break;
                    }
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public IconRuntime IconInstance { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }
        public ButtonCloseRuntime RemoveDeviceButtonInstance { get; protected set; }

        public InputDeviceSelectionItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 113f;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
             

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
            IconInstance = new IconRuntime();
            IconInstance.Name = "IconInstance";
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            RemoveDeviceButtonInstance = new ButtonCloseRuntime();
            RemoveDeviceButtonInstance.Name = "RemoveDeviceButtonInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            this.Children.Add(IconInstance);
            this.Children.Add(TextInstance);
            this.Children.Add(RemoveDeviceButtonInstance);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("ColorCategoryState", "Primary");
Background.SetProperty("StyleCategoryState", "Panel");

this.IconInstance.IconCategoryState = IconRuntime.IconCategory.Gamepad;
            this.IconInstance.X = 0f;
            this.IconInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.IconInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.IconInstance.Y = 5f;
            this.IconInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.IconInstance.YUnits = GeneralUnitType.PixelsFromSmall;

TextInstance.SetProperty("ColorCategoryState", "White");
TextInstance.SetProperty("StyleCategoryState", "H2");
            this.TextInstance.Height = -43f;
            this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance.Text = @"Input Device Name Here With 3 Lines";
            this.TextInstance.TextOverflowHorizontalMode = global::RenderingLibrary.Graphics.TextOverflowHorizontalMode.EllipsisLetter;
            this.TextInstance.TextOverflowVerticalMode = global::RenderingLibrary.Graphics.TextOverflowVerticalMode.TruncateLine;
            this.TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.TextInstance.Width = 0f;
            this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.X = 0f;
            this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TextInstance.Y = 39f;
            this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.TextInstance.YUnits = GeneralUnitType.PixelsFromSmall;

            this.RemoveDeviceButtonInstance.Height = 22f;
            this.RemoveDeviceButtonInstance.Width = 22f;
            this.RemoveDeviceButtonInstance.X = -4f;
            this.RemoveDeviceButtonInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.RemoveDeviceButtonInstance.XUnits = GeneralUnitType.PixelsFromLarge;
            this.RemoveDeviceButtonInstance.Y = 4f;
            this.RemoveDeviceButtonInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.RemoveDeviceButtonInstance.YUnits = GeneralUnitType.PixelsFromSmall;

        }
        partial void CustomInitialize();
    }
}
