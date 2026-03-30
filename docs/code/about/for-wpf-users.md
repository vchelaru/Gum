# For WPF Users

## Introduction

Gum's Forms system is intentionally modeled after WPF. If you know WPF, you already know the design principles behind Gum -- the same separation of concerns, the same control hierarchy, and the same data binding patterns all apply. This document maps WPF concepts to their Gum equivalents so you can apply the skills you already have rather than learning a new framework from scratch.

Gum can be used entirely in code (no tool required), and when doing so, your C# looks remarkably similar to how you would build WPF UI in C# without XAML. The mental model is the same: create controls, build a visual tree, set layout properties, and bind to view models.

## Control Naming

Gum controls share names with WPF. The following lists the main controls in Gum:

* Button
* CheckBox
* ComboBox
* Grid (experimental)
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

## FrameworkElement Base Class

In WPF, all controls inherit from `FrameworkElement`. Gum follows the same pattern -- every Forms control inherits from `FrameworkElement`, which serves the same role: the base class that provides layout, data binding, and the visual tree hookup.

```csharp
// WPF inheritance:
// UIElement → FrameworkElement → Control → ContentControl → ButtonBase → Button

// Gum inheritance:
// FrameworkElement → ButtonBase → Button
```

Gum's hierarchy is flatter, but the same idea applies: `FrameworkElement` is the base, and controls specialize from there.

## Visual/Logical Tree Separation

WPF separates the logical tree (your controls) from the visual tree (the actual rendered elements). Gum does the same thing.

Every `FrameworkElement` has a `Visual` property that points to its visual representation (a `GraphicalUiElement`, also known as `InteractiveGue`). This is conceptually identical to how a WPF `Button` has a visual tree of `Border`, `ContentPresenter`, etc. underneath it.

```csharp
// Initialize
// In WPF, you might dig into the visual tree:
var border = (Border)VisualTreeHelper.GetChild(myButton, 0);
border.Background = Brushes.Red;

// In Gum, you access the Visual property:
var buttonVisual = (ButtonVisual)myButton.Visual;
buttonVisual.Background.Color = Color.Red;
```

The key difference: WPF uses `ControlTemplate` in XAML to define the visual tree. Gum uses code-only visual classes (called DefaultVisuals) that build the visual tree programmatically. More on this in the Styling section below.

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

### Key Syntax Differences

| WPF | Gum | Notes |
|---|---|---|
| `button.Content = "text"` | `button.Text = "text"` | Gum buttons have a `Text` property directly |
| `panel.Children.Add(child)` | `panel.AddChild(child)` | Method instead of collection access |
| `list.ItemsSource = items` | `list.Items = items` | Same concept, different property name |

## Layout System

### Familiar Containers

Gum provides the same layout containers you know from WPF:

| WPF | Gum | Behavior |
|---|---|---|
| `StackPanel` | `StackPanel` | Stacks children vertically or horizontally |
| `Grid` | `Grid` (experimental) | Row/column-based layout |
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
Grid is currently experimental and may have breaking changes in future releases.
{% endhint %}

### Dock

WPF has `DockPanel` with `DockPanel.SetDock()`. Gum uses a `Dock()` extension method directly on elements:

```csharp
// Initialize
// WPF:
var panel = new DockPanel();
DockPanel.SetDock(myButton, Dock.Top);

// Gum:
myButton.Dock(Dock.Top);
myButton.Dock(Dock.Fill); // Fills remaining space
```

### Anchor

Gum also provides anchoring, which combines WPF's `HorizontalAlignment` and `VerticalAlignment` into a single call:

```csharp
// Initialize
// WPF:
myButton.HorizontalAlignment = HorizontalAlignment.Center;
myButton.VerticalAlignment = VerticalAlignment.Top;

// Gum:
myButton.Anchor(Anchor.Top); // Centered horizontally, pinned to top
```

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

## No Direct Visual Properties

Gum separates its visuals from the main controls, so many of the visual properties that exist on WPF are not present in Gum. This separation exists for the practical reason that Gum control visuals can be made of quite literally anything, so Gum cannot make assumptions through these properties.

For example, the following code is valid in WPF:

```csharp
// Initialize
// Assuming MyButton is a valid button:
MyButton.Background = Brushes.LightBlue;
```

This code is not valid in Gum because there is no guarantee that the button has a background, or if it does there is no guarantee on the background's type. It could be a NineSliceRuntime, SpriteRuntime, or even a 3D model or Tiled map.

To style a button, your code must access the visual object, which requires making some assumptions about how the Button is built.

If you are using a code-only setup, then you can cast the Visual property to the appropriate type to access its values. For example, the following code can be used to access and modify a Button's background:

```csharp
// Initialize
var buttonVisual = (ButtonVisual)MyButton.Visual;
buttonVisual.Background.Color = Color.Red;
```

This is similar to how in WPF you sometimes need to use `VisualTreeHelper` or `Template.FindName()` to access specific parts of a control's visual tree -- except in Gum the visual classes expose typed properties directly.

## Limited Layout Properties

Gum provides limited properties for its controls. Gum code allows changing position, size, dock, and anchor values. For example, the following would result in a button filling its parent and setting a margin of 4 pixels:

```csharp
// Initialize
MyButton.Dock(Dock.Fill);
// When docked to Fill, Width and Height are relative to the parent.
// A value of 0 means "same size as parent", so -8 means
// "8 pixels less than parent" (4 pixel margin on each side):
MyButton.Width = -4 * 2;
MyButton.Height = -4 * 2;
```

More advanced layout control must be performed through the `Visual` property:

```csharp
// Initialize
MyButton.Visual.XOrigin = HorizontalAlignment.Center;
```

This is conceptually similar to WPF's separation between high-level properties (like `Margin` and `HorizontalAlignment`) and low-level render properties accessed through `RenderTransform` or `VisualTreeHelper`.

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

Both respond to `INotifyCollectionChanged` -- add/remove items from the collection and the UI updates automatically.

When no template is set, both WPF and Gum fall back to calling `ToString()` on each item and displaying it as a label.

## Styling and Theming

WPF uses `ControlTemplate` and `Style` resources (usually in XAML) to define how controls look. Gum achieves the same thing through code-only visual classes and a centralized `Styling` object.

### DefaultVisuals (Gum's ControlTemplate)

Each Gum control type has a registered visual factory that creates its visual tree -- conceptually identical to a WPF `ControlTemplate`:

```csharp
// Initialize
// GumService initialization registers default visuals for all control types,
// similar to WPF loading its default theme. To override a specific control's visual:
FrameworkElement.DefaultFormsTemplates[typeof(Button)] =
    new VisualTemplate(() => new MyCustomButtonVisual());
```

A DefaultVisual class builds its visual tree in the constructor, the same way you would build a WPF control template in C#:

```csharp
// Initialize
// Conceptually similar to defining a ControlTemplate in C#:
// var template = new ControlTemplate(typeof(Button));
// var border = new FrameworkElementFactory(typeof(Border));
// border.SetValue(Border.BackgroundProperty, Brushes.Gray);
// var text = new FrameworkElementFactory(typeof(TextBlock));
// border.AppendChild(text);

// Gum DefaultVisual equivalent:
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

### Visual States (Gum's VisualStateManager)

WPF uses `VisualStateManager` to switch between states like `Normal`, `MouseOver`, `Pressed`. Gum uses `StateSaveCategory` with named states -- same concept, different API:

```csharp
// Initialize
// WPF VisualStateManager switches between states like:
// VisualStateManager.GoToState(myButton, "MouseOver", true);

// Gum controls define state categories and apply them automatically
// based on IsEnabled, mouse hover, focus, etc.
// The states are defined in the DefaultVisual constructor.
```

### Global Styling

Gum's `Styling.ActiveStyle` is similar to WPF's application-level resource dictionaries for theming:

```csharp
// Initialize
// WPF: Application.Current.Resources.MergedDictionaries.Add(myTheme);

// Gum:
Styling.ActiveStyle = new Styling(myCustomSpriteSheet);
// All controls created after this use the new style
```

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
| `VisualStateManager` | `StateSaveCategory` |
| `StackPanel` | `StackPanel` |
| `Grid` | `Grid` (experimental) |
| `DockPanel` | `Dock()` extension method |
| `Canvas` | Default absolute positioning |
| `ItemsSource` | `Items` |
| `DataTemplate` / `ItemTemplate` | `VisualTemplate` / `FrameworkElementTemplate` |
| `HorizontalAlignment` / `VerticalAlignment` | `Anchor()` extension method |
| `Margin` | Position offset values (e.g., `Width = -8` with `Dock.Fill`) |
| Application-level `ResourceDictionary` | `Styling.ActiveStyle` |

## What Gum Does Not Have

Some WPF concepts do not exist in Gum:

* **XAML** -- all UI is built in C# code or through the Gum tool
* **DependencyProperty / attached properties** -- standard C# properties are used instead
* **ICommand / commanding** -- use event handlers like `Click` instead
* **Routed events / tunneling** -- events are direct, not routed through the tree
* **Resource dictionaries / StaticResource / DynamicResource** -- no resource lookup system
* **Triggers / DataTriggers** -- use event handlers or manual state management
* **RelativeSource binding** -- bindings reference property names directly
* **Measure/Arrange layout passes** -- Gum uses its own layout engine with unit types

## Summary

If you are comfortable with WPF, you should feel at home with Gum. The architecture is intentionally parallel: controls inherit from `FrameworkElement`, visuals are separate from logic, `BindingContext` inherits down the tree, `INotifyPropertyChanged` drives updates, and collections use `INotifyCollectionChanged`. The main differences are practical -- no XAML, no `DependencyProperty` boilerplate, and a layout system designed for game rendering rather than document flow.

Apply the same design principles you would in WPF: separate your view models, bind your data, use the control hierarchy, and build your visual tree. The patterns transfer directly.
