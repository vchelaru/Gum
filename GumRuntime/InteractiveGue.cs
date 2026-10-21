using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.RenderingLibrary;
using GumDataTypes.Variables;

using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.ComponentModel;
using ToolsUtilitiesStandard.Helpers;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using GumRuntime;
using System.Collections;
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Diagnostics;

namespace Gum.Wireframe;

#region Event types
public class RoutedEventArgs : EventArgs
{
    public bool Handled { get; set; }
}

public class InputEventArgs : RoutedEventArgs
{
    /// <summary>
    /// The input device which was responsible for this event, such as the Gamepad.
    /// </summary>
    public object? InputDevice { get; set; }
}

/// <summary>
/// Contains information about objects which have been added to or removed from the current selection.
/// This is typically used by Gum Forms for elements which support selection such as ListBox.
/// </summary>
public class SelectionChangedEventArgs
{
    /// <summary>
    /// The items which were just removed from selection.
    /// </summary>
    public IList RemovedItems { get; private set; } = new List<Object>();
    /// <summary>
    /// The items which were just added to selection.
    /// </summary>
    public IList AddedItems { get; private set; } = new List<Object>();
}
#endregion

/// <summary>
/// The base object for all interactive Gum runtime objects. It provides events for cursor interaction which can be used
/// to create full interactive controls
/// </summary>
public partial class InteractiveGue : GraphicalUiElement
{

    // The focus slot, the last pushed visual and the next push/click queues are facades over the current
    // GumContext (#6066): the context that adopted the process statics uses the fields, every other
    // service uses its own, so one service's focus change or queued action never reaches another's.
    static readonly List<Action> _processWideNextPushActions = new List<Action>();
    static readonly List<Action> _processWideNextClickActions = new List<Action>();

    static List<Action> nextPushActions => GumContext.Own?.NextPushActions ?? _processWideNextPushActions;
    static List<Action> nextClickActions => GumContext.Own?.NextClickActions ?? _processWideNextClickActions;

    // Test seam: how many next-push actions are queued (pending or waiting for the next frame).
    internal static int PendingNextPushActionCount => nextPushActionHoldingList.Count + nextPushActions.Count;

    /// <summary>
    /// The current game time in seconds, assigned internally when calling GumService.Update. 
    /// This can be used by UI elements to perform
    /// timing-based actions, such as moving a slider on a long press.
    /// </summary>
    public static double CurrentGameTime { get; internal set; }


    static InteractiveGue? _processWideLastVisualPushed;

    public static InteractiveGue? LastVisualPushed
    {
        get { GumContext? own = GumContext.Own; return own != null ? own.LastVisualPushed : _processWideLastVisualPushed; }
        set { GumContext? own = GumContext.Own; if (own != null) { own.LastVisualPushed = value; } else { _processWideLastVisualPushed = value; } }
    }

    static IInputReceiver? _processWideInputReceiver;

    static IInputReceiver? currentInputReceiver
    {
        get { GumContext? own = GumContext.Own; return own != null ? own.CurrentInputReceiver : _processWideInputReceiver; }
        set { GumContext? own = GumContext.Own; if (own != null) { own.CurrentInputReceiver = value; } else { _processWideInputReceiver = value; } }
    }

    
    public static IInputReceiver? CurrentInputReceiver
    {
        get => currentInputReceiver;
        set
        {
            var differs = currentInputReceiver != value;
            if (differs)
            {
                var old = currentInputReceiver;
                currentInputReceiver = value;

                if (old != null)
                {
                    old.OnLoseFocus();
                }
            }
            currentInputReceiver = value;
            if (differs && currentInputReceiver != null)
            {
                currentInputReceiver.OnGainFocus();
            }


        }
    }

    /// <summary>
    /// Whether this instance supports events and whether the Cursor considers this when
    /// determining what it is over. Typically this is assigned once based 
    /// on its type, usually when objects are created from a Gum project.
    /// </summary>
    public bool HasEvents
    {
        get => _hasEvents;
        set
        {
            if (_hasEvents != value)
            {
                _hasEvents = value;
                // The cursor walk skips subtrees with no events, so it has to look at this one again.
                InvalidateSubtreeCaches();
            }
        }
    }
    bool _hasEvents;
    public bool ExposeChildrenEvents { get; set; } = true;

    /// <summary>
    /// Whether to check each individual child for raising UI events even if the cursor
    /// is outside of the bounds of this object. Setting this to false can have a slight
    /// performance cost since each child is checked even if the cursor is not over this.
    /// </summary>
    public bool RaiseChildrenEventsOutsideOfBounds { get; set; } = false;
    bool isEnabled = true;

    /// <summary>
    /// Whether this is enabled. If this is false, then this will not raise events.
    /// </summary>
    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            if (isEnabled != value)
            {
                isEnabled = value;
                EnabledChange?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool IsEnabledRecursively => GetIsEnabledRecursively(this);

    static bool GetIsEnabledRecursively(InteractiveGue interactiveGue)
    {
        if (!interactiveGue.IsEnabled)
        {
            return false;
        }
        else if (interactiveGue.Parent is InteractiveGue parent)
        {
            return GetIsEnabledRecursively(parent);
        }
        else
        {
            return true;
        }
    }

    private object? _formsControlAsObject;

    /// <summary>
    /// Provides an uncasted reference to the Gum Forms element which uses this as visual element.
    /// </summary>
    /// <remarks>
    /// Null for a plain visual. The default visuals create their control in their constructor unless
    /// passed tryCreateFormsObject: false, so their typed FormsControl properties treat it as set.
    /// </remarks>
    public virtual object? FormsControlAsObject
    {
        get => _formsControlAsObject;
        set
        {
            _formsControlAsObject = value;
            // The control may need a callback every frame (IUpdateEveryFrame), which an idle subtree skips.
            InvalidateSubtreeCaches();

            if (value != null)
            {
                // A custom variable whose generated property lives on the Forms class was held
                // until now, because the element's state is applied before this is assigned.
                ApplyPendingCustomVariables(value);
            }
        }
    }

    #region Events 

    /// <summary>
    /// Event raised when this is clicked by a cursor. A click occurs
    /// when the cursor is over this and is first pushed, then released.
    /// </summary>
    public event EventHandler? Click;

    /// <summary>
    /// Event raised when this is clicked by a cursor. Unlike <see cref="Click"/>, which is
    /// only raised on the clicked element, this bubbling event is first raised on the clicked
    /// element, then on its parents in sequence. If a handler sets the argument
    /// <see cref="RoutedEventArgs.Handled"/> to true, then parent objects will not have this
    /// event raised. This is useful for being notified when anything on an element's surface is
    /// clicked, even when the click lands on a child.
    /// </summary>
    public event Action<object, RoutedEventArgs>? ClickBubbling;

    /// <summary>
    /// Event raised when this is clicked by a cursor. A click occurs
    /// when the cursor is over this and is first pushed, then released.
    /// Preview events are received by parents before children, and if the event
    /// is handled, children do not receive the event.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? ClickPreview;

    /// <summary>
    /// Event raised when this is double-clicked by a cursor. A double-click occurs
    /// when the cursor is over this and the left mouse button is clicked twice in rapid succession.
    /// </summary>
    public event EventHandler? DoubleClick;

    /// <summary>
    /// Event raised every frame the primary button is held after pushing on this object, whether
    /// or not the cursor has moved. Unlike RollOver, it is raised even if the cursor is outside of
    /// the bounds of the object. This can be used if an object is to be moved by dragging since it
    /// will be raised even if the user moves the cursor quickly outside of its bounds.
    /// </summary>
    public event EventHandler? Dragging;

    /// <summary>
    /// Event raised when the Enabled property changed.
    /// </summary>
    public event EventHandler? EnabledChange;

    /// <summary>
    /// Event raised every frame the cursor is over this object whether the cursor has changed positions or not.
    /// </summary>
    public event EventHandler? HoverOver;

    private EventHandler? _losePush;
    /// <summary>
    /// Event raised when this loses a push. A push occurs when the
    /// cursor is over this window and the left mouse button is pushed. A push is lost
    /// if the left mouse button is released or if the user moves the cursor so that it
    /// is no longer over this while the mouse button is pressed. 
    /// </summary>
    /// <remarks>
    /// LosePush is often used to change the state of a button back to its regular state.
    /// </remarks>
    public event EventHandler? LosePush
    {
        add
        {
            _losePush += value;
            TrySubscribeToLosePushEvents();
        }
        remove
        {
            _losePush -= value;
        }
    }

    /// <summary>
    /// Event raised when the mouse wheel has been scrolled while the cursor is over this instance.
    /// This event is first raised on children, then bubbles up to parents. If a control sets the argument
    /// Handled to true, then parent objects will not receive this event.
    /// </summary>
    public event Action<object, RoutedEventArgs>? MouseWheelScroll;

    /// <summary>
    /// Event raised when this is pushed by a cursor. A push occurs
    /// when the cursor is over this and the left mouse button is pushed (not down last frame,
    /// down this frame). Preview events are received by parents before children, and if the event
    /// is handled, children do not receive the event.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? PushPreview;

    /// <summary>
    /// Event raised when this is pushed by a cursor. A push occurs
    /// when the cursor is over this and the left mouse button is pushed (not down last frame,
    /// down this frame).
    /// </summary>
    public event EventHandler? Push;

    /// <summary>
    /// Event raised when this Window is pushed, then is no longer the pushed window due to a cursor releasing the primary button.
    /// This can be used to detect the end of a drag operation, or to reset the state of a button.
    /// </summary>
    public event EventHandler? RemovedAsPushed;

    /// <summary>
    /// Event raised when this is right-clicked by a cursor. A right-click occurs
    /// when the cursor is over this and is first pushed, then released.
    /// </summary>
    public event EventHandler? RightClick;

    /// <summary>
    /// Event raised when the cursor first leaves this object.
    /// </summary>
    public event EventHandler? RollOff;

    /// <summary>
    /// Event raised when the cursor first moves over this object. This is only received if the
    /// cursor is moved directly over this object. If it is instead moved over a child object
    /// which has its own events, then the parent will receive this event.
    /// </summary>
    public event EventHandler? RollOn;

    /// <summary>
    /// Event raised every frame the cursor is over this object and the Cursor has changed position.
    /// This event is not raised if the cursor has moved off of the object. For events raised when the 
    /// cursor is not over this instance, see Dragging.
    /// </summary>
    public event EventHandler? RollOver;

    /// <summary>
    /// Event raised when the mouse rolls over this instance. Bubbling events are raised top-down, with the
    /// child object having the opportunity to handle the roll over first. If a control sets the argument 
    /// RoutedEventArgs Handled to true,
    /// then parent objects will not receive this event raised.
    /// </summary>
    public event Action<object, RoutedEventArgs>? RollOverBubbling;


    public void CallClick() => Click?.Invoke(this, EventArgs.Empty);
    public void CallRightClick() => RightClick?.Invoke(this, EventArgs.Empty);  

    // RollOff is determined outside of the individual InteractiveGue so we need to have this callable externally..
    public void TryCallRollOff(ICursor? cursor = null)
    {
        InputEventArgs args = new InputEventArgs { InputDevice = cursor };
        RollOff?.Invoke(this, args);
    }

    public void TryCallDragging()
    {
        Dragging?.Invoke(this, EventArgs.Empty);
    }

    public void TryCallRemoveAsPushed() =>
        RemovedAsPushed?.Invoke(this, EventArgs.Empty);

    public void TryCallRollOn() =>
        RollOn?.Invoke(this, EventArgs.Empty);

    public void TryCallRollOver() =>
        RollOver?.Invoke(this, EventArgs.Empty);

    public void TryCallHoverOver() =>
        HoverOver?.Invoke(this, EventArgs.Empty);

    public void TryCallPush() =>
        Push?.Invoke(this, EventArgs.Empty);

    #endregion

    // Whether this element or anything visible under it can react to the cursor (has HasEvents set).
    // Cached on the element until something under it changes.
    internal static bool SubtreeHasEvents(GraphicalUiElement element)
    {
        if (element._subtreeEventsState == 0)
        {
            bool hasEvents = (element as InteractiveGue)?.HasEvents == true;
            if (!hasEvents)
            {
                ObservableCollection<GraphicalUiElement> children = element.Children;
                for (int i = 0; i < children.Count; i++)
                {
                    GraphicalUiElement child = children[i];
                    // A hidden child is skipped by the cursor walk. Showing it invalidates this.
                    if (!child.IsHiddenForCaches && SubtreeHasEvents(child))
                    {
                        hasEvents = true;
                        break;
                    }
                }
            }
            element._subtreeEventsState = hasEvents ? (byte)2 : (byte)1;
        }
        return element._subtreeEventsState == 2;
    }

    internal static bool DoUiActivityRecursively(ICursor cursor, ref HandledActions handledActions, GraphicalUiElement currentItem, Layer? layer, bool isKnownToBeOver = false)
    {
        bool handledByChild = false;
        bool handledByThis = false;

        var asInteractive = currentItem as InteractiveGue;
        bool isOver = isKnownToBeOver || (asInteractive?.HasCursorOver(cursor, layer) ?? HasCursorOver(cursor, currentItem, layer));

        // Even though the cursor is over "this", we need to check if the cursor is over any children in case "this" exposes its children events:
        if (isOver && (asInteractive == null || asInteractive.ExposeChildrenEvents))
        {
            if (asInteractive != null && asInteractive.HasEvents && asInteractive.IsEnabledRecursively)
            {
                if (asInteractive.ClickPreview != null &&
                    !handledActions.HandledClickPreview && cursor.PrimaryClick)
                {
                    var args = new InputEventArgs() { InputDevice = cursor };
                    asInteractive.ClickPreview(asInteractive, args);
                    InvalidateCursorWorldCache();

                    if (args.Handled)
                    {
                        cursor.WindowPushed = asInteractive;
                        LastVisualPushed = asInteractive;

                        handledActions.HandledClickPreview = true;
                    }
                }
                if (asInteractive.PushPreview != null &&
                    !handledActions.handledPushPreview && cursor.PrimaryPush)
                {
                    var args = new InputEventArgs() { InputDevice = cursor };
                    asInteractive.PushPreview(asInteractive, args);
                    InvalidateCursorWorldCache();

                    if (args.Handled)
                    {
                        handledActions.handledPushPreview = true;
                    }
                }
            }


            #region Try handling by children

            if (currentItem.Children == null)
            {
                for (int i = currentItem.ContainedElements.Count - 1; i > -1; i--)
                {
                    var child = currentItem.ContainedElements[i] as GraphicalUiElement;

                    if (child != null && HasCursorOver(cursor, child, layer))
                    {
                        // hasCursorOver is only true when child is non-null.
                        handledByChild = DoUiActivityRecursively(cursor, ref handledActions, child!, layer);

                        if (handledByChild)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                // Let's see if any children have the cursor over:
                for (int i = currentItem.Children.Count - 1; i > -1; i--)
                {
                    var child = currentItem.Children[i];
                    // Children should always have the opportunity to handle activity,
                    // even if they are not components, because they may contain components as their children

                    // A child with no events anywhere under it cannot handle anything, so it is not tested.
                    if (child == null || !SubtreeHasEvents(child))
                    {
                        continue;
                    }

                    // If the child either has events or exposes children events, then give it a chance to handle this activity.
                    bool hasCursorOver = false;
                    if(child is InteractiveGue interactiveGue)
                    {
                        hasCursorOver = interactiveGue.HasCursorOver(cursor, layer);
                    }
                    else
                    {
                        hasCursorOver = HasCursorOver(cursor, child, layer);
                    }
                    if (hasCursorOver)
                    {
                        // The child was just tested, so the recursion does not test it again.
                        handledByChild = DoUiActivityRecursively(cursor, ref handledActions, child!, layer, isKnownToBeOver: true);

                        if (handledByChild)
                        {
                            break;
                        }

                    }
                }

            }

            #endregion
        }

        if (isOver)
        {
            var shouldTreatAsIsOver =
                IsComponentOrInstanceOfComponent(currentItem);

            // See VisualOverBehavior for an exaplanation about this code
            if (!shouldTreatAsIsOver && ICursor.VisualOverBehavior == VisualOverBehavior.OnlyIfEventsAreNotNullAndHasEventsIsTrue)
            {
                shouldTreatAsIsOver =
                asInteractive?.Push != null ||
                asInteractive?.Click != null ||
                asInteractive?.DoubleClick != null ||
                asInteractive?.RightClick != null ||

                asInteractive?.RollOn != null ||
                asInteractive?.RollOff != null ||
                asInteractive?.RollOver != null ||
                asInteractive?._losePush != null ||
                asInteractive?.HoverOver != null ||
                asInteractive?.Dragging != null ||
                asInteractive?.MouseWheelScroll != null;
                // if it has events and it has a Forms control, then let's consider it a click
                //|| asInteractive?.FormsControlAsObject != null
                // Update July 18, 2025 
                // We can't do this because
                // if we do, full screen containers
                // consume all clicks. We need to come
                // up with another way to do this. For now
                // the events are the hack
            }

            if (!shouldTreatAsIsOver && ICursor.VisualOverBehavior == VisualOverBehavior.IfHasEventsIsTrue)
            {
                shouldTreatAsIsOver = asInteractive?.HasEvents == true;
            }


            if (shouldTreatAsIsOver)
            {
                if (!handledByChild)
                {
                    // Feb. 21, 2018
                    // If not handled by
                    // children, then this
                    // can only handle if this
                    // exposes events. Otherwise,
                    // it shouldn't handle anything
                    // and the parent should be given
                    // the opportunity.
                    // I'm not sure why this was outside
                    // of the if(this.HasEvents)...seems intentional
                    // but it causes problems when the rootmost object
                    // exposes children events but doesn't handle its own
                    // events...
                    //handledByThis = true;

                    if (asInteractive?.HasEvents == true)
                    {
                        // moved from above, see comments there...
                        handledByThis = true;
                        cursor.VisualOver = asInteractive;
                        handledActions.SetVisualOver = true;

                        if (cursor.PrimaryPush && asInteractive.IsEnabledRecursively && handledActions.handledPushPreview == false)
                        {

                            cursor.WindowPushed = asInteractive;
                            LastVisualPushed = asInteractive;

                            if (asInteractive.Push != null)
                                asInteractive.Push(asInteractive, EventArgs.Empty);


                            //cursor.GrabWindow(this);

                        }
                        if(cursor.SecondaryPush && asInteractive.IsEnabledRecursively)
                        {
                            cursor.VisualRightPushed = asInteractive;

                            //if(asInteractive.RightPush != null)
                            //{
                                //...
                            //}
                        }

                        if (cursor.PrimaryClick && asInteractive.IsEnabledRecursively) // both pushing and clicking can occur in one frame because of buffered input
                        {
                            if (cursor.WindowPushed == asInteractive)
                            {
                                if (handledActions.HandledClickPreview == false)
                                {
                                    // A genuine click resolved on its target this frame. Record it
                                    // (independent of whether Click has subscribers) so the bubbling
                                    // pass below can raise ClickBubbling on this element and its ancestors.
                                    handledActions.DidClickOccur = true;
                                }

                                if (asInteractive.Click != null && handledActions.HandledClickPreview == false)
                                {
                                    // Should InputDevice be the cursor? Or the underlying hardware?
                                    // I don't know if we have access to the underlying hardware here...
                                    var args = new InputEventArgs() { InputDevice = cursor };
                                    asInteractive.Click(asInteractive, args);


                                }
                                if(asInteractive.DoubleClick != null && cursor.PrimaryDoubleClick && handledActions.HandledClickPreview == false)
                                {
                                    var args = new InputEventArgs() { InputDevice = cursor };
                                    asInteractive.DoubleClick(asInteractive, args);
                                }
                                //if (cursor.PrimaryClickNoSlide && ClickNoSlide != null)
                                //{
                                //    ClickNoSlide(this);
                                //}

                                // if (cursor.PrimaryDoubleClick && DoubleClick != null)
                                //   DoubleClick(this);
                            }
                            else
                            {
                                //if (SlideOnClick != null)
                                //{
                                //    SlideOnClick(this);
                                //}
                            }
                        }
                        if(cursor.SecondaryClick && asInteractive.IsEnabledRecursively)
                        {
                            if(cursor.VisualRightPushed == asInteractive)
                            {
                                if (asInteractive.RightClick != null)
                                {
                                    var args = new InputEventArgs() { InputDevice = cursor };
                                    asInteractive.RightClick(asInteractive, args);
                                }
                            }
                        }

                    }
                }
                if (asInteractive?.HasEvents == true && asInteractive?.IsEnabledRecursively == true)
                {
                    if (handledActions.HandledRollOver == false && (cursor.XChange != 0 || cursor.YChange != 0))
                    {
                        var args = new RoutedEventArgs();
                        asInteractive.RollOverBubbling?.Invoke(asInteractive, args);
                        handledActions.HandledRollOver = args.Handled;
                    }

                    if (cursor.ScrollWheelChange != 0 && handledActions.HandledMouseWheel == false)
                    {
                        var args = new RoutedEventArgs();
                        asInteractive.MouseWheelScroll?.Invoke(asInteractive, args);
                        handledActions.HandledMouseWheel = args.Handled;
                    }

                    // ClickBubbling is raised child-first as the recursion unwinds. DidClickOccur
                    // ensures this only runs for the actual click target and its ancestors; a handler
                    // setting Handled stops it from reaching further ancestors.
                    if (handledActions.DidClickOccur && handledActions.HandledClick == false)
                    {
                        var args = new RoutedEventArgs();
                        asInteractive.ClickBubbling?.Invoke(asInteractive, args);
                        handledActions.HandledClick = args.Handled;
                    }
                }

            }
        }

        return handledByThis || handledByChild;
    }

    protected override bool IsOutsideOfBoundsHitTestingEnabled =>
        RaiseChildrenEventsOutsideOfBounds == true || this.Tag is ScreenSave;


    /// <summary>
    /// Returns whether the argument cursor is over this instance. If RaiseChildrenEventsOutsideOfBounds is set
    /// to true, then each of the individual chilren are also checked if the cursor is not inside this object's bounds.
    /// </summary>
    /// <remarks>
    /// This method is marked as virtual so that derived types can override it to provide custom
    /// cursor hit detection.
    /// </remarks>
    /// <param name="cursor">The cursor to check whether it is over this.</param>
    /// <param name="layer">Optional layer to use for coordinate conversion. 
    /// If this is null, then the top-most parent GraphicalUiElement's layer will be used.</param>
    /// <returns>Whether the cursor is over this.</returns>
    public virtual bool HasCursorOver(ICursor cursor, Layer? layer = null)
    {
        layer = layer ?? ((GraphicalUiElement)this.GetTopParent()).Layer;

        bool toReturn = false;

        toReturn = CheckHasCursorOverOnThis(this, cursor, layer, toReturn);

        if (!toReturn && IsOutsideOfBoundsHitTestingEnabled)
        {
            toReturn = IsOverChildren(this, cursor, layer);
        }

        return toReturn;


    }

    private static bool HasCursorOver(ICursor cursor, GraphicalUiElement thisInstance, Layer? layer)
    {
        bool toReturn = false;

        var asInteractive = thisInstance as InteractiveGue;
        toReturn = CheckHasCursorOverOnThis(thisInstance, cursor, layer, toReturn);

        if (!toReturn && (asInteractive?.RaiseChildrenEventsOutsideOfBounds == true || thisInstance.Tag is ScreenSave))
        {
            toReturn = IsOverChildren(thisInstance, cursor, layer);
        }

        return toReturn;
    }

    // The cursor's world position for the cursor walk in progress on this thread, if any.
    [ThreadStatic]
    static CursorWorldCache s_cursorWorldCache;

    // Starts caching the world position of cursor until EndCursorWalk. Returns the cache that was active, if
    // a walk was already running, so a nested walk can restore it.
    internal static CursorWorldCache BeginCursorWalk(ICursor cursor)
    {
        CursorWorldCache previous = s_cursorWorldCache;
        s_cursorWorldCache = new CursorWorldCache { Cursor = cursor };
        return previous;
    }

    internal static void EndCursorWalk(CursorWorldCache previous) => s_cursorWorldCache = previous;

    // Event handlers can move the camera, so what was converted before one ran can no longer be trusted.
    static void InvalidateCursorWorldCache() => s_cursorWorldCache.Invalidate();

    private static bool CheckHasCursorOverOnThis(GraphicalUiElement thisInstance, ICursor cursor, Layer? layer, bool toReturn)
    {
        // If this is a touch screen, then the only way the cursor is over any
        // UI element is if the cursor is being pressed.
        // Even though the finger is technically not over any UI element when 
        // the user lifts it, we still want to consider UI logic so that the click action
        // can apply and events can be raised
        var shouldConsiderBasedOnInput = cursor.LastInputDevice != InputDevice.TouchScreen ||
            cursor.PrimaryDown ||
            cursor.PrimaryClick;

        var shouldProcess = shouldConsiderBasedOnInput &&
             (thisInstance as IVisible).AbsoluteVisible == true;

        if (shouldProcess)
        {

            var managers = thisInstance.EffectiveManagers as ISystemManagers ?? ISystemManagers.Default;


            // If there are no managers, we an still fall back to the default:
            // Actually we can't here we don't have access to defaults...
            //if (managers == null)
            //{
            //    managers = global::RenderingLibrary.SystemManagers.Default;
            //}

            if (managers != null)
            {
                float worldX;
                float worldY;
                // A subtree drawn into an externally-scaled render target can supply a transform
                // that maps the raw window pixel back into the space it was drawn in (issue #4096).
                // Apply it before screen-to-world so the camera/layer transform still composes on
                // top. Resolved by climbing to the nearest ancestor that set one.
                var hitTestTransform = thisInstance.EffectiveHitTestTransformMatrix;
                var camera = managers.Renderer.Camera;

                // During a walk over many elements the cursor sits in the same world position for all
                // of them that share a camera, layer and transform, so it is converted once.
                if (!s_cursorWorldCache.TryGet(cursor, camera, layer, hitTestTransform, out worldX, out worldY))
                {
                    float cursorScreenX = cursor.X;
                    float cursorScreenY = cursor.Y;
                    // Adjust by viewport values:
                    // todo ...
                    //cursorScreenX -= managers.Renderer.GraphicsDevice.Viewport.X;
                    //cursorScreenY -= managers.Renderer.GraphicsDevice.Viewport.Y;

                    if (hitTestTransform != null)
                    {
                        var transformed = System.Numerics.Vector2.Transform(
                            new System.Numerics.Vector2(cursorScreenX, cursorScreenY),
                            hitTestTransform.Value);
                        cursorScreenX = transformed.X;
                        cursorScreenY = transformed.Y;
                    }

                    if (layer != null)
                    {
                        layer.ScreenToWorld(
                            camera,
                            cursorScreenX, cursorScreenY,
                            out worldX, out worldY);
                    }
                    else
                    {
                        camera.ScreenToWorld(
                            cursorScreenX, cursorScreenY,
                            out worldX, out worldY);
                    }

                    s_cursorWorldCache.Store(cursor, camera, layer, hitTestTransform, worldX, worldY);
                }


                // for now we'll just rely on the bounds of the GUE itself

                toReturn = thisInstance.IsPointInside(worldX, worldY);
            }
            else
            {
                var thisInstanceName = thisInstance.Name ?? $"this {thisInstance.GetType()} instance (unnamed)";
                string message =
                    $"Could not determine whether the cursor is over {thisInstanceName} because" +
                    " it is not on any camera, nor is a default camera set up. Did you forget to add this (or its parent) to managers?";
                throw new Exception(message);
            }
        }

        return toReturn;
    }

    protected static bool IsOverChildren(GraphicalUiElement thisInstance, ICursor cursor, Layer? layer)
    {
        bool toReturn = false;
        if (thisInstance.Children == null)
        {
            // It's a screen
            foreach (var child in thisInstance.ContainedElements)
            {
                if (HasCursorOver(cursor, child, layer))
                {
                    toReturn = true;
                    break;
                }
            }
        }
        else
        {
            for (int i = 0; i < thisInstance.Children.Count; i++)
            {
                var child = thisInstance.Children[i] as GraphicalUiElement;

                if(child is InteractiveGue asInteractiveGue)
                {
                    if(asInteractiveGue.HasCursorOver(cursor, layer))
                    {
                        toReturn = true;
                        break;
                    }
                }
                else
                {
                    if (child != null && HasCursorOver(cursor, child, layer))
                    {
                        toReturn = true;
                        break;
                    }

                }
            }
        }

        return toReturn;
    }

    public bool IsInParentChain(InteractiveGue possibleParent)
    {
        if (Parent == possibleParent)
        {
            return true;
        }
        else if (Parent is InteractiveGue parentGue)
        {
            return parentGue.IsInParentChain(possibleParent);
        }
        else
        {
            return false;
        }
    }

    static bool IsComponentOrInstanceOfComponent(GraphicalUiElement gue)
    {
        if (gue.Tag is Gum.DataTypes.ComponentSave)
        {
            return true;
        }
        else if (gue.Tag is Gum.DataTypes.InstanceSave instance)
        {

            var baseType = instance.BaseType;

            if (
                baseType == "Arc" ||
                baseType == "Canvas" ||
                baseType == "Circle" ||
                baseType == "ColoredCircle" ||
                baseType == "ColoredRectangle" ||
                // Vic says - a user may want to click on a container like a track, 
                // so we prob should allow clicks?
                // Update - no - doing this seems to ruin all kinds of UI because containers
                // steal clicks from their children. We will check if the container has an explicit
                // event, otherwise, it will pass it along to its children.
                baseType == "Container" ||
                baseType == "LottieAnimation" ||

                baseType == "NineSlice" ||
                baseType == "Polygon" ||

                baseType == "Rectangle" ||
                baseType == "RoundedRectangle" ||
                baseType == "Sprite" ||
                baseType == "Svg" ||
                baseType == "Text")
            {
                return false;
            }
            else
            {
                // If we got here, then it's a component
                return true;
            }
        }
        return false;
    }

    public InteractiveGue()
    {
    }

    public InteractiveGue(IRenderable renderable) : base(renderable)
    {
    }

    bool _hasSubscribedLosePusheEvents = false;
    private void TrySubscribeToLosePushEvents()
    {
        if(!_hasSubscribedLosePusheEvents)
        {
            _hasSubscribedLosePusheEvents = true;
            Click += (_, _) => _losePush?.Invoke(this, EventArgs.Empty);
            RollOff += (_, args) =>
            {
                ICursor? cursor = (args as InputEventArgs)?.InputDevice as ICursor;

                if(cursor?.WindowPushed == this)
                {
                    _losePush?.Invoke(this, EventArgs.Empty);
                }

            };
        }
    }

    // See DoNextClickAndPushActions for details on why this is needed
    static readonly List<Action> _processWideNextPushActionHoldingList = new();

    static List<Action> nextPushActionHoldingList => GumContext.Own?.NextPushActionHoldingList ?? _processWideNextPushActionHoldingList;

    /// <summary>
    /// Adds an action to be called the next time the Cursor performs a push action 
    /// (the left button is not down the previous frame but is down this frame). The 
    /// argument action is invoked one time.
    /// </summary>
    /// <param name="action">The action to invoke one time.</param>
    /// <exception cref="ArgumentNullException">Thrown if the argument action is null.</exception>
    public static void AddNextPushAction(Action action)
    {
#if FULL_DIAGNOSTICS
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
#endif
        nextPushActionHoldingList.Add(action);
    }

    /// <summary>
    /// Adds an action to be called the next time the Cursor performs a push action, like
    /// <see cref="AddNextPushAction(Action)"/>, but the action is dropped without running if
    /// <paramref name="owner"/> has left the tree (it has no <see cref="GraphicalUiElement.EffectiveManagers"/>)
    /// by the time the push happens. Use this for handlers that touch the owner so they cannot
    /// run against a control that was removed or disposed.
    /// </summary>
    /// <param name="action">The action to invoke one time.</param>
    /// <param name="owner">The visual the action belongs to.</param>
    public static void AddNextPushAction(Action action, GraphicalUiElement owner)
    {
        AddNextPushAction(() =>
        {
            if (owner.EffectiveManagers != null)
            {
                action();
            }
        });
    }

    public static void ClearNextPushActions()
    {
        nextPushActionHoldingList.Clear();
        nextPushActions.Clear();
    }

    // See DoNextClickAndPushActions for details on why this is needed
    static readonly List<Action> _processWideNextClickActionHoldingList = new();

    static List<Action> nextClickActionHoldingList => GumContext.Own?.NextClickActionHoldingList ?? _processWideNextClickActionHoldingList;

    public static void ClearNextClickActions()
    {
        nextClickActionHoldingList.Clear();
        nextClickActions.Clear();
    }

    /// <summary>
    /// Adds an action to be called the next time the Cursor performs a click action
    /// (the left button was down last frame and is released this frame). The argument
    /// action is invoked one time.
    /// </summary>
    /// <param name="action">The action to invoke one time.</param>
    /// <exception cref="ArgumentNullException">Thrown if the argument action is null.</exception>
    public static void AddNextClickAction(Action action)
    {
#if FULL_DIAGNOSTICS
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
#endif
        nextClickActionHoldingList.Add(action);
    }

    /// <summary>
    /// Adds an action to be called the next time the Cursor performs a click action, like
    /// <see cref="AddNextClickAction(Action)"/>, but the action is dropped without running if
    /// <paramref name="owner"/> has left the tree (it has no <see cref="GraphicalUiElement.EffectiveManagers"/>)
    /// by the time the click happens.
    /// </summary>
    /// <param name="action">The action to invoke one time.</param>
    /// <param name="owner">The visual the action belongs to.</param>
    public static void AddNextClickAction(Action action, GraphicalUiElement owner)
    {
        AddNextClickAction(() =>
        {
            if (owner.EffectiveManagers != null)
            {
                action();
            }
        });
    }

    internal static void DoNextClickAndPushActions(ICursor cursor, bool isInWindow)
    {
        if (isInWindow == false) return;

        if(cursor.PrimaryClick)
        {
            if (nextClickActions.Count > 0)
            {
                var items = nextClickActions.ToList();
                // clear first so that any actions can add more click actions that won't get run this frame:
                nextClickActions.Clear();
                foreach (var item in items)
                {
                    item();
                }
            }
        }

        if(cursor.PrimaryPush)
        {
            if (nextPushActions.Count > 0)
            {
                var items = nextPushActions.ToList();
                nextPushActions.Clear();
                foreach (var item in items)
                {
                    item();
                }
            }
        }

        // When AddNextPushAction or AddNextClickAction are called, the user expects:
        // 1. That it will not be raised immediately if added inside a push/click event
        // 2. That it will have access to the WindowPushed/VisualOver
        // This means that we cannot immediately run new events, but that all events should
        // be run *after* we do our every-frame logic of detecting whether the user is over a
        // window.
        // To satisfy this, we store new events in a holding list, and then add them after we run through
        // existing items:
        nextClickActions.AddRange(nextClickActionHoldingList);
        nextPushActions.AddRange(nextPushActionHoldingList);

        nextClickActionHoldingList.Clear();
        nextPushActionHoldingList.Clear();
    }


    /// <summary>
    /// Creates a copy of this visual. The clone is visual-only: it has no Forms control
    /// (<see cref="FormsControlAsObject"/> is null, so a cloned <c>ButtonVisual</c> is not a working
    /// button) and none of this visual's event handlers. A Forms control is not recreated for the
    /// clone because a custom control often assigns its own Forms object during initialization, and
    /// the order of that assignment relative to other property assignments can change behavior.
    /// </summary>
    public override GraphicalUiElement Clone()
    {
        InteractiveGue clone = (InteractiveGue)base.Clone();

        // The copied handlers belong to whoever subscribed to the source, usually its Forms
        // control, so clicking the clone would click the source's control. That control's Visual
        // is the source, so the clone has no control of its own.
        clone.Click = null;
        clone.ClickBubbling = null;
        clone.ClickPreview = null;
        clone.DoubleClick = null;
        clone.Dragging = null;
        clone.EnabledChange = null;
        clone.HoverOver = null;
        clone._losePush = null;
        clone._hasSubscribedLosePusheEvents = false;
        clone.MouseWheelScroll = null;
        clone.PushPreview = null;
        clone.Push = null;
        clone.RemovedAsPushed = null;
        clone.RightClick = null;
        clone.RollOff = null;
        clone.RollOn = null;
        clone.RollOver = null;
        clone.RollOverBubbling = null;
        clone._formsControlAsObject = null;

        return clone;
    }

    public override void RemoveFromManagers()
    {
        base.RemoveFromManagers();

        if(InteractiveGue.CurrentInputReceiver == this.FormsControlAsObject || InteractiveGue.CurrentInputReceiver == this)
        {
            InteractiveGue.CurrentInputReceiver = null;
        }
    }

    public override string ToString()
    {
        if(this.FormsControlAsObject != null)
        {
            return $"{base.ToString()} for {FormsControlAsObject.GetType()}" ;
        }
        else
        {
            return base.ToString();
        }
    }
}

public interface IInputReceiver
{
    /// <summary>
    /// Called by the engine automatically when an IInputReceiver gains focus.
    /// </summary>
    /// <remarks>
    /// The implementation of this method should raise the GainFocus event.
    /// </remarks>
    void OnGainFocus();

    /// <summary>
    /// Called by the engine automatically when an IInputReceiver loses focus.
    /// </summary>
    void OnLoseFocus();

    /// <summary>
    /// Called every frame if this has focus. Allows general every-frame updates such as
    /// handling gamepad input.
    /// </summary>
    void OnFocusUpdate();

    /// <summary>
    /// Called every frame before OnFocusUpdate with the root-most control calling this first, then
    /// down to its children. If this is handled, children do not receive this event.
    /// </summary>
    /// <param name="args">Args, which if IsHandled is set to true prevent children from receiving this </param>
    void OnFocusUpdatePreview(RoutedEventArgs args);

    void DoKeyboardAction(IInputReceiverKeyboard keyboard);

    IInputReceiver? ParentInputReceiver { get; }
}

public enum InputDevice
{
    TouchScreen = 1,
    Mouse = 2
}

public enum Cursors
{
    Arrow,
    SizeNESW,
    SizeNS,
    SizeNWSE,
    SizeWE,
    // more may be added in the future
}

/// <summary>
/// Determines how to evaluate whether to show visual over states based on the HasEvents property.
/// </summary>
/// <remarks>
/// FlatRedBall was built around the assumption that HasEvents is not enough to determine whether
/// a visual should respond to Cursor events. This is because users may set HasEvents on a StandardElement
/// in the Gum tool, which would default all instances of that standard to have the value set to true. This 
/// could result in controls which use this visual unexpectedly having their events swallowed by the child visual.
/// To avoid this, FlatRedBall checks both HasEvents and whether any events are actually attached to the visual.
/// However, this behavior is very confusing, and as new runtimes are introduced we can simplify things. To continue
/// to support the old FRB behavior, this enum exists to allow users to select the old behavior or the new simplified behavior.
/// Over time, we may get rid of the old behavior completely, or at least default to the new one.
/// </remarks>
public enum VisualOverBehavior
{
    /// <summary>
    /// VisualOver is only set if HasEvents is true and if the visual has events attached. This
    /// exists to support old projects. New projects should use IfHasEventsIsTrue
    /// </summary>
    OnlyIfEventsAreNotNullAndHasEventsIsTrue,
    /// <summary>
    /// VisualOver is set whenever HasEvents is true, regardless of whether any specific event has
    /// a subscriber. This is the default and recommended behavior.
    /// </summary>
    IfHasEventsIsTrue
}
public interface ICursor
{
    /// <summary>
    /// Controls how an element is determined to be "over" the cursor for event purposes. Defaults
    /// to <see cref="VisualOverBehavior.IfHasEventsIsTrue"/>, matching what
    /// <c>FormsUtilities.InitializeDefaults</c> assigns; the legacy value exists only for explicit opt-in.
    /// </summary>
    public static VisualOverBehavior VisualOverBehavior { get; set; } =
        VisualOverBehavior.IfHasEventsIsTrue;

    public Cursors? CustomCursor { get; set; }
    InputDevice LastInputDevice { get; }
    int X { get; }
    int Y { get; }
    float XRespectingGumZoomAndBounds();
    float YRespectingGumZoomAndBounds();

    double LastPrimaryPushTime { get; }
    double LastPrimaryClickTime { get; }

    int XChange { get; }
    int YChange { get; }

    int ScrollWheelChange { get; }

    float ZVelocity { get; }

    bool PrimaryPush { get; }
    bool PrimaryDown { get; }
    bool PrimaryClick { get; }
    /// <summary>
    /// Returns whether the cursor has been clicked without movement between the push and release.
    /// Simple implementations can return PrimaryClick, but more complex implementations may want to
    /// consider a movement threshold.
    /// </summary>
    bool PrimaryClickNoSlide { get; }
    bool PrimaryDoubleClick { get; }
    bool PrimaryDoublePush { get; }

    bool SecondaryPush { get; }
    bool SecondaryDown { get; }
    bool SecondaryClick { get; }
    bool SecondaryDoubleClick { get; }

    bool MiddlePush { get; }
    bool MiddleDown { get; }
    bool MiddleClick { get; }
    bool MiddleDoubleClick { get; }

    /// <summary>
    /// Returns the visual that was last pushed.
    /// </summary>
    InteractiveGue? WindowPushed { get; set; }

    InteractiveGue? VisualRightPushed { get; set; }

    [Obsolete("Use VisualOver instead")]
    InteractiveGue? WindowOver { get; set; }

    InteractiveGue? VisualOver { get; set; }

    /// <summary>
    /// Returns the visual that was last pushed. Defaults to <see cref="WindowPushed"/>, the older name
    /// for the same value.
    /// </summary>
    InteractiveGue? VisualPushed
    {
        get => WindowPushed;
        set => WindowPushed = value;
    }

    /// <summary>
    /// The X position the cursor had on the previous frame. Defaults to <see cref="X"/> minus <see cref="XChange"/>.
    /// </summary>
    int LastX => X - XChange;

    /// <summary>
    /// The Y position the cursor had on the previous frame. Defaults to <see cref="Y"/> minus <see cref="YChange"/>.
    /// </summary>
    int LastY => Y - YChange;

    /// <summary>
    /// The Forms control whose visual the cursor is currently over, or null if there is none or the
    /// visual has no Forms control. Defaults to the control of <see cref="VisualOver"/>.
    /// </summary>
    Gum.Forms.Controls.FrameworkElement? FrameworkElementOver =>
        VisualOver?.FormsControlAsObject as Gum.Forms.Controls.FrameworkElement;

    /// <summary>
    /// The Forms control that was under the cursor when the primary button was pushed, or null if there
    /// is none or the visual has no Forms control. Defaults to the control of <see cref="VisualPushed"/>.
    /// </summary>
    Gum.Forms.Controls.FrameworkElement? FrameworkElementPushed =>
        VisualPushed?.FormsControlAsObject as Gum.Forms.Controls.FrameworkElement;

    /// <summary>
    /// The Forms control that was under the cursor when the right button was pushed, or null if there
    /// is none or the visual has no Forms control. Defaults to the control of <see cref="VisualRightPushed"/>.
    /// </summary>
    Gum.Forms.Controls.FrameworkElement? FrameworkElementRightPushed =>
        VisualRightPushed?.FormsControlAsObject as Gum.Forms.Controls.FrameworkElement;

    /// <summary>
    /// The 2D transform applied to the raw device position to produce <see cref="X"/> and <see cref="Y"/>,
    /// used when Gum is drawn to a render target or a modified viewport. Read-only here: set it on the
    /// concrete cursor. Defaults to the identity matrix, which is what a cursor that applies no transform reports.
    /// On MonoGame, KNI and FNA the concrete <c>Cursor</c> keeps its XNA <c>Matrix</c> property, and this member
    /// is the platform-neutral 2D view of it (Z and perspective are not included).
    /// </summary>
    System.Numerics.Matrix3x2 TransformMatrix => System.Numerics.Matrix3x2.Identity;

    /// <summary>
    /// Clears the cursor's per-frame input state. Does nothing by default.
    /// </summary>
    void ClearInputValues() { }

    public void Activity(double currentGameTimeTotalSeconds);
}

public interface IInputReceiverKeyboard
    //<T> where T : struct, System.Enum
{
    bool IsShiftDown { get; }
    bool IsCtrlDown { get; }
    bool IsAltDown { get; }

    /// <summary>
    /// Whether the macOS Command key is held. Text controls accept it in place of Ctrl for the clipboard and
    /// select-all shortcuts (Command+C/X/V/A), which is where macOS users press them. Defaults to false, so
    /// runtimes that do not report Command keep Ctrl-only shortcuts.
    /// </summary>
    bool IsCommandDown => false;

    /// <summary>
    /// Returns the keys typed this frame in the shared <see cref="Gum.Forms.Input.Keys"/>
    /// space. Implementations must translate from any native key type to Gum/XNA values
    /// before yielding.
    /// </summary>
    IEnumerable<Gum.Forms.Input.Keys> KeysTyped { get; }

    /// <summary>
    /// Whether this keyboard can surface an inline soft keyboard through
    /// <see cref="ShowKeyboard"/> / <see cref="HideKeyboard"/>. When true, text controls use
    /// it instead of the modal <c>INativeTextInput</c> dialog, because typed characters arrive
    /// through the normal input pipeline. Defaults to false.
    /// </summary>
    bool SupportsInlineKeyboard => false;

    /// <summary>
    /// Default no-op. Runtimes with soft-keyboard / IME support (e.g. Android) override
    /// this to surface the native on-screen keyboard.
    /// </summary>
    void ShowKeyboard() { }

    /// <summary>
    /// Default no-op. Runtimes with soft-keyboard / IME support (e.g. Android) override
    /// this to dismiss the native on-screen keyboard.
    /// </summary>
    void HideKeyboard() { }


    string GetStringTyped();

    /// <summary>
    /// Advances the keyboard one frame. Must be called once per frame before any
    /// <see cref="KeysTyped"/> / <see cref="GetStringTyped"/> / key-query reads, so per-frame
    /// caches reset on the correct boundary.
    /// </summary>
    /// <param name="gameTime">The total game time, in seconds.</param>
    void Activity(double gameTime);

    /// <summary>
    /// Returns whether the specified key is currently held down.
    /// </summary>
    /// <param name="key">The key to query, in the shared <see cref="Gum.Forms.Input.Keys"/> space.</param>
    bool KeyDown(Gum.Forms.Input.Keys key);

    /// <summary>
    /// Returns whether the specified key was pressed this frame (down this frame, not down last frame).
    /// </summary>
    /// <param name="key">The key to query, in the shared <see cref="Gum.Forms.Input.Keys"/> space.</param>
    bool KeyPushed(Gum.Forms.Input.Keys key);

    /// <summary>
    /// Returns whether the specified key was released this frame (down last frame, not down this frame).
    /// </summary>
    /// <param name="key">The key to query, in the shared <see cref="Gum.Forms.Input.Keys"/> space.</param>
    bool KeyReleased(Gum.Forms.Input.Keys key);

    /// <summary>
    /// Returns whether the specified key was "typed" this frame — either pushed this frame,
    /// or held long enough to trigger the OS key-repeat. Matches the behavior of
    /// <c>Microsoft.Xna.Framework.Input.Keys</c>-based <c>KeyTyped</c> on MonoGame and
    /// <c>Raylib.IsKeyPressedRepeat</c> on Raylib.
    /// </summary>
    /// <param name="key">The key to query, in the shared <see cref="Gum.Forms.Input.Keys"/> space.</param>
    bool KeyTyped(Gum.Forms.Input.Keys key);
}



// The cursor's position in world space, kept for one cursor walk for the camera, layer and hit-test
// transform it was converted for. A struct so starting a walk allocates nothing.
internal struct CursorWorldCache
{
    public ICursor? Cursor;
    private bool _hasValue;
    private global::RenderingLibrary.Camera? _camera;
    private Layer? _layer;
    private System.Numerics.Matrix3x2? _hitTestTransform;
    private float _worldX;
    private float _worldY;

    public bool TryGet(ICursor cursor, global::RenderingLibrary.Camera camera, Layer? layer,
        System.Numerics.Matrix3x2? hitTestTransform, out float worldX, out float worldY)
    {
        if (_hasValue && ReferenceEquals(cursor, Cursor) && ReferenceEquals(camera, _camera) &&
            ReferenceEquals(layer, _layer) && hitTestTransform == _hitTestTransform)
        {
            worldX = _worldX;
            worldY = _worldY;
            return true;
        }
        worldX = 0;
        worldY = 0;
        return false;
    }

    public void Store(ICursor cursor, global::RenderingLibrary.Camera camera, Layer? layer,
        System.Numerics.Matrix3x2? hitTestTransform, float worldX, float worldY)
    {
        // Only a walk that began caching for this cursor keeps what it converted.
        if (ReferenceEquals(cursor, Cursor))
        {
            _hasValue = true;
            _camera = camera;
            _layer = layer;
            _hitTestTransform = hitTestTransform;
            _worldX = worldX;
            _worldY = worldY;
        }
    }

    public void Invalidate() => _hasValue = false;
}

// "Already handled" flags threaded through the DoUiActivityRecursively walk. A struct passed by ref
// so the walk allocates it on the stack rather than the heap each frame; ref threading preserves the
// shared-mutable-reference semantics the class had, and each Update gets its own instance.
struct HandledActions
{
    public bool HandledMouseWheel;
    public bool HandledRollOver;
    public bool HandledClickPreview;
    public bool handledPushPreview;
    public bool SetVisualOver;

    /// <summary>
    /// Set to true once a primary click has resolved on its target (the element where the
    /// push originated and the release occurred). This gates the bubbling ClickBubbling event
    /// so it only fires on the clicked element and its ancestors, not on unrelated elements
    /// the cursor happens to be over.
    /// </summary>
    public bool DidClickOccur;

    /// <summary>
    /// Set to true when a ClickBubbling handler marks the event Handled, stopping it from
    /// bubbling to further ancestors.
    /// </summary>
    public bool HandledClick;
}
public static class GueInteractiveExtensionMethods
{

    static List<GraphicalUiElement> internalList = new List<GraphicalUiElement>();
    public static void DoUiActivityRecursively(this GraphicalUiElement gue, ICursor cursor, IInputReceiverKeyboard keyboard, double currentGameTimeInSeconds)
    {
        internalList.Clear();
        internalList.Add(gue);

        DoUiActivityRecursively(internalList, cursor, keyboard, currentGameTimeInSeconds);
    }

    static List<IInputReceiver> previewList = new();

    // Tests the roots from the topmost down until one takes the cursor.
    static void DoUiActivityOnRoots(IList<GraphicalUiElement> gues, ICursor cursor, ref HandledActions actions)
    {
        CursorWorldCache previousWorldCache = InteractiveGue.BeginCursorWalk(cursor);
        try
        {
            for (int i = gues.Count - 1; i > -1; i--)
            {
                var gue = gues[i];

                // Other platforms besides FRB are more explicit on their additions to roots or GumBatch, so
                // process this every time

#if FRB
                // This check allows the user to remove a GUE from managers and not null it out.
                // Even though it might be proper to null it out, this removes "yet another thing to remember"
                if (gue.EffectiveManagers != null)
#endif
                {
                    // A root with no events anywhere under it cannot handle anything, so it is not tested.
                    if (InteractiveGue.SubtreeHasEvents(gue))
                    {
                        InteractiveGue.DoUiActivityRecursively(cursor, ref actions, gue, gue.Layer);
                    }
                }
                if (cursor.VisualOver != null)
                {
                    break;
                }
            }
        }
        finally
        {
            InteractiveGue.EndCursorWalk(previousWorldCache);
        }
    }
    public static void DoUiActivityRecursively(IList<GraphicalUiElement> gues, ICursor cursor, IInputReceiverKeyboard keyboard, double currentGameTimeInSeconds)
    {
#if FULL_DIAGNOSTICS
        if (cursor == null)
        {
            throw new ArgumentNullException(nameof(cursor));
        }
#endif

        InteractiveGue.CurrentGameTime = currentGameTimeInSeconds;
        var visualOverBefore = cursor.VisualOver;
        var windowPushedBefore = cursor.WindowPushed;
        var VisualRightPushedBefore = cursor.VisualRightPushed;

        var cursorX = cursor.XRespectingGumZoomAndBounds();
        var cursorY = cursor.YRespectingGumZoomAndBounds();

        var isInWindow = cursorX >= 0 && cursorX < GraphicalUiElement.CanvasWidth &&
            cursorY >= 0 && cursorY < GraphicalUiElement.CanvasHeight;


        HandledActions actions = new HandledActions();
        var lastVisualOver = cursor.VisualOver;


        cursor.VisualOver = null;
        DoUiActivityOnRoots(gues, cursor, ref actions);

        var visualOverAsInteractive = cursor.VisualOver as InteractiveGue;
        if (visualOverAsInteractive != null)
        {
            if (lastVisualOver != visualOverAsInteractive)
            {
                visualOverAsInteractive.TryCallRollOn();
            }

            visualOverAsInteractive.TryCallHoverOver();
            if (cursor.XChange != 0 || cursor.YChange != 0)
            {
                visualOverAsInteractive.TryCallRollOver();
            }
        }

        if (!actions.SetVisualOver)
        {
            cursor.VisualOver = null;
        }
        else if(cursor.VisualOver == null)
        {
            cursor.VisualOver = visualOverBefore;
        }

        if(visualOverBefore != cursor.VisualOver)
        {
            if (visualOverBefore is InteractiveGue interactiveBefore)
            {
                interactiveBefore.TryCallRollOff(cursor);
            }
        }
        if(windowPushedBefore != cursor.WindowPushed || 
            (windowPushedBefore != null && cursor.PrimaryDown == false))
        {
            if(windowPushedBefore is InteractiveGue interactiveBefore)
            {
                interactiveBefore.TryCallRemoveAsPushed();
            }
        }
        if(cursor.PrimaryDown == false)
        {
            InteractiveGue.LastVisualPushed = null;
            cursor.WindowPushed = null;
        }
        if(cursor.SecondaryDown == false)
        {
            cursor.VisualRightPushed = null;
        }
        if(cursor.WindowPushed != null && cursor.PrimaryDown)
        {
            cursor.WindowPushed.TryCallDragging();
        }

        // the click/push actions need to be after the UI activity
        // Update September 13, 2025
        // Why do they need to happen after UI activity? I can understand
        // why they need to happen after updating the cursor properties, but
        // if they happen after, then any button that registers a next click in
        // its own click will then immediately have the click fire on the same frame.
        // This is confusing behavior because users would expect it to be the *next* click
        // not the current click, and this makes closing windows that were just opened much
        // harder to do.
        // Update 2 - The reason this logic must happen after normal UI logic is because some
        // actions added may inspect the VisualOver or WindowPushed properties, and those are 
        // set during the UI activity.
        // This does cause the problem of click and push events being called immediately, which
        // means the InteractiveGue must only run events which are at least 1 frame old
        InteractiveGue.DoNextClickAndPushActions(cursor, isInWindow);

        if (InteractiveGue.CurrentInputReceiver != null)
        {
            var receiver = InteractiveGue.CurrentInputReceiver;

            previewList.Clear();
            previewList.Add(receiver);


            var parent = receiver.ParentInputReceiver;
            while(parent != null)
            {
                previewList.Insert(0, parent);
                parent = parent.ParentInputReceiver;
            }

            bool wasCancelled = false;
            foreach(var toPreview in previewList)
            {
                var args = new RoutedEventArgs();
                if(!wasCancelled)
                {
                    toPreview.OnFocusUpdatePreview(args);
                    wasCancelled = args.Handled;

                    if(wasCancelled)
                    {
                        break;
                    }    
                }
            }

            if(!wasCancelled)
            {
                receiver.DoKeyboardAction(keyboard);
                receiver.OnFocusUpdate();
            }
        }
    }
}

