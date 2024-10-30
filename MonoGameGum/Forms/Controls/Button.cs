using Gum.Wireframe;
using MonoGameGum.Forms.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.Controls
{
    public class Button : ButtonBase
    {
        /// <summary>
        /// The name of the Category containing visual states for the Button object.
        /// </summary>
        public const string ButtonCategoryState = "ButtonCategoryState";
        #region Fields/Properties

        GraphicalUiElement textComponent;

        RenderingLibrary.Graphics.Text coreTextObject;

        /// <summary>
        /// Text displayed by the button. This property requires that the TextInstance instance be present in the Gum component.
        /// If the TextInstance instance is not present, an exception will be thrown in DEBUG mode
        /// </summary>
        public string Text
        {
            get
            {
#if DEBUG
                ReportMissingTextInstance();
#endif
                return coreTextObject.RawText;
            }
            set
            {
#if DEBUG
                ReportMissingTextInstance();
#endif
                // go through the component instead of the core text object to force a layout refresh if necessary
                textComponent?.SetProperty("Text", value);
            }
        }

        /// <summary>
        /// Whether the button is enabled or not. When disabled, the button will not respond to user input, and will display
        /// a disabled state.
        /// </summary>
        public override bool IsEnabled
        {
            get
            {
                return base.IsEnabled;
            }
            set
            {
                base.IsEnabled = value;

                UpdateState();
            }
        }

        #endregion

        #region Initialize Methods

        public Button() : base() { }

        public Button(InteractiveGue visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            // text component is optional:
            textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");
            coreTextObject = textComponent?.RenderableComponent as RenderingLibrary.Graphics.Text;
            base.ReactToVisualChanged();
        }


        #endregion

        #region UpdateTo Methods

        public override void UpdateState()
        {
            var state = base.GetDesiredState();

            Visual.SetProperty(ButtonCategoryState, state);
        }

        #endregion

        #region Utilities

#if DEBUG
        private void ReportMissingTextInstance()
        {
            if (textComponent == null)
            {
                throw new Exception(
                    $"This button was created with a Gum component ({Visual?.ElementSave}) " +
                    "that does not have an instance called 'TextInstance'. A 'TextInstance' instance must be added to modify the button's Text property.");
            }
        }
#endif

        #endregion
    }
}
