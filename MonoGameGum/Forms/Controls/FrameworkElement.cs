using Gum.Wireframe;


using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using System.Threading;
using Gum.DataTypes.Variables;




#if FRB
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Microsoft.Xna.Framework.Input;
using static FlatRedBall.Input.Xbox360GamePad;
using FlatRedBall.Forms.GumExtensions;
using FlatRedBall.Forms.Input;
using FlatRedBall.Forms.Data;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using BindableGue = global::Gum.Wireframe.GraphicalUiElement;
using Buttons = FlatRedBall.Input.Xbox360GamePad.Button;
namespace FlatRedBall.Forms.Controls;
#elif RAYLIB
using RaylibGum;
using RaylibGum.Input;
using Keys = Raylib_cs.KeyboardKey;

#else
using Keys = Microsoft.Xna.Framework.Input.Keys;
using GamePad = MonoGameGum.Input.GamePad;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Input;
#endif


#if !FRB
using Gum.Forms.Data;
namespace Gum.Forms.Controls;
#endif

#region Enums

public enum TabDirection
{
    Up,
    Down
}

public enum TabbingFocusBehavior
{
    FocusableIfInputReceiver,
    SkipOnTab
}
#endregion

#region Events

#if !FRB
public class KeyEventArgs : EventArgs
{
    public Keys Key { get; set; }
}
#endif

public delegate void KeyEventHandler(object sender, KeyEventArgs e);

#endregion



public class FrameworkElement : INotifyPropertyChanged
{
    #region Fields/Properties

#if FRB
    public static Cursor MainCursor => GuiManager.Cursor;

    public static FlatRedBall.Input.Keyboard MainKeyboard => FlatRedBall.Input.Keyboard.Main;

    public static List<Xbox360GamePad> GamePadsForUiControl => GuiManager.GamePadsForUiControl;
#else
    public static ICursor MainCursor { get; set; }

    public static IInputReceiverKeyboard MainKeyboard { get; set; }

#if !FRB
    public Cursors? CustomCursor { get; set; }
#endif

    public static List<GamePad> GamePadsForUiControl { get; private set; } = new List<GamePad>();

#if MONOGAME || KNI || FNA
    public static List<IInputReceiverKeyboardMonoGame> KeyboardsForUiControl { get; private set; } = new List<IInputReceiverKeyboardMonoGame>();
#endif

#endif

#if !FRB
    // March 15, 2025 - should these be a part of FrameworkElement?
    // Or instead should they be moved to GumService

    /// <summary>
    /// Container used to hold popups such as the ListBox which appears when clicking on a combo box.
    /// </summary>
    public static InteractiveGue PopupRoot { get; set; }

    /// <summary>
    /// Container used to hold modal objects. If any object is added to this container, then all other
    /// UI does not receive events.
    /// </summary>
    public static InteractiveGue ModalRoot { get; set; }

#endif

    protected bool isFocused;
    protected double timeFocused;
    /// <summary>
    /// Whether this element has input focus. If true, the element shows its focused
    /// state and receives input from keyboards and gamepads.
    /// </summary>
    public virtual bool IsFocused
    {
        get { return isFocused; }
        set
        {
            if (value != isFocused)
            {
                isFocused = value && IsEnabled;

                if (isFocused && this is IInputReceiver inputReceiver)
                {
                    InteractiveGue.CurrentInputReceiver = inputReceiver;
                }

                UpdateState();

                PushValueToViewModel();

                if (isFocused)
                {
                    timeFocused = InteractiveGue.CurrentGameTime;
                    GotFocus?.Invoke(this, null);
                }
                else
                {
                    LostFocus?.Invoke(this, null);

                    if (this is IInputReceiver inputReceiver2 && InteractiveGue.CurrentInputReceiver == inputReceiver2)
                    {
                        InteractiveGue.CurrentInputReceiver = null;
                    }
                }
            }
            // this resolves possible stale states:
            else
            {
                if (isFocused && this is IInputReceiver inputReceiver)
                {
                    InteractiveGue.CurrentInputReceiver = inputReceiver;
                }
            }

        }
    }

    [Obsolete] 
    protected Dictionary<string, string> vmPropsToUiProps = null!;

    internal PropertyRegistry PropertyRegistry { get; }

    public object BindingContext
    {
        get => Visual?.BindingContext;
        set
        {
            if (Visual != null)
            {
                Visual.BindingContext = value;
            }
        }
    }

    public event EventHandler<BindingContextChangedEventArgs>? BindingContextChanged;
    internal event EventHandler<BindingContextChangedEventArgs>? InheritedBindingContextChanged;

    /// <summary>
    /// The height in pixels. This is a calculated value considering HeightUnits and Height.
    /// </summary>
    public float ActualHeight => Visual.GetAbsoluteHeight();
    /// <summary>
    /// The width in pixels. This is a calculated value considering WidthUnits and Width;
    /// </summary>
    public float ActualWidth => Visual.GetAbsoluteWidth();

    /// <summary>
    /// Returns the left of this element in absolute (screen) coordinates
    /// </summary>
    public float AbsoluteLeft => Visual.AbsoluteLeft;
    /// <summary>
    /// Returns the top of this element in absolute (screen) coordinates
    /// </summary>
    public float AbsoluteTop => Visual.AbsoluteTop;

    public float Height
    {
        get { return Visual.Height; }
        set
        {
#if DEBUG
            if (float.IsNaN(value))
            {
                throw new Exception("NaN value not supported for FrameworkElement Height");
            }
            if (float.IsPositiveInfinity(value) || float.IsNegativeInfinity(value))
            {
                throw new Exception();
            }
#endif
            Visual.Height = value;
        }
    }
    public float Width
    {
        get { return Visual.Width; }
        set
        {
#if DEBUG
            if (float.IsNaN(value))
            {
                throw new Exception("NaN value not supported for FrameworkElement Width");
            }
            if (float.IsPositiveInfinity(value) || float.IsNegativeInfinity(value))
            {
                throw new Exception();
            }
            if(Visual == null)
            {
                throw new NullReferenceException($"Cannot set Width because Visual hasn't yet been set on this {GetType()}");
            }
#endif
            Visual.Width = value;
        }
    }

#if FRB
    /// <summary>
    /// The X position of the left side of the element in pixels.
    /// </summary>
    [Obsolete("Use AbsoluteLeft")]
    public float ActualX => Visual.GetLeft();

    /// <summary>
    /// The Y position of the top of the element in pixels (positive Y is down).
    /// </summary>
    [Obsolete("Use AbsoluteTop")]
    public float ActualY => Visual.GetTop();
#endif

    public float X
    {
        get { return Visual.X; }
        set { Visual.X = value; }
    }
    public float Y
    {
        get { return Visual.Y; }
        set { Visual.Y = value; }
    }

    public void Anchor(Anchor anchor) => Visual.Anchor(anchor);
    public void Dock(Dock dock) => Visual.Dock(dock);

    bool isEnabled = true;
    /// <summary>
    /// Whether the element is enabled or not. When disabled, the element does
    /// not respond to user input, and displays a disabled state.
    /// </summary>

    public virtual bool IsEnabled
    {
        get => isEnabled;
        set
        {
            if (isEnabled != value)
            {
                if (value == false && IsFocused)
                {
                    // If we disabled this, then unfocus it, and select next tab
                    //this.HandleTab(TabDirection.Down, this);
                    // Update 10/2/2020
                    // Actually this is causing
                    // some annoying behavior. If
                    // a button is used to buy items
                    // which can be bought multiple times,
                    // the button may become disabled after
                    // the user has no more money, automatically
                    // focusing on the next button which may result
                    // in unintended actions.
                }
                isEnabled = value;
                Visual.IsEnabled = value;

                UpdateState();
            }
        }
    }

    public bool IsVisible
    {
        get { return Visual.Visible; }
        set { Visual.Visible = value; }
    }

    public string Name
    {
        get { return Visual.Name; }
        set { Visual.Name = value; }
    }

    public FrameworkElement ParentFrameworkElement
    {
        get
        {
            var parent = this.Visual.Parent;

            while (parent is InteractiveGue parentGue)
            {
                var parentForms = parentGue.FormsControlAsObject as FrameworkElement;

                if (parentForms != null)
                {
                    return parentForms;
                }
                else
                {
                    parent = parent.Parent;
                }
            }

            return null;
        }
    }

    InteractiveGue visual;
    public InteractiveGue Visual
    {
        get => visual;
        set
        {
#if DEBUG
            // allow the visual to be un-assigned if it was assigned before, like if a forms control is getting removed.
            if (value == null && visual == null)
            {
                throw new ArgumentNullException("Visual cannot be assigned to null");
            }
#endif
            InteractiveGue oldVisual = visual;
            if (visual != value)
            {
#if DEBUG
                if(value?.FormsControlAsObject != null)
                {
                    var message =
                        $"Cannot set the {this.GetType().Name}'s Visual to {value.Name} because the assigned Visual is already the Visual for another framework element of type {value.FormsControlAsObject}";
                    throw new InvalidOperationException(message);
                }

#endif


                if (visual != null)
                {
                    // unsubscribe:
                    visual.BindingContextChanged -= OnVisualBindingContextChanged;
                    visual.InheritedBindingContextChanged -= OnVisualInheritedBindingContextChanged;
                    visual.EnabledChange -= HandleEnabledChanged;
                    visual.ParentChanged -= HandleParentChanged;
                    ReactToVisualRemoved();
                }


                visual = value;

                if (visual != null)
                {

                    if(visual is InteractiveGue newVisualInteractiveGue)
                    {
                        newVisualInteractiveGue.FormsControlAsObject = this;
                    }
                    

                    visual.BindingContextChanged += OnVisualBindingContextChanged;
                    visual.InheritedBindingContextChanged += OnVisualInheritedBindingContextChanged;
                    visual.EnabledChange += HandleEnabledChanged;
                    visual.ParentChanged += HandleParentChanged;
                }

                if (oldVisual?.BindingContext != value?.BindingContext)
                {
                    OnVisualBindingContextChanged(this, new()
                    {
                        OldBindingContext = oldVisual?.BindingContext,
                        NewBindingContext = value?.BindingContext
                    });
                }

                ReactToVisualChanged();
            }

        }
    }


    private void OnVisualBindingContextChanged(object? sender, BindingContextChangedEventArgs args)
    {
        BindingContextChanged?.Invoke(sender, args);
        OnPropertyChanged(nameof(BindingContext));
        OnBindingContextChanged(sender, args);
    }

    internal void OnVisualInheritedBindingContextChanged(object? sender, BindingContextChangedEventArgs args) =>
        InheritedBindingContextChanged?.Invoke(sender, args);

    /// <summary>
    /// Contains the default association between Forms Controls and Gum Runtime Types. 
    /// This dictionary enabled forms controls (like TextBox) to automatically create their own visuals.
    /// The key in the dictionary is the type of Forms control.
    /// </summary>
    /// <remarks>
    /// This dictionary simplifies working with FlatRedBall.Forms in code. It allows one piece of code 
    /// (which may be generated by Glue) to associate the Forms controls with a Gum runtime type. Once 
    /// this association is made, controls can be created without specifying a gum runtime. For example:
    /// var button = new Button();
    /// button.Visual.AddToManagers();
    /// button.Click += HandleButtonClick;
    /// 
    /// Note that this association is used when instantiating a new Forms type in code, but it is not used when instantiating
    /// a new Gum runtime type - the Gum runtime must instantiate and associate its Forms object in its own code.
    /// </remarks>
    /// <example>
    /// FrameworkElement.DefaultFormsComponents[typeof(FlatRedBall.Forms.Controls.Button)] = 
    ///     typeof(ProjectName.GumRuntimes.LargeMenuButtonRuntime);
    /// </example>
    [Obsolete("Use DefaultFormsTemplates")]
    public static Dictionary<Type, Type> DefaultFormsComponents { get; private set; } = new Dictionary<Type, Type>();

    public static Dictionary<Type, VisualTemplate> DefaultFormsTemplates { get; private set; } = new Dictionary<Type, VisualTemplate>();

    protected static InteractiveGue GetGraphicalUiElementFor(FrameworkElement element)
    {
        var type = element.GetType();
        return GetGraphicalUiElementForFrameworkElement(type);
    }

    public static InteractiveGue? GetGraphicalUiElementForFrameworkElement(Type type)
    {
        if(DefaultFormsTemplates.ContainsKey(type))
        {
            return DefaultFormsTemplates[type].CreateContent(null, createFormsInternally:false) as InteractiveGue;
        }
        else if (DefaultFormsComponents.ContainsKey(type))
        {
            var gumType = DefaultFormsComponents[type];
            // The bool/bool constructor is required to match the FlatRedBall.Forms functionality
            // of being able to be Gum-first or forms-first. The 2nd bool in particular tells the runtime
            // whether to create a forms object. Yes, this is less convenient for the user who is manually
            // creating runtimes, but it's worth it for the standard behavior of the user creating instances
            // of Gum objects, and to be able to create Forms objects in Gum tool
            var boolBoolConstructor = gumType.GetConstructor(new[] { typeof(bool), typeof(bool) });
            if(boolBoolConstructor != null)
            {
                return boolBoolConstructor.Invoke(new object[] { true, false }) as InteractiveGue;
            }
            else
            {
                throw new Exception($"The Runtime type {gumType} needs to have a two-argument constructor, both bools, where" +
                    " the second argument controls whether the object should internally create a Forms instance.");
            }

        }
        else
        {
            var baseType = type.BaseType;
            if (baseType == typeof(object) || baseType == typeof(FrameworkElement))
            {
                //var message =
                //    $"Could not find default Gum Component for {type}. You can solve this by adding a Gum type for {type} to " +
                //    $"{nameof(FrameworkElement)}.{nameof(DefaultFormsTemplates)}, or constructing the Gum object itself.";

                //throw new Exception(message);
                return null;
            }
            else
            {
                return GetGraphicalUiElementForFrameworkElement(baseType);
            }
        }
    }

    public TabbingFocusBehavior GamepadTabbingFocusBehavior { get; set; } = TabbingFocusBehavior.FocusableIfInputReceiver;

    #endregion

    #region Events

    public event EventHandler GotFocus;
    public event EventHandler LostFocus;
    public event EventHandler Loaded;
    public event KeyEventHandler KeyDown;

    #endregion

    #region Constructor

    public FrameworkElement()
    {
        PropertyRegistry = new(this);
        var possibleVisual = GetGraphicalUiElementFor(this);
        if(possibleVisual != null)
        {
            Visual = possibleVisual;
            Visual.FormsControlAsObject = this;
        }
    }

    public FrameworkElement(InteractiveGue visual)
    {
        PropertyRegistry = new(this);
        if (visual != null)
        {
            this.Visual = visual;
            this.Visual.FormsControlAsObject = this;
        }
    }

    #endregion

    #region Hide/Show/Add/Remove 

    public virtual void AddChild(FrameworkElement child)
    {
        if (child.Visual == null)
        {
            throw new InvalidOperationException($"The child of type {child.GetType()} must have a Visual before being added to the parent");
        }
        if (this.Visual == null)
        {
            throw new InvalidOperationException("This must have its Visual set before having children added");
        }

        child.Visual.Parent = this.Visual;
    }

    public virtual void AddChild(GraphicalUiElement child)
    {
        if(child == null)
        {
            throw new ArgumentNullException(nameof(child));
        }
        if (this.Visual == null)
        {
            throw new InvalidOperationException("This must have its Visual set before having children added");
        }

        child.Parent = this.Visual;

    }

    public void Close()
    {
#if FRB
        if (!FlatRedBallServices.IsThreadPrimary())
        {

            InstructionManager.AddSafe(CloseInternal);
        }
        else
#endif
        {
            CloseInternal();
        }
    }


    private void CloseInternal()
    {
        var inputReceiver = InteractiveGue.CurrentInputReceiver;
        if (inputReceiver != null)
        {
            if (inputReceiver is InteractiveGue gue)
            {
                if (gue.IsInParentChain(this.Visual))
                {
                    InteractiveGue.CurrentInputReceiver = null;
                }
            }
            else if (inputReceiver is FrameworkElement frameworkElement)
            {
                if (frameworkElement.Visual?.IsInParentChain(this.Visual) == true)
                {
                    InteractiveGue.CurrentInputReceiver = null;
                }
            }
        }
        Visual.RemoveFromManagers();
    }

    /// <summary>
    /// Displays this element visually and adds it to the underlying managers for Cursor interaction.
    /// </summary>
    /// <remarks>
    /// This is typically only called if the element is instantiated in code. Elements added
    /// to a Gum screen in Gum will automatically be displayed when the Screen is created, and calling
    /// this will result in the object being added twice.</remarks>
    /// <param name="layer">The layer to add this to, can be null to add it directly to managers</param>
    [Obsolete("Do not use this method. Either add this to the Root, to a Screen, or to a parent container")]
#if FRB
    public void Show(FlatRedBall.Graphics.Layer layer = null)
#else

    public void Show(Layer layer = null)
#endif
    {
#if DEBUG
        if (Visual == null)
        {
            throw new InvalidOperationException("Visual must be set before calling Show");
        }
#endif

#if FRB
        Layer gumLayer = null;
        if(layer != null)
        {
            gumLayer = Gum.GumIdb.Self.GumLayersOnFrbLayer(layer).FirstOrDefault();

#if DEBUG
            if(gumLayer == null)
            {
                throw new InvalidOperationException("Could not find a Gum layer on this FRB layer");
            }
#endif
        }

        if (!FlatRedBallServices.IsThreadPrimary())
        {
            InstructionManager.AddSafe(() =>
            {
                Visual.AddToManagers(RenderingLibrary.SystemManagers.Default, gumLayer);
            });

        }
        else
#endif
        {
            Visual.AddToManagers(global::RenderingLibrary.SystemManagers.Default,
#if FRB
                gumLayer);
#else
                layer);
#endif
        }
    }

#if FRB
    /// <summary>
    /// Displays this visual element (calls Show), and returns a task which completes once
    /// the dialog is removed.
    /// </summary>
    /// <param name="frbLayer">The FlatRedBall Layer to be used to display the element.</param>
    /// <returns>A task which will complete once this element is removed from managers.</returns>
    public async Task<bool?> ShowDialog(FlatRedBall.Graphics.Layer frbLayer = null)
    {
#if DEBUG
        if (Visual == null)
        {
            throw new InvalidOperationException("Visual must be set before calling Show");
        }
#endif
        var semaphoreSlim = new SemaphoreSlim(1);

        void HandleRemovedFromManagers(object sender, EventArgs args) => semaphoreSlim.Release();
        Visual.RemovedFromGuiManager += HandleRemovedFromManagers;

        semaphoreSlim.Wait();
        Show(frbLayer);
        await semaphoreSlim.WaitAsync();

        Visual.RemovedFromGuiManager -= HandleRemovedFromManagers;
        // for now, return null, todo add dialog results

        semaphoreSlim.Dispose();

        return null;
    }
#endif

    #endregion

    #region Cursor Hit Detection

#if FRB
    protected bool GetIfIsOnThisOrChildVisual(Cursor cursor)
#else
    protected bool GetIfIsOnThisOrChildVisual(ICursor cursor)
#endif
    {
        var isOnThisOrChild =
            cursor.WindowOver == this.Visual ||
            (cursor.WindowOver != null && cursor.WindowOver.IsInParentChain(this.Visual));

        return isOnThisOrChild;
    }

    #endregion

    /// <summary>
    /// Calls the loaded event. This should not be called in custom code, but instead is called by Gum
    /// </summary>
    public virtual void CallLoaded() => Loaded?.Invoke(this, null);

    protected void RaiseKeyDown(KeyEventArgs e)
    {
        KeyDown?.Invoke(this, e);
    }

    /// <summary>
    /// Every-frame logic. This will automatically be called if this element is added to the FrameworkElementManager
    /// </summary>
    public virtual void Activity()
    {

    }

    public void RepositionToKeepInScreen()
    {
#if DEBUG
        if (Visual == null)
        {
            throw new InvalidOperationException("Visual hasn't yet been set");
        }
        if (Visual.Parent != null)
        {
            throw new InvalidOperationException("This cannot be moved to keep in screen because it depends on its parent's position");
        }
#endif
        //var cameraTop = 0;
        var cameraBottom = Renderer.Self.Camera.ClientHeight / Renderer.Self.Camera.Zoom;
        //var cameraLeft = 0;
        var cameraRight = Renderer.Self.Camera.ClientWidth / Renderer.Self.Camera.Zoom;

        var thisBottom = this.Visual.AbsoluteY + this.Visual.GetAbsoluteHeight();
        if (thisBottom > cameraBottom)
        {
            // assume absolute positioning (for now?)
            this.Y -= (thisBottom - cameraBottom);
        }
    }

    protected virtual void ReactToVisualChanged() { }

    protected virtual void RefreshInternalVisualReferences() { }

    /// <summary>
    /// Method raised when the current visual is changed or set to null. If the visual
    /// is being replaced, this is called before the new visual is assigned.
    /// </summary>
    protected virtual void ReactToVisualRemoved()
    {

    }

    public T? GetVisual<T>(string? name = null) where T : GraphicalUiElement
    {
        var currentItem = Visual;

        return GetVisual<T>(name,  currentItem);
    }

    private T? GetVisual<T>(string? name, GraphicalUiElement currentItem) where T : GraphicalUiElement
    {
        if(IsMatch(currentItem))
        {
            return currentItem as T;
        }

        if (currentItem.Children != null)
        {
            foreach (var child in currentItem.Children)
            {
                var found = GetVisual<T>(name, child as GraphicalUiElement);
                if (found != null)
                {
                    return found;
                }
            }
        }
        else
        {
            foreach(var item in currentItem.ContainedElements)
            {
                var found = GetVisual<T>(name, item as GraphicalUiElement);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;

        bool IsMatch(GraphicalUiElement potentialMatch)
        {
            var isNameMatch = string.IsNullOrEmpty(name) || potentialMatch.Name == name;
            return isNameMatch && currentItem is T;
        }
    }

    /// <summary>
    /// Searches recursively for and returns a GraphicalUiElement (visual) in this instance by name. Returns null
    /// if not found.
    /// </summary>
    /// <param name="name">The case-sensitive name to search for.</param>
    /// <returns>The found GraphicalUiElement, or null if no match is found.</returns>
    public GraphicalUiElement? GetVisual(string name) =>
        Visual.GetGraphicalUiElementByName(name) as GraphicalUiElement;



    public StateSave GetState(string stateName)
    {
        foreach (var category in Visual.Categories.Values)
        {
            foreach (var state in category.States)
            {
                if (state.Name == stateName)
                {
                    return state;
                }
            }
        }
        throw new InvalidOperationException($"Could not find a state named {stateName}");
    }

    #region Binding/ViewModel

    public void SetBinding(string uiProperty, string vmProperty) => SetBinding(uiProperty, new Binding(vmProperty));

    public void SetBinding(string uiProperty, Binding binding)
    {
        PropertyRegistry.SetBinding(uiProperty, binding);
    }
    
    public void ClearBinding(string uiProperty)
    {
        PropertyRegistry.ClearBinding(uiProperty);
    }

    [Obsolete("Use OnBindingContextChanged")]
    protected virtual void HandleVisualBindingContextChanged(object sender, BindingContextChangedEventArgs args) { }

    protected virtual void OnBindingContextChanged(object sender, BindingContextChangedEventArgs args) =>
        HandleVisualBindingContextChanged(sender, args);

    protected void PushValueToViewModel([CallerMemberName] string uiPropertyName = null)
    {
        OnPropertyChanged(uiPropertyName);
    }

    #endregion

    #region Tabbing

    /// <summary>
    /// Whether to use left and right directions as navigation. If false, left and right directions are ignored for navigation.
    /// </summary>
    public bool IsUsingLeftAndRightGamepadDirectionsForNavigation { get; set; } = true;

#if FRB
    protected void HandleGamepadNavigation(Xbox360GamePad gamepad)
#else
    protected void HandleGamepadNavigation(GamePad gamepad)
#endif
    {
        // todo for raylib...
#if !RAYLIB
        if (gamepad.ButtonRepeatRate(Buttons.DPadDown) ||
            (IsUsingLeftAndRightGamepadDirectionsForNavigation && gamepad.ButtonRepeatRate(Buttons.DPadRight)) ||
            gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Down) ||
            (IsUsingLeftAndRightGamepadDirectionsForNavigation && gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Right)))
        {
            this.HandleTab(TabDirection.Down, this, loop:true);
        }
        else if (gamepad.ButtonRepeatRate(Buttons.DPadUp) ||
            (IsUsingLeftAndRightGamepadDirectionsForNavigation && gamepad.ButtonRepeatRate(Buttons.DPadLeft)) ||
            gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Up) ||
            (IsUsingLeftAndRightGamepadDirectionsForNavigation && gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Left)))
        {
            this.HandleTab(TabDirection.Up, this, loop: true);
        }
#endif
    }

#if FRB
    protected void HandleGamepadNavigation(GenericGamePad gamepad)
    {
        AnalogStick leftStick = gamepad.AnalogSticks.Length > 0
            ? gamepad.AnalogSticks[0]
            : null;

        if (gamepad.DPadRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Down) ||
            (IsUsingLeftAndRightGamepadDirectionsForNavigation && gamepad.DPadRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Right)) ||
            leftStick?.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Down) == true ||
            (IsUsingLeftAndRightGamepadDirectionsForNavigation && leftStick?.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Right) == true))
        {
            this.HandleTab(TabDirection.Down, this);
        }
        else if (gamepad.DPadRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Up) ||
            (IsUsingLeftAndRightGamepadDirectionsForNavigation && gamepad.DPadRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Left)) ||
            leftStick?.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Up) == true ||
            (IsUsingLeftAndRightGamepadDirectionsForNavigation && leftStick?.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Left) == true))
        {
            this.HandleTab(TabDirection.Up, this);
        }
    }
    protected void HandleInputDeviceNavigation(IInputDevice inputDevice)
    {
        var wasUpPressed = inputDevice.DefaultUpPressable.WasJustPressedOrRepeated ||
            (IsUsingLeftAndRightGamepadDirectionsForNavigation && inputDevice.DefaultLeftPressable.WasJustPressedOrRepeated);

        var wasDownPressed = inputDevice.DefaultDownPressable.WasJustPressedOrRepeated ||
            (IsUsingLeftAndRightGamepadDirectionsForNavigation && inputDevice.DefaultRightPressable.WasJustPressedOrRepeated);

        if (wasDownPressed)
        {
            this.HandleTab(TabDirection.Down, this);
        }
        else if (wasUpPressed)
        {
            this.HandleTab(TabDirection.Up, this);
        }
    }
#endif

    public virtual bool IsTabNavigationEnabled => true;

#if !FRB && (MONOGAME || KNI || FNA)

    /// <summary>
    /// List of key combinations that will trigger shifting focus
    /// to the next control (typically called "tabbing").
    /// </summary>
    public static List<KeyCombo> TabKeyCombos { get; set; } = new ()
    {
        new KeyCombo { PushedKey = Keys.Tab, IsTriggeredOnRepeat = true }
    };

    /// <summary>
    /// List of key combinations that will trigger shifting focus
    /// to the previous control.
    /// </summary>
    public static List<KeyCombo> TabReverseKeyCombos { get; set; } = new ()
    {
        new KeyCombo { HeldKey = Keys.LeftShift, PushedKey = Keys.Tab, IsTriggeredOnRepeat = true },
        new KeyCombo { HeldKey = Keys.RightShift, PushedKey = Keys.Tab, IsTriggeredOnRepeat = true },
    };

    /// <summary>
    /// List of key combinations that will trigger a click action
    /// on controls which can be clicked such as Button, ComboBox, and CheckBox
    /// </summary>
    public static List<KeyCombo> ClickCombos { get; set; } = new ()
    {
        new KeyCombo { PushedKey = Keys.Enter },
        new KeyCombo { PushedKey = Keys.Space },
    };

    protected void HandleKeyboardFocusUpdate()
    {
        foreach (var keyboard in KeyboardsForUiControl)
        {
            foreach(var tabKeyCombo in TabKeyCombos)
            {
                if(tabKeyCombo.IsComboPushed())
                {
                    // This allows TextBoes to set AcceptsTab to true
                    if(IsTabNavigationEnabled == true || tabKeyCombo.PushedKey != Keys.Tab)
                    {
                        this.HandleTab(TabDirection.Down, this, loop: true);
                        break; // one tab per frame
                    }
                }
            }

            foreach(var tabReverseKeyCombo in TabReverseKeyCombos)
            {
                if(tabReverseKeyCombo.IsComboPushed())
                {
                    // This allows TextBoes to set AcceptsTab to true
                    if (IsTabNavigationEnabled == true || tabReverseKeyCombo.PushedKey != Keys.Tab)
                    {
                        this.HandleTab(TabDirection.Up, this, loop: true);
                        break;
                    }
                }
            }
        }
    }
#endif

    /// <summary>
    /// Shifts focus to the next or previous element in the tab depending on the tabDirection argument.
    /// </summary>
    /// <param name="tabDirection">The direction to tab</param>
    /// <param name="requestingElement">The element which is requesting the tab. This can be a parent of the current element. If null is passed, then this element is 
    /// treated as the origin of the tab action.</param>
    /// <param name="loop">Whether to loop around to the beginning or end if at the last focusable item.</param>
    public void HandleTab(TabDirection tabDirection = TabDirection.Down, FrameworkElement requestingElement = null, bool loop = false)
    {
        requestingElement = requestingElement ?? this;

        ////////////////////Early Out/////////////////
        if (((IVisible)requestingElement.Visual).AbsoluteVisible == false)
        {
            return;
        }
        /////////////////End Early Out/////////////////

        var parentGue = requestingElement.Visual.Parent as InteractiveGue;
        var requestingElementVisual = requestingElement.Visual;

        HandleTab(tabDirection, requestingElementVisual, parentGue, shouldAskParent: true, loop:loop);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tabDirection"></param>
    /// <param name="requestingVisual">The current element that is either focused, or that has been tested for focus and failed. If this is null
    /// then the first or last (depending on direction) element is selected.</param>
    /// <param name="parentVisual"></param>
    /// <param name="shouldAskParent"></param>
    /// <param name="loop"></param>
    /// <returns></returns>
    // This should stay public so that it can be called with a null requestingVisual to select the first child.
    public static bool HandleTab(TabDirection tabDirection, InteractiveGue requestingVisual,
        InteractiveGue parentVisual, bool shouldAskParent, bool loop)
    {
        void UnFocusRequestingVisual()
        {
            if (requestingVisual?.FormsControlAsObject is FrameworkElement requestingFrameworkElement)
            {
                requestingFrameworkElement.IsFocused = false;
            }
        }

        IList<GraphicalUiElement> requestingVisualSiblings = parentVisual?.Children.Cast<GraphicalUiElement>().ToList();
        if (requestingVisualSiblings == null && requestingVisual != null)
        {
            requestingVisualSiblings = requestingVisual.ElementGueContainingThis?.ContainedElements.Where(item => item.Parent == null).ToList();
        }

        //// early out/////////////
        if (requestingVisualSiblings == null)
        {
            return false;
        }

        int newIndex;

        if (requestingVisual == null)
        {
            newIndex = tabDirection == TabDirection.Down ? 0 : requestingVisualSiblings.Count - 1;
        }
        else
        {
            int index = 0;

            if (tabDirection == TabDirection.Down)
            {
                index = 0;
            }
            else
            {
                index = requestingVisualSiblings.Count - 1;
            }

            for (int i = 0; i < requestingVisualSiblings.Count; i++)
            {
                if (requestingVisualSiblings[i] == requestingVisual)
                {
                    index = i;
                    break;
                }
            }



            if (tabDirection == TabDirection.Down)
            {
                newIndex = index + 1;
            }
            else
            {
                newIndex = index - 1;
            }
        }

        var didChildHandle = false;
        var didReachEndOfChildren = false;
        while (true)
        {
            if ((newIndex >= requestingVisualSiblings.Count && tabDirection == TabDirection.Down) ||
                (newIndex < 0 && tabDirection == TabDirection.Up))
            {
                didReachEndOfChildren = true;
                break;
            }
            else
            {
                var childAtI = requestingVisualSiblings[newIndex] as InteractiveGue;
                var elementAtI = childAtI?.FormsControlAsObject as FrameworkElement;

                if (CanElementBeFocused(elementAtI))
                {
                    elementAtI.IsFocused = true;

                    UnFocusRequestingVisual();

                    didChildHandle = true;
                    break;
                }
                else
                {
                    if (childAtI?.Visible == true && childAtI.IsEnabled && (elementAtI == null || elementAtI.IsEnabled))
                    {
                        // let this try to handle it:
                        didChildHandle = HandleTab(tabDirection, null, childAtI, shouldAskParent: false, loop:loop);

                        if (didChildHandle)
                        {
                            UnFocusRequestingVisual();
                        }
                    }

                    if (!didChildHandle)
                    {
                        if (tabDirection == TabDirection.Down)
                        {
                            newIndex++;
                        }
                        else
                        {
                            newIndex--;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        if (didChildHandle == false)
        {
            if (didReachEndOfChildren)
            {
                bool didFocusNewItem = false;
                if (shouldAskParent)
                {
#if FRB
                    // if this is a dominant window, don't allow tabbing out
                    var isDominant = GuiManager.DominantWindows.Contains(parentVisual);

                    if(!isDominant)
#endif
                    {
                        if (parentVisual?.Parent != null)
                        {
                            var grandparentVisual = parentVisual.Parent as InteractiveGue;
                            didFocusNewItem = HandleTab(tabDirection, parentVisual, grandparentVisual, shouldAskParent: true, loop:loop);
                        }
                        else
                        {
                            didFocusNewItem = HandleTab(tabDirection, parentVisual, null, shouldAskParent: true, loop: loop);

                            if(didFocusNewItem == false && didReachEndOfChildren && loop)
                            {
                                // If we asked the parent and it didn't focus a new item, and if the parent doesn't have its own parent, then we
                                // start back down the children of the parent:
                                InteractiveGue? firstChild = null;
                                if(parentVisual.Children != null)
                                {
                                    foreach(var child in parentVisual.Children)
                                    {
                                        if (child is InteractiveGue ig)
                                        {
                                            firstChild = ig;
                                            break;
                                        }
                                    }
                                }
                                firstChild = firstChild ?? requestingVisual;

                                didFocusNewItem = HandleTab(tabDirection, null, firstChild, shouldAskParent: true, loop: false);
                            }
                        }
                    }
                }
                if (didFocusNewItem)
                {
                    UnFocusRequestingVisual();
                }
                return didFocusNewItem;
            }
        }
        return didChildHandle;
    }

    static bool CanElementBeFocused(FrameworkElement element)
    {
        return element is IInputReceiver &&
                    element.IsVisible == true &&
                    element.IsEnabled &&
                    element.Visual.HasEvents &&
                    element.GamepadTabbingFocusBehavior == TabbingFocusBehavior.FocusableIfInputReceiver;
    }

    #endregion

    #region Updating State (visual appearance)

    /// <summary>
    /// Gets the state according to the element's current properties (such as whether it is enabled) and applies it
    /// to refresh the Visual's appearance.
    /// </summary>
    public virtual void UpdateState() { }


    [Obsolete("Use DisabledStateName")]
    public const string DisabledState = "Disabled";
    [Obsolete("Use DisabledFocusedStateName")]
    public const string DisabledFocusedState = "DisabledFocused";
    [Obsolete("Use EnabledStateName")]
    public const string EnabledState = "Enabled";
    [Obsolete("Use FocusedStateName")]
    public const string FocusedState = "Focused";
    [Obsolete("Use HighlightedStateName")]
    public const string HighlightedState = "Highlighted";
    [Obsolete("Use HighlightedFocusedStateName")]
    public const string HighlightedFocusedState = "HighlightedFocused";
    [Obsolete("Use PushedStateName")]
    public const string PushedState = "Pushed";

    public const string DisabledStateName = "Disabled";
    public const string DisabledFocusedStateName = "DisabledFocused";
    public const string EnabledStateName = "Enabled";
    public const string FocusedStateName = "Focused";
    public const string HighlightedStateName = "Highlighted";
    public const string HighlightedFocusedStateName = "HighlightedFocused";
    public const string PushedStateName = "Pushed";

    public const string SelectedStateName = "Selected";



    protected string GetDesiredState()
    {
        var cursor = MainCursor;

#if DEBUG
        if (cursor == null)
        {
            throw new InvalidOperationException("MainCursor must be assigned before performing any UI logic");
        }
#endif


        bool isPushInputHeldDown = GetIfPushInputIsHeld();
        
        var primaryDown = cursor.PrimaryDown;

        var isTouchScreen = cursor.LastInputDevice == InputDevice.TouchScreen;

        if (IsEnabled == false)
        {
            if (isFocused)
            {
                return DisabledFocusedStateName;
            }
            else
            {
                return DisabledStateName;
            }
        }
        else if (IsFocused)
        {
            if (cursor.WindowPushed == visual && primaryDown)
            {
                return PushedStateName;
            }
            else if (isPushInputHeldDown)
            {
                return PushedStateName;
            }
            // Even if the cursor is reported as being over the button, if the
            // cursor got its input from a touch screen then the cursor really isn't
            // over anything. Therefore, we only show the highlighted state if the cursor
            // is a physical on-screen cursor
            else if (GetIfIsOnThisOrChildVisual(cursor) &&
                !isTouchScreen)
            {
                return HighlightedFocusedStateName;
            }
            else
            {
                return FocusedStateName;
            }
        }
        else if (GetIfIsOnThisOrChildVisual(cursor))
        {
            if (cursor.WindowPushed == visual && primaryDown)
            {
                return PushedStateName;
            }
            // Even if the cursor is reported as being over the button, if the
            // cursor got its input from a touch screen then the cursor really isn't
            // over anything. Therefore, we only show the highlighted state if the cursor
            // is a physical on-screen cursor
            else if (!isTouchScreen && (cursor.WindowPushed == null || cursor.WindowPushed == visual))
            {
                return HighlightedStateName;
            }
            else
            {
                return EnabledStateName;
            }
        }
        else
        {
            return EnabledStateName;
        }
    }

    protected virtual bool GetIfPushInputIsHeld() =>
        GetIfGamepadOrKeyboardPrimaryPushInputIsHeld();

    protected bool GetIfGamepadOrKeyboardPrimaryPushInputIsHeld()
    {
        bool isPushInputHeldDown = false;

#if !RAYLIB
        for (int i = 0; i < GamePadsForUiControl.Count; i++)
        {
            isPushInputHeldDown = isPushInputHeldDown || (GamePadsForUiControl[i].ButtonDown(Buttons.A));
        }

#if (MONOGAME || KNI) && !FRB
        if (!isPushInputHeldDown)
        {
            for (int i = 0; i < KeyboardsForUiControl.Count; i++)
            {
                foreach (var combo in FrameworkElement.ClickCombos)
                {
                    if (combo.IsComboDown())
                    {
                        isPushInputHeldDown = true;
                    }
                }
            }
        }
#endif
#endif
        return isPushInputHeldDown;
    }

    protected string GetDesiredStateWithChecked(bool? isChecked)
    {
        var baseState = GetDesiredState();

        if (isChecked == true)
        {
            return baseState + "On";
        }
        else if (isChecked == false)
        {
            return baseState + "Off";
        }
        else
        {
            return baseState + "Indeterminate";
        }
    }


    private void HandleParentChanged(object? sender, GraphicalUiElement.ParentChangedEventArgs e)
    {
        var parentGue = Visual?.Parent as GraphicalUiElement;
        if (parentGue?.EffectiveManagers != null)
        {
            CallLoadedRecursively(Visual!);
        }
    }

    private void CallLoadedRecursively(GraphicalUiElement gue)
    {
        var frameworkElement = (gue as InteractiveGue)?.FormsControlAsObject as FrameworkElement;
        frameworkElement?.Loaded?.Invoke(this, EventArgs.Empty);

        foreach(var child in gue.Children)
        {
            if(child is GraphicalUiElement childGue)
            {
                CallLoadedRecursively(childGue);
            }
        }
    }

#if FRB
    void HandleEnabledChanged(IWindow window)
#else
    void HandleEnabledChanged(object sender, EventArgs args)
#endif
    {
        if (Visual != null)
        {
            this.IsEnabled = Visual.IsEnabled;
        }
    }

    #endregion

    public override string ToString()
    {
        return $"{this.Visual?.Name} ({this.GetType().Name})";
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public bool IsDataBound(string propertyName) => PropertyRegistry.GetBindingExpression(propertyName) != null;
}
