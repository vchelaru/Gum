using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.Controls
{
    #region Enums

    public enum ScrollBarVisibility
    {
        /// <summary>
        /// The ScrollBar will display only if needed based on the size of the inner panel
        /// </summary>
        Auto = 1,
        /// <summary>
        /// The ScrollBar will remain invisible even if the contents of the inner panel exceed the size of its container
        /// </summary>
        Hidden = 2,
        /// <summary>
        /// The ScrollBar will always display
        /// </summary>
        Visible = 3
    }

    #endregion


    public class ScrollViewer : FrameworkElement
    {
        public const string VerticalScrollBarInstanceName = "VerticalScrollBarInstance";


        #region Fields/Properties

        bool reactToInnerPanelPositionOrSizeChanged = true;

        protected ScrollBar verticalScrollBar;

        GraphicalUiElement innerPanel;
        public GraphicalUiElement InnerPanel => innerPanel;

        protected GraphicalUiElement clipContainer;

        ScrollBarVisibility verticalScrollBarVisibility = ScrollBarVisibility.Auto;
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get
            {
                return verticalScrollBarVisibility;
            }
            set
            {
                if (value != verticalScrollBarVisibility)
                {
                    verticalScrollBarVisibility = value;
                    UpdateVerticalScrollBarValues();
                }
            }
        }

        #endregion

        #region Initialize

        public ScrollViewer() : base() { }

        public ScrollViewer(InteractiveGue visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            var scrollBarVisualAsGue = Visual.GetGraphicalUiElementByName(VerticalScrollBarInstanceName);
#if DEBUG
            if(scrollBarVisualAsGue == null)
            {
                throw new InvalidOperationException($"Could not find a child with the name {VerticalScrollBarInstanceName}");
            }
#endif
            var scrollBarVisual = scrollBarVisualAsGue as InteractiveGue;

#if DEBUG
            if(scrollBarVisual == null)
            {
                throw new InvalidOperationException($"The child with the name {VerticalScrollBarInstanceName} was found, but is not an InteractiveGue." +
                    $" Did you forget to set forms associations for this type?");
            }
#endif

            if (scrollBarVisual.FormsControlAsObject == null)
            {
                verticalScrollBar = new ScrollBar(scrollBarVisual);
            }
            else
            {
                verticalScrollBar = scrollBarVisual.FormsControlAsObject as ScrollBar;
            }
            verticalScrollBar.ValueChanged += HandleVerticalScrollBarValueChanged;
            // Depending on the height and width units, the scroll bar may get its update
            // called before or after this. We can't bet on the order, so we have to handle
            // both this and the scroll bar's height value changes, and adjust according to both:
            var thumbVisual =
                verticalScrollBar.Visual.GetGraphicalUiElementByName("ThumbInstance");
            verticalScrollBar.Visual.SizeChanged += HandleVerticalScrollBarThumbSizeChanged;


            innerPanel = Visual.GetGraphicalUiElementByName("InnerPanelInstance");
            innerPanel.SizeChanged += HandleInnerPanelSizeChanged;
            innerPanel.PositionChanged += HandleInnerPanelPositionChanged;
            clipContainer = Visual.GetGraphicalUiElementByName("ClipContainerInstance");

            Visual.MouseWheelScroll += HandleMouseWheelScroll;
            Visual.RollOverBubbling += HandleRollOver;
            Visual.SizeChanged += HandleVisualSizeChanged;

            UpdateVerticalScrollBarValues();

            base.ReactToVisualChanged();
        }

        protected override void ReactToVisualRemoved()
        {
            if (Visual != null)
            {
                Visual.MouseWheelScroll -= HandleMouseWheelScroll;
                Visual.RollOverBubbling -= HandleRollOver;
                Visual.SizeChanged -= HandleVisualSizeChanged;

                innerPanel.SizeChanged -= HandleInnerPanelSizeChanged;
                innerPanel.PositionChanged -= HandleInnerPanelPositionChanged;

                verticalScrollBar.Visual.SizeChanged -= HandleVerticalScrollBarThumbSizeChanged;
                verticalScrollBar.ValueChanged -= HandleVerticalScrollBarValueChanged;

            }

            base.ReactToVisualRemoved();
        }

        private void HandleRollOver(object sender, RoutedEventArgs args)
        {
            var cursor = MainCursor;
            var isTouchScreen = false;
            if (cursor.PrimaryDown && isTouchScreen)
            {
                verticalScrollBar.Value -= cursor.YChange /
                    global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom;
                args.Handled = true;
            }
        }

        private void HandleMouseWheelScroll(object sender, RoutedEventArgs args)
        {
            var cursor = MainCursor;
            var valueBefore = verticalScrollBar.Value;
            // Do we want to use the small change? Or have some separate value that the user can set?
            verticalScrollBar.Value -= cursor.ScrollWheelChange * verticalScrollBar.SmallChange;

            args.Handled = verticalScrollBar.Value != valueBefore;
        }


        #endregion

        #region Scroll Methods

        public void ScrollToBottom()
        {
            verticalScrollBar.Value = verticalScrollBar.Maximum;
        }

        #endregion

        #region Event Handlers

        private void HandleVerticalScrollBarValueChanged(object sender, EventArgs e)
        {
            reactToInnerPanelPositionOrSizeChanged = false;
            innerPanel.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            innerPanel.Y = -(float)verticalScrollBar.Value;
            reactToInnerPanelPositionOrSizeChanged = true;
        }

        private void HandleInnerPanelSizeChanged(object sender, EventArgs e)
        {
            if (reactToInnerPanelPositionOrSizeChanged)
            {
                UpdateVerticalScrollBarValues();
            }
        }

        private void HandleVisualSizeChanged(object sender, EventArgs args)
        {
            if (reactToInnerPanelPositionOrSizeChanged)
            {
                UpdateVerticalScrollBarValues();
            }
        }

        private void HandleVerticalScrollBarThumbSizeChanged(object sender, EventArgs args)
        {
            if (reactToInnerPanelPositionOrSizeChanged)
            {
                UpdateVerticalScrollBarValues();
            }
        }

        private void HandleInnerPanelPositionChanged(object sender, EventArgs e)
        {
            if (reactToInnerPanelPositionOrSizeChanged)
            {
                UpdateVerticalScrollBarValues();
            }
        }

        #endregion

        #region UpdateTo methods

        // Currently this is public because Gum objects don't have events
        // when positions and sizes change. Eventually, we'll have this all
        // handled internally and this can be made private.
        public void UpdateVerticalScrollBarValues()
        {
            verticalScrollBar.Minimum = 0;
            verticalScrollBar.ViewportSize = clipContainer.GetAbsoluteHeight();

            var innerPanelHeight = innerPanel.GetAbsoluteHeight();
            var clipContainerHeight = clipContainer.GetAbsoluteHeight();
            var maxValue = innerPanelHeight - clipContainerHeight;

            maxValue = System.Math.Max(0, maxValue);

            verticalScrollBar.Maximum = maxValue;

            verticalScrollBar.SmallChange = 10;
            verticalScrollBar.LargeChange = verticalScrollBar.ViewportSize;

            switch (verticalScrollBarVisibility)
            {
                case ScrollBarVisibility.Hidden:
                    verticalScrollBar.IsVisible = false;
                    break;
                case ScrollBarVisibility.Visible:
                    verticalScrollBar.IsVisible = true;
                    break;
                case ScrollBarVisibility.Auto:
                    verticalScrollBar.IsVisible = innerPanelHeight > clipContainerHeight;
                    break;
            }

            string state = verticalScrollBar.IsVisible ?
                "VerticalScrollVisible" :
                "NoScrollBar";

            const string category = "ScrollBarVisibilityState";



            Visual.SetProperty(category, state);
        }


        #endregion

    }
}
