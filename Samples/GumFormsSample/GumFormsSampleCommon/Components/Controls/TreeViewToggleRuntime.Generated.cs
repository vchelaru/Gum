//Code for Controls/TreeViewToggle (Container)
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
    public partial class TreeViewToggleRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/TreeViewToggle", typeof(TreeViewToggleRuntime));
        }
        public enum ToggleCategory
        {
            EnabledOn,
            EnabledOff,
            DisabledOn,
            DisabledOff,
            HighlightedOn,
            HighlightedOff,
            PushedOn,
            PushedOff,
        }

        ToggleCategory mToggleCategoryState;
        public ToggleCategory ToggleCategoryState
        {
            get => mToggleCategoryState;
            set
            {
                mToggleCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case ToggleCategory.EnabledOn:
                            IconInstance.SetProperty("IconColor", "White");
                            this.IconInstance.Rotation = -90f;
                            this.IconInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                            this.IconInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
                            NineSliceInstance.SetProperty("ColorCategoryState", "Primary");
                            break;
                        case ToggleCategory.EnabledOff:
                            IconInstance.SetProperty("IconColor", "White");
                            this.IconInstance.Rotation = 0f;
                            this.IconInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                            this.IconInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                            NineSliceInstance.SetProperty("ColorCategoryState", "Primary");
                            break;
                        case ToggleCategory.DisabledOn:
                            IconInstance.SetProperty("IconColor", "Gray");
                            this.IconInstance.Rotation = -90f;
                            this.IconInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                            this.IconInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
                            NineSliceInstance.SetProperty("ColorCategoryState", "DarkGray");
                            break;
                        case ToggleCategory.DisabledOff:
                            IconInstance.SetProperty("IconColor", "Gray");
                            this.IconInstance.Rotation = 0f;
                            this.IconInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                            this.IconInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                            NineSliceInstance.SetProperty("ColorCategoryState", "DarkGray");
                            break;
                        case ToggleCategory.HighlightedOn:
                            IconInstance.SetProperty("IconColor", "White");
                            this.IconInstance.Rotation = -90f;
                            this.IconInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                            this.IconInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
                            NineSliceInstance.SetProperty("ColorCategoryState", "PrimaryLight");
                            break;
                        case ToggleCategory.HighlightedOff:
                            IconInstance.SetProperty("IconColor", "White");
                            this.IconInstance.Rotation = 0f;
                            this.IconInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                            this.IconInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                            NineSliceInstance.SetProperty("ColorCategoryState", "PrimaryLight");
                            break;
                        case ToggleCategory.PushedOn:
                            IconInstance.SetProperty("IconColor", "White");
                            this.IconInstance.Rotation = -90f;
                            this.IconInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                            this.IconInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
                            NineSliceInstance.SetProperty("ColorCategoryState", "PrimaryDark");
                            break;
                        case ToggleCategory.PushedOff:
                            IconInstance.SetProperty("IconColor", "White");
                            this.IconInstance.Rotation = 0f;
                            this.IconInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                            this.IconInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                            NineSliceInstance.SetProperty("ColorCategoryState", "PrimaryDark");
                            break;
                    }
                }
            }
        }
        public NineSliceRuntime NineSliceInstance { get; protected set; }
        public IconRuntime IconInstance { get; protected set; }

        public TreeViewToggleRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 24f;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
             
            this.Width = 24f;
            this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;

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
            IconInstance = new IconRuntime();
            IconInstance.Name = "IconInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(NineSliceInstance);
            NineSliceInstance.Children.Add(IconInstance);
        }
        private void ApplyDefaultVariables()
        {
NineSliceInstance.SetProperty("ColorCategoryState", "Primary");
NineSliceInstance.SetProperty("StyleCategoryState", "Bordered");

this.IconInstance.IconCategoryState = IconRuntime.IconCategory.Arrow2;
            this.IconInstance.Height = 0f;
            this.IconInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.IconInstance.Width = 0f;
            this.IconInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

        }
        partial void CustomInitialize();
    }
}
