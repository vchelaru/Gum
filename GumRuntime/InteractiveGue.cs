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
/// The base object for all Gum runtime objects. It contains functionality for
/// setting variables, states, and performing layout. The GraphicalUiElement can
/// wrap an underlying rendering object.
/// </summary>
public partial class InteractiveGue : BindableGue
{
    static List<Action> nextPushActions = new List<Action>();
    static List<Action> nextClickActions = new List<Action>();

    /// <summary>
    /// The current game time, assigned internally when calling GumService.Update. This can be used by UI elements to perform
    /// timing-based actions, such as moving a slider on a long press.
    /// </summary>
    public static double CurrentGameTime { get; internal set; }

    static IInputReceiver currentInputReceiver;

    
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
    /// on its type, usually when objects are created from a Gum project. Objects which 
    /// should consume cursor events without raising them should keep this value set to true
    /// but should set IsEnabled to false.
    /// </summary>
    public bool HasEvents { get; set; } = true;
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
                EnabledChange?.Invoke(this, null);
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

    /// <summary>
    /// Provides an uncasted reference to the Gum Forms element which uses this as visual element.
    /// </summary>
    public virtual object FormsControlAsObject { get; set; }

    #region Events 

    /// <summary>
    /// Event raised whenever this is clicked by a cursor. A click occurs
    /// when the cursor is over this and is first pushed, then released.
    /// </summary>
    public event EventHandler Click;

    public event EventHandler<RoutedEventArgs> ClickPreview;
    public event EventHandler<RoutedEventArgs> PushPreview;

    /// <summary>
    /// Event raised whenever this is double-clicked by a cursor. A double-click occurs
    /// when the cursor is over this and the left mouse button is clicked twice in rapid succession.
    /// </summary>
    public event EventHandler DoubleClick;

    /// <summary>
    /// Event which is raised whenever this is right-clicked by a cursor. A right-click occurs
    /// when the cursor is over this and is first pushed, then released.
    /// </summary>
    public event EventHandler RightClick;

    /// <summary>
    /// Event which is raised whenever this is pushed by a cursor. A push occurs
    /// when the cursor is over this and the left mouse button is pushed (not down last frame,
    /// down this frame).
    /// </summary>
    public event EventHandler Push;

    /// <summary>
    /// Event which is raised whenever this loses a push. A push occurs when the
    /// cursor is over this window and the left mouse button is pushed. A push is lost
    /// if the left mouse button is released or if the user moves the cursor so that it
    /// is no longer over this while the mouse button is pressed. 
    /// </summary>
    /// <remarks>
    /// LosePush is often used to change the state of a button back to its regular state.
    /// </remarks>
    //public event EventHandler LosePush;
    public event EventHandler LosePush
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
    private EventHandler _losePush;


    /// <summary>
    /// Event raised when the cursor first moves over this object.
    /// </summary>
    public event EventHandler RollOn;
    /// <summary>
    /// Event when the cursor first leaves this object.
    /// </summary>
    public event EventHandler RollOff;
    /// <summary>
    /// Event raised every frame the cursor is over this object and the Cursor has changed position.
    /// This event is not raized if the cursor has moved off of the object. For events raised when the 
    /// cursor is not over this instance, see Dragging.
    /// </summary>
    public event EventHandler RollOver;

    /// <summary>
    /// Event raised every frame the cursor is over this object whether the Cursor has changed positions or not.
    /// </summary>
    public event EventHandler HoverOver;

    /// <summary>
    /// Event raised when the cursor pushes on an object and moves. This is similar to RollOver, 
    /// but is raised even if outside of the bounds of the object. This can be used if an 
    /// object is to be moved by dragging since it will be raised even if the user moves the 
    /// cursor quickly outside of its bounds.
    /// </summary>
    public event EventHandler Dragging;

    /// <summary>
    /// Event raised when the Enabled property changed.
    /// </summary>
    public event EventHandler EnabledChange;

    /// <summary>
    /// Eent raised when the mouse wheel has been scrolled while the cursor is over this instance.
    /// This event is raised bottom-up, with the root object having the opportunity to handle the roll over.
    /// If a control sets the argument RoutedEventArgs Handled to true, the children objects 
    /// will not have this event raised.
    /// </summary>
    public event Action<object, RoutedEventArgs> MouseWheelScroll;

    /// <summary>
    /// Event raised when the mouse rolls over this instance. This event is raised top-down, with the
    /// child object having the opportunity to handle the roll over first. If a control sets the argument 
    /// RoutedEventArgs Handled to true,
    /// then parent objects will not have this event raised.
    /// </summary>
    public event Action<object, RoutedEventArgs> RollOverBubbling;

    /// <summary>
    /// Event raised when this Window is pushed, then is no longer the pushed window due to a cursor releasing the primary button.
    /// This can be used to detect the end of a drag operation, or to reset the state of a button.
    /// </summary>
    public event EventHandler RemovedAsPushed;

    public void CallClick() => Click?.Invoke(this, EventArgs.Empty);
    public void CallRightClick() => RightClick?.Invoke(this, EventArgs.Empty);  

    // RollOff is determined outside of the individual InteractiveGue so we need to have this callable externally..
    public void TryCallRollOff()
    {
        RollOff?.Invoke(this, EventArgs.Empty);
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

    private bool DoUiActivityRecursively(ICursor cursor, Layer layer, HandledActions handledActions = null)
    {
        return DoUiActivityRecursively(cursor, handledActions, this, layer);

    }

    internal static bool DoUiActivityRecursively(ICursor cursor, HandledActions handledActions, GraphicalUiElement currentItem, Layer layer)
    { 
        handledActions = handledActions ?? new HandledActions();
        bool handledByChild = false;
        bool handledByThis = false;

        bool isOver = HasCursorOver(cursor, currentItem, layer);
        var asInteractive = currentItem as InteractiveGue;

        // Even though the cursor is over "this", we need to check if the cursor is over any children in case "this" exposes its children events:
        if (isOver && (asInteractive == null || asInteractive.ExposeChildrenEvents))
        {
            if(asInteractive != null && asInteractive.HasEvents  && asInteractive.IsEnabledRecursively)
            {
                if(asInteractive.ClickPreview != null &&
                    !handledActions.HandledClickPreview && cursor.PrimaryClick)
                {
                    var args = new InputEventArgs() { InputDevice = cursor };
                    asInteractive.ClickPreview(asInteractive, args);

                    if(args.Handled)
                    {
                        cursor.WindowPushed = asInteractive;
                        handledActions.HandledClickPreview = true;
                    }
                }
                if(asInteractive.PushPreview != null &&
                    !handledActions.handledPushPreview && cursor.PrimaryPush)
                {
                    var args = new InputEventArgs() { InputDevice = cursor };
                    asInteractive.PushPreview(asInteractive, args);

                    if (args.Handled)
                    {
                        handledActions.handledPushPreview = true;
                    }
                }
            }


            #region Try handling by children

            if(currentItem.Children == null)
            {
                for(int i = currentItem.ContainedElements.Count - 1; i > -1; i--)
                {
                    var child = currentItem.ContainedElements[i] as GraphicalUiElement;

                    if (child != null && HasCursorOver(cursor, child, layer))
                    {
                        handledByChild = DoUiActivityRecursively(cursor, handledActions, child, layer);

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
                    var child = currentItem.Children[i] as GraphicalUiElement;
                        // Children should always have the opportunity to handle activity,
                        // even if they are not components, because they may contain components as their children


                    // If the child either has events or exposes children events, then give it a chance to handle this activity.

                    if (child != null && HasCursorOver(cursor, child, layer))
                    {
                        handledByChild = DoUiActivityRecursively(cursor, handledActions, child, layer);

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
            if (IsComponentOrInstanceOfComponent(currentItem)
                ||
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
                asInteractive?.MouseWheelScroll != null
                // if it has events and it has a Forms control, then let's consider it a click
                //|| asInteractive?.FormsControlAsObject != null
                // Update July 18, 2025 
                // We can't do this because
                // if we do, full screen containers
                // consume all clicks. We need to come
                // up with another way to do this. For now
                // the events are the hack
                )
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
                        cursor.WindowOver = asInteractive;
                        handledActions.SetWindowOver = true;

                        if (cursor.PrimaryPush && asInteractive.IsEnabledRecursively && handledActions.handledPushPreview == false)
                        {

                            cursor.WindowPushed = asInteractive;

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
                }

            }
        }

        return handledByThis || handledByChild;
    }

    /// <summary>
    /// Returns whether the argument cursor is over this instance. If RaiseChildrenEventsOutsideOfBounds is set
    /// to true, then each of the individual chilren are also checked if the cursor is not inside this object's bounds.
    /// </summary>
    /// <param name="cursor">The cursor to check whether it is over this.</param>
    /// <returns>Whether the cursor is over this.</returns>
    public bool HasCursorOver(ICursor cursor)
    {
        var layer = (this.GetTopParent() as GraphicalUiElement).Layer;
        return HasCursorOver(cursor, this, layer);
    }

    private static bool HasCursorOver(ICursor cursor, GraphicalUiElement thisInstance, Layer layer)
    { 
        bool toReturn = false;

        var asInteractive = thisInstance as InteractiveGue;

        // If this is a touch screen, then the only way the cursor is over any
        // UI element is if the cursor is being pressed.
        // Even though the finger is technically not over any UI element when 
        // the user lifts it, we still want to consider UI logic so that the click action
        // can apply and events can be raised
        // todo - implement it later
        //var shouldConsiderBasedOnInput = cursor.LastInputDevice != InputDevice.TouchScreen ||
        //    cursor.PrimaryDown ||
        //    cursor.PrimaryClick;

        bool shouldConsiderBasedOnInput = true;

        var shouldProcess = shouldConsiderBasedOnInput &&
             (thisInstance as IVisible).AbsoluteVisible == true;

        if (shouldProcess)
        {
            int cursorScreenX = cursor.X;
            int cursorScreenY = cursor.Y;
            float worldX;
            float worldY;

            var managers = thisInstance.EffectiveManagers as ISystemManagers;


            // If there are no managers, we an still fall back to the default:
            // Actually we can't here we don't have access to defaults...
            //if (managers == null)
            //{
            //    managers = global::RenderingLibrary.SystemManagers.Default;
            //}

            if (managers != null)
            {
                // Adjust by viewport values:
                // todo ...
                //cursorScreenX -= managers.Renderer.GraphicsDevice.Viewport.X;
                //cursorScreenY -= managers.Renderer.GraphicsDevice.Viewport.Y;

                var camera = managers.Renderer.Camera;

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


                // for now we'll just rely on the bounds of the GUE itself

                toReturn = global::RenderingLibrary.IPositionedSizedObjectExtensionMethods.HasCursorOver(
                    thisInstance, worldX, worldY);
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

        if (!toReturn && (asInteractive?.RaiseChildrenEventsOutsideOfBounds == true || thisInstance.Tag is ScreenSave  ))
        {
            if(thisInstance.Children == null)
            {
                // It's a screen
                foreach(var child in thisInstance.ContainedElements)
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
        else if (gue.Tag is Gum.DataTypes.InstanceSave)
        {
            var instance = gue.Tag as Gum.DataTypes.InstanceSave;

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
            Click += (not, used) => _losePush?.Invoke(this, EventArgs.Empty);
            RollOff += (not, used) => _losePush?.Invoke(this, EventArgs.Empty);
        }
    }

    // See DoNextClickAndPushActions for details on why this is needed
    static List<Action> nextPushActionHoldingList = new();

    /// <summary>
    /// Adds an action to be called the next time the Cursor performs a push action 
    /// (the left button is not down the previous frame but is down this frame). The 
    /// argument action is invoked one time.
    /// </summary>
    /// <param name="action">The action to invoke one time.</param>
    /// <exception cref="ArgumentNullException">Thrown if the argument action is null.</exception>
    public static void AddNextPushAction(Action action)
    {
#if DEBUG
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
#endif
        nextPushActionHoldingList.Add(action);
    }

    // See DoNextClickAndPushActions for details on why this is needed
    static List<Action> nextClickActionHoldingList = new();

    /// <summary>
    /// Adds an action to be called the next time the Cursor performs a click action
    /// (the left button was down last frame and is released this frame). The argument
    /// action is invoked one time.
    /// </summary>
    /// <param name="action">The action to invoke one time.</param>
    /// <exception cref="ArgumentNullException">Thrown if the argument action is null.</exception>
    public static void AddNextClickAction(Action action)
    {
#if DEBUG
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
#endif
        nextClickActionHoldingList.Add(action);
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

        // Whenever AddNextPushAction or AddNextClickAction are called, the user expects:
        // 1. That it will not be raised immediately if added inside a push/click event
        // 2. That it will have access to the WindowPushed/WindowOver
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
    /// down to its children. If this is handled, children do not recieve this event.
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

public interface ICursor
{
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

    InteractiveGue? WindowPushed { get; set; }
    InteractiveGue? VisualRightPushed { get; set; }
    InteractiveGue? WindowOver { get; set; }

    public void Activity(double currentGameTimeTotalSeconds);
}

public interface IInputReceiverKeyboard
{
    bool IsShiftDown { get; }
    bool IsCtrlDown { get; }
    bool IsAltDown { get; }

    // FRB has this, but we don't have access to XNA-likes here, so we can't include it
    //IReadOnlyCollection<Microsoft.Xna.Framework.Input.Keys> KeysTyped { get; }

    string GetStringTyped();
}


class HandledActions
{
    public bool HandledMouseWheel;
    public bool HandledRollOver;
    public bool HandledClickPreview;
    public bool handledPushPreview;
    public bool SetWindowOver;
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
    public static void DoUiActivityRecursively(IList<GraphicalUiElement> gues, ICursor cursor, IInputReceiverKeyboard keyboard, double currentGameTimeInSeconds)
    {
#if DEBUG
        if(cursor == null)
        {
            throw new ArgumentNullException(nameof(cursor));
        }
#endif

        InteractiveGue.CurrentGameTime = currentGameTimeInSeconds;
        var windowOverBefore = cursor.WindowOver;
        var windowPushedBefore = cursor.WindowPushed;
        var VisualRightPushedBefore = cursor.VisualRightPushed;

        var cursorX = cursor.XRespectingGumZoomAndBounds();
        var cursorY = cursor.YRespectingGumZoomAndBounds();

        var isInWindow = cursorX >= 0 && cursorX < GraphicalUiElement.CanvasWidth &&
            cursorY >= 0 && cursorY < GraphicalUiElement.CanvasHeight;


        HandledActions actions = new HandledActions();
        var lastWindowOver = cursor.WindowOver;


        cursor.WindowOver = null;
        for(int i = gues.Count-1; i > -1; i--)
        {
            var gue = gues[i];

            // This check allows the user to remove a GUE from managers and not null it out.
            // Even though it might be proper to null it out, this removes "yet another thing to remember"
            if(gue.EffectiveManagers != null)
            {
                InteractiveGue.DoUiActivityRecursively(cursor, actions, gue, gue.Layer);
            }
            if(cursor.WindowOver != null)
            {
                break;
            }
        }

        var windowOverAsInteractive = cursor.WindowOver as InteractiveGue;
        if (windowOverAsInteractive != null)
        {
            if (lastWindowOver != windowOverAsInteractive)
            {
                windowOverAsInteractive.TryCallRollOn();
            }

            windowOverAsInteractive.TryCallHoverOver();
            if (cursor.XChange != 0 || cursor.YChange != 0)
            {
                windowOverAsInteractive.TryCallRollOver();
            }
        }

        if (!actions.SetWindowOver)
        {
            cursor.WindowOver = null;
        }
        else if(cursor.WindowOver == null)
        {
            cursor.WindowOver = windowOverBefore;
        }

        if(windowOverBefore != cursor.WindowOver)
        {
            string GetInfoFor(InteractiveGue interactive)
            {
                return interactive?.Name + " " + interactive?.GetType();
            }
            if (windowOverBefore is InteractiveGue interactiveBefore)
            {
                interactiveBefore.TryCallRollOff();
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
            cursor.WindowPushed = null;
        }
        if(cursor.SecondaryDown == false)
        {
            cursor.VisualRightPushed = null;
        }
        if(cursor.WindowPushed != null && cursor.PrimaryDown && (cursor.XChange != 0 || cursor.YChange != 0))
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
        // actions added may inspect the WindowOver or WindowPushed properties, and those are 
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

