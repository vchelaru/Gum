using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RenderingLibrary.Graphics;
using RenderingLibrary;




#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using MonoGameGum.Input;
using FlatRedBall.Forms.Controls;
namespace FlatRedBall.Forms;
#elif RAYLIB
using Gum.Forms.Controls;
namespace Gum.Forms;

#else
using MonoGameGum.Forms.Controls;
using MonoGameGum.Input;
namespace MonoGameGum.Forms;
#endif

#region ResizeMode Enum

/// <summary>
/// Values for an element's resize behavior.
/// </summary>
public enum ResizeMode
{
    /// <summary>
    /// Resizing using the cursor is not enabled
    /// </summary>
    NoResize,
    /// <summary>
    /// Resizing is enabled according to the enabled border instances.
    /// </summary>
    CanResize
}

#endregion

/// <summary>
/// A resizable, movable FrameworkElement
/// </summary>
public class Window : FrameworkElement
{
    public const string WindowCategoryName = "WindowCategory";

    List<FrameworkElement> _children = new List<FrameworkElement>();

    public float TitleHeight
    {
        get => titleBar?.Height ?? 0;
        set
        {
            if(titleBar != null)
            {
                titleBar.Height = value;
            }
        }
    }
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

    public Window() : base()
    {
        if(Visual != null)
        {
            // this allows the window to consume cursor events:
            this.Visual.Click += (_,_) => { };
        }
    }


    public Window(InteractiveGue visual) : base(visual)
    {
        if (Visual != null)
        {
            // this allows the window to consume cursor events:
            this.Visual.Click += (_, _) => { };
        }
    }

    protected override void ReactToVisualChanged()
    {
        RefreshInternalVisualReferences();
        base.ReactToVisualChanged();
    }

    protected override void RefreshInternalVisualReferences()
    {

        if (Visual != null)
        {
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

    float? titleGrabbedInXOffset;
    float? titleGrabbedInYOffset;

    #endregion

    private void HandleVisualDragging(object? sender, EventArgs e)
    {
        bool shouldConsiderMove = true;

        var cursorX = FrameworkElement.MainCursor.XRespectingGumZoomAndBounds();
        var cursorY = FrameworkElement.MainCursor.YRespectingGumZoomAndBounds();

        if(this.Visual.Parent != null)
        {
            // see if the cursor is over the parent, recursively
            shouldConsiderMove = IsOverParentRecursively(this.Visual.Parent,
                cursorX, cursorY);
        }

        ////////////////////////////Early Out/////////////////////////////
        if(!shouldConsiderMove)
        {
            return;
        }
        //////////////////////////End Early Out/////////////////////////////


        if (leftGrabbedInOffset != null)
        {
            var desiredLeft = cursorX - leftGrabbedInOffset.Value;
            var difference = desiredLeft - Visual.AbsoluteLeft;

            switch (Visual.XOrigin)
            {
                case global::RenderingLibrary.Graphics.HorizontalAlignment.Left:
                    Visual.X += difference;
                    Visual.Width -= difference;
                    break;
                case global::RenderingLibrary.Graphics.HorizontalAlignment.Center:
                    Visual.X += difference / 2f;
                    Visual.Width -= difference;
                    break;
                case global::RenderingLibrary.Graphics.HorizontalAlignment.Right:
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
                case global::RenderingLibrary.Graphics.VerticalAlignment.Top:
                    Visual.Y += difference;
                    Visual.Height -= difference;
                    break;
                case global::RenderingLibrary.Graphics.VerticalAlignment.Center:
                    Visual.Y += difference / 2f;
                    Visual.Height -= difference;
                    break;
                case global::RenderingLibrary.Graphics.VerticalAlignment.Bottom:
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
                case global::RenderingLibrary.Graphics.HorizontalAlignment.Left:
                    Visual.Width += difference;
                    break;
                case global::RenderingLibrary.Graphics.HorizontalAlignment.Center:
                    Visual.X += difference / 2f;
                    Visual.Width += difference;
                    break;
                case global::RenderingLibrary.Graphics.HorizontalAlignment.Right:
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
                case global::RenderingLibrary.Graphics.VerticalAlignment.Top:
                    Visual.Height += difference;
                    break;
                case global::RenderingLibrary.Graphics.VerticalAlignment.Center:
                    Visual.Y = Visual.Y + difference / 2f;
                    Visual.Height += difference;
                    break;
                case global::RenderingLibrary.Graphics.VerticalAlignment.Bottom:
                    Visual.Y += difference;
                    Visual.Height += difference;
                    break;
            }
        }

        if (titleGrabbedInXOffset != null)
        {
            var desiredLeft = cursorX - titleGrabbedInXOffset.Value;
            var desiredTop = cursorY - titleGrabbedInYOffset.Value;

            var differenceX = desiredLeft - Visual.AbsoluteLeft;
            var differenceY = desiredTop - Visual.AbsoluteTop;

            Visual.X += differenceX;
            Visual.Y += differenceY;
        }

    }

    private bool IsOverParentRecursively(IRenderableIpso item, float cursorX, float cursorY)
    {
        var isOver = false;
        var interactiveGue = item as InteractiveGue;
        if(interactiveGue?.RaiseChildrenEventsOutsideOfBounds == true)
        {
            isOver = true;
        }

        if(!isOver)
        {
            // bounds matter, so let's see if the cursor is inside:
            var left = item.GetAbsoluteLeft();
            var right = item.GetAbsoluteRight();
            var top = item.GetAbsoluteTop();
            var bottom = item.GetAbsoluteBottom();

            isOver = left <= cursorX && right >= cursorX &&
                top <= cursorY && bottom >= cursorY;
        }

        if(isOver)
        {
            var asGue = interactiveGue ?? item as GraphicalUiElement;

            var parent = asGue?.Parent;

            if(parent != null)
            {
                isOver = IsOverParentRecursively(parent, cursorX, cursorY);
            }
        }

        return isOver;
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

        titleGrabbedInXOffset = null;
        titleGrabbedInYOffset = null;

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
                // If the cursor is within the title, we can move:
                titleGrabbedInXOffset = cursorX - Visual.AbsoluteLeft;
                titleGrabbedInYOffset = cursorY - Visual.AbsoluteTop;
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


}
