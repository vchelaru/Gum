# For WPF Users

## Introduction

Gum's Forms system is intentionally modeled after WPF. If you know WPF, you already know the design principles behind Gum -- the same separation of concerns, the same control hierarchy, and the same data binding patterns all apply. This document maps WPF concepts to their Gum equivalents so you can apply the skills you already have rather than learning a new framework from scratch.

Gum can be used entirely in code (no tool required), and when doing so, your C# looks remarkably similar to how you would build WPF UI in C# without XAML. The mental model is the same: create controls, build a visual tree, set layout properties, and bind to view models.

## Control Naming

Gum controls share names with WPF. The following lists the main controls in Gum:

* Button
* CheckBox
* ComboBox
* Grid
* ItemsControl
* Label
* ListBox
* ListBoxItem
* Menu
* MenuItem
* Panel
* PasswordBox
* RadioButton
* ScrollBar
* ScrollViewer
* StackPanel
* Slider
* Splitter
* TextBox
* Window

{% hint style="info" %}
Unlike WPF, Gum's `Button` does not inherit from `ContentControl`. Buttons have a `Text` property for their label but do not support arbitrary content. If you need a clickable region with custom visuals, you can build a custom control using `InteractiveGue` as the visual base.
{% endhint %}

## FrameworkElement Base Class

In WPF, all controls inherit from `FrameworkElement`. Gum follows the same pattern -- every Forms control inherits from `FrameworkElement`, which serves the same role: the base class that provides layout, data binding, and the visual tree hookup.

```csharp
// WPF inheritance:
// UIElement -> FrameworkElement -> Control -> ContentControl -> ButtonBase -> Button

// Gum inheritance:
// FrameworkElement -> ButtonBase -> Button
```

Gum's hierarchy is flatter, but the same idea applies: `FrameworkElement` is the base, and controls specialize from there.

## Visual/Logical Tree Separation

WPF separates the logical tree (your controls) from the visual tree (the actual rendered elements). Gum does the same thing.

Every `FrameworkElement` has a `Visual` property that points to its visual representation. The visual is a `GraphicalUiElement` -- the base class for all visual objects in Gum, responsible for layout and rendering. `InteractiveGue` is a subclass of `GraphicalUiElement` that adds cursor interaction (hover, click, drag events). All Forms control visuals inherit from `InteractiveGue`.

This is conceptually identical to how a WPF `Button` has a visual tree of `Border`, `ContentPresenter`, etc. underneath it.

```csharp
// Initialize
// In WPF, you might dig into the visual tree:
var border = (Border)VisualTreeHelper.GetChild(myButton, 0);
border.Background = Brushes.Red;

// In Gum, you access the Visual property:
var buttonVisual = (ButtonVisual)myButton.Visual;
buttonVisual.BackgroundColor = Color.Red;
```

The key difference: WPF uses `ControlTemplate` in XAML to define the visual tree. Gum uses code-only visual classes (called DefaultVisuals) that build the visual tree programmatically. More on this in the Styling section below.

For more details, see the [GraphicalUiElement](../gum-code-reference/graphicaluielement/) and [InteractiveGue](../gum-code-reference/interactivegue/) reference pages.

## Getting Controls on Screen

In WPF, you set `Window.Content` or add elements to a panel that's already in the visual tree. Gum works similarly -- after initialization, a root container exists and you attach controls to it using `AddToRoot()`:

```csharp
// Initialize
// WPF:
// Controls added in XAML or via window.Content / grid.Children.Add(...)

// Gum:
var button = new Button();
button.AddToRoot(); // Adds to Gum's root container, making it visible on screen
```

`AddToRoot()` is the equivalent of adding a control to WPF's top-level container. Child controls added to a parent via `AddChild` do not need to call `AddToRoot` -- only top-level elements do.

Gum has three root containers, similar to how WPF has the main window plus popup layers:

* **Root** -- the main container (used by `AddToRoot()`)
* **PopupRoot** -- a layer above Root for elements like dropdown menus and tooltips
* **ModalRoot** -- the topmost layer for modal dialogs that block interaction with content below

To remove a control from the screen, call `RemoveFromRoot()` -- the inverse of `AddToRoot()`. For children, remove them from their parent with the parent's `RemoveChild` method.

For more information on initialization and the root container, see the [GumService](../gum-code-reference/gumservice-gumui/) page, the [ModalRoot and PopupRoot](../gum-code-reference/gumservice-gumui/modalroot-and-popuproot.md) page, and the [setup guides](../getting-started/setup/adding-initializing-gum/).

## Building UI in Code (No XAML)

Gum has no XAML equivalent. All UI is built in C#, similar to how you can construct WPF UI entirely in C# code-behind. The patterns are nearly identical.

{% hint style="info" %}
Before creating any controls, you must initialize Gum. This registers the default visuals for all control types -- similar to how WPF loads its default theme at startup. The initialization call varies by platform:

* **MonoGame/KNI/FNA:** `GumService.Default.Initialize(this)` in your Game's `Initialize` method
* **raylib:** `GumService.Default.Initialize()` after calling `Raylib.InitWindow`. See [raylib setup](../getting-started/setup/adding-initializing-gum/raylib-raylib-cs.md) for a full example.
{% endhint %}

```csharp
// Initialize
// WPF in C# (no XAML):
var stack = new StackPanel();
stack.Orientation = Orientation.Vertical;

var button = new Button();
button.Content = "Click Me";
button.Width = 120;
button.Height = 40;
button.Click += (_, _) => HandleClick();
stack.Children.Add(button);

var list = new ListBox();
list.ItemsSource = myItems;
stack.Children.Add(list);
```

```csharp
// Initialize
// Gum equivalent:
var stack = new StackPanel();
stack.AddToRoot();
stack.Orientation = Orientation.Vertical;
stack.Spacing = 4;

var button = new Button();
button.Text = "Click Me";
button.Width = 120;
button.Height = 40;
button.Click += (_, _) => HandleClick();
stack.AddChild(button);

var list = new ListBox();
list.Items = myItems;
stack.AddChild(list);
```

The structure is the same: create controls, set properties, wire events, add to parent. If you can build WPF UI in C#, you can build Gum UI.

{% hint style="info" %}
Unlike WPF, Gum has no `Dispatcher` or UI thread requirement. Gum runs inside a game loop where all code executes on the main thread. You do not need `Dispatcher.Invoke` or `Dispatcher.BeginInvoke` -- just set properties directly.

Async event handlers work naturally -- just like WPF, `await` resumes on the main thread thanks to an internal synchronization context. You can write `async void` event handlers (e.g., `button.Click += async (s, e) => { await LoadDataAsync(); myLabel.Text = "Done"; }`) and the UI update after the `await` runs on the game thread automatically.
{% endhint %}

### Key Syntax Differences

| WPF | Gum | Notes |
|---|---|---|
| `button.Content = "text"` | `button.Text = "text"` | Gum buttons have a `Text` property directly (no `ContentControl`) |
| `panel.Children.Add(child)` | `panel.AddChild(child)` | Method instead of collection access |
| `list.ItemsSource = items` | `list.Items = items` | Same concept, different property name |
| _(add to visual tree via XAML)_ | `control.AddToRoot()` | Attaches top-level elements to the screen |

## Layout System

### Familiar Containers

Gum provides the same layout containers you know from WPF:

| WPF | Gum | Behavior |
|---|---|---|
| `StackPanel` | `StackPanel` | Stacks children vertically or horizontally |
| `Grid` | `Grid` | Row/column-based layout |
| `DockPanel` | `Dock()` extension method | Dock children to edges |
| `Canvas` | Default (absolute) positioning | Position children by X/Y coordinates |

### StackPanel

Works the same as WPF:

```csharp
// Initialize
var stack = new StackPanel();
stack.Orientation = Orientation.Vertical;
stack.Spacing = 8; // WPF uses Margin on children; Gum has Spacing on the panel
```

### Grid

Gum's Grid uses the same `RowDefinition`/`ColumnDefinition` model as WPF, including `GridLength` and `GridUnitType` (Auto, Star, Absolute):

```csharp
// Initialize
// WPF:
var grid = new Grid();
grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
Grid.SetRow(myButton, 0);
Grid.SetColumn(myButton, 0);
grid.Children.Add(myButton);
```

```csharp
// Initialize
// Gum:
var grid = new Grid();
grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(200)));
grid.AddChild(myButton, row: 0, column: 0);
```

The main difference: WPF uses attached properties (`Grid.SetRow`), while Gum passes row/column as parameters to `AddChild`.

{% hint style="info" %}
Grid is fully supported and can be used in production code. However, its API syntax is experimental and may have breaking changes in future releases.
{% endhint %}

### Dock

WPF has `DockPanel` as a dedicated container with `DockPanel.SetDock()` as an attached property. Gum takes a different approach -- `Dock()` is an extension method that you call directly on any element. There is no `DockPanel` container; docking works with any parent.

`Dock()` sets multiple layout properties at once (position, size units, and alignment) to achieve the docking behavior. It is a convenience method, not a container type.

```csharp
// Initialize
// WPF:
var panel = new DockPanel();
DockPanel.SetDock(myButton, Dock.Top);
panel.Children.Add(myButton);

// Gum (any parent works):
panel.AddChild(myButton);
myButton.Dock(Dock.Top);
myButton.Dock(Dock.Fill); // Sizes to 100% of parent (not "remaining space" like WPF's LastChildFill)
```

### Anchor

Gum also provides anchoring, which combines WPF's `HorizontalAlignment` and `VerticalAlignment` into a single call. Like `Dock`, `Anchor` is an extension method that works with any parent:

```csharp
// Initialize
// WPF:
myButton.HorizontalAlignment = HorizontalAlignment.Center;
myButton.VerticalAlignment = VerticalAlignment.Top;

// Gum:
myButton.Anchor(Anchor.Top); // Centered horizontally, pinned to top
```

For the full list of `Anchor` and `Dock` values and their behavior, see the [Anchor and Dock](../layout/anchor-and-dock.md) page.

### Width and Height

WPF uses `double` values for width and height. Gum uses `float` values paired with unit types, which gives you more flexibility:

```csharp
// Initialize
// WPF - fixed size:
myButton.Width = 100;

// Gum - fixed size (same):
myButton.Width = 100;

// Gum - percentage of parent (no direct WPF equivalent without a Grid Star column):
myButton.Visual.WidthUnits = DimensionUnitType.PercentageOfParent;
myButton.Visual.Width = 50; // 50% of parent
```

For more on Gum's unit system, see the [Layout Introduction](../layout/introduction-to-gum-layout.md) page.

### Margin and Padding

WPF has dedicated `Margin` and `Padding` properties. Gum does not have these as named properties. Instead, spacing is achieved through layout values:

**Spacing between siblings** -- use `StackPanel.Spacing` (similar to WPF's `ItemsPanel` with uniform margin):

```csharp
// Initialize
var stack = new StackPanel();
stack.Spacing = 8; // 8 pixels between each child
```

**Outer margin (space around an element)** -- the approach depends on the layout context:

In a `StackPanel`, children can adjust their `X` and `Y` positions to add one-off spacing. This shifts the child and all subsequent children in the stack:

```csharp
// Initialize
var stack = new StackPanel();
stack.Spacing = 8;

var button = new Button();
button.Visual.Y = 12; // Adds 12px of extra space before this button (and shifts all subsequent children)
stack.AddChild(button);
```

You can also use `Dock()` to have children size relative to their parent. With directional values like `Dock.Left` or `Dock.Top`, the child sizes to the parent in the non-dominant axis. With `Dock.Fill`, both axes become relative. In either case, negative width or height values shrink the element inward, creating a margin effect:

```csharp
// Initialize
myButton.Dock(Dock.Fill);
// Width/Height are now relative to parent. -8 means "8 pixels
// smaller than parent" -- effectively a 4px margin on each side:
myButton.Width = -4 * 2;
myButton.Height = -4 * 2;
```

**Inner padding** -- Gum uses an inner container pattern rather than a padding property. See the [Padding](../../gum-tool/tutorials-and-examples/examples/padding.md) page for details.

This is one of the bigger mental model shifts from WPF. The general rule: use `Spacing` for gaps between siblings, size offsets for margins on filled elements, and container nesting for padding.

## Controls vs Visuals: What Goes Where

This is an important concept that does not have a direct WPF parallel. In WPF, you can set `myButton.Background = Brushes.Red` directly on the control. Gum separates controls from their visuals more strictly.

For basic UI work -- creating controls, setting text, wiring events, binding data -- you never need to touch the visual layer. You only need to access `.Visual` when you want to change how a control *looks*: colors, fonts, textures, or state-specific appearance. Think of it as the difference between using a WPF control normally vs. editing its `ControlTemplate`.

**On the control (`FrameworkElement`):** Position, size, `Dock`, `Anchor`, `Text`, `IsEnabled`, events, binding.

**On the visual (`.Visual`):** Colors, fonts, textures, layout units, origins, visual state definitions -- anything related to appearance.

This separation exists because Gum control visuals can be made of anything -- a `NineSliceRuntime` (a texture that stretches without distorting its corners, like WPF's `Border` with a 9-patch image), a `SpriteRuntime` (a texture/image, like WPF's `Image`), a `TextRuntime` (rendered text, like WPF's `TextBlock`), or even custom renderables. Since the visual structure is not guaranteed, the control itself cannot expose properties like `Background`.

To style a control, cast its `Visual` to the appropriate type. Each control has a corresponding visual class (the control name plus `Visual`):

```csharp
// Initialize
// Change a button's background color:
var buttonVisual = (ButtonVisual)myButton.Visual;
buttonVisual.BackgroundColor = Color.Red;

// Change a text box's colors:
var textBoxVisual = (TextBoxVisual)myTextBox.Visual;
textBoxVisual.BackgroundColor = Color.DarkGray;
textBoxVisual.ForegroundColor = Color.White;
```

{% hint style="warning" %}
Use color properties like `BackgroundColor` rather than directly setting `Background.Color`. Direct property changes are temporary -- they get overwritten when the control changes state (e.g., on hover). The color properties persist across state changes.
{% endhint %}

For the full list of visual types and their styling properties, see the [Styling Individual Controls](../styling/code-only-styling/styling-individual-controls.md) page.

### Fonts

In WPF, you set `FontFamily`, `FontSize`, and `FontWeight` directly on controls. In Gum, font properties live on `TextRuntime` visuals -- you access them through the control's visual:

```csharp
// Initialize
// WPF:
myButton.FontSize = 24;
myButton.FontFamily = new FontFamily("Arial");

// Gum -- access the TextRuntime inside the control's visual:
var buttonVisual = (ButtonVisual)myButton.Visual;
buttonVisual.TextInstance.FontSize = 24;
buttonVisual.TextInstance.Font = "Arial";
```

Gum supports two font modes: a built-in system that renders fonts at runtime (where you set properties like `FontSize` and `Font` as shown above), and pre-built bitmap fonts (`.fnt` files) for crisp, consistent rendering at specific sizes. Game UIs typically use bitmap fonts. For the full details on font loading and configuration, see the [Fonts](../standard-visuals/textruntime/fonts.md) page.

## Binding

### BindingContext vs DataContext

Binding in Gum works similarly to WPF, but the syntax is slightly different. Gum uses `BindingContext` rather than `DataContext`, matching .NET MAUI's naming. Like WPF's `DataContext`, `BindingContext` is inherited by children automatically:

```csharp
// Initialize
// WPF:
MyPanel.DataContext = MyViewModel;

// Gum (same behavior):
MyPanel.BindingContext = MyViewModel;
// All children of MyPanel automatically inherit MyViewModel as their BindingContext
```

### SetBinding

Gum relies on binding to classes which implement `INotifyPropertyChanged` and `INotifyCollectionChanged`. If a class implements these interfaces, then binding to their properties automatically results in Gum controls updating whenever the bound property changes.

Binding is performed using the name of a property rather than a static `DependencyProperty`:

```csharp
// Initialize
// WPF:
MyButton.DataContext = MyViewModel;
MyButton.SetBinding(Button.TextProperty, nameof(MyViewModel.ButtonText));
```

```csharp
// Initialize
// Gum:
MyButton.BindingContext = MyViewModel;
MyButton.SetBinding(nameof(Button.Text), nameof(MyViewModel.ButtonText));
```

Gum also supports lambda-based binding for compile-time safety:

```csharp
// Initialize
MyButton.SetBinding<MyViewModel>(nameof(Button.Text), vm => vm.ButtonText);
```

### Binding Object

Gum provides a `Binding` class that mirrors WPF's, with the same concepts:

```csharp
// Initialize
// WPF:
MyTextBox.SetBinding(TextBox.TextProperty, new Binding(nameof(MyViewModel.Name))
{
    Mode = BindingMode.TwoWay,
    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
    Converter = myConverter,
    FallbackValue = "(none)"
});
```

```csharp
// Initialize
// Gum:
MyTextBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(MyViewModel.Name))
{
    Mode = BindingMode.TwoWay,
    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
    Converter = myConverter,
    FallbackValue = "(none)"
});
```

The `Binding` class supports the same modes you know from WPF:

| Property | WPF | Gum |
|---|---|---|
| `Mode` | `BindingMode.OneWay`, `TwoWay`, `OneWayToSource` | Same enums, same behavior |
| `UpdateSourceTrigger` | `PropertyChanged`, `LostFocus`, `Default` | Same enums, same behavior |
| `Converter` | `IValueConverter` | Same interface |
| `FallbackValue` | Fallback when binding fails | Same |
| `TargetNullValue` | Value when source is null | Same |

### Binding Errors

In WPF, binding errors appear in the Output window. Gum handles binding failures silently -- if a property name is mistyped in `SetBinding`, the binding simply has no effect. No exception is thrown and no diagnostic output is produced. Use `nameof()` expressions wherever possible to catch typos at compile time:

```csharp
// Initialize
// Prefer this (compile-time checked):
myButton.SetBinding(nameof(Button.Text), nameof(MyViewModel.ButtonText));

// Over this (silent failure if mistyped):
myButton.SetBinding("Txt", "ButtonText");
```

### No DependencyProperty Required

Since Gum does not use the `DependencyProperty` type (or `BindableProperty` if you are familiar with .NET MAUI), the creation of bindable properties requires much less code as shown in the following blocks:

```csharp
// WPF
public static readonly DependencyProperty TextProperty =
    DependencyProperty.Register(
        "Text",                           // Property name
        typeof(string),                   // Property type
        typeof(MyCustomControl),          // Owner type
        new PropertyMetadata(string.Empty)); // Default value and metadata

// CLR wrapper property
public string Text
{
    get { return (string)GetValue(TextProperty); }
    set { SetValue(TextProperty, value); }
}
```

```csharp
// Gum
string _text;
public string Text
{
    get => _text;
    set
    {
        _text = value;
        PushValueToViewModel();
    }
}
```

## Events

Gum events follow the same patterns as WPF:

```csharp
// Initialize
// WPF:
myButton.Click += (sender, e) => HandleClick();

// Gum (identical):
myButton.Click += (sender, e) => HandleClick();
```

Common events map directly:

| WPF | Gum |
|---|---|
| `Button.Click` | `Button.Click` |
| `CheckBox.Checked` / `Unchecked` | `CheckBox.Checked` / `Unchecked` |
| `TextBox.TextChanged` | `TextBox.TextChanged` |
| `ListBox.SelectionChanged` | `ListBox.SelectionChanged` |

{% hint style="info" %}
WPF uses routed events that bubble and tunnel through the visual tree. Gum events are direct -- they fire only on the control itself, not on ancestors. If you need to respond to interaction events not exposed by the control (such as dragging), you can subscribe to events on the control's `Visual`, which is an `InteractiveGue` and exposes lower-level events like `Dragging`, `RollOver`, and `RightClick`. See the [InteractiveGue](../gum-code-reference/interactivegue/) page for the full event list.
{% endhint %}

### Keyboard and Gamepad Input

WPF has built-in keyboard navigation with `KeyDown`, `KeyUp`, `Focus()`, and `TabIndex`. Gum provides equivalent support for both keyboard and gamepad -- important for game UI where controller navigation is common:

* **Keyboard** -- `TextBox` handles keyboard input automatically. For other controls, Gum provides keyboard-driven focus navigation. See the [Keyboard Support](../events-and-interactivity/keyboard-support.md) page.
* **Tab / focus order** -- Gum supports tabbing between controls, similar to WPF's tab navigation. See the [Tabbing / Moving Focus](../events-and-interactivity/tabbing-moving-focus.md) page.
* **Gamepad** -- Gum supports gamepad-driven UI navigation (D-pad to move focus, A to select), which has no WPF equivalent. See the [Gamepad Support](../events-and-interactivity/gamepad-support.md) page.

## ItemsControl and Templating

WPF's `ItemsControl` pattern -- bind a collection, define a template, get generated items -- exists in Gum with the same structure:

```csharp
// Initialize
// WPF (C#, no XAML):
var listBox = new ListBox();
listBox.ItemsSource = myObservableCollection;
listBox.ItemTemplate = myDataTemplate;

// Gum:
var listBox = new ListBox();
listBox.Items = myObservableCollection;
listBox.VisualTemplate = new VisualTemplate(() => new MyCustomItemVisual());
```

The visual created by the `VisualTemplate` factory must be a `GraphicalUiElement` (or subclass like `InteractiveGue`). Gum wraps each item in a `ListBoxItem` automatically -- your template defines the visual appearance inside each item. When a `BindingContext` is set on the parent, each generated item receives its corresponding collection element as its own `BindingContext`, so bindings inside the template work the same as they do in WPF's `DataTemplate`.

Both respond to `INotifyCollectionChanged` -- add/remove items from the collection and the UI updates automatically.

When no template is set, both WPF and Gum fall back to calling `ToString()` on each item and displaying it as a label.

## Styling and Theming

WPF uses `ControlTemplate` and `Style` resources (usually in XAML) to define how controls look. Gum achieves the same thing through code-only visual classes and a centralized styling system.

### DefaultVisuals (Gum's ControlTemplate)

Each Gum control type has a registered visual factory that creates its visual tree -- conceptually identical to a WPF `ControlTemplate`. When you call `GumService.Default.Initialize(...)`, default visuals are registered for all control types, similar to WPF loading its default theme at startup.

To override how a control type looks globally:

```csharp
// Initialize
// Register a custom visual for all future Button instances:
FrameworkElement.DefaultFormsTemplates[typeof(Button)] =
    new VisualTemplate(() => new MyCustomButtonVisual());
```

A DefaultVisual class builds its visual tree in the constructor. The visual tree is composed of standard visual types:

* **`NineSliceRuntime`** -- a texture that stretches without distorting its corners (like WPF's `Border` with an image brush using nine-slice/nine-patch scaling)
* **`TextRuntime`** -- rendered text (like WPF's `TextBlock`)
* **`SpriteRuntime`** -- a texture/image (like WPF's `Image`)
* **`ColoredRectangleRuntime`** -- a solid color fill (like a `Rectangle` with a `SolidColorBrush`)
* **`ContainerRuntime`** -- an invisible grouping element (like a `Panel` or `Grid` used purely for layout)

For more on the standard visual types, see the [Standard Elements](../standard-visuals/) page.

```csharp
// Initialize
// Gum DefaultVisual (conceptually similar to a WPF ControlTemplate):
public class ButtonVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }
    public TextRuntime TextInstance { get; private set; }

    public ButtonVisual() : base(new InvisibleRenderable())
    {
        Width = 128;
        HeightUnits = DimensionUnitType.RelativeToChildren;

        Background = new NineSliceRuntime();
        Background.Dock(Dock.Fill);
        this.AddChild(Background);

        TextInstance = new TextRuntime();
        TextInstance.Dock(Dock.Fill);
        this.AddChild(TextInstance);
    }
}
```

For a full tutorial on creating custom controls, see the [Creating New Controls](../getting-started/tutorials/code-only-gum-forms-tutorial/creating-new-controls.md) page.

### Visual States (Gum's VisualStateManager)

WPF uses `VisualStateManager` to switch between states like `Normal`, `MouseOver`, `Pressed`. Gum uses a similar system -- each visual class defines named states that are applied automatically in response to user interaction.

You access and customize states by casting the `Visual` to its typed class and modifying its `States` object:

```csharp
// Initialize
var button = new Button();
button.AddToRoot();
var buttonVisual = (ButtonVisual)button.Visual;

// Customize the Highlighted (hover) state:
var highlightedState = buttonVisual.States.Highlighted;
highlightedState.Clear();
highlightedState.Apply = () =>
{
    buttonVisual.BackgroundColor = Color.Yellow;
};

// Apply immediately to see the current state:
button.UpdateState();
```

States are applied automatically as the user interacts with the control -- you do not need to call `GoToState` manually as in WPF. You just define what each state looks like, and Gum handles the transitions.

For the full list of states per control type and more examples, see the [Styling Using States](../styling/code-only-styling/styling-using-states.md) page.

### Global Styling

Gum's `Styling.ActiveStyle` is similar to WPF's application-level resource dictionaries for theming. It provides named color roles (like `Primary`, `Accent`, `InputBackground`, `TextPrimary`) that are applied to all controls created after the style is set:

```csharp
// Initialize
// WPF: Application.Current.Resources.MergedDictionaries.Add(myTheme);

// Gum:
Styling.ActiveStyle.Colors.Primary = Color.DarkGreen;
Styling.ActiveStyle.Colors.InputBackground = Color.FromNonPremultiplied(30, 30, 30, 255);
Styling.ActiveStyle.Colors.TextPrimary = Color.White;
// All controls created after this point use these colors
```

{% hint style="info" %}
`ActiveStyle` affects controls created after the style is set. To restyle existing controls, modify their visuals individually or recreate them.
{% endhint %}

For the full list of color roles and what they affect, see the [Styling Using ActiveStyles](../styling/code-only-styling/styling-using-activestyles.md) page. For an overview of all styling approaches, see the [Code-Only Styling](../styling/code-only-styling/) page.

## Quick Reference: WPF to Gum

| WPF Concept | Gum Equivalent |
|---|---|
| `FrameworkElement` | `FrameworkElement` |
| Visual Tree | `GraphicalUiElement` / `InteractiveGue` hierarchy |
| Logical Tree | `FrameworkElement` hierarchy |
| `DataContext` | `BindingContext` |
| `DependencyProperty` | Standard C# properties + `PushValueToViewModel()` |
| `IValueConverter` | `IValueConverter` (same interface) |
| `ControlTemplate` | DefaultVisual classes |
| `VisualStateManager` / `GoToState` | `States` object on typed visuals (applied automatically) |
| `StackPanel` | `StackPanel` |
| `Grid` | `Grid` |
| `DockPanel` | `Dock()` extension method (works on any parent) |
| `Canvas` | Default absolute positioning |
| `ContentControl` | No equivalent (buttons use `Text`, not `Content`) |
| `ItemsSource` | `Items` |
| `DataTemplate` / `ItemTemplate` | `VisualTemplate` / `FrameworkElementTemplate` |
| `HorizontalAlignment` / `VerticalAlignment` | `Anchor()` extension method |
| `Margin` | Size offsets (e.g., `Width = -8` with `Dock.Fill`) or `StackPanel.Spacing` |
| `Padding` | Inner container pattern (no property) |
| Application-level `ResourceDictionary` | `Styling.ActiveStyle` |
| Routed events | Direct events only (no bubbling/tunneling) |
| `Border` with image brush | `NineSliceRuntime` (nine-patch stretching) |
| `Image` | `SpriteRuntime` |
| `TextBlock` | `TextRuntime` |
| `Rectangle` with `SolidColorBrush` | `ColoredRectangleRuntime` |
| `Button.IsDefault` / `IsCancel` | Not supported (handle Enter/Esc globally in your game loop) |

## What Gum Does Not Have

Some WPF concepts do not exist in Gum:

* **XAML** -- all UI is built in C# code or through the Gum tool
* **DependencyProperty / attached properties** -- standard C# properties are used instead
* **ContentControl** -- buttons and similar controls use `Text` rather than arbitrary `Content`
* **ICommand / commanding** -- use event handlers like `Click` instead
* **Routed events / tunneling** -- events are direct, not routed through the tree
* **Resource dictionaries / StaticResource / DynamicResource** -- no resource lookup system
* **Triggers / DataTriggers** -- use event handlers or manual state management
* **RelativeSource binding** -- bindings reference property names directly
* **Measure/Arrange layout passes** -- Gum uses its own layout engine with unit types
* **Margin / Padding properties** -- spacing is achieved through layout values, `Spacing`, and container nesting
* **Button.IsDefault / IsCancel** -- Game UIs typically handle default/cancel actions via gamepad or global input state machines rather than localized button properties.

## Summary

If you are comfortable with WPF, you should feel at home with Gum. The architecture is intentionally parallel: controls inherit from `FrameworkElement`, visuals are separate from logic, `BindingContext` inherits down the tree, `INotifyPropertyChanged` drives updates, and collections use `INotifyCollectionChanged`. The main differences are practical -- no XAML, no `DependencyProperty` boilerplate, and a layout system designed for game rendering rather than document flow.

The biggest shift from WPF is the strict control/visual separation. In WPF, you can set `myButton.Background` directly; in Gum, you cast the `Visual` to its typed class and set properties there. Once you internalize this pattern, everything else follows naturally.

One architectural difference worth noting: unlike WPF, which handles rendering automatically via the dispatcher, Gum runs inside a game loop. Your game's `Update` and `Draw` calls drive Gum's layout and rendering each frame. The getting started guides below walk through this setup.

Apply the same design principles you would in WPF: separate your view models, bind your data, use the control hierarchy, and build your visual tree. The patterns transfer directly.

## Ready to Get Started?

This page covers the conceptual mapping from WPF to Gum. To start building, follow the setup guide for your platform:

* [MonoGame / KNI / FNA](../getting-started/setup/adding-initializing-gum/monogame-kni-fna/)
* [raylib](../getting-started/setup/adding-initializing-gum/raylib-raylib-cs.md)

These guides walk through NuGet installation, initialization, and the game loop integration so you can get your first Gum controls on screen.
