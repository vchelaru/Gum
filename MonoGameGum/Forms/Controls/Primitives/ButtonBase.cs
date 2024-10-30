using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.Controls.Primitives
{
    public class ButtonBase : FrameworkElement
    {
        /// <summary>
        /// Event raised when the user pushes, then releases the control.
        /// This means the cursor is over the button, the button was originally pushed,
        /// the primary button was pressed last frame, but is no longer pressed this frame.
        /// The "click" terminology comes from the Cursor's PrimaryClick property.
        /// </summary>
        public event EventHandler Click;

        /// <summary>
        /// Event raised when the user pushes on the control. 
        /// This means the cursor is over the button and the primary button was not pressed last frame, but is pressed this frame.
        /// The "push" terminology comes from the Cursor's PrimaryPush property.
        /// </summary>
        public event EventHandler Push;
        //public event FocusUpdateDelegate FocusUpdate;

        /// <summary>
        /// Event raised when any button is pressed on an Xbox360GamePad which is being used by the 
        /// GuiManager.GamePadsForUiControl.
        /// </summary>
        //public event Action<Xbox360GamePad.Button> ControllerButtonPushed;
        //public event Action<int> GenericGamepadButtonPushed;

        //public event Action<FlatRedBall.Input.Mouse.MouseButtons> MouseButtonPushed;

        public ButtonBase() : base() { }

        public ButtonBase(InteractiveGue visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            Visual.Click += this.HandleClick;
            Visual.Push += this.HandlePush;
            Visual.LosePush += this.HandleLosePush;
            Visual.RollOn += this.HandleRollOn;
            Visual.RollOff += this.HandleRollOff;

            base.ReactToVisualChanged();

            UpdateState();
        }


        private void HandleClick(object sender, EventArgs args)
        {
            UpdateState();

            OnClick();

            Click?.Invoke(this, null);
            //MouseButtonPushed?.Invoke(FlatRedBall.Input.Mouse.MouseButtons.LeftButton);
        }

        private void HandlePush(object sender, EventArgs args)
        {
            UpdateState();

            Push?.Invoke(this, null);
        }

        private void HandleLosePush(object sender, EventArgs args)
        {
            UpdateState();
        }

        private void HandleRollOn(object sender, EventArgs args)
        {
            UpdateState();
        }

        private void HandleRollOff(object sender, EventArgs args)
        {
            UpdateState();
        }

        protected virtual void OnClick() { }

        public void PerformClick()
        {
            HandleClick(this, EventArgs.Empty);
        }
    }
}
