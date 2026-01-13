using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Converters;
using System.Collections;
using RenderingLibrary;




#if FRB
using MonoGameGum.Forms.Controls;
using Microsoft.Xna.Framework.Input;
using static FlatRedBall.Input.Xbox360GamePad;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using Buttons = FlatRedBall.Input.Xbox360GamePad.Button;
using GamepadButton = FlatRedBall.Input.Xbox360GamePad.Button;
namespace FlatRedBall.Forms.Controls;
#elif RAYLIB
using RaylibGum.Input;
using Keys = Raylib_cs.KeyboardKey;

#else
using MonoGameGum.Input;
using GamepadButton = Microsoft.Xna.Framework.Input.Buttons;
using Microsoft.Xna.Framework.Input;
#endif


#if !FRB
namespace Gum.Forms.Controls;

#endif

public class ComboBox :
#if FRB
    FrameworkElement,
#else
    Gum.Forms.Controls.FrameworkElement, 
#endif 
    IInputReceiver
{
    #region Fields/Properties

    ListBox listBox;
    GraphicalUiElement textComponent;
    global::RenderingLibrary.Graphics.IText coreTextObject;

    /// <summary>
    /// Replaces the internal ListBox with the provided instance.
    /// This replaces the Items, SelectedObject, and SelectedIndex unless
    /// the properties are bound.
    /// </summary>
    public ListBox ListBox
    {
        get => listBox;
        set
        {
            // If the user replaces this, it's up to the user to set the positions
            // and parenting properly!
            if(value != listBox)
            {
                listBox = value;



                listBox.IsVisible = false;
            }
        }
    }

    public string? Text
    {
        get => coreTextObject.RawText;
        set
        {
            if (value != Text)
            {
                coreTextObject.RawText = value;

                PushValueToViewModel();
            }
        }
    }

    string _displayMemberPath;
    public string DisplayMemberPath
    {
        get => _displayMemberPath;
        set
        {
            _displayMemberPath = value;
            UpdateToObject(SelectedObject);
        }
    }
    
    public IList Items
    {
        get => listBox.Items;
        set
        {
            if (value != Items)
            {
                listBox.Items = value;

                PushValueToViewModel();
            }
        }
    }


    [Obsolete("Use VisualTemplate")]
    public Type ListBoxItemGumType
    {
        get { return listBox.ListBoxItemGumType; }
        set
        {
#if FULL_DIAGNOSTICS
            if (listBox == null)
            {
                throw new Exception("Visual must be set before assigning the ListBoxItemType");
            }
#endif
            listBox.ListBoxItemGumType = value;
        }
    }

    [Obsolete("Use FrameworkElementTemplate")]
    public Type ListBoxItemFormsType
    {
        get { return listBox.ListBoxItemFormsType; }
        set
        {
#if FULL_DIAGNOSTICS
            if (listBox == null)
            {
                throw new Exception("Visual must be set before assigning the ListBoxItemType");
            }
#endif
            listBox.ListBoxItemFormsType = value;
        }
    }

    public FrameworkElementTemplate FrameworkElementTemplate
    {
        get => listBox.FrameworkElementTemplate;
        set => listBox.FrameworkElementTemplate = value;
    }

    public VisualTemplate VisualTemplate
    {
        get => listBox.VisualTemplate;
        set => listBox.VisualTemplate = value;
    }

    public object SelectedObject
    {
        get => listBox.SelectedObject;
        set => listBox.SelectedObject = value;
    }
    public int SelectedIndex
    {
        get => listBox.SelectedIndex;
        set => listBox.SelectedIndex = value;
    }

    public List<Keys> IgnoredKeys => throw new NotImplementedException();

    public bool TakingInput => throw new NotImplementedException();

    public IInputReceiver NextInTabSequence { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    // These values are the list box values before the list box has been expanded.
    // The reason we need these is because the list box may have values (like Width)
    // which depend on the combo box. Normal gum objects automatically handle layout,
    // but the ListBox needs to be detached from its parent ComboBox when it is shown so
    // that it can be moved above all other UI (a sorting issue). But since the ListBox is
    // detached from its parent, we can't have it automatically update its size to the parent.
    // Therefore, the list box showing process is as follows:
    // 1. Apply the values below to the list box (force it to have the same values it had before it was shown)
    // 2. Let the Gum layout engine do its updating
    // 3. Read the absolute x,y,width, and height (this happens in the show function)
    // 4. Detach the list box from its parent and add it with a positive infinity Z so it sorts on top
    // 5. Apply the absolute values to the list box
    // This will result in the list box shown at the proper location and size.
    DimensionUnitType listBoxWidthUnits;
    DimensionUnitType listBoxHeightUnits;
    GeneralUnitType listBoxXUnits;
    GeneralUnitType listBoxYUnits;
    float listBoxX;
    float listBoxY;
    float listBoxWidth;
    float listBoxHeight;

    public bool IsDropDownOpen
    {
        get => listBox.IsVisible;
        set
        {
            if (value && IsDropDownOpen == false)
            {
                ShowListBoxInstance();
            }
            else if (!value && IsDropDownOpen)
            {
                HideListBox();
            }
        }
    }

    bool _hadFocusOnListBoxOpened;

    #endregion

    #region Events

    public event Action<object, SelectionChangedEventArgs> SelectionChanged;
    public event Action<IInputReceiver> FocusUpdate;
    public event Action<GamepadButton> ControllerButtonPushed;
    public event Action<int> GenericGamepadButtonPushed;

    #endregion

    #region Initialize Methods

    public ComboBox() : base() { }

    public ComboBox(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        RefreshInternalVisualReferences();

#if FRB
        Visual.Click += _=> this.HandleClick(this, EventArgs.Empty);
        Visual.Push += _ => this.HandlePush(this, EventArgs.Empty);
        Visual.LosePush += _ => this.HandleLosePush(this, EventArgs.Empty);
        Visual.RollOn += _ => this.HandleRollOn(this, EventArgs.Empty);
        Visual.RollOff += _ => this.HandleRollOff(this, EventArgs.Empty);
#else
        Visual.Click += this.HandleClick;
        Visual.Push += this.HandlePush;
        Visual.LosePush += this.HandleLosePush;
        Visual.RollOn += this.HandleRollOn;
        Visual.RollOff += this.HandleRollOff;
#endif

        listBox.SelectionChanged += HandleSelectionChanged;
        listBox.ItemPushed += HandleListBoxItemPushed;


        base.ReactToVisualChanged();

        UpdateState();
    }

    protected override void RefreshInternalVisualReferences()
    {
        var listBoxInstance = Visual.GetGraphicalUiElementByName("ListBoxInstance") as InteractiveGue;
        textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");

#if FULL_DIAGNOSTICS
        if (listBoxInstance == null)
        {
            throw new Exception("Gum object must have an object called \"ListBoxInstance\"");
        }

        if (textComponent == null)
        {
            throw new Exception("Gum object must have an object called \"TextInstance\"");
        }
#endif
        coreTextObject = textComponent.RenderableComponent as
            global::RenderingLibrary.Graphics.IText;

        // remove it because it's gotta be a "popup"

        Visual.Children.Remove(listBoxInstance);
        listBoxInstance.RemoveFromManagers();

        if (listBoxInstance.FormsControlAsObject == null)
        {
            listBox = new ListBox(listBoxInstance);
        }
        else
        {
            listBox = listBoxInstance.FormsControlAsObject as ListBox;

#if FULL_DIAGNOSTICS
            if (listBox == null)
            {
                var message = $"The ListBoxInstance Gum component inside the combo box {Visual.Name} is of type {listBoxInstance.FormsControlAsObject.GetType().Name}, but it should be of type ListBox";
                throw new Exception(message);
            }
#endif
        }
        var effectiveParent = listBox.Visual.EffectiveParentGue as InteractiveGue;
        if (effectiveParent != null)
        {
            effectiveParent.RaiseChildrenEventsOutsideOfBounds = true;
        }
        else
        {
            Visual.RaiseChildrenEventsOutsideOfBounds = true;
        }

        listBox.IsVisible = false;
        Text = null;
    }

    #endregion

    #region Event Handler Methods

    private void HandleClick(object sender, EventArgs args)
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

    private void HandlePush(object sender, EventArgs args)
    {
        if (IsDropDownOpen)
        {
            HideListBox();
        }
        else
        {
            ShowListBoxInstance();
        }

    }

    private void ShowListBoxInstance()
    {
        _hadFocusOnListBoxOpened = IsFocused;
        listBox.IsVisible = true;

        // Adding this to the Visual will force a layout....
        this.Visual.Children.Add(listBox.Visual);
        // ... so grab the absolute values:
        var x = listBox.Visual.AbsoluteX;
        var y = listBox.Visual.AbsoluteY;
        var width = listBox.Visual.GetAbsoluteWidth();
        var height = listBox.Visual.GetAbsoluteHeight();

        listBoxWidthUnits = listBox.Visual.WidthUnits;
        listBoxHeightUnits = listBox.Visual.HeightUnits;
        listBoxXUnits = listBox.Visual.XUnits;
        listBoxYUnits = listBox.Visual.YUnits;

        listBoxX = listBox.Visual.X;
        listBoxY = listBox.Visual.Y;
        listBoxWidth = listBox.Visual.Width;
        listBoxHeight = listBox.Visual.Height;

        // Now that we have the values, detach it:
        this.Visual.Children.Remove(listBox.Visual);
        listBox.X = x;
        listBox.Y = y;
        listBox.Visual.WidthUnits = DimensionUnitType.Absolute;
        listBox.Visual.HeightUnits = DimensionUnitType.Absolute;
        listBox.Width = width;
        listBox.Height = height;
        ListBox.ShowPopupListBox(listBox, this.Visual);


#if FRB
        GuiManager.AddNextPushAction(TryHideFromPush);
        GuiManager.SortZAndLayerBased();
        listBox.RepositionToKeepInScreen();

#else

        InteractiveGue.AddNextPushAction(TryHideFromPush);
#endif


        UpdateState();
    }

    private void TryHideFromPush()
    {
        var cursor = MainCursor;


        var clickedOnThisOrChild =
            cursor.WindowOver == this.Visual ||
            (cursor.WindowOver != null && cursor.WindowOver.IsInParentChain(this.Visual)) ||
            (cursor.WindowOver != null && cursor.WindowOver.IsInParentChain(this.listBox.Visual))
            ;

        if (clickedOnThisOrChild == false)
        {
            HideListBox();
        }
        else
        {
            // not sure why we have to re-add this here on a push.
            // Update - because when we click on something like the scroll
            // view in the popup listbox, this event gets raised. We don't want
            // to close this, but we may still want to next click if it's outside
            // of the list box. If the user keeps clicking on the scrollbar, this event
            // keeps getting raised, and it re-adds itself.
#if FRB
            GuiManager.AddNextPushAction(TryHideFromPush);
#else
            InteractiveGue.AddNextPushAction(TryHideFromPush);
#endif
        }
    }

    private void HideListBox()
    {
        if (Visual.EffectiveManagers != null && listBox.IsVisible)
        {
            listBox.IsVisible = false;
            listBox.Visual.RemoveFromManagers();

            listBox.Visual.XUnits = listBoxXUnits;
            listBox.Visual.YUnits = listBoxYUnits;
            listBox.Visual.WidthUnits = listBoxWidthUnits;
            listBox.Visual.HeightUnits = listBoxHeightUnits;

            listBox.Visual.X = listBoxX;
            listBox.Visual.Y = listBoxY;
            listBox.Visual.Width = listBoxWidth;
            listBox.Visual.Height = listBoxHeight;

            Visual.EffectiveManagers.Renderer.MainLayer.Remove(listBox.Visual);
#if FRB
#else
            listBox.Visual.GetTopParent()?.Children.Remove(listBox.Visual);
#endif

            UpdateState();
        }

        if(_hadFocusOnListBoxOpened)
        {
            IsFocused = true;
        }
    }

    private void HandleLosePush(object sender, EventArgs args)
    {
        UpdateState();
    }

    private void HandleSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        // If we bind the Text, then don't set this here, the binding will take care of it
        if (!IsDataBound(nameof(Text)))
        {
            UpdateToObject(listBox.SelectedObject);
        }

        // Why do we hide the list box here? We don't want to do it if
        // the user uses the arrow keys to change a selection do we?
        //HideListBox();

        PushValueToViewModel(nameof(SelectedObject));
        PushValueToViewModel(nameof(SelectedIndex));

        SelectionChanged?.Invoke(this, args);


    }

    public virtual void UpdateToObject(object o)
    {
        if(!string.IsNullOrEmpty(DisplayMemberPath ))
        {
            var display = o
                ?.GetType()
                ?.GetProperty(DisplayMemberPath)
                ?.GetValue(o, null) as string;

            coreTextObject.RawText = display?.ToString();
        }
        else
        {
            coreTextObject.RawText = o?.ToString();
        }
    }

    private void HandleListBoxItemPushed(object sender, EventArgs args)
    {
        HideListBox();
    }

#endregion

    #region UpdateTo Methods

    public override void UpdateState()
    {
        const string category = "ComboBoxCategoryState";

        var state = base.GetDesiredState();

        Visual.SetProperty(category, state);
    }

    #endregion

    #region IInputReceiver Methods

    public IInputReceiver? ParentInputReceiver =>
    this.GetParentInputReceiver();

    public void OnFocusUpdatePreview(RoutedEventArgs args)
    {
    }
    public void OnFocusUpdate()
    {
        if (IsDropDownOpen)
        {
            DoOpenDropDownFocusUpdate();
        }
        else
        {
            DoClosedDropDownFocusUpdate();
        }

#if MONOGAME && !FRB
        base.HandleKeyboardFocusUpdate();
#endif

    }

    private void DoOpenDropDownFocusUpdate()
    {
        var xboxGamepads = FrameworkElement.GamePadsForUiControl;

        for (int i = 0; i < xboxGamepads.Count; i++)
        {
            var gamepad = xboxGamepads[i];

            var movedDown = gamepad.ButtonRepeatRate(GamepadButton.DPadDown) ||
                gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Down);

            var movedUp = gamepad.ButtonRepeatRate(GamepadButton.DPadUp) ||
                     gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Up);

            var pressedButton = gamepad.ButtonPushed(GamepadButton.A) ||
                gamepad.ButtonPushed(GamepadButton.B);
            DoDropDownOpenFocusUpdate(movedDown, movedUp, pressedButton);
        }
#if FRB

        var genericGamepads = GuiManager.GenericGamePadsForUiControl;
        for(int i = 0; i < genericGamepads.Count; i++)
        {
            var gamepad = genericGamepads[i];

            var movedDown = gamepad.DPadRepeatRate(Xbox360GamePad.DPadDirection.Down) ||
                (gamepad.AnalogSticks.Length > 0 && gamepad.AnalogSticks[0].AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection.Down));
            var movedUp = gamepad.DPadRepeatRate(Xbox360GamePad.DPadDirection.Up) ||
                (gamepad.AnalogSticks.Length > 0 && gamepad.AnalogSticks[0].AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection.Up));

            var inputDevice = gamepad as IInputDevice;

            var pressedButton = inputDevice.DefaultPrimaryActionInput.WasJustPressed || inputDevice.DefaultBackInput.WasJustPressed;

            DoDropDownOpenFocusUpdate(movedDown, movedUp, pressedButton);
        }
#endif
    }

    private void DoDropDownOpenFocusUpdate(bool movedDown, bool movedUp, bool pressedButton)
    {
        if (movedDown)
        {
            if (Items.Count > 0)
            {
                if (SelectedIndex < 0 && Items.Count > 0)
                {
                    SelectedIndex = 0;
                }
                else if (SelectedIndex < Items.Count - 1)
                {
                    SelectedIndex++;
                }

                this.listBox.ListBoxItems[SelectedIndex].IsFocused = true;
            }
        }
        else if (movedUp)
        {
            if (Items.Count > 0)
            {
                if (SelectedIndex < 0 && Items.Count > 0)
                {
                    SelectedIndex = 0;
                }
                else if (SelectedIndex > 0)
                {
                    SelectedIndex--;
                }

                this.listBox.ListBoxItems[SelectedIndex].IsFocused = true;
            }
        }

        if (pressedButton)
        {
            IsDropDownOpen = false;
        }
    }

    private void DoClosedDropDownFocusUpdate()
    {
        var gamepads = FrameworkElement.GamePadsForUiControl;

        for (int i = 0; i < gamepads.Count; i++)
        {
            var gamepad = gamepads[i];

            HandleGamepadNavigation(gamepad);

            if (gamepad.ButtonPushed(GamepadButton.A))
            {
                OpenAndFocusListBox();
            }

            void RaiseIfPushedAndEnabled(GamepadButton button)
            {
                if (IsEnabled && gamepad.ButtonPushed(button))
                {
                    ControllerButtonPushed?.Invoke(button);
                }
            }

            RaiseIfPushedAndEnabled(GamepadButton.B);
            RaiseIfPushedAndEnabled(GamepadButton.X);
            RaiseIfPushedAndEnabled(GamepadButton.Y);
            RaiseIfPushedAndEnabled(GamepadButton.Start);
            RaiseIfPushedAndEnabled(GamepadButton.Back);
        }

        void OpenAndFocusListBox()
        {
            IsDropDownOpen = true;

            this.listBox.IsFocused = true;
            this.listBox.DoListItemsHaveFocus = true;
            
            if (SelectedIndex > -1 && SelectedIndex < this.Items.Count)
            {
                this.listBox.ListBoxItems[SelectedIndex].IsFocused = true;
            }
            
        }
#if FRB

        var genericGamepads = GuiManager.GenericGamePadsForUiControl;
        for(int i = 0; i < genericGamepads.Count; i++)
        {
            var gamepad = genericGamepads[i];

            HandleGamepadNavigation(gamepad);

            if ((gamepad as IInputDevice).DefaultConfirmInput.WasJustPressed)
            {
                IsDropDownOpen = IsDropDownOpen = true;

                if (SelectedIndex > -1 && SelectedIndex < this.Items.Count)
                {
                    this.listBox.ListBoxItems[SelectedIndex].IsFocused = true;
                }
            }

            if(IsEnabled)
            {
                for(int buttonIndex = 0; buttonIndex < gamepad.NumberOfButtons; i++)
                {
                    if(gamepad.ButtonPushed(buttonIndex))
                    {
                        GenericGamepadButtonPushed?.Invoke(buttonIndex);
                    }
                }
            }
        }
#endif
#if (MONOGAME || KNI) && !FRB

        foreach (var keyboard in KeyboardsForUiControl)
        {
            var wasPushed = false;
            var wasReleased = false;
            foreach(var item in FrameworkElement.ClickCombos)
            {
                if(item.IsComboPushed())
                {
                    wasPushed = true;
                }
                if(item.IsComboReleased())
                {
                    wasReleased = true;
                    UpdateState();
                }
            }
            if(wasPushed)
            {
                InteractiveGue.LastVisualPushed = this.Visual;
                this.HandleClick(this, new InputEventArgs() { InputDevice = keyboard });

                OpenAndFocusListBox();
                UpdateState();
            }
            if(wasReleased)
            {
                UpdateState();
            }
        }


        base.HandleKeyboardFocusUpdate();
#endif


    }

    public void OnGainFocus()
    {
        IsFocused = true;
    }

    [Obsolete("Use OnLoseFocus")]
    public void LoseFocus() => OnLoseFocus();
    public void OnLoseFocus()
    {
        IsFocused = false;
    }

    public void ReceiveInput()
    {
    }

    public void HandleKeyDown(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
    {
    }

    public void HandleCharEntered(char character)
    {
    }

#if !FRB
    public void DoKeyboardAction(IInputReceiverKeyboard keyboard)
    {
        foreach (Keys key in keyboard.KeysTyped)
        {
            HandleKeyDown(key,
                keyboard.IsShiftDown,
                keyboard.IsAltDown,
                keyboard.IsCtrlDown);
        }
    }
#endif

#endregion
}
