using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using Gum.DataTypes.Variables;
using System.Linq;





#if FRB
using MonoGameGum.Forms.Controls;
using FlatRedBall.Forms.Input;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using Buttons = FlatRedBall.Input.Xbox360GamePad.Button;
namespace FlatRedBall.Forms.Controls;
#else
using MonoGameGum.Input;
#endif

#if !FRB
namespace Gum.Forms.Controls;
#endif

#region TextCompositionEventArgs Class
public class TextCompositionEventArgs : RoutedEventArgs
{
    /// <summary>
    /// The new text value.
    /// </summary>
    public string Text { get; }
    public TextCompositionEventArgs(string text) { Text = text; }
}
#endregion

public abstract class TextBoxBase :
#if FRB
    FrameworkElement,
#else
    Gum.Forms.Controls.FrameworkElement,
#endif
    IInputReceiver
{
    #region Fields/Properties

    [Obsolete("Use IsFocused instead")]
    public bool HasFocus
    {
        get => IsFocused;
        set => IsFocused = value;

    }
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
    protected global::RenderingLibrary.Graphics.Text coreTextObject;


    protected GraphicalUiElement placeholderComponent;
    protected global::RenderingLibrary.Graphics.Text placeholderTextObject;

    protected GraphicalUiElement selectionInstance;

    List<GraphicalUiElement> _selectionInstances = new List<GraphicalUiElement>();

    GraphicalUiElement selectionTemplate;

    GraphicalUiElement caretComponent;

    public event Action<IInputReceiver> FocusUpdate;

    public bool LosesFocusWhenClickedOff { get; set; } = true;

    protected int caretIndex;
    public int CaretIndex
    {
        get => caretIndex; 
        set
        {
            bool valueChanged = value != caretIndex;

            caretIndex = value;
            UpdateCaretPositionFromCaretIndex();

            if (valueChanged)
            {
                OffsetTextToKeepCaretInView();
                PushValueToViewModel();
                CaretIndexChanged?.Invoke(this, EventArgs.Empty);
            }
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

    protected abstract string? DisplayedText { get; }

    TextWrapping textWrapping = TextWrapping.NoWrap;
    public TextWrapping TextWrapping
    {
        get => textWrapping;
        set
        {
            if (value != textWrapping)
            {
                textWrapping = value;
                UpdateStateForSingleOrMultiLine();
                // RefreshTemplateFromSelectionInstance after UpdateToTextWrappingChanged so the state has applied when we clone
                RefreshTemplateFromSelectionInstance();
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

                var maxSelectionLengthAllowed = 0;
                if (!string.IsNullOrEmpty(this.DisplayedText))
                {
                    maxSelectionLengthAllowed = DisplayedText.Length - selectionStart;
                }

                selectionLength = System.Math.Min(maxSelectionLengthAllowed, value);
                UpdateToSelection();
                UpdateCaretVisibility();
                RaiseSelectionChanged();
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

    public virtual string? Placeholder
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

    public bool IsReadOnly { get; set; }

    public bool IsCaretVisibleWhenReadOnly { get; set; }

    /// <summary>
    /// Whether pressing the tab key inserts a tab character. If true, then
    /// tab characters can be inserted.
    /// </summary>
    public bool AcceptsTab { get; set; } = false;

    public override bool IsTabNavigationEnabled => AcceptsTab == false;

    #endregion

    #region Events

    public event Action<Buttons> ControllerButtonPushed;
    public event Action<object, TextCompositionEventArgs> PreviewTextInput;
    public event EventHandler CaretIndexChanged;
    protected void RaiseCaretIndexChanged() => CaretIndexChanged?.Invoke(this, EventArgs.Empty);
    public event EventHandler SelectionChanged;
    protected void RaiseSelectionChanged() => SelectionChanged?.Invoke(this, EventArgs.Empty);
    protected virtual TextCompositionEventArgs RaisePreviewTextInput(string newText)
    {
        var args = new TextCompositionEventArgs(newText);
        PreviewTextInput?.Invoke(this, args);

        return args;
    }

    public IInputReceiver? ParentInputReceiver =>
        this.GetParentInputReceiver();

    #endregion

    #region Initialize Methods

    public TextBoxBase() : base() { }

    public TextBoxBase(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        if (textComponent != null)
        {
            // unsubscribe on old:
            textComponent.PropertyChanged -= HandleTextComponentPropertyChanged;
        }
        RefreshInternalVisualReferences();



#if FRB
        Visual.Click += _ => this.HandleClick(this, EventArgs.Empty);
        Visual.Push += _ => this.HandlePush(this, EventArgs.Empty);
        Visual.RollOn += _ => this.HandleRollOn(this, EventArgs.Empty);
        Visual.RollOver += _ => this.HandleRollOver(this, EventArgs.Empty);
        Visual.DragOver += _ => this.HandleDrag(this, EventArgs.Empty);
        Visual.RollOff += _ => this.HandleRollOff(this, EventArgs.Empty);
#else
        Visual.Click += this.HandleClick;
        Visual.Push += this.HandlePush;
        Visual.RollOn += this.HandleRollOn;
        Visual.RollOver += this.HandleRollOver;
        Visual.Dragging += this.HandleDrag;
        Visual.RollOff += this.HandleRollOff;
#endif
        Visual.SizeChanged += HandleVisualSizeChanged;

        this.textComponent.PropertyChanged += HandleTextComponentPropertyChanged;
        base.ReactToVisualChanged();

        // don't do this, the layout may not have yet been performed yet:
        //OffsetTextToKeepCaretInView();

        IsFocused = false;
    }

    protected virtual void RefreshInternalVisualReferences()
    {
        textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");
        caretComponent = base.Visual.GetGraphicalUiElementByName("CaretInstance");

        // optional:

        if (_selectionInstances == null)
        {
            _selectionInstances = new List<GraphicalUiElement>();
        }

        selectionInstance = base.Visual.GetGraphicalUiElementByName("SelectionInstance");
        if (selectionInstance != null)
        {
            _selectionInstances.Add(selectionInstance);
        }

        RefreshTemplateFromSelectionInstance();

        placeholderComponent = base.Visual.GetGraphicalUiElementByName("PlaceholderTextInstance");

#if FULL_DIAGNOSTICS
        if (textComponent == null) throw new Exception("Gum object must have an object called \"TextInstance\"");
        if (caretComponent == null) throw new Exception("Gum object must have an object called \"CaretInstance\"");
#endif

        coreTextObject = textComponent.RenderableComponent as 
            global::RenderingLibrary.Graphics.Text;
        placeholderTextObject = placeholderComponent?.RenderableComponent as
            global::RenderingLibrary.Graphics.Text;

#if FULL_DIAGNOSTICS
        if (coreTextObject == null) throw new Exception("The Text instance must be of type Text");
#endif
        this.textComponent.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        caretComponent.X = 0;
    }

    private void HandleTextComponentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch(e.PropertyName)
        {
            case "Text":
                OnTextChanged(this.coreTextObject.RawText);
                break;
            case "HorizontalAlignment":
                UpdateCaretPositionFromCaretIndex();
                UpdateToSelection();
                break;
        }
    }

    protected virtual void OnTextChanged(string value)
    {

    }

    private void RefreshTemplateFromSelectionInstance()
    {
        if (selectionInstance != null)
        {
            // We need to do an if-check to support older FRB games that don't have this
            if (GraphicalUiElement.CloneRenderableFunction != null || selectionInstance?.RenderableComponent is ICloneable)
            { 
                selectionTemplate = selectionInstance.Clone();
            }

            // Go to > 0 so that we don't delete the original
            for (int i = _selectionInstances.Count - 1; i > 0; i--)
            {
                var toRemove = _selectionInstances[i];
                var parent = toRemove.Parent;
                parent?.Children.Remove(toRemove);
            }
        }
    }


    #endregion

    #region Event Handler Methods

    private void HandleVisualSizeChanged(object sender, EventArgs e)
    {
        OffsetTextToKeepCaretInView();
    }

    private void HandlePush(object sender, EventArgs args)
    {
        // September 7, 2025
        // When pushing on a TextBox,
        // the selection can begin. Since
        // the TextBox selection is changing,
        // the TextBox should immediately receive
        // focus.
        InteractiveGue.CurrentInputReceiver = this;

        if (MainCursor.PrimaryDoublePush)
        {
            indexPushed = null;
            selectionStart = 0;
            SelectionLength = DisplayedText?.Length ?? 0;
        }
        else
        {
            indexPushed = GetCaretIndexAtCursor();
            this.SelectionLength = 0;
            UpdateCaretIndexFromCursor();
        }
    }

    private void HandleClick(object sender, EventArgs args)
    {
        InteractiveGue.CurrentInputReceiver = this;
    }

    private void TryLoseFocusFromPush()
    {
        var cursor = MainCursor;


        var clickedOnThisOrChild =
            cursor.WindowOver == this.Visual ||
            (cursor.WindowOver != null && cursor.WindowOver.IsInParentChain(this.Visual));

        if (clickedOnThisOrChild == false && IsFocused && cursor.WindowPushed != this.Visual)
        {
            this.IsFocused = false;
        }
    }

    private void HandlePushOff()
    {
        if (MainCursor.WindowOver != Visual && 
            timeFocused != InteractiveGue.CurrentGameTime &&
            LosesFocusWhenClickedOff)
        {
            IsFocused = false;
        }
        else
        {
            InteractiveGue.AddNextPushAction(HandlePushOff);
        }
    }

    private void HandleRollOn(object sender, EventArgs args)
    {
        UpdateState();
    }

    private void HandleRollOver(object sender, EventArgs args)
    {
    }

    private void HandleDrag(object sender, EventArgs args)
    {
        if (MainCursor.LastInputDevice == InputDevice.Mouse)
        {
            if (MainCursor.WindowPushed == this.Visual && indexPushed != null && MainCursor.PrimaryDown && !MainCursor.PrimaryDoublePush)
            {
                var currentIndex = GetCaretIndexAtCursor();

                var minIndex = System.Math.Min(currentIndex, indexPushed.Value);

                var maxIndex = System.Math.Max(currentIndex, indexPushed.Value);

                selectionStart = minIndex;
                SelectionLength = maxIndex - minIndex;
            }
        }
        if (MainCursor.LastInputDevice == InputDevice.TouchScreen)
        {
            if (MainCursor.WindowPushed == this.Visual && MainCursor.PrimaryDown)
            {
                var xChange = MainCursor.XChange / global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom;

                var bitmapFont = this.coreTextObject.BitmapFont;
                var stringLength = bitmapFont.MeasureString(DisplayedText, global::RenderingLibrary.Graphics.HorizontalMeasurementStyle.Full);

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
        var cursorScreenX = MainCursor.XRespectingGumZoomAndBounds();
        var cursorScreenY = MainCursor.YRespectingGumZoomAndBounds();
        return GetCaretIndexAtPosition(cursorScreenX, cursorScreenY);
    }

    private int GetCaretIndexAtPosition(float screenX, float screenY)
    {
        var leftOfText = this.textComponent.GetAbsoluteLeft();
        var cursorOffset = screenX - leftOfText;

        int index = 0;

        if (TextWrapping == TextWrapping.NoWrap && !AcceptsReturn)
        {
            var textToUse = DisplayedText;
            index = GetIndex(cursorOffset, textToUse);
        }
        else
        {
            var bitmapFont = coreTextObject.BitmapFont;
            var lineHeight = bitmapFont.LineHeightInPixels;
            var topOfText = this.textComponent.GetAbsoluteTop();
            if (this.coreTextObject?.VerticalAlignment == global::RenderingLibrary.Graphics.VerticalAlignment.Center)
            {
                topOfText = this.textComponent.GetAbsoluteCenterY() - (lineHeight * coreTextObject.WrappedText.Count - 1) / 2.0f;
            }
            var cursorYOffset = screenY - topOfText;

            var lineOn = System.Math.Max(0, System.Math.Min((int)cursorYOffset / lineHeight, coreTextObject.WrappedText.Count - 1));

            if (lineOn < coreTextObject.WrappedText.Count)
            {
                string lineText = coreTextObject.WrappedText[lineOn];
                cursorOffset -= GetLineXOffsetForHorizontalAlignment(lineText);
                int indexInThisLine = GetIndex(cursorOffset, lineText);

                var isOnLastLine = lineOn == coreTextObject.WrappedText.Count - 1;
                if(!isOnLastLine && 
                    indexInThisLine == lineText.Length &&
                    indexInThisLine > 0 &&
                    char.IsWhiteSpace(lineText[indexInThisLine-1]))
                {
                    index = indexInThisLine - 1;
                }
                else
                {
                    index = indexInThisLine;
                }
            }

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
            global::RenderingLibrary.Graphics.BitmapCharacterInfo characterInfo = bitmapFont.GetCharacterInfo(character);

            int advance = 0;

            if (characterInfo != null)
            {
                //advance = characterInfo.GetXAdvanceInPixels(coreTextObject.BitmapFont.LineHeightInPixels);
                advance = characterInfo.XAdvance;
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

    /// <summary>
    /// Handles special situations only like [LEFT, HOME, END, BACK (Backspace), RIGHT, UP, DOWN, DELETE, CTRL+C, CTRL+X, CTRL+V, CTRL+A]
    /// Data comes from the MonogameGum.Input.Keyboard.Activity() method earlier in the call stack, which gets the data from Monogame's Keyboard.GetState()
    /// </summary>
    /// <param name="key"></param>
    /// <param name="isShiftDown"></param>
    /// <param name="isAltDown"></param>
    /// <param name="isCtrlDown"></param>
    public void HandleKeyDown(Microsoft.Xna.Framework.Input.Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
    {
        //////////////////////////Early Out////////////////////////
        if (!isFocused) return;
        /////////////////////////End Early Out/////////////////////
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
                        // Both WPF (and visual Studio) Move to the beginning of the
                        // current word. Discord works differently but we're going to 
                        // match WPF behavior. For example:
                        // 12 34
                        // If the cursor is at 4, pressing left moves the cursor
                        // before the 3 but after the space.
                        letterToMoveToFromCtrl = GetCtrlBeforeTarget(caretIndex - 1);
                        if (letterToMoveToFromCtrl != null)
                        {

                            if (letterToMoveToFromCtrl == caretIndex - 1)
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
                {
                    this.GetLineNumber(caretIndex, out int lineNumber, out int absoluteStartOfLine, out int _);
                    caretIndex = absoluteStartOfLine;
                }
                break;
            case Keys.End:
                {
                    if (string.IsNullOrEmpty(DisplayedText))
                    {
                        caretIndex = 0;
                    }
                    else
                    {
                        this.GetLineNumber(caretIndex, out int lineNumber, out int absoluteStartOfLine, out int _);
                        if(lineNumber == coreTextObject.WrappedText.Count-1)
                        {
                            caretIndex = (DisplayedText?.Length ?? 0);
                        }
                        else
                        {
                            var startofIndex = GetAbsoluteCharacterIndexForLine(lineNumber + 1);
                            caretIndex = startofIndex - 1;
                        }
                    }
                }
                break;
            case Keys.Back:
                if (!IsReadOnly)
                {
                    HandleBackspace(isCtrlDown);
                }
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
                if (!IsReadOnly)
                {
                    if (caretIndex < (DisplayedText?.Length ?? 0) || selectionLength > 0)
                    {
                        HandleDelete();
                    }
                }
                break;
            case Keys.C:
                    
                if (isCtrlDown)
                {
                    HandleCopy();
                }
                break;
            case Keys.X:
                if (!IsReadOnly)
                {
                    if (isCtrlDown)
                    {
                        HandleCut();
                    }
                }
                break;
            case Keys.V:
                if (!IsReadOnly)
                {
                    if (isCtrlDown)
                    {
                        HandlePaste();
                    }
                }
                break;
            case Keys.A:

                if(isCtrlDown)
                {
                    SelectAll();
                }
                break;
        }


        if (oldIndex != caretIndex)
        {
            UpdateToCaretChanged(oldIndex, caretIndex, isShiftDown);
            UpdateCaretPositionFromCaretIndex();
            OffsetTextToKeepCaretInView();
            CaretIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        var keyEventArg = new KeyEventArgs();
        keyEventArg.Key = key;
        KeyDown?.Invoke(this, keyEventArg);
    }

    private void MoveCursorUpOneLine()
    {
        GetAbsolutePositionsFromCaret(out float absoluteX, out float absoluteY, out int lineNumber);

        if (lineNumber == 0)
        {
            CaretIndex = 0;
        }
        else
        {
            var lineHeight = coreTextObject.BitmapFont.LineHeightInPixels;
            var newY = absoluteY - lineHeight;
            var index = GetCaretIndexAtPosition(absoluteX, newY);
            CaretIndex = index;
        }
    }

    private void MoveCursorDownOneLine()
    {
        GetAbsolutePositionsFromCaret(out float absoluteX, out float absoluteY, out int lineNumber);

        if (lineNumber == coreTextObject.WrappedText.Count - 1)
        {
            CaretIndex = DisplayedText?.Length ?? 0;
        }
        else
        {
            var lineHeight = coreTextObject.BitmapFont.LineHeightInPixels;
            var newY = absoluteY + lineHeight;
            var index = GetCaretIndexAtPosition(absoluteX, newY);
            CaretIndex = index;
        }
    }

    private void GetAbsolutePositionsFromCaret(out float absoluteX, out float absoluteY, out int lineNumber)
    {
        GetLineNumber(caretIndex, out lineNumber, out int absoluteStartOfLine, out int relativeIndexOnLine);

        // When holding SHIFT (selecting), the caret isn't positioned
        // automatically. Even if we set the CaretIndex (property), layout
        // is suspended due to the caretComponent being invisible. Therefore,
        // let's just extract out the values:
        //var absoluteX = caretComponent.GetAbsoluteCenterX();
        //var absoluteY = caretComponent.GetAbsoluteCenterY();
        absoluteX = 0f;
        if (lineNumber != -1 && lineNumber < coreTextObject.WrappedText.Count)
        {
            absoluteX = GetXCaretPositionForLineRelativeToTextParent(coreTextObject.WrappedText[lineNumber], relativeIndexOnLine);
        }
        absoluteY = GetCenterOfYForLinePixelsFromSmall(lineNumber);
        absoluteX += this.coreTextObject.Parent.GetAbsoluteLeft();
        absoluteY += this.coreTextObject.Parent.GetAbsoluteTop();
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

    public void OnFocusUpdatePreview(RoutedEventArgs args)
    {
    }

    public void OnFocusUpdate()
    {
        var gamepads = FrameworkElement.GamePadsForUiControl;

        for (int i = 0; i < gamepads.Count; i++)
        {
            var gamepad = gamepads[i];

            HandleGamepadNavigation(gamepad);

            if (gamepad.ButtonPushed(Buttons.A))
            {
                this.Visual.CallClick();

                ControllerButtonPushed?.Invoke(Buttons.A);
            }

        }

#if FRB
        var genericGamepads = GuiManager.GenericGamePadsForUiControl;
        for (int i = 0; i < genericGamepads.Count; i++)
        {
            var gamepad = genericGamepads[i];

            HandleGamepadNavigation(gamepad);

            var inputDevice = gamepad as IInputDevice;

            if (inputDevice.DefaultConfirmInput.WasJustPressed)
            {
                this.Visual.CallClick();

                ControllerButtonPushed?.Invoke(Xbox360GamePad.Button.A);
            }
        }
#endif

#if MONOGAME && !FRB
        base.HandleKeyboardFocusUpdate();
#endif
    }

    public void OnGainFocus()
    {
        IsFocused = true;
    }

    [Obsolete("Use OnLoseFocus instead")]
    public void LoseFocus() => OnLoseFocus();

    public void OnLoseFocus()
    {
        IsFocused = false;
    }

    /// <summary>
    /// Performs key actions based on the keyboard input gathered during Keyboard.cs's Activity() method
    /// </summary>
    /// <param name="keyboard">State of keyboard from TextInput and Keyboard.GetKeys</param>
    public void DoKeyboardAction(IInputReceiverKeyboard keyboard)
    {
#if !FRB
        ReceiveInput();

        var shift = keyboard.IsShiftDown;
        var ctrl = keyboard.IsCtrlDown;
        var alt = keyboard.IsAltDown;




        // This allocates. We could potentially make this return 
        // an IList or List. That's a breaking change for a tiny amount
        // of allocation....what to do....

        var asMonoGameKeyboard = (IInputReceiverKeyboardMonoGame)keyboard;

        // Handle all the special situations only based on Keyboard.GetState
        //   Situations: LEFT, HOME, END, BACK (Backspace), RIGHT, UP, DOWN, DELETE, CTRL+C, CTRL+X, CTRL+V, CTRL+A
        foreach (var key in asMonoGameKeyboard.KeysTyped)
        {
            HandleKeyDown(key, shift, alt, ctrl);
        }

        // String of letters typed and captured via the TextInput() Monogame event
        var stringTyped = keyboard.GetStringTyped();

        if (stringTyped != null)
        {
            for (int i = 0; i < stringTyped.Length; i++)
            {
                // If a \t character is here it could be from..
                // * pressing tab
                // * repeat rate tab
                // * paste
                // If AcceptsTab is false, we should ignore tabs altogether
                // Maybe in the future we can inspect if it was pasted but this
                // is trickier because we are relying on the Windows implementation.
                // receiver could get nulled out by itself when something like enter is pressed
                var character = stringTyped[i];
                if(character != '\t' || AcceptsTab)
                {
                    HandleCharEntered(character);
                }
            }
        }
#endif
    }

    public void ReceiveInput()
    {
    }
    #endregion

    #region UpdateTo Methods

    public override void UpdateState()
    {
        var cursor = MainCursor;

        if (IsEnabled == false)
        {
            Visual.SetProperty(CategoryName, DisabledStateName);
        }
        else if (IsFocused)
        {
            // todo - need to unify this by using
            // Focused instead of Selected. Will need
            // to do a gradual migration by checking which
            // state exists and setting the proper state...
            Visual.SetProperty(CategoryName, SelectedStateName);
            // Update June 15, 2025:
            // FocusedStateName is the
            // proper state, but we need
            // to respect old setups that 
            // still use selected, so we'll
            // set both.
            Visual.SetProperty(CategoryName, FocusedStateName);
        }
        else if (cursor.LastInputDevice != InputDevice.TouchScreen && Visual.EffectiveManagers != null 
            //&& Visual.HasCursorOver(cursor)
            && cursor.WindowOver == Visual
            )
        {
            Visual.SetProperty(CategoryName, HighlightedStateName);
        }
        else
        {
            Visual.SetProperty(CategoryName, EnabledStateName);
        }
    }

    public int GetAbsoluteCharacterIndexForLine(int lineNumber)
    {
        int absoluteIndex = 0;

        for(int i = 0; i < lineNumber && i < coreTextObject.WrappedText.Count; i++)
        {
            absoluteIndex += coreTextObject.WrappedText[i].Length;
        }
        return absoluteIndex;
    }

    /// <summary>
    /// Returns the line number, start of the line, and relative index into the line given an absolute character index
    /// </summary>
    /// <param name="absoluteCharacterIndex">The absolute character index, where 0 is the first character in the entire text box.</param>
    /// <param name="lineNumber">The line number containing the arugment character index.</param>
    /// <param name="absoluteStartOfLine">The index of the first character in the argument lineNumber.</param>
    /// <param name="relativeIndexOnLine">The relative index in the line, where 0 is the first character in the line.</param>
    public void GetLineNumber(int absoluteCharacterIndex, out int lineNumber, out int absoluteStartOfLine, out int relativeIndexOnLine)
    {
        lineNumber = 0;
        relativeIndexOnLine = absoluteCharacterIndex;
        absoluteStartOfLine = 0;

        for (int i = 0; i < coreTextObject.WrappedText.Count; i++)
        {
            var currentLine = coreTextObject.WrappedText[i];
            var lineLength = currentLine.Length;
            if (relativeIndexOnLine <= lineLength)
            {
                var shouldShowFirstOfNextLine =
                    // If we're at the very end of the line,
                    relativeIndexOnLine == lineLength &&
                    // the last character is whitespace,
                    currentLine.Length > 0 &&
                    // we have another line
                    lineNumber < coreTextObject.WrappedText.Count - 1 &&
                    // and the first letter on the next line is not whitespace
                    coreTextObject.WrappedText[lineNumber + 1].Length > 0 && !char.IsWhiteSpace(coreTextObject.WrappedText[lineNumber + 1][0]);

                if (!shouldShowFirstOfNextLine && lineLength > 0 && relativeIndexOnLine == lineLength && currentLine[lineLength - 1] == '\n')
                {
                    shouldShowFirstOfNextLine = true;
                }

                if (shouldShowFirstOfNextLine)
                {
                    relativeIndexOnLine -= lineLength;
                    absoluteStartOfLine += lineLength;
                    lineNumber++;
                }
                break;
            }
            else
            {
                absoluteStartOfLine += lineLength;
                relativeIndexOnLine -= lineLength;
                lineNumber++;
            }
        }

        lineNumber = System.Math.Min(lineNumber, coreTextObject.WrappedText.Count - 1);
    }

    protected void UpdateCaretPositionFromCaretIndex()
    {
        if (TextWrapping == TextWrapping.NoWrap && AcceptsReturn == false)
        {
            // make sure we measure a valid string
            var stringToMeasure = DisplayedText ?? "";

            SetXCaretPositionForLine(stringToMeasure, caretIndex);
        }
        else
        {
            GetLineNumber(caretIndex, out int lineNumber, out int _, out int relativeIndexOnLine);

            int lineLength = 0;
            if (lineNumber < coreTextObject.WrappedText.Count && lineNumber > -1)
            {
                var currentLine = coreTextObject.WrappedText[lineNumber];
                lineLength = currentLine.Length;
            }

            if (lineNumber == -1)
            {
                SetXCaretPositionForLine(string.Empty, 0);
            }
            else
            {
                SetXCaretPositionForLine(coreTextObject.WrappedText[lineNumber], relativeIndexOnLine);
            }


            float caretY = GetCenterOfYForLinePixelsFromSmall(
                // lineNumber can be -1, so treat it as 0 if so:
                System.Math.Max(0, lineNumber));
            
            switch(caretComponent.YOrigin)
            {
                case global::RenderingLibrary.Graphics.VerticalAlignment.Center:
                    // do nothing
                    break;
                case global::RenderingLibrary.Graphics.VerticalAlignment.Top:
                    caretY -= coreTextObject.LineHeightMultiplier * coreTextObject.BitmapFont.LineHeightInPixels / 2.0f;
                    break;
            }


            switch (caretComponent.YUnits)
            {
                case global::Gum.Converters.GeneralUnitType.PixelsFromSmall:
                    caretComponent.Y = caretY;

                    break;
                case global::Gum.Converters.GeneralUnitType.PixelsFromMiddle:
                    caretComponent.Y = caretY - textComponent.GetAbsoluteHeight() / 2.0f;
                    break;
            }
        }
    }

    private void UpdateToIsFocused()
    {
        UpdateCaretVisibility();

        if(caretComponent.Visible)
        {
            UpdateCaretPositionFromCaretIndex();
        }

        UpdateState();

        if (isFocused)
        {
            InteractiveGue.AddNextPushAction(HandlePushOff);

            if (InteractiveGue.CurrentInputReceiver != this)
            {
                InteractiveGue.CurrentInputReceiver = this;
            }
#if ANDROID
            FlatRedBall.Input.InputManager.Keyboard.ShowKeyboard();
#endif

        }
        else if (!isFocused)
        {
            if (InteractiveGue.CurrentInputReceiver == this)
            {
                InteractiveGue.CurrentInputReceiver = null;
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
        var isCaretVisible = (isFocused || IsCaretVisibleWhenNotFocused)
         // Visual Studio and VSCode show the caret when you have a selection
         // Apps like Discord and (it seems) WPF TextBoxes do not.
         // We are going to mimic WPF for now, but we may want to make this
         // editable.
         && selectionLength == 0;

        if(IsReadOnly && isCaretVisible)
        {
            isCaretVisible = IsCaretVisibleWhenReadOnly;
        }

        caretComponent.Visible = isCaretVisible;
    }

    bool _acceptsReturn;
    /// <summary>
    /// Whether pressing the return key adds a newline to the text box. If false, the return key does not add a newline.
    /// </summary>
    /// <remarks>
    /// Setting this value to true makes the Visual use its 
    /// LineModeCategoryState.Multi state.</remarks>
    public bool AcceptsReturn
    {
        get => _acceptsReturn;
        set
        {
            if(_acceptsReturn != value)
            {
                _acceptsReturn = value;
                UpdateStateForSingleOrMultiLine();
                // RefreshTemplateFromSelectionInstance after UpdateToTextWrappingChanged so the state has applied when we clone
                RefreshTemplateFromSelectionInstance();
            }
        }
    }

    private void UpdateStateForSingleOrMultiLine()
    {
        if( Visual.Categories.TryGetValue("LineModeCategory", out StateSaveCategory? category))
        {
            var stateToSet = "Single";


            if (textWrapping == TextWrapping.Wrap || AcceptsReturn)
            {
                stateToSet = "Multi";

                const string multiNoWrap = "MultiNoWrap";

                if (textWrapping == TextWrapping.NoWrap && category.States.Any(item => item.Name == multiNoWrap))
                {
                    stateToSet = multiNoWrap;
                }
            }


            Visual.SetProperty("LineModeCategoryState", stateToSet);
        }
    }

    List<SelectionPosition> selectionStartEnds = new List<SelectionPosition>();

    /// <summary>
    /// Updates the Selection visuals to match the current selection values.
    /// </summary>
    protected void UpdateToSelection()
    {

        if (selectionInstance != null && selectionLength > 0 && DisplayedText?.Length > 0)
        {
            UpdateSelectionStartEnds();

            while (_selectionInstances.Count < selectionStartEnds.Count)
            {
                var newSelection = selectionTemplate.Clone();
                _selectionInstances.Add(newSelection);
                var parentToAddTo = selectionInstance.Parent;
                var indexToAddTo = parentToAddTo.Children.IndexOf(selectionInstance) + 1;
                parentToAddTo.Children.Insert(indexToAddTo, newSelection);
            }

            foreach (var item in _selectionInstances)
            {
                item.Visible = false;
            }

            for (int i = 0; i < selectionStartEnds.Count; i++)
            {
                var selection = _selectionInstances[i];

                selection.X = selectionStartEnds[i].XStart;
                selection.Y = selectionStartEnds[i].Y;
                selection.Width = selectionStartEnds[i].Width;
                selection.Visible = true;
                selection.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            }
        }
        else if (selectionInstance != null)
        {
            for (int i = 0; i < _selectionInstances.Count; i++)
            {
                _selectionInstances[i].Visible = false;
            }
        }
    }

    private void UpdateSelectionStartEnds()
    {
        selectionStartEnds.Clear();
        var substring = DisplayedText.Substring(0, selectionStart);

        if (this.TextWrapping == TextWrapping.Wrap || AcceptsReturn)
        {
            GetLineNumber(selectionStart, out int startLineNumber, out int absoluteStartOfFirstLine, out int startRelativeIndexInLine);

            GetLineNumber(selectionStart + selectionLength, out int endLineNumber, out int absoluteStartOfLastLine, out int endRelativeIndexInLine);

            int absoluteStartOfCurrentLine = absoluteStartOfFirstLine;

            for (int i = startLineNumber; i < endLineNumber + 1; i++)
            {
                var lineOfText = this.coreTextObject.WrappedText[i];

                int startOfSelectionInThisLineAbsolute = 0;

                if (i == startLineNumber)
                {
                    startOfSelectionInThisLineAbsolute = absoluteStartOfFirstLine + startRelativeIndexInLine;
                }
                else
                {
                    startOfSelectionInThisLineAbsolute = absoluteStartOfCurrentLine;
                }

                var startOfSelectionInThisLineRelative = startOfSelectionInThisLineAbsolute - absoluteStartOfCurrentLine;

                var startXForSelection = GetXCaretPositionForLineRelativeToTextParent(lineOfText, startOfSelectionInThisLineRelative);

                var endRelative = 0;
                if (i == endLineNumber)
                {
                    endRelative = endRelativeIndexInLine;
                }
                else
                {
                    endRelative = lineOfText.Length;
                }

                var endXForSelection = GetXCaretPositionForLineRelativeToTextParent(lineOfText, endRelative);

                var selectionPosition = new SelectionPosition();
                selectionPosition.XStart = startXForSelection;
                var offsetPixelsFromSmall = GetCenterOfYForLinePixelsFromSmall(i);

                switch (selectionTemplate.YOrigin)
                {
                    case global::RenderingLibrary.Graphics.VerticalAlignment.Center:
                        // do nothing
                        break;
                    case global::RenderingLibrary.Graphics.VerticalAlignment.Top:
                        offsetPixelsFromSmall -= coreTextObject.LineHeightMultiplier * coreTextObject.BitmapFont.LineHeightInPixels / 2.0f;
                        break;
                }

                switch (selectionTemplate.YUnits)
                {
                    case global::Gum.Converters.GeneralUnitType.PixelsFromSmall:
                        selectionPosition.Y = offsetPixelsFromSmall;
                        break;
                    case global::Gum.Converters.GeneralUnitType.PixelsFromMiddle:
                        selectionPosition.Y = offsetPixelsFromSmall - textComponent.GetAbsoluteHeight() / 2.0f;
                        break;
                }

                selectionPosition.Width = endXForSelection - startXForSelection;

                selectionStartEnds.Add(selectionPosition);
                absoluteStartOfCurrentLine += lineOfText.Length;
            }
        }
        else
        {
            var selectionPosition = new SelectionPosition();
            var firstMeasure = this.coreTextObject.BitmapFont.MeasureString(substring, global::RenderingLibrary.Graphics.HorizontalMeasurementStyle.Full);
            substring = DisplayedText.Substring(0, selectionStart + selectionLength);

            selectionPosition.XStart = this.textComponent.X + firstMeasure;
            selectionPosition.Y = this.textComponent.Y;
            selectionPosition.Width = 1 +
                this.coreTextObject.BitmapFont.MeasureString(substring, global::RenderingLibrary.Graphics.HorizontalMeasurementStyle.Full) - firstMeasure;

            selectionStartEnds.Add(selectionPosition);
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
        // intentionally don't check AcceptsReturn here:
        if (this.TextWrapping == TextWrapping.NoWrap )
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

    #region Get Positions

    struct SelectionPosition
    {
        public float Y;
        public float XStart;
        public float Width;
    }

    private void SetXCaretPositionForLine(string stringToMeasure, int indexIntoLine)
    {
        var newPosition = GetXCaretPositionForLineRelativeToTextParent(stringToMeasure, indexIntoLine);

        // assumes caret and text have the same parent
        this.caretComponent.X = newPosition;
    }

    private float GetXCaretPositionRelativeToTextParent(int absoluteIndex)
    {
        int charactersLeft = absoluteIndex;
        foreach (var line in coreTextObject.WrappedText)
        {
            if (charactersLeft <= line.Length)
            {
                return GetXCaretPositionForLineRelativeToTextParent(line, charactersLeft);
            }
            else
            {
                charactersLeft -= line.Length;
            }
        }

        return 0;
    }

    private float GetXCaretPositionForLineRelativeToTextParent(string stringToMeasure, int indexIntoLine)
    {
        indexIntoLine = System.Math.Min(indexIntoLine, stringToMeasure.Length);
        var substring = stringToMeasure.Substring(0, indexIntoLine);
        caretComponent.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        if (this.coreTextObject.BitmapFont != null)
        {
            var measure = this.coreTextObject.BitmapFont.MeasureString(substring, global::RenderingLibrary.Graphics.HorizontalMeasurementStyle.Full);
            return measure + this.textComponent.X + GetLineXOffsetForHorizontalAlignment(stringToMeasure);
        }
        else
        {
            return caretComponent.X = GetLineXOffsetForHorizontalAlignment(stringToMeasure);
        }
    }

    public float GetLineXOffsetForHorizontalAlignment(string stringToMeasure)
    {
        if (coreTextObject.HorizontalAlignment == global::RenderingLibrary.Graphics.HorizontalAlignment.Left)
            return 0;

        float measuredLineWidth = coreTextObject.MeasureString(stringToMeasure);
        float textComponentWidth = textComponent.GetAbsoluteWidth();
        float gapBetweenTextAndEdge = textComponentWidth - measuredLineWidth;
        if (coreTextObject.HorizontalAlignment == global::RenderingLibrary.Graphics.HorizontalAlignment.Center)
            gapBetweenTextAndEdge /= 2.0f;
        return gapBetweenTextAndEdge;
    }

    float CoreTextObjectHeight =>
        coreTextObject.GetAbsoluteBottom() - coreTextObject.GetAbsoluteTop();

    private float GetCenterOfYForLinePixelsFromSmall(int lineNumber)
    {
        var lineHeight = coreTextObject.BitmapFont.LineHeightInPixels;

        float offset;

        if (coreTextObject.VerticalAlignment == global::RenderingLibrary.Graphics.VerticalAlignment.Center)
        {
            offset = lineNumber * lineHeight;
            offset -= lineHeight * (coreTextObject.WrappedText.Count - 1) / 2.0f;
            offset += CoreTextObjectHeight / 2.0f;
        }
        else
        {
            offset = (lineNumber + .5f) * lineHeight;
        }
        var caretY = (textComponent as IPositionedSizedObject).Y + offset;
        return caretY;
    }


    #endregion


    public abstract void SelectAll();

    protected abstract void TruncateTextToMaxLength();

    #region Utilities

    protected int? GetCtrlBeforeTarget(int index)
    {
        var afterRemovingSpaces = GetNonSpaceIndexAtOrBefore(index);

        if (afterRemovingSpaces != null)
        {
            var nextSpace = GetSpaceIndexAtOrBefore(afterRemovingSpaces.Value);

            if (nextSpace != null)
            {
                return nextSpace.Value + 1;
            }
        }

        return null;
    }

    int? GetNonSpaceIndexAtOrBefore(int index)
    {
        // first get non-space index at or before:
        if (DisplayedText != null)
        {
            index = System.Math.Min(index, DisplayedText.Length - 1);
            for (int i = index; i > 0; i--)
            {
                var isNotSpace = !Char.IsWhiteSpace(DisplayedText[i]);

                if (isNotSpace)
                {
                    return i;
                }
            }
        }

        return null;

    }

    int? GetSpaceIndexAtOrBefore(int index)
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
