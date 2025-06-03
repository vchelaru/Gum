using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGameGum.Input;


#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using FlatRedBall.Forms.Controls;
namespace FlatRedBall.Forms;
#else
using MonoGameGum.Forms.Controls;
namespace MonoGameGum.Forms;
#endif


public enum ResizeMode
{
    NoResize,
    CanResize
}

/// <summary>
/// A resizable, movable FrameworkElement
/// </summary>
public class Window : FrameworkElement
{
    public const string WindowCategoryName = "WindowCategory";

    List<FrameworkElement> _children = new List<FrameworkElement>();

    public float CaptionHeight { get; set; } = 36;
    public ResizeMode ResizeMode { get; set; } = ResizeMode.CanResize;


    GraphicalUiElement innerPanel;

    InteractiveGue? titleBar;

    InteractiveGue? borderTopLeft;
    InteractiveGue? borderTop;
    InteractiveGue? borderTopRight;
    InteractiveGue? borderLeft;
    InteractiveGue? borderRight;
    InteractiveGue? borderBottomLeft;
    InteractiveGue? borderBottom;
    InteractiveGue? borderBottomRight;

    public GraphicalUiElement InnerPanel => innerPanel;


    /// <summary>
    /// Returns a read-only list of the children FrameworkElements of this panel. 
    /// </summary>
    /// <remarks>
    /// This list is updated whenever the underlying Visual's Children change.
    /// This only contains FrameworkElements, so if a non-FrameworkElement Visual
    /// is added to this panel, it will not appear in this list.
    /// </remarks>
    public IReadOnlyList<FrameworkElement> Children
    {
        get => _children;
    }

    #region Initialize Methods

    public Window() : base() { }

    public Window(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        RefreshInternalVisualReferences();
        base.ReactToVisualChanged();
    }

    protected override void RefreshInternalVisualReferences()
    {

        if (Visual != null)
        {
            //Visual.Push += HandleVisualPush;
            //Visual.Dragging += HandleVisualDragging;
            innerPanel = Visual.GetGraphicalUiElementByName("InnerPanelInstance");

            titleBar = Visual.GetGraphicalUiElementByName("TitleBarInstance") as InteractiveGue;
            if (titleBar != null)
            {
                titleBar.Push += HandleVisualPush;
                titleBar.Dragging += HandleVisualDragging;
            }

            borderTopLeft = Visual.GetGraphicalUiElementByName("BorderTopLeftInstance") as InteractiveGue;
            TryAssignCursor(borderTopLeft, Cursors.SizeNWSE);

            borderTop = Visual.GetGraphicalUiElementByName("BorderTopInstance") as InteractiveGue;
            TryAssignCursor(borderTop, Cursors.SizeNS);

            borderTopRight = Visual.GetGraphicalUiElementByName("BorderTopRightInstance") as InteractiveGue;
            TryAssignCursor(borderTopRight, Cursors.SizeNESW);

            borderLeft = Visual.GetGraphicalUiElementByName("BorderLeftInstance") as InteractiveGue;
            TryAssignCursor(borderLeft, Cursors.SizeWE);

            borderRight = Visual.GetGraphicalUiElementByName("BorderRightInstance") as InteractiveGue;
            TryAssignCursor(borderRight, Cursors.SizeWE);

            borderBottomLeft = Visual.GetGraphicalUiElementByName("BorderBottomLeftInstance") as InteractiveGue;
            TryAssignCursor(borderBottomLeft, Cursors.SizeNESW);

            borderBottom = Visual.GetGraphicalUiElementByName("BorderBottomInstance") as InteractiveGue;
            TryAssignCursor(borderBottom, Cursors.SizeNS);

            borderBottomRight = Visual.GetGraphicalUiElementByName("BorderBottomRightInstance") as InteractiveGue;
            TryAssignCursor(borderBottomRight, Cursors.SizeNWSE);

            void TryAssignCursor(InteractiveGue? interactiveGue, Cursors cursor)
            {
                if (interactiveGue == null) return;

                interactiveGue.Push += HandleVisualPush;
                interactiveGue.Dragging += HandleVisualDragging;
                var frameworkElement = interactiveGue.FormsControlAsObject as FrameworkElement;
                if (frameworkElement != null)
                {
                    frameworkElement.CustomCursor = cursor;
                }
            }

#if DEBUG
            if (innerPanel == null)
            {
                throw new InvalidOperationException("Window Visual must contain a child named InnerPanelInstance");
            }
#endif

            Visual.ExposeChildrenEvents = true;

            // Note - if Visual is changed multiple times, this causes a slight
            // memory leak. However there is no way around this unless we have an
            // event for when the visual is removed.
            InnerPanel.Children.CollectionChanged += (s, e) =>
            {
                // When the children change, we need to update our internal list
                // to match the visual's children
                _children.Clear();
                foreach (var child in Visual.Children)
                {
                    if (child is InteractiveGue gue && gue.FormsControlAsObject is FrameworkElement frameworkElement)
                    {
                        _children.Add(frameworkElement);
                    }
                }
            };
        }

        base.ReactToVisualChanged();
    }


    float? leftGrabbedInOffset;
    float? topGrabbedInOffset;
    float? rightGrabbedInOffset;
    float? bottomGrabbedInOffset;

    float? captionGrabbedInXOffset;
    float? captionGrabbedInYOffset;


    private void HandleVisualDragging(object? sender, EventArgs e)
    {
        var cursorX = FrameworkElement.MainCursor.XRespectingGumZoomAndBounds();
        var cursorY = FrameworkElement.MainCursor.YRespectingGumZoomAndBounds();

        if (leftGrabbedInOffset != null)
        {
            var desiredLeft = cursorX - leftGrabbedInOffset.Value;
            var difference = desiredLeft - Visual.AbsoluteLeft;

            switch (Visual.XOrigin)
            {
                case RenderingLibrary.Graphics.HorizontalAlignment.Left:
                    Visual.X += difference;
                    Visual.Width -= difference;
                    break;
                case RenderingLibrary.Graphics.HorizontalAlignment.Center:
                    Visual.X += difference / 2f;
                    Visual.Width -= difference;
                    break;
                case RenderingLibrary.Graphics.HorizontalAlignment.Right:
                    Visual.Width -= difference;
                    break;
            }
        }
        if (topGrabbedInOffset != null)
        {
            var desiredTop = cursorY - topGrabbedInOffset.Value;
            var difference = desiredTop - Visual.AbsoluteTop;

            switch (Visual.YOrigin)
            {
                case RenderingLibrary.Graphics.VerticalAlignment.Top:
                    Visual.Y += difference;
                    Visual.Height -= difference;
                    break;
                case RenderingLibrary.Graphics.VerticalAlignment.Center:
                    Visual.Y += difference / 2f;
                    Visual.Height -= difference;
                    break;
                case RenderingLibrary.Graphics.VerticalAlignment.Bottom:
                    Visual.Height -= difference;
                    break;
            }
        }
        if (rightGrabbedInOffset != null)
        {
            var desiredRight = cursorX + rightGrabbedInOffset.Value;
            var difference = desiredRight - Visual.AbsoluteRight;

            switch (Visual.XOrigin)
            {
                case RenderingLibrary.Graphics.HorizontalAlignment.Left:
                    Visual.Width += difference;
                    break;
                case RenderingLibrary.Graphics.HorizontalAlignment.Center:
                    Visual.X += difference / 2f;
                    Visual.Width += difference;
                    break;
                case RenderingLibrary.Graphics.HorizontalAlignment.Right:
                    Visual.X += difference;
                    Visual.Width += difference;
                    break;
            }
        }
        if (bottomGrabbedInOffset != null)
        {
            var desiredBottom = cursorY + bottomGrabbedInOffset.Value;
            var difference = desiredBottom - Visual.AbsoluteBottom;

            switch (Visual.YOrigin)
            {
                case RenderingLibrary.Graphics.VerticalAlignment.Top:
                    Visual.Height += difference;
                    break;
                case RenderingLibrary.Graphics.VerticalAlignment.Center:
                    Visual.Y = Visual.Y + difference / 2f;
                    Visual.Height += difference;
                    break;
                case RenderingLibrary.Graphics.VerticalAlignment.Bottom:
                    Visual.Y += difference;
                    Visual.Height += difference;
                    break;
            }
        }

        if (captionGrabbedInXOffset != null)
        {
            var desiredLeft = cursorX - captionGrabbedInXOffset.Value;
            var desiredTop = cursorY - captionGrabbedInYOffset.Value;

            var differenceX = desiredLeft - Visual.AbsoluteLeft;
            var differenceY = desiredTop - Visual.AbsoluteTop;

            Visual.X += differenceX;
            Visual.Y += differenceY;
        }

    }
#if FRB
    private void HandleVisualPush(IWindow window)
#else
    private void HandleVisualPush(object? sender, EventArgs e)
#endif
    {
        leftGrabbedInOffset = null;
        topGrabbedInOffset = null;
        rightGrabbedInOffset = null;
        bottomGrabbedInOffset = null;

        captionGrabbedInXOffset = null;
        captionGrabbedInYOffset = null;



        var cursorX = FrameworkElement.MainCursor.XRespectingGumZoomAndBounds();
        var cursorY = FrameworkElement.MainCursor.YRespectingGumZoomAndBounds();

        // If the cursor is within the resize border, we can resize:
        if (ResizeMode == ResizeMode.CanResize)
        {
            // first check the edges:
            var relativeToLeftIn = cursorX - Visual.AbsoluteLeft;
            var relativeToTopIn = cursorY - Visual.AbsoluteTop;
            var relativeToRightIn = Visual.AbsoluteRight - cursorX;
            var relativeToBottomIn = Visual.AbsoluteBottom - cursorY;

            if (sender == borderTopLeft || sender == borderLeft || sender == borderBottomLeft)
            {
                leftGrabbedInOffset = relativeToLeftIn;
            }
            else if (sender == borderTopRight || sender == borderRight || sender == borderBottomRight)
            {
                rightGrabbedInOffset = relativeToRightIn;
            }

            if (sender == borderTopLeft || sender == borderTop || sender == borderTopRight)
            {
                topGrabbedInOffset = relativeToTopIn;
            }
            else if (sender == borderBottomLeft || sender == borderBottom || sender == borderBottomRight)
            {
                bottomGrabbedInOffset = relativeToBottomIn;
            }
        }


        var grabbedBorder = leftGrabbedInOffset.HasValue || topGrabbedInOffset.HasValue ||
            rightGrabbedInOffset.HasValue || bottomGrabbedInOffset.HasValue;

        if (!grabbedBorder)
        {
            if (sender == titleBar)
            {
                // If the cursor is within the caption, we can move:
                captionGrabbedInXOffset = cursorX - Visual.AbsoluteLeft;
                captionGrabbedInYOffset = cursorY - Visual.AbsoluteTop;
            }
        }

    }

    public override void AddChild(FrameworkElement child)
    {
        this.InnerPanel.Children.Add(child.Visual);
    }

    public override void AddChild(GraphicalUiElement child)
    {
        this.InnerPanel.Children.Add(child);
    }

    #endregion

}
