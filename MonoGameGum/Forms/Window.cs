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
#endif

#if !FRB
using Gum.Forms.Controls;
namespace Gum.Forms;

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
public class Window : Gum.Forms.Controls.FrameworkElement
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

    ResizeMode _resizeMode = ResizeMode.CanResize;
    public ResizeMode ResizeMode
    {
        get => _resizeMode;
        set
        {
            if (value != _resizeMode)
            {
                _resizeMode = value;
                AssignCursorsOnBorders();

            }
            ResizeModeChanged?.Invoke(this, EventArgs.Empty);
        }
    }


    GraphicalUiElement innerPanel;

    InteractiveGue? titleBar;

    InteractiveGue? _borderTopLeft;
    InteractiveGue? _borderTop;
    InteractiveGue? _borderTopRight;
    InteractiveGue? _borderLeft;
    InteractiveGue? _borderRight;
    InteractiveGue? _borderBottomLeft;
    InteractiveGue? _borderBottom;
    InteractiveGue? _borderBottomRight;

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

    public event EventHandler? ResizeModeChanged;

    #region Initialize Methods

    public Window() : base()
    {
        InitializeInternal();
    }
    public Window(InteractiveGue visual) : base(visual)
    {
        InitializeInternal();
    }

    private void InitializeInternal()
    {
        if(Visual != null)
        {
            // this allows the window to consume cursor events:
            this.Visual.Click += (_,_) => { };
        }
        ResizeMode = ResizeMode.CanResize;
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

            _borderTopLeft = Visual.GetGraphicalUiElementByName("BorderTopLeftInstance") as InteractiveGue;

            _borderTop = Visual.GetGraphicalUiElementByName("BorderTopInstance") as InteractiveGue;

            _borderTopRight = Visual.GetGraphicalUiElementByName("BorderTopRightInstance") as InteractiveGue;

            _borderLeft = Visual.GetGraphicalUiElementByName("BorderLeftInstance") as InteractiveGue;

            _borderRight = Visual.GetGraphicalUiElementByName("BorderRightInstance") as InteractiveGue;

            _borderBottomLeft = Visual.GetGraphicalUiElementByName("BorderBottomLeftInstance") as InteractiveGue;

            _borderBottom = Visual.GetGraphicalUiElementByName("BorderBottomInstance") as InteractiveGue;

            _borderBottomRight = Visual.GetGraphicalUiElementByName("BorderBottomRightInstance") as InteractiveGue;

            AssignCursorsOnBorders();

#if FULL_DIAGNOSTICS
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

    private void AssignCursorsOnBorders()
    {
        if(ResizeMode == ResizeMode.CanResize)
        {
            TryAssignCursor(_borderTopLeft, Cursors.SizeNWSE);
            TryAssignCursor(_borderTop, Cursors.SizeNS);
            TryAssignCursor(_borderTopRight, Cursors.SizeNESW);
            TryAssignCursor(_borderLeft, Cursors.SizeWE);
            TryAssignCursor(_borderRight, Cursors.SizeWE);
            TryAssignCursor(_borderBottomLeft, Cursors.SizeNESW);
            TryAssignCursor(_borderBottom, Cursors.SizeNS);
            TryAssignCursor(_borderBottomRight, Cursors.SizeNWSE);
        }
        else
        {
            TryAssignCursor(_borderTopLeft, null);
            TryAssignCursor(_borderTop, null);
            TryAssignCursor(_borderTopRight, null);
            TryAssignCursor(_borderLeft, null);
            TryAssignCursor(_borderRight, null);
            TryAssignCursor(_borderBottomLeft, null);
            TryAssignCursor(_borderBottom, null);
            TryAssignCursor(_borderBottomRight, null);
        }

            void TryAssignCursor(InteractiveGue? interactiveGue, Cursors? cursor)
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
                    {
                        var widthBefore = Visual.GetAbsoluteWidth();
                        Visual.Width -= difference;
                        var addedWidth = Visual.GetAbsoluteWidth() - widthBefore;
                        Visual.X -= addedWidth;
                    }
                    break;
                case global::RenderingLibrary.Graphics.HorizontalAlignment.Center:
                    {
                        var widthBefore = Visual.GetAbsoluteWidth();
                        Visual.Width -= difference;
                        var addedWidth = Visual.GetAbsoluteWidth() - widthBefore;
                        Visual.X -= addedWidth / 2f;
                    }
                    break;
                case global::RenderingLibrary.Graphics.HorizontalAlignment.Right:
                    Visual.Width -= difference;
                    break;
            }
            if (Visual.MinWidth != null)
            {
                Visual.Width = Math.Max(Visual.MinWidth.Value, Visual.Width);
            }
        }
        if (topGrabbedInOffset != null)
        {
            var desiredTop = cursorY - topGrabbedInOffset.Value;
            var difference = desiredTop - Visual.AbsoluteTop;

            switch (Visual.YOrigin)
            {
                case global::RenderingLibrary.Graphics.VerticalAlignment.Top:
                    {
                        var heightBefore = Visual.GetAbsoluteHeight();
                        Visual.Height -= difference;
                        var addedHeight = Visual.GetAbsoluteHeight() - heightBefore;
                        Visual.Y -= addedHeight;
                    }
                    break;
                case global::RenderingLibrary.Graphics.VerticalAlignment.Center:
                    {
                        var heightBefore = Visual.GetAbsoluteHeight();
                        Visual.Height -= difference;
                        var addedHeight = Visual.GetAbsoluteHeight() - heightBefore;
                        Visual.Y -= addedHeight / 2f;
                    }
                    break;
                case global::RenderingLibrary.Graphics.VerticalAlignment.Bottom:
                    Visual.Height -= difference;
                    break;
            }
            if (Visual.MinHeight != null)
            {
                Visual.Height = Math.Max(Visual.MinHeight.Value, Visual.Height);
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
                    {
                        var widthBefore = Visual.GetAbsoluteWidth();
                        Visual.Width += difference;
                        var addedWidth = Visual.GetAbsoluteWidth() - widthBefore;
                        Visual.X += addedWidth / 2f;
                    }

                    break;
                case global::RenderingLibrary.Graphics.HorizontalAlignment.Right:
                    {
                        var widthBefore = Visual.GetAbsoluteWidth();
                        Visual.Width += difference;
                        var addedWidth = Visual.GetAbsoluteWidth() - widthBefore;
                        Visual.X += addedWidth;
                    }
                    break;
            }
            if(Visual.MinWidth != null)
            {
                Visual.Width = Math.Max(Visual.MinWidth.Value, Visual.Width);
            }
        }
        if (bottomGrabbedInOffset != null)
        {
            // todo - finish here
            var desiredBottom = cursorY + bottomGrabbedInOffset.Value;
            var difference = desiredBottom - Visual.AbsoluteBottom;

            switch (Visual.YOrigin)
            {
                case global::RenderingLibrary.Graphics.VerticalAlignment.Top:
                    Visual.Height += difference;
                    break;
                case global::RenderingLibrary.Graphics.VerticalAlignment.Center:
                    {
                        var heightBefore = Visual.GetAbsoluteHeight();
                        Visual.Height += difference;
                        var addedHeight = Visual.GetAbsoluteHeight() - heightBefore;
                        Visual.Y += addedHeight / 2f;

                    }
                    break;
                case global::RenderingLibrary.Graphics.VerticalAlignment.Bottom:
                    {
                        var heightBefore = Visual.GetAbsoluteHeight();
                        Visual.Height += difference;
                        var addedHeight = Visual.GetAbsoluteHeight() - heightBefore;
                        Visual.Y += addedHeight;
                    }
                    break;
            }
            if(Visual.MinHeight != null)
            {
                Visual.Height = Math.Max(Visual.MinHeight.Value, Visual.Height);
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

            if (sender == _borderTopLeft || sender == _borderLeft || sender == _borderBottomLeft)
            {
                leftGrabbedInOffset = relativeToLeftIn;
            }
            else if (sender == _borderTopRight || sender == _borderRight || sender == _borderBottomRight)
            {
                rightGrabbedInOffset = relativeToRightIn;
            }

            if (sender == _borderTopLeft || sender == _borderTop || sender == _borderTopRight)
            {
                topGrabbedInOffset = relativeToTopIn;
            }
            else if (sender == _borderBottomLeft || sender == _borderBottom || sender == _borderBottomRight)
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
