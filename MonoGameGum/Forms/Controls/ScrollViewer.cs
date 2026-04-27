using Gum.Wireframe;
using System;
using System.Collections.Generic;




#if FRB
using Microsoft.Xna.Framework.Input;
using FlatRedBall.Forms.Input;
using MonoGameGum.Forms.Controls;
using GamepadButton = FlatRedBall.Input.Xbox360GamePad.Button;

using FlatRedBall.Gui;
using FlatRedBall.Input;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using Buttons = FlatRedBall.Input.Xbox360GamePad.Button;
namespace FlatRedBall.Forms.Controls;
#elif XNALIKE
using GamepadButton = Microsoft.Xna.Framework.Input.Buttons;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Input;
#else
using Gum.Input;
using GamepadButton = Gum.Input.GamepadButton;
#endif

#if !FRB
namespace Gum.Forms.Controls;

#endif

#region Enums

public enum ScrollBarVisibility
{
    /// <summary>
    /// The ScrollBar displays only if needed based on the size of the inner panel
    /// </summary>
    Auto = 1,
    /// <summary>
    /// The ScrollBar remains invisible even if the contents of the inner panel exceed the size of its container
    /// </summary>
    Hidden = 2,
    /// <summary>
    /// The ScrollBar always displays
    /// </summary>
    Visible = 3
}

#endregion

#region ScrollChangedEventArgs

public class ScrollChangedEventArgs : EventArgs
{
    // todo - may expand this in the future, but creating this now
    // to future proof event handlers
}

#endregion

/// <summary>
/// A control for displaying stacked items with a scroll bar. Items can be
/// FrameworkElements (Forms) or regular visuals (GraphicalUiElements).
/// </summary>
public class ScrollViewer :
#if FRB
    FrameworkElement,
#else
    Gum.Forms.Controls.FrameworkElement, 
#endif

    IInputReceiver
{
    public const string VerticalScrollBarInstanceName = "VerticalScrollBarInstance";
    public const string HorizontalScrollBarInstanceName = "HorizontalScrollBarInstance";

    /// <summary>
    /// Name of the optional overlay container that hosts sticky-header visuals
    /// in the underlying Visual.
    /// </summary>
    public const string StickyHeaderOverlayInstanceName = "StickyHeaderOverlayInstance";

    public const string ScrollViewerCategoryName = "ScrollViewerCategory";
    public const string ScrollBarVisibilityCategoryName = "ScrollBarVisibility";

#if FRB
    public bool TakingInput => throw new NotImplementedException();
    public IInputReceiver NextInTabSequence { get; set; }
    public List<Keys> IgnoredKeys => throw new NotImplementedException();
    public void ReceiveInput()
    {
    }
    [Obsolete("Use OnLoseFocus instead")]
    public void LoseFocus() => OnLoseFocus();
    public void HandleKeyDown(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
    {
        var args = new KeyEventArgs();
        args.Key = key;
        base.RaiseKeyDown(args);
    }
    public void HandleCharEntered(char character)
    {
    }

#endif
    /// <summary>
    /// Raised every frame while this control has input focus. Can be used
    /// to perform custom per-frame logic while the control is focused.
    /// </summary>
    public event Action<IInputReceiver>? FocusUpdate;

    #region Fields/Properties

    bool reactToInnerPanelPositionOrSizeChanged = true;

    protected ScrollBar? verticalScrollBar;
    protected ScrollBar? horizontalScrollBar;

    GraphicalUiElement innerPanel;
    public GraphicalUiElement InnerPanel => innerPanel;

    protected GraphicalUiElement clipContainer;
    public GraphicalUiElement ClipContainer => clipContainer;

    GraphicalUiElement? _stickyHeaderOverlay;
    /// <summary>
    /// The container that hosts sticky-header visuals. Lives as a sibling of
    /// <see cref="InnerPanel"/> inside <see cref="ClipContainer"/>. Null if the
    /// underlying visual does not provide a child named
    /// <see cref="StickyHeaderOverlayInstanceName"/>; in that case sticky-header
    /// registration is a no-op.
    /// </summary>
    public GraphicalUiElement? StickyHeaderOverlay => _stickyHeaderOverlay;

    private sealed class StickyHeaderEntry
    {
        public GraphicalUiElement Header;
        public GraphicalUiElement Placeholder;
        public EventHandler HeaderSizeChangedHandler;
    }

    private readonly List<StickyHeaderEntry> _stickyHeaders = new();

    ScrollBarVisibility verticalScrollBarVisibility = ScrollBarVisibility.Auto;
    public ScrollBarVisibility VerticalScrollBarVisibility
    {
        get => verticalScrollBarVisibility;
        set
        {
            if (value != verticalScrollBarVisibility)
            {
                verticalScrollBarVisibility = value;
                UpdateVerticalScrollBarValues();
            }
        }
    }

    ScrollBarVisibility horizontalScrollBarVisibility = ScrollBarVisibility.Auto;
    public ScrollBarVisibility HorizontalScrollBarVisibility
    {
        get => horizontalScrollBarVisibility;
        set
        {
            if (value != horizontalScrollBarVisibility)
            {
                horizontalScrollBarVisibility = value;
                UpdateHorizontalScrollBarValues();
            }
        }
    }

    /// <inheritdoc cref="Gum.Forms.Controls.Primitives.RangeBase.SmallChange"/>
    /// <remarks>
    /// This property sets the SmallChange value for both vertical and horizontal scroll bars.
    /// </remarks>
    public double SmallChange
    {
        get => verticalScrollBar.SmallChange;
        set
        {
            verticalScrollBar.SmallChange = value;
            if(horizontalScrollBar != null)
            {
                horizontalScrollBar.SmallChange = value;
            }
        }
    }

    public double LargeChange
    {
        get => verticalScrollBar.LargeChange;
        set
        {
            verticalScrollBar.LargeChange = value;
            if(horizontalScrollBar != null)
            {
                horizontalScrollBar.LargeChange = value;
            }
        }
    }

    /// <summary>
    /// The vertical scroll bar value. Assigning this automatically scrolls
    /// the ScrollViewer to the desired location. Percentage-based scrolling
    /// can be perofrmed by using VerticalScrollBarMaximum. 
    /// </summary>
    public double VerticalScrollBarValue
    {
        get => verticalScrollBar.Value;
        set
        {
            verticalScrollBar.Value = value;
            PushValueToViewModel();
        }
    }

    public double HorizontalScrollBarValue
    {
        get => horizontalScrollBar?.Value ?? 0;
        set
        {
            if(horizontalScrollBar != null)
            {
                horizontalScrollBar.Value = value;
                PushValueToViewModel();
            }
        }
    }

    /// <summary>
    /// Gets the maximum amount the scroll bar can be scrolled to. This value is automatically
    /// assigned by the ScrollViewer in response to children being added.
    /// </summary>
    public double VerticalScrollBarMaximum =>
        verticalScrollBar.Maximum;

    public double HorizontalScrollBarMaximum =>
        horizontalScrollBar?.Maximum ?? 0;

    public ScrollBar? VerticalScrollBar => verticalScrollBar;

    public ScrollBar? HorizontalScrollBar => horizontalScrollBar;

    bool doItemsHaveFocus;
    public bool DoItemsHaveFocus
    {
        get => doItemsHaveFocus;
        set
        {
            var changedValue = false;
            if (!IsFocused && value)
            {
                IsFocused = true;
                changedValue = true;
            }

            if(doItemsHaveFocus != value)
            {
                doItemsHaveFocus = value;
                changedValue = true;
            }

            if(changedValue)
            {
                if(doItemsHaveFocus == false)
                {
                    var currentReceiver = InteractiveGue.CurrentInputReceiver;
                    if (currentReceiver.ParentInputReceiver == this)
                    {
                        this.IsFocused = true;
                    }
                }
                else
                {
                    // find first InputReceiver and give it focus:
                    for (int i = 0; i < this.InnerPanel.Children.Count; i++)
                    {
                        var childAsInputReceiver = this.InnerPanel.Children[i] as IInputReceiver;

                        if (childAsInputReceiver == null &&
                            this.InnerPanel.Children[i] is InteractiveGue interactiveGue)
                        {
                            childAsInputReceiver = interactiveGue.FormsControlAsObject as IInputReceiver;
                        }

                        if (childAsInputReceiver != null)
                        {
                            InteractiveGue.CurrentInputReceiver = childAsInputReceiver;
                            break;
                        }
                    }
                }
            }
            // todo - give the first item focus:
            //if (SelectedIndex > -1 && SelectedIndex < ListBoxItemsInternal.Count)
            //{
            //    ListBoxItemsInternal[SelectedIndex].IsFocused = doListBoxItemsHaveFocus;
            //}
        }
    }

    SizeMode _verticalSizeMode;
    /// <summary>
    /// Sets whether vertical sizing (height of entire control) is fixed (based on the size of the ScrollViewer's Visual) or 
    /// auto (based on the size of the inner panel's contents).
    /// </summary>
    /// <remarks>
    /// This property depends on the underlying Visual implementing the VerticalSizeMode category
    /// with an Auto state. If this is not set, then this property simply prevents the ScrollBarVisibility 
    /// category from being set.
    /// </remarks>
    public SizeMode VerticalSizeMode
    {
        get => _verticalSizeMode;
        set
        {
            if (_verticalSizeMode != value)
            {
                _verticalSizeMode = value;
                UpdateToSizeMode();
            }
        }
    }

    SizeMode _horizontalSizeMode;
    /// <summary>
    /// Sets whether horizontal sizing (width of the entire control) is fixed (based on the size of the ScrollViewer's Visual) or
    /// auto (based on the size of the inner panel's contents).
    /// </summary>
    /// <remarks>
    /// This property depends on the underlying Visual implementing the HorizontalSizeMode category
    /// with an Auto state. If this is not set, then this property simply prevents the ScrollBarVisibility
    /// category from being set.
    /// </remarks>
    public SizeMode HorizontalSizeMode
    {
        get => _horizontalSizeMode;
        set
        {
            if(_horizontalSizeMode != value)
            {
                _horizontalSizeMode = value;
                UpdateToSizeMode();
            }
        }
    }

    #endregion

    #region Initialize

    public ScrollViewer() : base() { }

    public ScrollViewer(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        var verticalScrollBarVisualAsGue = Visual.GetGraphicalUiElementByName(VerticalScrollBarInstanceName);
        var horizontalScrollBarVisualAsGue = Visual.GetGraphicalUiElementByName(HorizontalScrollBarInstanceName);
#if FULL_DIAGNOSTICS
        // Since ScrollViewer is the base for ItemsControl, and since ItemsControl is the base for Menu, we have to make
        // scrollbars optional
        //if (scrollBarVisualAsGue == null)
        //{
        //    throw new InvalidOperationException($"Could not find a child with the name {VerticalScrollBarInstanceName}");
        //}
#endif

        RefreshInternalVisualReferences();

        var vericalScrollBarVisual = verticalScrollBarVisualAsGue as InteractiveGue;
        var horizontalScrollBarVisual = horizontalScrollBarVisualAsGue as InteractiveGue;
#if FULL_DIAGNOSTICS
        //if (scrollBarVisual == null)
        //{
        //    throw new InvalidOperationException($"The child with the name {VerticalScrollBarInstanceName} was found, but is not an InteractiveGue." +
        //        $" Did you forget to set forms associations for this type?");
        //}
#endif

        if (vericalScrollBarVisual != null)
        {
            if (vericalScrollBarVisual.FormsControlAsObject == null)
            {
                verticalScrollBar = new ScrollBar(vericalScrollBarVisual);
            }
            else
            {
                verticalScrollBar = vericalScrollBarVisual.FormsControlAsObject as ScrollBar;
            }

            verticalScrollBar.ValueChanged += HandleVerticalScrollBarValueChanged;

            // Not sure if we want to set these here. This was moved out of 
            // UpdateVerticalScrollBarValues so that it's only set once before
            // CustomInitialize for UI, so usually this is okay. But eventually 
            // the user may want to swap out controls and doing so might reset this
            // value causing confusion? If so, we'd need to store off a temp value.
            verticalScrollBar.SmallChange = 10;
            if(verticalScrollBar.ViewportSize > 0)
            {
                // May 14, 2025 - The ViewportSize can be negative or 0 if the
                // visual for the ScrollViewer hasn't yet been given a height value.
                // This may be given a valid height value after the visual is assigned,
                // but in the meantime the large change should not be set to a negative value.
                verticalScrollBar.LargeChange = verticalScrollBar.ViewportSize;
            }


            // Depending on the height and width units, the scroll bar may get its update
            // called before or after this. We can't bet on the order, so we have to handle
            // both this and the scroll bar's height value changes, and adjust according to both:
            verticalScrollBar.Visual.SizeChanged += HandleVerticalScrollBarThumbSizeChanged;
        }

        if (horizontalScrollBarVisual != null)
        {
            if (horizontalScrollBarVisual.FormsControlAsObject == null)
            {
                horizontalScrollBar = new ScrollBar(horizontalScrollBarVisual);
            }
            else
            {
                horizontalScrollBar = horizontalScrollBarVisual.FormsControlAsObject as ScrollBar;
            }

            horizontalScrollBar.Orientation = Orientation.Horizontal;
            horizontalScrollBar.ValueChanged += HandleHorizontalScrollBarValueChanged;

            horizontalScrollBar.SmallChange = 10;
            if (horizontalScrollBar.ViewportSize > 0)
            {
                horizontalScrollBar.LargeChange = horizontalScrollBar.ViewportSize;
            }

            horizontalScrollBar.Visual.SizeChanged += HandleHorizontalScrollBarThumbSizeChanged;
        }

        if (innerPanel != null)
        {
            innerPanel.SizeChanged += HandleInnerPanelSizeChanged;
            innerPanel.PositionChanged += HandleInnerPanelPositionChanged;
        }

        Visual.MouseWheelScroll += HandleMouseWheelScroll;
        Visual.RollOverBubbling += HandleRollOver;
        Visual.SizeChanged += HandleVisualSizeChanged;

        UpdateVerticalScrollBarValues();
        UpdateHorizontalScrollBarValues();

        base.ReactToVisualChanged();

        UpdateState();
    }

    protected override void RefreshInternalVisualReferences()
    {
        innerPanel = Visual.GetGraphicalUiElementByName("InnerPanelInstance");
        clipContainer = Visual.GetGraphicalUiElementByName("ClipContainerInstance");
        _stickyHeaderOverlay = Visual.GetGraphicalUiElementByName(StickyHeaderOverlayInstanceName);
    }

    /// <inheritdoc/>
    protected override void ReactToVisualRemoved()
    {
        if (Visual != null)
        {
            Visual.MouseWheelScroll -= HandleMouseWheelScroll;
            Visual.RollOverBubbling -= HandleRollOver;
            Visual.SizeChanged -= HandleVisualSizeChanged;

            if(innerPanel != null)
            {
                innerPanel.SizeChanged -= HandleInnerPanelSizeChanged;
                innerPanel.PositionChanged -= HandleInnerPanelPositionChanged;
            }

            if(verticalScrollBar != null)
            {
                verticalScrollBar.Visual.SizeChanged -= HandleVerticalScrollBarThumbSizeChanged;
                verticalScrollBar.ValueChanged -= HandleVerticalScrollBarValueChanged;
            }

            if(horizontalScrollBar != null)
            {
                horizontalScrollBar.Visual.SizeChanged -= HandleHorizontalScrollBarThumbSizeChanged;
                horizontalScrollBar.ValueChanged -= HandleHorizontalScrollBarValueChanged;
            }

        }

        base.ReactToVisualRemoved();
    }

    private void HandleRollOver(object sender, RoutedEventArgs args)
    {
        if (MainCursor.PrimaryDown && MainCursor.LastInputDevice == InputDevice.TouchScreen)
        {
            verticalScrollBar.Value -= MainCursor.YChange /
                global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom;

            if(horizontalScrollBar != null)
            {
                horizontalScrollBar.Value -= MainCursor.XChange /
                    global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom;
            }

            args.Handled = true;
        }
    }


    #endregion

    #region Events

    public event EventHandler ScrollChanged;

    #endregion

    public override void AddChild(FrameworkElement child)
    {
        if (InnerPanel != null)
        {
            this.InnerPanel.Children.Add(child.Visual);
        }
        else
        {
            base.AddChild(child);
        }
    }

    public override void RemoveChild(FrameworkElement child)
    {
        if (InnerPanel != null)
        {
            this.InnerPanel.Children.Remove(child.Visual);
        }
        else
        {
            base.RemoveChild(child);
        }
    }

    public override void AddChild(GraphicalUiElement child)
    {
        if (InnerPanel != null)
        {
            this.InnerPanel.Children.Add(child);
        }
        else
        {
            base.AddChild(child);
        }
    }

    public override void RemoveChild(GraphicalUiElement child)
    {
        if (InnerPanel != null)
        {
            this.InnerPanel.Children.Remove(child);
        }
        else
        {
            base.RemoveChild(child);
        }
    }


    #region Scroll Methods

    private void HandleMouseWheelScroll(object sender, RoutedEventArgs args)
    {
        if(verticalScrollBar != null)
        {
            var isShiftHeld = false;

            var hasExplicitKeyboardsForUiControl = false;

#if !FRB
            foreach (var keyboard in FrameworkElement.KeyboardsForUiControl)
            {
                isShiftHeld |= keyboard.IsShiftDown;
                hasExplicitKeyboardsForUiControl = true;
            }
#endif

            if(!hasExplicitKeyboardsForUiControl)
            {
                if (FrameworkElement.MainKeyboard != null)
                {
                    isShiftHeld |= FrameworkElement.MainKeyboard.IsShiftDown;
                }
            }

            if (isShiftHeld == false)
            {
                var valueBefore = verticalScrollBar.Value;

                // Do we want to use the small change? Or have some separate value that the user can set?
                verticalScrollBar.Value -= MainCursor.ZVelocity * verticalScrollBar.SmallChange;

                args.Handled = verticalScrollBar.Value != valueBefore;
            }
            else if(horizontalScrollBar != null)
            {
                var valueBefore = horizontalScrollBar.Value;

                horizontalScrollBar.Value -= MainCursor.ZVelocity * horizontalScrollBar.SmallChange;

                args.Handled = horizontalScrollBar.Value != valueBefore;
            }
        }
    }

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

        ScrollChanged?.Invoke(this, EventArgs.Empty);

        RecomputeStickyHeaders();
    }

    private void HandleHorizontalScrollBarValueChanged(object sender, EventArgs e)
    {
        if(horizontalScrollBar != null)
        {
            reactToInnerPanelPositionOrSizeChanged = false;
            innerPanel.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            innerPanel.X = -(float)horizontalScrollBar.Value;
            reactToInnerPanelPositionOrSizeChanged = true;
            ScrollChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void HandleInnerPanelSizeChanged(object sender, EventArgs e)
    {
        if (reactToInnerPanelPositionOrSizeChanged)
        {
            if(this.VerticalSizeMode == SizeMode.Fixed)
            {
                UpdateHorizontalScrollBarValues();
            }
            if(this.VerticalSizeMode == SizeMode.Fixed)
            {
                UpdateVerticalScrollBarValues();
            }
        }
        RecomputeStickyHeaders();
    }

    private void HandleVisualSizeChanged(object sender, EventArgs args)
    {
        if (reactToInnerPanelPositionOrSizeChanged)
        {
            if(this.HorizontalSizeMode == SizeMode.Fixed)
            {
                UpdateHorizontalScrollBarValues();
            }
            if (this.VerticalSizeMode == SizeMode.Fixed)
            {
                UpdateVerticalScrollBarValues();
            }
        }
        RecomputeStickyHeaders();
    }

    private void HandleVerticalScrollBarThumbSizeChanged(object sender, EventArgs args)
    {
        if (reactToInnerPanelPositionOrSizeChanged)
        {
            UpdateVerticalScrollBarValues();
        }
    }

    private void HandleHorizontalScrollBarThumbSizeChanged(object sender, EventArgs args)
    {
        if (reactToInnerPanelPositionOrSizeChanged)
        {
            UpdateHorizontalScrollBarValues();
        }
    }

    private void HandleInnerPanelPositionChanged(object sender, EventArgs e)
    {
        if (reactToInnerPanelPositionOrSizeChanged)
        {
            UpdateVerticalScrollBarValues();
            UpdateHorizontalScrollBarValues();
        }
    }

    #endregion

    #region Sticky headers

    /// <summary>
    /// Registers a header visual to be pinned to the top of the viewport while
    /// its corresponding placeholder is scrolled out of view. The header is
    /// reparented into <see cref="StickyHeaderOverlay"/>; the placeholder must
    /// already be a child of <see cref="InnerPanel"/> (or somewhere it will be
    /// laid out by the scroll content). The placeholder's height is sized to
    /// match the header and is kept in sync as the header resizes.
    /// </summary>
    /// <remarks>
    /// If the underlying visual does not provide a sticky-header overlay
    /// container, this method is a no-op.
    /// </remarks>
    public void RegisterStickyHeader(GraphicalUiElement header, GraphicalUiElement placeholder)
    {
        if (header == null)
        {
            throw new ArgumentNullException(nameof(header));
        }
        if (placeholder == null)
        {
            throw new ArgumentNullException(nameof(placeholder));
        }
        if (_stickyHeaderOverlay == null)
        {
            return;
        }

        // Reparent the header into the overlay. The header positions itself
        // freely (Regular layout) so direct Y assignment is not clobbered by
        // the inner panel's stack.
        if (header.Parent != _stickyHeaderOverlay)
        {
            header.Parent = _stickyHeaderOverlay;
        }

        // Size the placeholder to occupy the slot the header would have had in
        // the stack. We track height only; width follows the parent's stack.
        placeholder.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        placeholder.Height = header.GetAbsoluteHeight();

        StickyHeaderEntry entry = new()
        {
            Header = header,
            Placeholder = placeholder,
        };
        entry.HeaderSizeChangedHandler = (_, _) =>
        {
            entry.Placeholder.Height = entry.Header.GetAbsoluteHeight();
            RecomputeStickyHeaders();
        };
        header.SizeChanged += entry.HeaderSizeChangedHandler;

        _stickyHeaders.Add(entry);
        RecomputeStickyHeaders();
    }

    /// <inheritdoc cref="RegisterStickyHeader(GraphicalUiElement, GraphicalUiElement)"/>
    public void RegisterStickyHeader(FrameworkElement header, FrameworkElement placeholder)
    {
        if (header == null)
        {
            throw new ArgumentNullException(nameof(header));
        }
        if (placeholder == null)
        {
            throw new ArgumentNullException(nameof(placeholder));
        }
        RegisterStickyHeader(header.Visual, placeholder.Visual);
    }

    /// <inheritdoc cref="RegisterStickyHeader(GraphicalUiElement, GraphicalUiElement)"/>
    public void RegisterStickyHeader(FrameworkElement header, GraphicalUiElement placeholder)
    {
        if (header == null)
        {
            throw new ArgumentNullException(nameof(header));
        }
        RegisterStickyHeader(header.Visual, placeholder);
    }

    /// <inheritdoc cref="RegisterStickyHeader(GraphicalUiElement, GraphicalUiElement)"/>
    public void RegisterStickyHeader(GraphicalUiElement header, FrameworkElement placeholder)
    {
        if (placeholder == null)
        {
            throw new ArgumentNullException(nameof(placeholder));
        }
        RegisterStickyHeader(header, placeholder.Visual);
    }

    /// <summary>
    /// Removes a previously-registered sticky header. The header and its
    /// placeholder remain where they are; the caller is responsible for any
    /// further teardown.
    /// </summary>
    public void UnregisterStickyHeader(GraphicalUiElement header)
    {
        if (header == null)
        {
            return;
        }
        for (int i = _stickyHeaders.Count - 1; i >= 0; i--)
        {
            StickyHeaderEntry entry = _stickyHeaders[i];
            if (entry.Header == header)
            {
                entry.Header.SizeChanged -= entry.HeaderSizeChangedHandler;
                _stickyHeaders.RemoveAt(i);
            }
        }
        RecomputeStickyHeaders();
    }

    /// <inheritdoc cref="UnregisterStickyHeader(GraphicalUiElement)"/>
    public void UnregisterStickyHeader(FrameworkElement header)
    {
        if (header == null)
        {
            return;
        }
        UnregisterStickyHeader(header.Visual);
    }

    /// <summary>
    /// Removes all sticky-header registrations.
    /// </summary>
    public void ClearStickyHeaders()
    {
        for (int i = 0; i < _stickyHeaders.Count; i++)
        {
            StickyHeaderEntry entry = _stickyHeaders[i];
            entry.Header.SizeChanged -= entry.HeaderSizeChangedHandler;
        }
        _stickyHeaders.Clear();
    }

    private void RecomputeStickyHeaders()
    {
        if (_stickyHeaderOverlay == null || _stickyHeaders.Count == 0)
        {
            return;
        }

        // Sort by placeholder absolute Y so the "next header" relationship
        // matches what the user sees, regardless of registration order.
        _stickyHeaders.Sort(static (a, b) =>
            a.Placeholder.AbsoluteTop.CompareTo(b.Placeholder.AbsoluteTop));

        float overlayTop = _stickyHeaderOverlay.AbsoluteTop;

        for (int i = 0; i < _stickyHeaders.Count; i++)
        {
            StickyHeaderEntry entry = _stickyHeaders[i];

            float naturalRelY = entry.Placeholder.AbsoluteTop - overlayTop;
            // Pin against the top of the overlay (Y=0) once we've scrolled
            // past the placeholder.
            float headerY = Math.Max(naturalRelY, 0f);

            // Bump: if the next header's placeholder is closing in on our
            // bottom edge, slide off to make room.
            if (i + 1 < _stickyHeaders.Count)
            {
                float nextRelY = _stickyHeaders[i + 1].Placeholder.AbsoluteTop - overlayTop;
                float maxY = nextRelY - entry.Header.GetAbsoluteHeight();
                if (headerY > maxY)
                {
                    headerY = maxY;
                }
            }

            entry.Header.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            entry.Header.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            entry.Header.Y = headerY;
        }
    }

    #endregion

    #region Focus-related methods

    public IInputReceiver? ParentInputReceiver =>
    this.GetParentInputReceiver();

    public virtual void OnGainFocus()
    {
        IsFocused = true;

    }

    public virtual void OnLoseFocus()
    {
        IsFocused = false;
    }

    public void OnFocusUpdatePreview(RoutedEventArgs args)
    {
        // todo - check for ESC and return handled, steal focus from children
#if XNALIKE && !FRB
        foreach(var keyboard in FrameworkElement.KeyboardsForUiControl)
        {
            // eventually we want to support combos but for now use esc:
            if(keyboard.KeyPushed(Keys.Escape))
            {
                DoItemsHaveFocus = false;
                IsFocused = true;
                args.Handled = true;
                break;
            }
        }
#endif
    }

    public virtual void OnFocusUpdate()
    {
        if (DoItemsHaveFocus)
        {
            DoItemFocusUpdate();
        }
        else
        {
            DoTopLevelFocusUpdate();
        }

#if !FRB
        base.HandleKeyboardFocusUpdate();
#endif

        FocusUpdate?.Invoke(this);
    }

    private void DoTopLevelFocusUpdate()
    {
        var gamepads = FrameworkElement.GamePadsForUiControl;

        for (int i = 0; i < gamepads.Count; i++)
        {
            var gamepad = gamepads[i];

            HandleGamepadNavigation(gamepad);

            if (gamepad.ButtonPushed(GamepadButton.A))
            {
                DoItemsHaveFocus = true;
            }

            // ListBox has this, does ScrollViewer need it?
            //void RaiseIfPushedAndEnabled(Buttons button)
            //{
            //    if (IsEnabled && gamepad.ButtonPushed(button))
            //    {
            //        ControllerButtonPushed?.Invoke(button);
            //    }
            //}

            //RaiseIfPushedAndEnabled(Buttons.B);
            //RaiseIfPushedAndEnabled(Buttons.X);
            //RaiseIfPushedAndEnabled(Buttons.Y);
            //RaiseIfPushedAndEnabled(Buttons.Start);
            //RaiseIfPushedAndEnabled(Buttons.Back);
            //RaiseIfPushedAndEnabled(Buttons.DPadLeft);
            //RaiseIfPushedAndEnabled(Buttons.DPadRight);

#if FRB
            //RaiseIfPushedAndEnabled(Buttons.LeftStickAsDPadLeft);
            //RaiseIfPushedAndEnabled(Buttons.LeftStickAsDPadRight);
#endif
        }



#if !FRB

        foreach (var keyboard in KeyboardsForUiControl)
        {
            foreach (var keyCombo in FrameworkElement.ClickCombos)
            {
                if (keyCombo.IsComboPushed())
                {
                    DoItemsHaveFocus = true;
                    break;
                }
            }
        }
#endif
    }

    private void DoItemFocusUpdate()
    {

    }

    public void DoKeyboardAction(IInputReceiverKeyboard keyboard)
    {
    }

#endregion

    #region UpdateTo methods

    private void UpdateToSizeMode()
    {
        if(VerticalSizeMode == SizeMode.Auto)
        {
            string state = VerticalSizeMode == SizeMode.Fixed ? "Fixed" : "Auto";
            const string category = "VerticalSizeModeState";
            Visual.SetProperty(category, state);
        }
        if(HorizontalSizeMode == SizeMode.Auto)
        {
            string state = VerticalSizeMode == SizeMode.Fixed ? "Fixed" : "Auto";
            const string category = "HorizontalSizeModeState";
            Visual.SetProperty(category, state);
        }

    }

    // Currently this is public because Gum objects don't have events
    // when positions and sizes change. Eventually, we'll have this all
    // handled internally and this can be made private.
    public void UpdateVerticalScrollBarValues()
    {
        if (verticalScrollBar == null)
        {
            return;
        }

        // Set the values here:
        SetVerticalScrollBarValuesFromVisuals();

        // Record the inner panel height before (possibly) changing the
        // scroll bar height...
        var innerPanelHeight = innerPanel.GetAbsoluteHeight();

        switch (verticalScrollBarVisibility)
        {
            case ScrollBarVisibility.Hidden:
                verticalScrollBar.IsVisible = false;
                break;
            case ScrollBarVisibility.Visible:
                verticalScrollBar.IsVisible = true;
                break;
            case ScrollBarVisibility.Auto:
                {
                    var clipContainerHeight = clipContainer.GetAbsoluteHeight();
                    verticalScrollBar.IsVisible = innerPanelHeight > clipContainerHeight;
                }
                break;
        }
        SetScrollBarState();


        // now that we've set the visibility state, let's see if the height has changed
        var didHeightChange = innerPanel.GetAbsoluteHeight() != innerPanelHeight;
        if (didHeightChange)
        {
            // It changed, which can adjust the scroll bar height so let's adjust it again
            SetVerticalScrollBarValuesFromVisuals();
        }

        void SetVerticalScrollBarValuesFromVisuals()
        {
            verticalScrollBar.Minimum = 0;
            verticalScrollBar.ViewportSize = clipContainer.GetAbsoluteHeight();

            var innerPanelHeight = innerPanel.GetAbsoluteHeight();
            var clipContainerHeight = clipContainer.GetAbsoluteHeight();
            var maxValue = innerPanelHeight - clipContainerHeight;

            maxValue = System.Math.Max(0, maxValue);

            verticalScrollBar.Maximum = maxValue;
        }

        var oldReact = reactToInnerPanelPositionOrSizeChanged;
        reactToInnerPanelPositionOrSizeChanged = false;
        UpdateToSizeMode();

        reactToInnerPanelPositionOrSizeChanged = oldReact;
    }

    private void SetScrollBarState()
    {
        var isVerticalVisible = verticalScrollBar?.IsVisible == true;
        var isHorizontalVisible = horizontalScrollBar?.IsVisible == true;

        string state = 
            isVerticalVisible && isHorizontalVisible ? "BothScrollVisible"
            : isVerticalVisible ? "VerticalScrollVisible"
            : isHorizontalVisible ? "HorizontalScrollVisible"
            : "NoScrollBar";

        const string category = "ScrollBarVisibilityState";

        Visual.SetProperty(category, state);
    }

    private void UpdateHorizontalScrollBarValues()
    {
        if (horizontalScrollBar == null)
        {
            return;
        }
        // Set the values here:
        SetHorizontalSrollBarValuesFromVisuals();
        // Record the inner panel width before (possibly) changing the
        // scroll bar width...
        var innerPanelWidth = innerPanel.GetAbsoluteWidth();

        switch (horizontalScrollBarVisibility)
        {
            case ScrollBarVisibility.Hidden:
                horizontalScrollBar.IsVisible = false;
                break;
            case ScrollBarVisibility.Visible:
                horizontalScrollBar.IsVisible = true;
                break;
            case ScrollBarVisibility.Auto:
                {
                    var clipContainerWidth = clipContainer.GetAbsoluteWidth();
                    horizontalScrollBar.IsVisible = innerPanelWidth > clipContainerWidth;
                }
                break;
        }

        SetScrollBarState();


        // now that we've set the visibility state, let's see if the width has changed
        var didWidthChange = innerPanel.GetAbsoluteWidth() != innerPanelWidth;
        if (didWidthChange)
        {
            // It changed, which can adjust the scroll bar width so let's adjust it again
            SetHorizontalSrollBarValuesFromVisuals();
        }

        void SetHorizontalSrollBarValuesFromVisuals()
        {
            horizontalScrollBar.Minimum = 0;
            horizontalScrollBar.ViewportSize = clipContainer.GetAbsoluteWidth();

            var innerPanelWidth = innerPanel.GetAbsoluteWidth();
            var clipContainerWidth = clipContainer.GetAbsoluteWidth();
            var maxValue = innerPanelWidth - clipContainerWidth;

            maxValue = System.Math.Max(0, maxValue);

            horizontalScrollBar.Maximum = maxValue;
        }
    }

    public override void UpdateState()
    {
        var state = base.GetDesiredState();

        Visual.SetProperty(ScrollViewerCategoryName + "State", state);
    }

    /// <inheritdoc/>
    public override void ApplyRuntimeProperties()
    {
        if (innerPanel != null)
        {
            if (verticalScrollBar != null)
            {
                innerPanel.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
                innerPanel.Y = -(float)verticalScrollBar.Value;
            }
            if (horizontalScrollBar != null)
            {
                innerPanel.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
                innerPanel.X = -(float)horizontalScrollBar.Value;
            }
        }
        base.ApplyRuntimeProperties();
    }

    #endregion
}
