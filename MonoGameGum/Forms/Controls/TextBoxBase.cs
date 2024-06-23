using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

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

    public abstract class TextBoxBase : FrameworkElement, IInputReceiver
    {
        #region Fields/Properties

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

        bool isCaretVisibleWhenNotFocused;
        /// <summary>
        /// Whether the caret is visible when not focused. If true, the caret will always stay visible even if the TextBox has lost focus.
        /// </summary>
        public bool IsCaretVisibleWhenNotFocused
        {
            get => isCaretVisibleWhenNotFocused;
            set
            {
                if (value != isCaretVisibleWhenNotFocused)
                {
                    isCaretVisibleWhenNotFocused = value;
                    UpdateCaretVisibility();
                }
            }
        }

        public string Placeholder
        {
            get => placeholderTextObject?.RawText;
            set
            {
                if (placeholderTextObject != null)
                {
                    placeholderTextObject.RawText = value;
                }
            }
        }

        protected abstract string CategoryName { get; }

        int? maxLength;
        public int? MaxLength
        {
            get => maxLength;
            set
            {
                maxLength = value;
                TruncateTextToMaxLength();
            }
        }

        #endregion

        #region Events

        //public event Action<Xbox360GamePad.Button> ControllerButtonPushed;

        public event Action<object, TextCompositionEventArgs> PreviewTextInput;

        protected TextCompositionEventArgs RaisePreviewTextInput(string newText)
        {
            var args = new TextCompositionEventArgs(newText);
            PreviewTextInput?.Invoke(this, args);

            return args;
        }

        #endregion

        #region Initialize Methods

        public TextBoxBase() : base() { }

        public TextBoxBase(InteractiveGue visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");
            caretComponent = base.Visual.GetGraphicalUiElementByName("CaretInstance");

            // optional:
            selectionInstance = base.Visual.GetGraphicalUiElementByName("SelectionInstance");
            placeholderComponent = base.Visual.GetGraphicalUiElementByName("PlaceholderTextInstance");

            coreTextObject = textComponent.RenderableComponent as RenderingLibrary.Graphics.Text;
            placeholderTextObject = placeholderComponent?.RenderableComponent as RenderingLibrary.Graphics.Text;
#if DEBUG
            if (textComponent == null) throw new Exception("Gum object must have an object called \"Text\"");
            if (coreTextObject == null) throw new Exception("The Text instance must be of type Text");
            if (caretComponent == null) throw new Exception("Gum object must have an object called \"Caret\"");
#endif

            Visual.Click += this.HandleClick;
            Visual.Push += this.HandlePush;
            Visual.RollOn += this.HandleRollOn;
            Visual.RollOver += this.HandleRollOver;
            Visual.Dragging += this.HandleDrag;
            Visual.RollOff += this.HandleRollOff;
            Visual.SizeChanged += HandleVisualSizeChanged;

            this.textComponent.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            caretComponent.X = 0;
            base.ReactToVisualChanged();

            // don't do this, the layout may not have yet been performed yet:
            //OffsetTextToKeepCaretInView();

            IsFocused = false;
        }


        #endregion

        #region Event Handler Methods

        private void HandleVisualSizeChanged(object sender, EventArgs e)
        {
            OffsetTextToKeepCaretInView();
        }

        private void HandlePush(object sender, EventArgs args)
        {
            indexPushed = GetCaretIndexAtCursor();

        }

        private void HandleClick(object sender, EventArgs args)
        {
            CurrentInputReceiver = this;

            if (MainCursor.PrimaryDoubleClick)
            {
                selectionStart = 0;
                SelectionLength = DisplayedText?.Length ?? 0;
            }
            else if (MainCursor.PrimaryClickNoSlide)
            {
                UpdateCaretIndexFromCursor();
            }

            if (this.LosesFocusWhenClickedOff)
            {
                InteractiveGue.AddNextPushAction(TryLoseFocusFromPush);
            }

        }

        private void TryLoseFocusFromPush()
        {
            var cursor = MainCursor;


            var clickedOnThisOrChild =
                cursor.WindowOver == this.Visual ||
                (cursor.WindowOver != null && cursor.WindowOver.IsInParentChain(this.Visual));

            if (clickedOnThisOrChild == false && IsFocused)
            {
                this.IsFocused = false;
            }
        }

        private void HandleClickOff()
        {
            if (MainCursor.WindowOver != Visual && timeFocused != InteractiveGue.CurrentGameTime &&
                LosesFocusWhenClickedOff)
            {
                IsFocused = false;
            }
        }

        private void HandleRollOn(object sender, EventArgs args)
        {
            UpdateState();
        }

        private void HandleRollOver(object sender, EventArgs args)
        {
            var isMouse = true;
            if (isMouse)
            {
                if (MainCursor.WindowPushed == this.Visual && indexPushed != null && MainCursor.PrimaryDown)
                {
                    var currentIndex = GetCaretIndexAtCursor();

                    var minIndex = System.Math.Min(currentIndex, indexPushed.Value);

                    var maxIndex = System.Math.Max(currentIndex, indexPushed.Value);

                    selectionStart = minIndex;
                    SelectionLength = maxIndex - minIndex;
                }
            }
        }

        private void HandleDrag(object sender, EventArgs args)
        {
            var isTouchScreen = false;
            if (isTouchScreen)
            {
                if (MainCursor.WindowPushed == this.Visual && MainCursor.PrimaryDown)
                {
                    var xChange = MainCursor.XChange / RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom;


                    var bitmapFont = this.coreTextObject.BitmapFont;
                    var stringLength = bitmapFont.MeasureString(DisplayedText);

                    var minimumShift = System.Math.Min(
                        edgeToTextPadding,
                        textComponent.Parent.Width - stringLength - edgeToTextPadding);

                    var maximumShift = edgeToTextPadding;
                    var newTextValue = System.Math.Min(
                        textComponent.X + xChange,
                        maximumShift);

                    newTextValue = System.Math.Max(newTextValue, minimumShift);

                    var amountToShift = newTextValue - textComponent.X;
                    textComponent.X += amountToShift;
                    caretComponent.X += amountToShift;
                }
            }
        }

        private void HandleRollOff(object sender, EventArgs args)
        {
            UpdateState();
        }

        private void UpdateCaretIndexFromCursor()
        {
            int index = GetCaretIndexAtCursor();

            CaretIndex = index;
        }

        private int GetCaretIndexAtCursor()
        {
            var cursorScreenX = MainCursor.X;
            var cursorScreenY = MainCursor.X;
            return GetCaretIndexAtPosition(cursorScreenX, cursorScreenY);
        }

        private int GetCaretIndexAtPosition(float screenX, float screenY)
        {
            var leftOfText = this.textComponent.GetAbsoluteLeft();
            var cursorOffset = screenX - leftOfText;

            int index = 0;

            if (TextWrapping == TextWrapping.NoWrap)
            {
                var textToUse = DisplayedText;
                index = GetIndex(cursorOffset, textToUse);
            }
            else
            {
                var bitmapFont = coreTextObject.BitmapFont;
                var lineHeight = bitmapFont.LineHeightInPixels;
                var topOfText = this.textComponent.GetAbsoluteTop();
                var cursorYOffset = screenY - topOfText;

                var lineOn = System.Math.Min((int)cursorYOffset / lineHeight, coreTextObject.WrappedText.Count - 1);

                index = GetIndex(cursorOffset, coreTextObject.WrappedText[lineOn]);

                for (int line = 0; line < lineOn; line++)
                {
                    index += coreTextObject.WrappedText[line].Length;
                }

            }

            return index;
        }

        private int GetIndex(float cursorOffset, string textToUse)
        {
            var index = textToUse?.Length ?? 0;
            float distanceMeasuredSoFar = 0;
            var bitmapFont = this.coreTextObject.BitmapFont;

            for (int i = 0; i < (textToUse?.Length ?? 0); i++)
            {
                char character = textToUse[i];
                RenderingLibrary.Graphics.BitmapCharacterInfo characterInfo = bitmapFont.GetCharacterInfo(character);

                int advance = 0;

                if (characterInfo != null)
                {
                    advance = characterInfo.GetXAdvanceInPixels(coreTextObject.BitmapFont.LineHeightInPixels);
                }

                distanceMeasuredSoFar += advance;

                // This should find which side of the character you're closest to, but for now it's good enough...
                if (distanceMeasuredSoFar > cursorOffset)
                {
                    var halfwayPoint = distanceMeasuredSoFar - (advance / 2.0f);
                    if (halfwayPoint > cursorOffset)
                    {
                        index = i;
                    }
                    else
                    {
                        index = i + 1;
                    }
                    break;
                }
            }

            return index;
        }

        public void HandleKeyDown(Microsoft.Xna.Framework.Input.Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
        {
            if (isFocused)
            {
                var oldIndex = caretIndex;

                switch (key)
                {
                    case Microsoft.Xna.Framework.Input.Keys.Left:
                        // todo - extract this so that we can also use CTRL for shift and delete/backspace...
                        if (selectionLength != 0 && isShiftDown == false)
                        {
                            caretIndex = selectionStart;
                            SelectionLength = 0;
                        }
                        else if (caretIndex > 0)
                        {
                            int? letterToMoveToFromCtrl = null;
                            if (isCtrlDown)
                            {
                                letterToMoveToFromCtrl = GetSpaceIndexBefore(caretIndex - 1);
                                if (letterToMoveToFromCtrl != null)
                                {

                                    // match Visual Studio behavior, and go after the last space
                                    if (letterToMoveToFromCtrl != caretIndex - 1)
                                    {
                                        // we found a space, now select one to the right...
                                        letterToMoveToFromCtrl++;
                                    }
                                    else
                                    {
                                        letterToMoveToFromCtrl = null;
                                    }
                                }
                                else
                                {
                                    letterToMoveToFromCtrl = 0;
                                }
                            }

                            caretIndex = letterToMoveToFromCtrl ?? (caretIndex - 1);
                        }
                        break;
                    case Keys.Home:
                        caretIndex = 0;
                        break;
                    case Keys.End:
                        caretIndex = (DisplayedText?.Length ?? 0);
                        break;
                    case Keys.Back:
                        HandleBackspace(isCtrlDown);
                        break;
                    case Microsoft.Xna.Framework.Input.Keys.Right:
                        if (selectionLength != 0 && isShiftDown == false)
                        {
                            caretIndex = selectionStart + selectionLength;
                            SelectionLength = 0;
                        }
                        else if (caretIndex < (DisplayedText?.Length ?? 0))
                        {
                            int? letterToMoveToFromCtrl = null;

                            if (isCtrlDown)
                            {
                                letterToMoveToFromCtrl = GetSpaceIndexAfter(caretIndex + 1);
                                if (letterToMoveToFromCtrl != null)
                                {

                                    // match Visual Studio behavior, and go after the last space
                                    if (letterToMoveToFromCtrl != caretIndex + 1)
                                    {
                                        letterToMoveToFromCtrl++;
                                    }
                                    else
                                    {
                                        letterToMoveToFromCtrl = null;
                                    }
                                }
                                else
                                {
                                    letterToMoveToFromCtrl = DisplayedText?.Length ?? 0;
                                }
                            }

                            caretIndex = letterToMoveToFromCtrl ?? (caretIndex + 1);

                        }
                        break;
                    case Keys.Up:
                        MoveCursorUpOneLine();
                        break;
                    case Keys.Down:
                        MoveCursorDownOneLine();
                        break;
                    case Microsoft.Xna.Framework.Input.Keys.Delete:
                        if (caretIndex < (DisplayedText?.Length ?? 0) || selectionLength > 0)
                        {
                            HandleDelete();
                        }
                        break;
                    case Keys.C:
                        if (isCtrlDown)
                        {
                            HandleCopy();
                        }
                        break;
                    case Keys.X:
                        if (isCtrlDown)
                        {
                            HandleCut();
                        }
                        break;
                    case Keys.V:
                        if (isCtrlDown)
                        {
                            HandlePaste();
                        }
                        break;
                }


                if (oldIndex != caretIndex)
                {
                    UpdateToCaretChanged(oldIndex, caretIndex, isShiftDown);
                    UpdateCaretPositionToCaretIndex();
                    OffsetTextToKeepCaretInView();
                }

                var keyEventArg = new KeyEventArgs();
                keyEventArg.Key = key;
                KeyDown?.Invoke(this, keyEventArg);


            }
        }

        private void MoveCursorUpOneLine()
        {
            var absoluteX = caretComponent.GetAbsoluteCenterX();
            var absoluteY = caretComponent.GetAbsoluteCenterY();

            var lineHeight = coreTextObject.BitmapFont.LineHeightInPixels;

            var newY = absoluteY - lineHeight;

            var index = GetCaretIndexAtPosition(absoluteX, newY);

            CaretIndex = index;
        }

        private void MoveCursorDownOneLine()
        {
            var absoluteX = caretComponent.GetAbsoluteCenterX();
            var absoluteY = caretComponent.GetAbsoluteCenterY();

            var lineHeight = coreTextObject.BitmapFont.LineHeightInPixels;

            var newY = absoluteY + lineHeight;

            var index = GetCaretIndexAtPosition(absoluteX, newY);

            CaretIndex = index;
        }

        protected virtual void HandleCopy()
        {

        }

        protected virtual void HandleCut()
        {

        }

        protected virtual void HandlePaste()
        {

        }

        protected virtual void UpdateToCaretChanged(int oldIndex, int newIndex, bool isShiftDown)
        {
            if (isShiftDown)
            {
                var change = oldIndex - newIndex;

                if (SelectionLength == 0)
                {
                    // set the field (doesn't update the selection visuals)...
                    selectionStart = System.Math.Min(oldIndex, newIndex);
                    // ...now set the property to update the visuals.
                    SelectionLength = System.Math.Abs(oldIndex - newIndex);
                }
                else
                {
                    int leftMost = 0;
                    int rightMost = 0;
                    if (oldIndex == selectionStart)
                    {
                        leftMost = System.Math.Min(selectionStart + selectionLength, newIndex);
                        rightMost = System.Math.Max(selectionStart + selectionLength, newIndex);
                    }
                    else
                    {
                        leftMost = System.Math.Min(selectionStart, newIndex);
                        rightMost = System.Math.Max(selectionStart, newIndex);
                    }

                    selectionStart = leftMost;
                    SelectionLength = rightMost - leftMost;
                }
            }
            else
            {
                SelectionLength = 0;
            }
        }

        public abstract void HandleBackspace(bool isCtrlDown = false);

        protected abstract void HandleDelete();

        public abstract void HandleCharEntered(char character);

        public void OnFocusUpdate()
        {
            //var gamepads = GuiManager.GamePadsForUiControl;

            //for (int i = 0; i < gamepads.Count; i++)
            //{
            //    var gamepad = gamepads[i];

            //    HandleGamepadNavigation(gamepad);

            //    if (gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.A))
            //    {
            //        this.Visual.CallClick();

            //        ControllerButtonPushed?.Invoke(Xbox360GamePad.Button.A);
            //    }

            //}

            //var genericGamepads = GuiManager.GenericGamePadsForUiControl;
            //for (int i = 0; i < genericGamepads.Count; i++)
            //{
            //    var gamepad = genericGamepads[i];

            //    HandleGamepadNavigation(gamepad);

            //    var inputDevice = gamepad as IInputDevice;

            //    if (inputDevice.DefaultConfirmInput.WasJustPressed)
            //    {
            //        this.Visual.CallClick();

            //        ControllerButtonPushed?.Invoke(Xbox360GamePad.Button.A);
            //    }
            //}

        }

        public void OnGainFocus()
        {
            IsFocused = true;
        }

        public void LoseFocus()
        {
            IsFocused = false;

        }

        public void ReceiveInput()
        {

        }
        #endregion

        #region UpdateTo Methods

        protected override void UpdateState()
        {
            var cursor = MainCursor;

            var isTouchScreen = false;

            if (IsEnabled == false)
            {
                Visual.SetProperty(CategoryName, "Disabled");
            }
            else if (IsFocused)
            {
                Visual.SetProperty(CategoryName, "Selected");
            }
            else if (!isTouchScreen && Visual.HasCursorOver(cursor))
            {
                Visual.SetProperty(CategoryName, "Highlighted");
            }
            else
            {
                Visual.SetProperty(CategoryName, "Enabled");
            }
        }

        protected void UpdateCaretPositionToCaretIndex()
        {
            if (TextWrapping == TextWrapping.NoWrap)
            {
                // make sure we measure a valid string
                var stringToMeasure = DisplayedText ?? "";

                SetCaretPositionForLine(stringToMeasure, caretIndex);
            }
            else
            {
                int charactersLeft = caretIndex;
                int lineNumber = 0;

                for (int i = 0; i < coreTextObject.WrappedText.Count; i++)
                {
                    var lineLength = coreTextObject.WrappedText[i].Length;
                    if (charactersLeft <= lineLength)
                    {
                        SetCaretPositionForLine(coreTextObject.WrappedText[i], charactersLeft);
                        break;
                    }
                    else
                    {
                        charactersLeft -= lineLength;
                        lineNumber++;
                    }
                }

                var lineHeight = coreTextObject.BitmapFont.LineHeightInPixels;

                if (TextWrapping == TextWrapping.Wrap)
                {
                    caretComponent.Y = (textComponent as IPositionedSizedObject).Y +
                        lineNumber * lineHeight;
                }
            }
        }

        private void SetCaretPositionForLine(string stringToMeasure, int indexIntoLine)
        {
            indexIntoLine = System.Math.Min(indexIntoLine, stringToMeasure.Length);
            var substring = stringToMeasure.Substring(0, indexIntoLine);
            caretComponent.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            if (this.coreTextObject.BitmapFont != null)
            {
                var measure = this.coreTextObject.BitmapFont.MeasureString(substring);
                caretComponent.X = measure + this.textComponent.X;
            }
            else
            {
                caretComponent.X = 0;
            }
        }

        private void UpdateToIsFocused()
        {
            UpdateCaretVisibility();
            UpdateState();

            if (isFocused)
            {
                InteractiveGue.AddNextClickAction(HandleClickOff);

                if (FrameworkElement.CurrentInputReceiver != this)
                {
                    FrameworkElement.CurrentInputReceiver = this;
                }
#if ANDROID
                FlatRedBall.Input.InputManager.Keyboard.ShowKeyboard();
#endif

            }
            else if (!isFocused)
            {
                if (FrameworkElement.CurrentInputReceiver == this)
                {
                    FrameworkElement.CurrentInputReceiver = null;
#if ANDROID
                    FlatRedBall.Input.InputManager.Keyboard.HideKeyboard();
#endif
                }

                // Vic says - why do we need to deselect when it loses focus? It could stay selected
                //SelectionLength = 0;
            }
        }

        private void UpdateCaretVisibility()
        {
            caretComponent.Visible = (isFocused || IsCaretVisibleWhenNotFocused) && selectionLength == 0;
        }

        private void UpdateToTextWrappingChanged()
        {
            if (textWrapping == TextWrapping.Wrap)
            {
                Visual.SetProperty("LineModeCategory", "Multi");
            }
            else // no wrap
            {
                Visual.SetProperty("LineModeCategory", "Single");
            }
        }

        protected void UpdateToSelection()
        {
            if (selectionInstance != null && selectionLength > 0 && DisplayedText?.Length > 0)
            {
                selectionInstance.Visible = true;

                selectionInstance.XUnits =
                    global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

                var substring = DisplayedText.Substring(0, selectionStart);
                var firstMeasure = this.coreTextObject.BitmapFont.MeasureString(substring);
                selectionInstance.X = this.textComponent.X + firstMeasure;

                substring = DisplayedText.Substring(0, selectionStart + selectionLength);
                selectionInstance.Width = 1 +
                    this.coreTextObject.BitmapFont.MeasureString(substring) - firstMeasure;

            }
            else if (selectionInstance != null)
            {
                selectionInstance.Visible = false;
            }
        }

        /// <summary>
        /// The maximum distance between the edge of the control and the text.
        /// Either we will want to make this customizable at some point, or remove
        /// this value and base it on some value of a parent, like we do for the scroll
        /// bar. This would require the Text to have a custom parent specifically defining
        /// the range of the text object.
        /// </summary>
        const float edgeToTextPadding = 5;

        protected void OffsetTextToKeepCaretInView()
        {
            if (this.TextWrapping == TextWrapping.NoWrap)
            {
                this.textComponent.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
                this.caretComponent.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

                float leftOfCaret = caretComponent.GetAbsoluteLeft();
                float rightOfCaret = caretComponent.GetAbsoluteLeft() + caretComponent.GetAbsoluteWidth();

                float leftOfParent = caretComponent.EffectiveParentGue.GetAbsoluteLeft();
                float rightOfParent = leftOfParent + caretComponent.EffectiveParentGue.GetAbsoluteWidth();

                float shiftAmount = 0;
                if (rightOfCaret > rightOfParent)
                {
                    shiftAmount = rightOfParent - rightOfCaret - edgeToTextPadding;
                }
                if (leftOfCaret < leftOfParent)
                {
                    shiftAmount = leftOfParent - leftOfCaret + edgeToTextPadding;
                }

                if (shiftAmount != 0)
                {
                    this.textComponent.X += shiftAmount;
                    this.caretComponent.X += shiftAmount;
                }
            }
            else
            {
                // do nothing...except we may want to offset Y at some point
            }
        }

        protected void UpdatePlaceholderVisibility()
        {
            if (placeholderTextObject != null)
            {
                placeholderComponent.Visible = string.IsNullOrEmpty(coreTextObject.RawText);
            }
        }

        #endregion

        public abstract void SelectAll();

        protected abstract void TruncateTextToMaxLength();

        #region Utilities

        protected int? GetSpaceIndexBefore(int index)
        {
            if (DisplayedText != null)
            {
                for (int i = index - 1; i > 0; i--)
                {
                    var isSpace = Char.IsWhiteSpace(DisplayedText[i]);

                    if (isSpace)
                    {
                        return i;
                    }
                }
            }

            return null;
        }

        protected int? GetSpaceIndexAfter(int index)
        {
            if (DisplayedText != null)
            {
                for (int i = index; i < DisplayedText.Length; i++)
                {
                    var isSpace = Char.IsWhiteSpace(DisplayedText[i]);

                    if (isSpace)
                    {
                        return i;
                    }
                }
            }

            return null;
        }


        #endregion
    }
}
