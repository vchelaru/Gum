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
#if UWP
using System.Reflection;
#endif

namespace Gum.Wireframe
{
    public class RoutedEventArgs
    {
        public bool Handled { get; set; }
    }

    public class SelectionChangedEventArgs
    {
        public IList RemovedItems { get; private set; } = new List<Object>();
        public IList AddedItems { get; private set; } = new List<Object>();
    }

    public class BindingContextChangedEventArgs : EventArgs
    {
        public object OldBindingContext { get; set; }
    }

    /// <summary>
    /// The base object for all Gum runtime objects. It contains functionality for
    /// setting variables, states, and performing layout. The GraphicalUiElement can
    /// wrap an underlying rendering object.
    /// </summary>
    public partial class InteractiveGue : GraphicalUiElement
    {
        static List<Action> nextPushActions = new List<Action>();
        static List<Action> nextClickActions = new List<Action>();
        public static double CurrentGameTime { get; set; }

        static IInputReceiver currentInputReceiver;
        public static IInputReceiver CurrentInputReceiver
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

        public bool HasEvents { get; set; } = true;
        public bool ExposeChildrenEvents { get; set; } = true;

        public bool RaiseChildrenEventsOutsideOfBounds { get; set; } = false;
        bool isEnabled = true;
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

        public virtual object FormsControlAsObject { get; set; }

        #region Events

        public event EventHandler Click;
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
        public event EventHandler LosePush;


        /// <summary>
        /// Event raised when the cursor first moves over this object.
        /// </summary>
        public event EventHandler RollOn;
        /// <summary>
        /// Event when the cursor first leaves this object.
        /// </summary>
        public event EventHandler RollOff;
        /// <summary>
        /// Event raised every frame the cursor is over this object.
        /// </summary>
        public event EventHandler RollOver;

        /// <summary>
        /// Event raised when the cursor pushes on an object and moves. This is similar to RollOver, but is raised even
        /// if outside of the bounds of the object.
        /// </summary>
        public event EventHandler Dragging;

        public event EventHandler EnabledChange;

        /// <summary>
        /// Eent raised when the mouse wheel has been scrolled while the cursor is over this instance.
        /// This event is raised bottom-up, with the root object having the opportunity to handle the roll over.
        /// If a control sets the argument RoutedEventArgs Handled to true, the children objects 
        /// will not have this event raised.
        /// </summary>
        public event Action<object, RoutedEventArgs> MouseWheelScroll;

        /// <summary>
        /// Event raised when the mouse rolls over this instance. This event is raised bottom-up, with the
        /// root object having the opportunity to handle the roll over. If a control sets the argument RoutedEventArgs Handled to true,
        /// then children objects will not have this event raised.
        /// </summary>
        public event Action<object, RoutedEventArgs> RollOverBubbling;

        /// <summary>
        /// Event raised when this Window is pushed, then is no longer the pushed window due to a cursor releasing the primary button.
        /// </summary>
        public event EventHandler RemovedAsPushed;


        // RollOff is determined outside of the individual InteractiveGue so we need to have this callable externally..
        public void TryCallRollOff()
        {
            if (RollOff != null)
            {
                RollOff(this, EventArgs.Empty);
            }
        }

        public void TryCallDragging()
        {
            if(Dragging != null)
            {
                Dragging(this, EventArgs.Empty);
            }
        }

        public void TryCallRemoveAsPushed()
        {
            RemovedAsPushed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        private bool DoUiActivityRecursively(ICursor cursor, HandledActions handledActions = null)
        {
            return DoUiActivityRecursively(cursor, handledActions, this);

        }

        internal static bool DoUiActivityRecursively(ICursor cursor, HandledActions handledActions, GraphicalUiElement currentItem)
        { 
            handledActions = handledActions ?? new HandledActions();
            bool handledByChild = false;
            bool handledByThis = false;

            bool isOver = HasCursorOver(cursor, currentItem);
            var asInteractive = currentItem as InteractiveGue;

            // Even though the cursor is over "this", we need to check if the cursor is over any children in case "this" exposes its children events:
            if (isOver && (asInteractive == null || asInteractive.ExposeChildrenEvents))
            {
                #region Try handling by children

                if(currentItem.Children == null)
                {
                    for(int i = currentItem.ContainedElements.Count - 1; i > -1; i--)
                    {
                        var child = currentItem.ContainedElements[i] as GraphicalUiElement;

                        if (child != null && HasCursorOver(cursor, child))
                        {
                            handledByChild = DoUiActivityRecursively(cursor, handledActions, child);

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

                        if (child != null &&  HasCursorOver(cursor, child))
                        {
                            handledByChild = DoUiActivityRecursively(cursor, handledActions, child);

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
                    asInteractive?.Click != null
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
                            var lastWindowOver = cursor.WindowOver;

                            // moved from above, see comments there...
                            handledByThis = true;
                            cursor.WindowOver = asInteractive;
                            handledActions.SetWindowOver = true;

                            if (cursor.PrimaryPush && asInteractive.IsEnabled)
                            {

                                cursor.WindowPushed = asInteractive;

                                if (asInteractive.Push != null)
                                    asInteractive.Push(asInteractive, EventArgs.Empty);


                                //cursor.GrabWindow(this);

                            }

                            if (cursor.PrimaryClick && asInteractive.IsEnabled) // both pushing and clicking can occur in one frame because of buffered input
                            {
                                if (cursor.WindowPushed == asInteractive)
                                {
                                    if (asInteractive.Click != null)
                                    {
                                        asInteractive.Click(asInteractive, EventArgs.Empty);
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
                            if(asInteractive.RollOn != null && lastWindowOver != asInteractive)
                            {
                                asInteractive.RollOn(asInteractive, EventArgs.Empty);
                            }

                            if (asInteractive.RollOver != null && (cursor.XChange != 0 || cursor.YChange != 0))
                            {
                                asInteractive.RollOver(asInteractive, EventArgs.Empty);
                            }
                            
                        }
                    }
                    if (asInteractive?.HasEvents == true && asInteractive?.IsEnabled == true)
                    {
                        if (handledActions.HandledRollOver == false)
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

        public bool HasCursorOver(ICursor cursor)
        {
            return HasCursorOver(cursor, this);
        }

        private static bool HasCursorOver(ICursor cursor, GraphicalUiElement thisInstance)
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

                    //if (this.mLayer != null)
                    //{
                    //    mLayer.ScreenToWorld(
                    //        camera,
                    //        cursorScreenX, cursorScreenY,
                    //        out worldX, out worldY);
                    //}
                    //else
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
                        " it is not on any camera, nor is a default camera set up";
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
                        if (HasCursorOver(cursor, child))
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

                        if (child != null && HasCursorOver(cursor, child))
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

                if (
                    instance.BaseType == "ColoredRectangle" ||

                    // Vic says - a user may want to click on a container like a track, 
                    // so we prob should allow clicks?
                    // Update - no doing this seems to ruin all kinds of UI because containers
                    // steal clicks from their children. We will check if the container has an explicit
                    // event, otherwise, it will pass it along to its children.
                    instance.BaseType == "Container" ||
                    instance.BaseType == "NineSlice" ||

                    instance.BaseType == "Sprite" ||
                    instance.BaseType == "Text")
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
            InitializeEvents();
        }

        public InteractiveGue(IRenderable renderable) : base(renderable)
        {
            InitializeEvents();
        }

        private void InitializeEvents()
        {
            Click += (not, used) => LosePush?.Invoke(this, EventArgs.Empty);
            RollOff += (not, used) => LosePush?.Invoke(this, EventArgs.Empty);
        }

        public static void AddNextPushAction(Action action)
        {
#if DEBUG
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
#endif
            nextPushActions.Add(action);
        }
        public static void AddNextClickAction(Action action)
        {
#if DEBUG
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
#endif
            nextClickActions.Add(action);
        }
        internal static void DoNextClickActions()
        {

            if (nextClickActions.Count > 0)
            {
                var items = nextClickActions.ToList();
                nextClickActions.Clear();
                foreach (var item in items)
                {
                    item();
                }

            }
        }

        internal static void DoNextPushActions()
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

        void DoKeyboardAction(IInputReceiverKeyboard keyboard);
    }

    public interface ICursor
    {
        int X { get; }
        int Y { get; }
        int XChange { get; }
        int YChange { get; }

        int ScrollWheelChange { get; }

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

        bool SecondaryPush { get; }
        bool SecondaryDown { get; }
        bool SecondaryClick { get; }
        bool SecondaryDoubleClick { get; }

        bool MiddlePush { get; }
        bool MiddleDown { get; }
        bool MiddleClick { get; }
        bool MiddleDoubleClick { get; }

        InteractiveGue WindowPushed { get; set; }
        InteractiveGue WindowOver { get; set; }
    }

    public interface IInputReceiverKeyboard
    {
        bool IsShiftDown { get; }
        bool IsCtrlDown { get; }
        bool IsAltDown { get; }

        //IReadOnlyCollection<T> KeysTyped { get; }

        string GetStringTyped();
    }


    class HandledActions
    {
        public bool HandledMouseWheel;
        public bool HandledRollOver;
        public bool SetWindowOver;
    }
    public static class GueInteractiveExtensionMethods
    {
        public static void DoUiActivityRecursively(this GraphicalUiElement gue, ICursor cursor, IInputReceiverKeyboard keyboard, double currentGameTimeInSeconds)
        {
            InteractiveGue.CurrentGameTime = currentGameTimeInSeconds;
            var windowOverBefore = cursor.WindowOver;
            var windowPushedBefore = cursor.WindowPushed;

            HandledActions actions = new HandledActions();
            InteractiveGue.DoUiActivityRecursively(cursor, actions, gue);

            if(!actions.SetWindowOver)
            {
                cursor.WindowOver = null;
            }

            if(windowOverBefore != cursor.WindowOver)
            {
                if(windowOverBefore is InteractiveGue interactiveBefore)
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
            if(cursor.WindowPushed != null && cursor.PrimaryDown && (cursor.XChange != 0 || cursor.YChange != 0))
            {
                cursor.WindowPushed.TryCallDragging();
            }

            // the click/push actions need to be after the UI activity
            if (cursor.PrimaryClick)
            {
                InteractiveGue.DoNextClickActions();

            }

            if (cursor.PrimaryPush)
            {
                InteractiveGue.DoNextPushActions();
            }

            if(InteractiveGue.CurrentInputReceiver != null)
            {
                InteractiveGue.CurrentInputReceiver.DoKeyboardAction(keyboard);
            }
        }
    }
}

