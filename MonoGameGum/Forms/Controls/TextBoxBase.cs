#if MONOGAME || KNI || XNA4 || FNA
#define XNALIKE
#endif
using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using Gum.DataTypes.Variables;
using System.Linq;

#if FRB
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Forms.Controls;
using FlatRedBall.Forms.Input;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using GamepadButton = FlatRedBall.Input.Xbox360GamePad.Button;
using RenderingLibrary.Graphics;
namespace FlatRedBall.Forms.Controls;
#elif XNALIKE
using Microsoft.Xna.Framework.Input;
using RenderingLibrary.Graphics;
using MonoGameGum.Input;
using GamepadButton = Microsoft.Xna.Framework.Input.Buttons;
#else
using Gum.Input;
using GamepadButton = Gum.Input.GamepadButton;
using Keys = Gum.Forms.Input.Keys;
using Gum.Renderables;
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
    protected Text coreTextObject;


    protected GraphicalUiElement placeholderComponent;
    protected Text placeholderTextObject;

    protected GraphicalUiElement selectionInstance;
    float _selectionInstanceYOffset;

    List<GraphicalUiElement> _selectionInstances = new List<GraphicalUiElement>();

    GraphicalUiElement selectionTemplate;

    GraphicalUiElement caretComponent;

    /// <summary>
    /// Raised every frame while this control has input focus. Can be used
    /// to perform custom per-frame logic while the control is focused.
    /// </summary>
    public event Action<IInputReceiver>? FocusUpdate;

    /// <summary>
    /// Whether clicking outside the text box causes it to lose focus. Defaults to <c>true</c>.
    /// </summary>
    public bool LosesFocusWhenClickedOff { get; set; } = true;

    /// <summary>
    /// If true, focusing this control displays the OS-provided modal keyboard dialog
    /// (via MonoGame's <c>Microsoft.Xna.Framework.Input.KeyboardInput.Show</c>). The text
    /// entered in the dialog is applied to this control when the user accepts. Defaults to
    /// <c>true</c> on Android and iOS, and <c>false</c> on every other platform, because:
    /// <list type="bullet">
    ///   <item><description>Desktop (Windows/Mac/Linux): hardware keyboard input works directly, and the native dialog is a stub — enabling it would break normal typing.</description></item>
    ///   <item><description>Web (Blazor WebAssembly, including mobile browsers): <c>KeyboardInput.Show</c> is not implemented on the browser runtime, so the call is bypassed regardless of this flag. Note that a mobile device running the game inside a browser reports <c>IsBrowser()</c>, not <c>IsAndroid()</c>/<c>IsIOS()</c>.</description></item>
    /// </list>
    /// You can override the default by setting this property explicitly after the control is created.
    /// </summary>
    public bool ShowNativeKeyboardOnFocus { get; set; }
        = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    /// <summary>
    /// Title shown at the top of the native keyboard dialog when
    /// <see cref="ShowNativeKeyboardOnFocus"/> is <c>true</c>. Ignored on platforms that
    /// do not display a native dialog.
    /// </summary>
    public string NativeKeyboardTitle { get; set; } = "Enter text";

    /// <summary>
    /// Description text shown below the title on the native keyboard dialog when
    /// <see cref="ShowNativeKeyboardOnFocus"/> is <c>true</c>. Ignored on platforms that
    /// do not display a native dialog.
    /// </summary>
    public string NativeKeyboardDescription { get; set; } = string.Empty;

    // Guards against re-entering the native dialog if focus toggles while it is visible.
    bool _isNativeKeyboardShowing;

    protected int caretIndex;
    /// <summary>
    /// Gets or sets the zero-based character position of the caret. Setting this value
    /// updates the caret visual and scrolls the text to keep the caret in view.
    /// </summary>
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

    /// <summary>
    /// A list of keys that should be ignored by this control.
    /// </summary>
    public List<Keys> IgnoredKeys => null;

    /// <summary>
    /// Whether this control is currently capable of receiving input. Always true for TextBoxBase.
    /// </summary>
    public bool TakingInput => true;

    /// <summary>
    /// The next control to receive focus when the user presses the tab key.
    /// </summary>
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

    /// <summary>
    /// The text currently displayed in the text box. This may differ from the actual
    /// internal text (e.g. in a PasswordBox).
    /// </summary>
    protected abstract string? DisplayedText { get; }

    TextWrapping textWrapping = TextWrapping.NoWrap;
    /// <summary>
    /// Gets or sets the text wrapping behavior. When set to <see cref="Gum.Forms.TextWrapping.Wrap"/>,
    /// text wraps to multiple lines. Defaults to <see cref="Gum.Forms.TextWrapping.NoWrap"/>.
    /// </summary>
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
    /// <summary>
    /// Gets or sets the zero-based character index of the start of the current selection.
    /// </summary>
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
    /// <summary>
    /// Gets or sets the number of characters in the current selection. A value of 0 means
    /// nothing is selected. The selection spans from <see cref="SelectionStart"/> to
    /// <see cref="SelectionStart"/> + <see cref="SelectionLength"/>.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the placeholder text displayed when the text box is empty.
    /// Setting this property applies localization if a <see cref="Gum.Localization.LocalizationService"/> is registered.
    /// To bypass localization, use <see cref="SetPlaceholderNoTranslate"/>.
    /// </summary>
    public virtual string? Placeholder
    {
        get => placeholderTextObject?.RawText;
        set
        {
            if (placeholderComponent != null)
            {
                // go through the component instead of the renderable to apply localization and force a layout refresh
                placeholderComponent.SetProperty("Text", value);
            }
        }
    }

    /// <summary>
    /// Sets the placeholder text without applying localization/translation.
    /// </summary>
    /// <remarks>
    /// This is a method rather than a property because the "no translate" state is not preserved on
    /// the underlying text renderable — only the final string is stored.
    /// Use this for placeholder text that should not be localized.
    /// </remarks>
    public void SetPlaceholderNoTranslate(string? value)
    {
        if (placeholderComponent != null)
        {
            placeholderComponent.SetProperty("TextNoTranslate", value);
        }
    }

    /// <summary>
    /// The name of the Gum state category used to apply visual states (Enabled, Focused, etc.)
    /// to this control's visual. Each concrete text box type provides its own category name
    /// so the correct set of states is applied at runtime.
    /// </summary>
    protected abstract string CategoryName { get; }

    int? maxLength;
    /// <summary>
    /// The maximum number of characters the user can enter. When set, typing, pasting, and
    /// programmatic text assignment are all truncated to this length. A value of <c>null</c>
    /// (the default) means no limit. This is equivalent to WPF's
    /// <c>TextBox.MaxLength</c>.
    /// To limit how many characters are <em>displayed</em> without restricting input,
    /// see <see cref="TextBox.MaxLettersToShow"/> instead.
    /// </summary>
    public int? MaxLength
    {
        get => maxLength;
        set
        {
            maxLength = value;
            TruncateTextToMaxLength();
        }
    }

    /// <summary>
    /// Whether the text box is read-only. When <c>true</c>, the user cannot type, paste,
    /// delete, or otherwise modify the text, but can still select and copy.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Whether the caret is visible when <see cref="IsReadOnly"/> is <c>true</c>.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool IsCaretVisibleWhenReadOnly { get; set; }

    /// <summary>
    /// Whether pressing the tab key inserts a tab character. If true, then
    /// tab characters can be inserted and tab navigation (moving focus to the next control) is disabled.
    /// </summary>
    public bool AcceptsTab { get; set; } = false;

    /// <summary>
    /// Returns true if tab navigation is enabled. This is the inverse of <see cref="AcceptsTab"/>.
    /// </summary>
    public override bool IsTabNavigationEnabled => AcceptsTab == false;

    #endregion

    #region Events

    /// <summary>
    /// Raised when a controller button is pushed while the control is focused.
    /// </summary>
    public event Action<GamepadButton> ControllerButtonPushed;
    /// <summary>
    /// Raised before new text is inserted (by typing or pasting). Set
    /// <see cref="RoutedEventArgs.Handled"/> to <c>true</c> to cancel the insertion.
    /// Similar to WPF's <c>PreviewTextInput</c>.
    /// </summary>
    public event Action<object, TextCompositionEventArgs> PreviewTextInput;
    /// <summary>
    /// Raised when the <see cref="CaretIndex"/> changes.
    /// </summary>
    public event EventHandler CaretIndexChanged;
    protected void RaiseCaretIndexChanged() => CaretIndexChanged?.Invoke(this, EventArgs.Empty);
    /// <summary>
    /// Raised when the selection (SelectionStart or SelectionLength) changes.
    /// </summary>
    public event EventHandler SelectionChanged;
    protected void RaiseSelectionChanged() => SelectionChanged?.Invoke(this, EventArgs.Empty);
    protected virtual TextCompositionEventArgs RaisePreviewTextInput(string newText)
    {
        var args = new TextCompositionEventArgs(newText);
        PreviewTextInput?.Invoke(this, args);

        return args;
    }

    /// <summary>
    /// The parent input receiver for this control.
    /// </summary>
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

        if(textComponent != null)
        {
            this.textComponent.PropertyChanged += HandleTextComponentPropertyChanged;
        }
        base.ReactToVisualChanged();

        // don't do this, the layout may not have yet been performed yet:
        //OffsetTextToKeepCaretInView();

        IsFocused = false;
    }

    protected override void RefreshInternalVisualReferences()
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
            _selectionInstanceYOffset = selectionInstance.Y;
            _selectionInstances.Add(selectionInstance);
        }

        RefreshTemplateFromSelectionInstance();

        placeholderComponent = base.Visual.GetGraphicalUiElementByName("PlaceholderTextInstance");

#if FULL_DIAGNOSTICS
        if (textComponent == null) throw new Exception("Gum object must have an object called \"TextInstance\"");
        if (caretComponent == null) throw new Exception("Gum object must have an object called \"CaretInstance\"");
#endif

        coreTextObject = textComponent.RenderableComponent as 
            Text;
        placeholderTextObject = placeholderComponent?.RenderableComponent as
            Text;

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

    private void HandleVisualSizeChanged(object? sender, EventArgs e)
    {
        OffsetTextToKeepCaretInView();
    }

    private void HandlePush(object? sender, EventArgs args)
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

    private void HandleClick(object? sender, EventArgs args)
    {
        InteractiveGue.CurrentInputReceiver = this;
    }

    private void TryLoseFocusFromPush()
    {
        var cursor = MainCursor;


        var clickedOnThisOrChild =
            cursor.VisualOver == this.Visual ||
            (cursor.VisualOver != null && cursor.VisualOver.IsInParentChain(this.Visual));

        if (clickedOnThisOrChild == false && IsFocused && cursor.WindowPushed != this.Visual)
        {
            this.IsFocused = false;
        }
    }

    private void HandlePushOff()
    {
        if (MainCursor.VisualOver != Visual && 
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

    private void HandleRollOn(object? sender, EventArgs args)
    {
        UpdateState();
    }

    private void HandleRollOver(object? sender, EventArgs args)
    {
    }

    private void HandleDrag(object? sender, EventArgs args)
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

                var stringLength = coreTextObject.MeasureString(DisplayedText, global::RenderingLibrary.Graphics.HorizontalMeasurementStyle.Full);

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

    private void HandleRollOff(object? sender, EventArgs args)
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
            var lineHeight = coreTextObject.LineHeightInPixels;
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

#if XNALIKE

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
#else
        for (int i = 0; i < (textToUse?.Length ?? 0); i++)
        {
            // Is there a faster way to do this?
            distanceMeasuredSoFar = coreTextObject.MeasureString(textToUse.Substring(0, i + 1));

            // This should find which side of the character you're closest to, but for now it's good enough...
            if (distanceMeasuredSoFar > cursorOffset)
            {
                var distanceBefore = coreTextObject.MeasureString(textToUse.Substring(0, i));
                var advance = distanceMeasuredSoFar - distanceBefore;
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
#endif
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
    public void HandleKeyDown(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
    {
        //////////////////////////Early Out////////////////////////
        if (!isFocused) return;
        /////////////////////////End Early Out/////////////////////
        var oldIndex = caretIndex;

        switch (key)
        {
            case Keys.Left:
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
            case Keys.Right:
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
            case Keys.Delete:
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
            var lineHeight = coreTextObject.LineHeightInPixels;
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
            var lineHeight = coreTextObject.LineHeightInPixels;
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

            if (gamepad.ButtonPushed(GamepadButton.A))
            {
                this.Visual.CallClick();

                ControllerButtonPushed?.Invoke(GamepadButton.A);
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

#if !FRB
        base.HandleKeyboardFocusUpdate();
#endif

        FocusUpdate?.Invoke(this);
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

        // Handle all the special situations only based on Keyboard.GetState
        //   Situations: LEFT, HOME, END, BACK (Backspace), RIGHT, UP, DOWN, DELETE, CTRL+C, CTRL+X, CTRL+V, CTRL+A
        foreach (Keys key in keyboard.KeysTyped)
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

    private string? _savedText;
    private int _savedCaretIndex;
    private int _savedSelectionLength;

    /// <inheritdoc/>
    public override void SaveRuntimeProperties()
    {
        _savedText = coreTextObject?.RawText;
        _savedCaretIndex = caretIndex;
        _savedSelectionLength = selectionLength;
        base.SaveRuntimeProperties();
    }

    /// <inheritdoc/>
    public override void ApplyRuntimeProperties()
    {
        if (_savedText != null && coreTextObject != null)
        {
            textComponent?.SetProperty("Text", _savedText);
            caretIndex = System.Math.Min(_savedCaretIndex, _savedText.Length);
            selectionLength = _savedSelectionLength;

            // UpdateToIsFocused calls UpdateState which re-applies categorical
            // state — must run first so placeholder/selection updates aren't undone
            UpdateToIsFocused();
            UpdatePlaceholderVisibility();
            UpdateToSelection();
        }
        _savedText = null;
        base.ApplyRuntimeProperties();
    }

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
            && cursor.VisualOver == Visual
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
        // This is a copy of the method in TextExtensions.cs, but since that
        // requires a Text and not an IText, that can't be referenced here.
        // Doing so would require a little more refactoring. If this method 
        // changes, make the change in TextExtensions, or clean it up.

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
                    caretY -= coreTextObject.LineHeightMultiplier * coreTextObject.LineHeightInPixels / 2.0f;
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
            TryShowNativeKeyboard();

            // FRB1 (FlatRedBall) ships its own Android keyboard helper and uses these calls
            // to drive it. The MonoGameGum Android path is handled inside TryShowNativeKeyboard /
            // TryHideNativeKeyboard via our own Keyboard.Android.cs partial (ported from FRB),
            // so this FRB-only block is kept but not used by the MonoGameGum build.
#if ANDROID && FRB
            FlatRedBall.Input.InputManager.Keyboard.ShowKeyboard();
#endif
        }
        else if (!isFocused)
        {
            if (InteractiveGue.CurrentInputReceiver == this)
            {
                InteractiveGue.CurrentInputReceiver = null;
                TryHideNativeKeyboard();
#if ANDROID && FRB
                FlatRedBall.Input.InputManager.Keyboard.HideKeyboard();
#endif
            }

            // Vic says - why do we need to deselect when it loses focus? It could stay selected
            //SelectionLength = 0;
        }
    }

    /// <summary>
    /// Applies text that came back from the native on-screen keyboard dialog. Called on the
    /// game loop thread (marshaled through <c>GumService.Default.DeferredQueue</c>), so
    /// overrides may safely mutate control state. Overridden by <see cref="TextBox"/> to
    /// write to <c>Text</c>, and by <see cref="PasswordBox"/> to write to <c>Password</c>.
    /// The default implementation does nothing, so subclasses that do not collect text
    /// (conceptually none at the moment) are unaffected.
    /// </summary>
    /// <param name="value">The string the user entered, as returned by the native dialog. Never null.</param>
    protected virtual void SetTextFromNativeKeyboardInput(string value) { }

    /// <summary>
    /// Whether the native keyboard dialog should mask entered characters (password mode).
    /// Overridden by <see cref="PasswordBox"/> to return <c>true</c>. Defaults to <c>false</c>
    /// for regular <see cref="TextBox"/> input.
    /// </summary>
    protected virtual bool UseNativeKeyboardPasswordMode => false;

    private void TryShowNativeKeyboard()
    {
        // FNA does not ship Microsoft.Xna.Framework.Input.KeyboardInput, so the modal path
        // (used on iOS and as the pre-inline fallback) won't compile against it. FNA's own
        // native-keyboard story is different and out of scope here. FRB has its own path in
        // UpdateToIsFocused; Raylib does not need any of this.
#if !FRB && !RAYLIB && !FNA && !SOKOL
        if (!ShowNativeKeyboardOnFocus)
        {
            return;
        }

#if ANDROID
        // On Android we use an inline soft keyboard (ported from FRB's Keyboard.Android.cs).
        // Typed characters flow through the normal InputReceiver pipeline via
        // Keyboard.GetStringTyped / KeysTyped — no modal dialog, no deferred-queue marshaling
        // needed because all input arrives on the UI thread and is drained by
        // ProcessAndroidKeys each frame.
        //
        // IsAndroidVersionAtLeast(21) is required by CA1416 — see TryHideNativeKeyboard.
        if (OperatingSystem.IsAndroidVersionAtLeast(21))
        {
            global::Gum.Forms.Controls.FrameworkElement.MainKeyboard?.ShowKeyboard();
        }
#else
        // iOS (and any future platform without an inline implementation) falls back to
        // MonoGame's KeyboardInput.Show modal. Browser (Blazor) is explicitly skipped —
        // KeyboardInput.Show is not implemented there and would throw or hang.
        if (_isNativeKeyboardShowing || OperatingSystem.IsBrowser())
        {
            return;
        }

        _isNativeKeyboardShowing = true;

        var task = Microsoft.Xna.Framework.Input.KeyboardInput.Show(
            NativeKeyboardTitle,
            NativeKeyboardDescription,
            DisplayedText ?? string.Empty,
            UseNativeKeyboardPasswordMode);

        task.ContinueWith(t =>
        {
            // The continuation runs on whichever thread KeyboardInput.Show completes on,
            // which is not guaranteed to be the game loop thread. UI/layout mutation must
            // happen on the game loop thread, so route the result through GumService's
            // DeferredQueue — it is thread-safe and drains on the next Update.
            global::MonoGameGum.GumService.Default.DeferredQueue.Enqueue(() =>
            {
                _isNativeKeyboardShowing = false;
                if (t.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && t.Result != null)
                {
                    SetTextFromNativeKeyboardInput(t.Result);
                }
            });
        });
#endif
#endif
    }

    private void TryHideNativeKeyboard()
    {
#if ANDROID && !FRB && !RAYLIB
        // Dismiss the soft keyboard when the TextBox loses focus so users aren't left
        // staring at an IME over an un-focused UI. Only meaningful on Android — iOS's
        // modal KeyboardInput.Show dismisses itself.
        //
        // The IsAndroidVersionAtLeast guard is redundant at runtime (the whole block is
        // inside #if ANDROID) but is required by the CA1416 platform-compatibility analyzer,
        // which does not track #if directives — only runtime OS checks.
        if (OperatingSystem.IsAndroidVersionAtLeast(21))
        {
            global::Gum.Forms.Controls.FrameworkElement.MainKeyboard?.HideKeyboard();
        }
#endif
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
                // Use the stored offset rather than reading selectionInstance.Y at runtime.
                // selectionInstance IS _selectionInstances[0], so writing selection.Y at i==0
                // would mutate the value we'd read back, causing the offset to accumulate
                // (selection drifts down by _selectionInstanceYOffset pixels per update).
                selection.Y = selectionStartEnds[i].Y + _selectionInstanceYOffset;
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
                        offsetPixelsFromSmall -= coreTextObject.LineHeightMultiplier * coreTextObject.LineHeightInPixels / 2.0f;
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
            var firstMeasure = this.coreTextObject.MeasureString(substring, global::RenderingLibrary.Graphics.HorizontalMeasurementStyle.Full);
            substring = DisplayedText.Substring(0, selectionStart + selectionLength);

            selectionPosition.XStart = this.textComponent.X + firstMeasure;
            selectionPosition.Y = this.textComponent.Y;
            selectionPosition.Width = 1 +
                this.coreTextObject.MeasureString(substring, global::RenderingLibrary.Graphics.HorizontalMeasurementStyle.Full) - firstMeasure;

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

        // The unified MeasureString overload internally handles the no-font
        // case (falling back to DefaultBitmapFont, or zero under TEST). The
        // outer null-guard previously used on MonoGame is preserved as a
        // portable "do we have something to measure against" check - on
        // Raylib there is no BitmapFont property, so we guard on the
        // coreTextObject itself. In practice this is always non-null here,
        // but keeping the else branch preserves the historical side effect
        // on caretComponent.X when no measurement can be performed.
        if (this.coreTextObject != null)
        {
            var measure = this.coreTextObject.MeasureString(substring, global::RenderingLibrary.Graphics.HorizontalMeasurementStyle.Full);
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
        var lineHeight = coreTextObject.LineHeightInPixels;

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
