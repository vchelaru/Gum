using Gum.Wireframe;
using MonoGameGum.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.Controls.Primitives
{
    public abstract class RangeBase : FrameworkElement
    {
        #region Fields/Properties

        protected Button thumb;

        // version 1 of this would use the thumb's parent. But this is problematic if the thumb
        // parent is re-assigned after the Slider is created. Instead we should look for an explicit
        // track:
        InteractiveGue explicitTrack;
        protected InteractiveGue Track => explicitTrack ?? thumb.Visual.EffectiveParentGue as InteractiveGue;

        /// <summary>
        /// Represents the X or Y offset of the cursor relative to the thumb when the thumb was grabbed.
        /// If the element is horizontal, this is an X value. If the element is vertical, this is a Y value.
        /// </summary>
        protected float cursorGrabOffsetRelativeToThumb = 0;

        public double LargeChange { get; set; }
        public double SmallChange { get; set; }

        double minimum = 0;
        /// <summary>
        /// The minimum inclusive value which can be set through the UI.
        /// </summary>
        public double Minimum
        {
            get => minimum;
            set
            {
                var oldValue = minimum;
                minimum = value;

                OnMinimumChanged(oldValue, minimum);
            }
        }

        double maximum = 1;
        /// <summary>
        /// The maximum inclusive value which can be set through the UI.
        /// </summary>
        public double Maximum
        {
            get => maximum;
            set
            {
                var oldValue = maximum;
                maximum = value;

                OnMaximumChanged(oldValue, maximum);
            }
        }

        double value;
        public double Value
        {
            get => value;
            set
            {
#if DEBUG
                if (double.IsNaN(value))
                {
                    throw new InvalidOperationException("Can't set the ScrollBar Value to NaN");
                }
#endif
                var oldValue = this.value;
                var newValue = value;


                // Cap the values first so the comparison is done against
                // the capped value
                newValue = System.Math.Min(newValue, Maximum);
                newValue = System.Math.Max(newValue, Minimum);

                if (oldValue != newValue)
                {
                    this.value = newValue;

                    OnValueChanged(oldValue, this.value);

                    ValueChanged?.Invoke(this, null);

                    if (MainCursor.WindowPushed != thumb.Visual)
                    {
                        // Make sure the user isn't currently grabbing the thumb
                        ValueChangeCompleted?.Invoke(this, null);
                    }

                    PushValueToViewModel();
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler ValueChanged;
        public event EventHandler ValueChangeCompleted;

        public event EventHandler ValueChangedByUi;

        #endregion

        #region Initialize

        public RangeBase() : base() { }

        public RangeBase(InteractiveGue visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();

            var thumbVisual = this.Visual.GetGraphicalUiElementByName("ThumbInstance") as InteractiveGue;
#if DEBUG
            if (thumbVisual == null)
            {
                var message =
                    $"The {this.GetType().Name} Gum object must have a button called ThumbInstance.";

                if(Visual.Children != null)
                {
                    if(Visual.Children.Count == 0)
                    {
                        message += " This visual has no children";
                    }
                    else
                    {
                        message += " The visual has the following children:";
                        foreach(var child in Visual.Children)
                        {
                            message += "\n" + child.Name;
                        }
                    }
                }
                else //if(Visual.Children == null)
                {
                    message += " This visual has null children";
                }

                throw new Exception(message);

            }
#endif

            if (thumbVisual.FormsControlAsObject == null)
            {
                thumb = new Button(thumbVisual);
            }
            else
            {
                thumb = thumbVisual.FormsControlAsObject as Button;
            }
            thumb.Push += HandleThumbPush;
            thumb.Visual.Dragging += HandleThumbRollOver;
            // do this before assigning any values like Minimum, Maximum
            var thumbHeight = thumb.ActualHeight;

            Visual.RollOver += HandleTrackRollOver;

            // read the height values and infer the Value and ViewportSize based on a 0 - 100

            // The attachments may not yet be set up, so set the explicitTrack's RaiseChildrenEventsOutsideOfBounds
            //var thumbParent = thumb.Visual.Parent as GraphicalUiElement;
            //if(thumbParent != null)
            //{
            //    thumbParent.RaiseChildrenEventsOutsideOfBounds = true;
            //}
            var trackLocal = this.Visual.GetGraphicalUiElementByName("TrackInstance");

#if DEBUG
            if(trackLocal == null)
            {
                throw new Exception($"Could not find a child named TrackInstance when creating a {this.GetType()}");
            }
            else if(!(trackLocal is InteractiveGue))
            {
                throw new Exception("Found a TrackInstance, but it is not an InteractiveGue");
            }
#endif

            var explicitTrack = (InteractiveGue)trackLocal;
            if (trackLocal is InteractiveGue trackAsInteractive)
            {
                trackAsInteractive.RaiseChildrenEventsOutsideOfBounds = true;
            }

            Minimum = 0;
            Maximum = 100;
            SmallChange = 10;
            Value = 0;

        }

        protected override void ReactToVisualRemoved()
        {
            base.ReactToVisualRemoved();

            thumb.Push -= HandleThumbPush;
            thumb.Visual.Dragging -= HandleThumbRollOver;
            Visual.RollOver -= HandleTrackRollOver;
        }

        #endregion


        protected abstract void HandleThumbPush(object sender, EventArgs e);

        private void HandleThumbRollOver(object sender, EventArgs args)
        {
            var cursor = MainCursor;

            if (cursor.WindowPushed == thumb.Visual)
            {
                UpdateThumbPositionToCursorDrag(cursor);
            }
        }

        private void HandleTrackRollOver(object sender, EventArgs args)
        {
            var cursor = MainCursor;

            if (cursor.WindowPushed == thumb.Visual)
            {
                UpdateThumbPositionToCursorDrag(cursor);
            }
        }

        protected virtual void OnMaximumChanged(double oldMaximum, double newMaximum)
        {
            if (Value > Maximum && Maximum >= Minimum)
            {
                Value = Maximum;
            }
        }
        protected virtual void OnMinimumChanged(double oldMinimum, double newMinimum)
        {
            if (Value < Minimum && Minimum <= Maximum)
            {
                Value = Minimum;
            }
        }

        protected virtual void OnValueChanged(double oldValue, double newValue) { }

        protected void RaiseValueChangeCompleted() => ValueChangeCompleted?.Invoke(this, null);

        protected void RaiseValueChangedByUi() => ValueChangedByUi?.Invoke(this, null);

        protected abstract void UpdateThumbPositionToCursorDrag(ICursor cursor);

    }
}
