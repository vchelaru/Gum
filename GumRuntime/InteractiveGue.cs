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
#if UWP
using System.Reflection;
#endif

namespace Gum.Wireframe
{
    /// <summary>
    /// The base object for all Gum runtime objects. It contains functionality for
    /// setting variables, states, and performing layout. The GraphicalUiElement can
    /// wrap an underlying rendering object.
    /// </summary>
    public partial class InteractiveGue : GraphicalUiElement
    {
        public bool HasEvents { get; set; } = true;
        public bool ExposeChildrenEvents { get; set; } = true;

        public bool RaiseChildrenEventsOutsideOfBounds { get; set; } = false;

        public bool IsEnabled { get; set; } = true;

        #region Events

        public event EventHandler Click;
        public event EventHandler Push;



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

        // RollOff is determined outside of the individual InteractiveGue so we need to have this callable externally..
        public void TryCallRollOff()
        {
            if (RollOff != null)
            {
                RollOff(this, EventArgs.Empty);
            }
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
                        //if (handledActions.HandledRollOver == false)
                        //{
                        //    FlatRedBall.Gui.RoutedEventArgs args = new FlatRedBall.Gui.RoutedEventArgs();
                        //    RollOverBubbling?.Invoke(this, args);
                        //    handledActions.HandledRollOver = args.Handled;
                        //}


                        //if (cursor.ZVelocity != 0 && handledActions.HandledMouseWheel == false)
                        //{
                        //    FlatRedBall.Gui.RoutedEventArgs args = new FlatRedBall.Gui.RoutedEventArgs();
                        //    MouseWheelScroll?.Invoke(this, args);
                        //    handledActions.HandledMouseWheel = args.Handled;
                        //}
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
                    string message =
                        "Could not determine whether the cursor is over this instance because" +
                        "this instance is not on any camera, nor is a default camera set up";
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

    }

    public interface ICursor
    {
        int X { get; }
        int Y { get; }
        int XChange { get; }
        int YChange { get; }

        bool PrimaryPush { get; }
        bool PrimaryDown { get; }
        bool PrimaryClick { get; }
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
    class HandledActions
    {
        public bool HandledMouseWheel;
        public bool HandledRollOver;
        public bool SetWindowOver;
    }
    public static class GueInteractiveExtensionMethods
    {
        public static void DoUiActivityRecursively(this GraphicalUiElement gue, ICursor cursor)
        {
            var windowOverBefore = cursor.WindowOver;

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
        }
    }
}

