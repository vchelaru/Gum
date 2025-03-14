//Code for Controls/ScrollViewer (Container)
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
    public partial class ScrollViewerRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/ScrollViewer", typeof(ScrollViewerRuntime));
        }
        public MonoGameGum.Forms.Controls.ScrollViewer FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.ScrollViewer;
        public enum ScrollBarVisibility
        {
            NoScrollBar,
            VerticalScrollVisible,
        }

        ScrollBarVisibility mScrollBarVisibilityState;
        public ScrollBarVisibility ScrollBarVisibilityState
        {
            get => mScrollBarVisibilityState;
            set
            {
                mScrollBarVisibilityState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case ScrollBarVisibility.NoScrollBar:
                            this.VerticalScrollBarInstance.Visible = false;
                            break;
                        case ScrollBarVisibility.VerticalScrollVisible:
                            this.VerticalScrollBarInstance.Visible = true;
                            break;
                    }
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public ScrollBarRuntime VerticalScrollBarInstance { get; protected set; }
        public ContainerRuntime ClipContainerInstance { get; protected set; }
        public ContainerRuntime InnerPanelInstance { get; protected set; }
        public ListBoxItemRuntime ListBoxItemInstance { get; protected set; }

        public ScrollViewerRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

             

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            if(tryCreateFormsObject)
            {
                if (FormsControl == null)
                {
                    FormsControlAsObject = new MonoGameGum.Forms.Controls.ScrollViewer(this);
                }
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            Background = new NineSliceRuntime();
            Background.Name = "Background";
            VerticalScrollBarInstance = new ScrollBarRuntime();
            VerticalScrollBarInstance.Name = "VerticalScrollBarInstance";
            ClipContainerInstance = new ContainerRuntime();
            ClipContainerInstance.Name = "ClipContainerInstance";
            InnerPanelInstance = new ContainerRuntime();
            InnerPanelInstance.Name = "InnerPanelInstance";
            ListBoxItemInstance = new ListBoxItemRuntime();
            ListBoxItemInstance.Name = "ListBoxItemInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            this.Children.Add(VerticalScrollBarInstance);
            this.Children.Add(ClipContainerInstance);
            ClipContainerInstance.Children.Add(InnerPanelInstance);
            InnerPanelInstance.Children.Add(ListBoxItemInstance);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("ColorCategoryState", "DarkGray");
Background.SetProperty("StyleCategoryState", "Bordered");
            this.Background.Height = 0f;
            this.Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Background.Width = 0f;
            this.Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Background.X = 0f;
            this.Background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.Background.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.Background.Y = 0f;
            this.Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Background.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.VerticalScrollBarInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.VerticalScrollBarInstance.XUnits = GeneralUnitType.PixelsFromLarge;
            this.VerticalScrollBarInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.VerticalScrollBarInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.ClipContainerInstance.ClipsChildren = true;
            this.ClipContainerInstance.Height = -4f;
            this.ClipContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ClipContainerInstance.Width = -27f;
            this.ClipContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ClipContainerInstance.X = 2f;
            this.ClipContainerInstance.Y = 2f;
            this.ClipContainerInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.ClipContainerInstance.YUnits = GeneralUnitType.PixelsFromSmall;

            this.InnerPanelInstance.Height = 0f;
            this.InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.InnerPanelInstance.Width = 0f;
            this.InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

this.ListBoxItemInstance.ListBoxItemCategoryState = ListBoxItemRuntime.ListBoxItemCategory.Highlighted;

        }
        partial void CustomInitialize();
    }
}
