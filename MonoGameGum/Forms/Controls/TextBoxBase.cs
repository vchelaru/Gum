using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.Controls
{
    public class TextCompositionEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// The new text value.
        /// </summary>
        public string Text { get; }
        public TextCompositionEventArgs(string text) { Text = text; }
    }

    public interface IInputReceiver
    {

    }

    public enum TextWrapping
    {
        // todo - support wrap with overflow

        /// <summary>
        /// No line wrapping is performed.
        /// </summary>
        NoWrap = 1,

        /// <summary>
        /// Line-breaking occurs if the line overflows beyond the available block width,
        /// even if the standard line breaking algorithm cannot determine any line break
        /// opportunity, as in the case of a very long word constrained in a fixed-width
        /// container with no scrolling allowed.
        /// </summary>
        Wrap = 2
    }

    public class TextBoxBase : FrameworkElement, IInputReceiver
    {
        public override bool IsFocused
        {
            get => base.IsFocused;
            set
            {
                base.IsFocused = value;
                UpdateToIsFocused();
            }
        }

        protected GraphicalUiElement textComponent;
        protected RenderingLibrary.Graphics.Text coreTextObject;


        protected GraphicalUiElement placeholderComponent;
        protected RenderingLibrary.Graphics.Text placeholderTextObject;

        protected GraphicalUiElement selectionInstance;

        GraphicalUiElement caretComponent;

        public event Action<IInputReceiver> FocusUpdate;

        public bool LosesFocusWhenClickedOff { get; set; } = true;

        protected int caretIndex;
        public int CaretIndex
        {
            get { return caretIndex; }
            set
            {
                caretIndex = value;
                UpdateCaretPositionToCaretIndex();
                OffsetTextToKeepCaretInView();
            }
        }

        public List<Keys> IgnoredKeys => null;


        public bool TakingInput => true;

        public IInputReceiver NextInTabSequence { get; set; }

        public override bool IsEnabled
        {
            get
            {
                return base.IsEnabled;
            }
            set
            {
                base.IsEnabled = value;
                if (!IsEnabled)
                {
                    IsFocused = false;
                }
                UpdateState();
            }
        }

        protected abstract string DisplayedText { get; }

        TextWrapping textWrapping = TextWrapping.NoWrap;
        public TextWrapping TextWrapping
        {
            get => textWrapping;
            set
            {
                if (value != textWrapping)
                {
                    UpdateToTextWrappingChanged();
                }
            }
        }

        /// <summary>
        /// The cursor index where the cursor was last pushed, used for drag+select
        /// </summary>
        private int? indexPushed;

        protected int selectionStart;
        public int SelectionStart
        {
            get { return selectionStart; }
            set
            {
                if (selectionStart != value)
                {
                    selectionStart = value;
                    UpdateToSelection();
                }
            }
        }

        protected int selectionLength;
        public int SelectionLength
        {
            get { return selectionLength; }
            set
            {
                if (selectionLength != value)
                {
                    if (value < 0)
                    {
                        throw new Exception($"Value cannot be less than 0, but is {value}");
                    }
                    selectionLength = value;
                    UpdateToSelection();
                    UpdateCaretVisibility();
                }
            }
        }

        // todo - this could move to the base class, if the base objects became input receivers
        public event Action<object, KeyEventArgs> KeyDown;

    }
}
