# Troubleshooting Events

## Introduction

Gum Forms controls provide built-in responses to user actions such as clicks, typing, and dragging. These responses may be purely visual — such as a `Button` highlighting when the cursor moves over it — or they may raise events.

If a control is not responding to input, its events may be suppressed for a number of reasons: the control (or one of its parents) could be invisible, disabled, outside the cursor's position, covered by a sibling, blocked by a modal, or have a parent that is not exposing its children's events.

Rather than walking the visual tree by hand, Gum exposes a diagnostic extension method on `Cursor` called `GetEventFailureReason` that returns a human-readable string describing why events are not being raised — or `null` if events should be working.

## GetEventFailureReason

`GetEventFailureReason` lives on the `ICursor` interface (typically `GumService.Default.Cursor`) and comes in several overloads:

| Overload | Use when | Example |
| --- | --- | --- |
| `GetEventFailureReason<T>()` | You have exactly one control of a given type (e.g. a single `ComboBox`). No field reference required. | `cursor.GetEventFailureReason<ComboBox>()` |
| `GetEventFailureReason(string name)` | You named the control and want to look it up by name. | `cursor.GetEventFailureReason("ConfirmButton")` |
| `GetEventFailureReason<T>(string name)` | You have multiple controls of the same type and want to disambiguate by name. | `cursor.GetEventFailureReason<Button>("Save")` |
| `GetEventFailureReason(FrameworkElement)` | You already have a reference to the control. | `cursor.GetEventFailureReason(myButton)` |
| `GetEventFailureReason(InteractiveGue)` | You want to diagnose a raw visual rather than a Forms control. | `cursor.GetEventFailureReason(myVisual)` |

Because the cursor's position affects the result, call this method in your update loop while attempting to interact with the control.

```csharp
// Add to using directives
using MonoGameGum.Input; // Adds GetEventFailureReason extension methods
```

### Looking up a control by type

The type-based overload is the easiest starting point — it searches `Root`, `PopupRoot`, and `ModalRoot` recursively for a control of the requested type, so you do not have to promote the control to a field or navigate the visual tree yourself.

```csharp
// Initialize
GumUI.Initialize(this);

var button = new Button();
button.AddToRoot();
button.IsVisible = false;
```

```csharp
// Update
GumUI.Update(gameTime);

var failureReason = GumUI.Cursor.GetEventFailureReason<Button>();
System.Diagnostics.Debug.WriteLine(failureReason);
```

Because the button is invisible, the output identifies the problem:

```
The argument Button is invisible so it will not raise events
```

### Looking up a control by name

When you have more than one control of the same type, assign each one a `Name` and look it up by name:

```csharp
// Initialize
var confirm = new Button { Name = "ConfirmButton" };
confirm.AddToRoot();

var cancel = new Button { Name = "CancelButton" };
cancel.AddToRoot();
```

```csharp
// Update
var failureReason = GumUI.Cursor.GetEventFailureReason("ConfirmButton");
System.Diagnostics.Debug.WriteLine(failureReason);
```

Names also make the diagnostic output more readable, since the message uses the name when one is set:

```
The argument Button named ConfirmButton is invisible so it will not raise events
```

### Combining type and name

For the common case where multiple controls share a name across different types (for example, a `Button` and a `Label` both named `Save`), combine the type and name overloads:

```csharp
// Update
var failureReason = GumUI.Cursor.GetEventFailureReason<Button>("Save");
System.Diagnostics.Debug.WriteLine(failureReason);
```

### When multiple controls match

If the lookup matches more than one control, `GetEventFailureReason` runs the diagnostic on each match and returns a combined report. This lets you see at a glance which instance is actually failing, rather than having to rerun the call with a narrower filter.

```
Found 3 elements matching type Button:
  [1] Button named OKButton
      → The parent NineSliceRuntime does not raise its children's events, preventing Button named OKButton from raising events
  [2] Button named CancelButton
      → (null — events appear to be working correctly)
  [3] Button
      → The cursor is over ListBox instead of Button
```

If no controls match, the return value explains that too:

```
No FrameworkElement matching type Button was found under Root, PopupRoot, or ModalRoot.
```

### Passing a control reference directly

When you already have a reference to the control (for example, from a field or a generated partial class), pass it directly. This is the original form of the API and is still the most direct when a reference is convenient.

```csharp
// Class scope
Button button;
```

```csharp
// Update
var failureReason = GumUI.Cursor.GetEventFailureReason(button);
System.Diagnostics.Debug.WriteLine(failureReason);
```

## Reading the Diagnostic Output

For failures that depend on the control's position in the visual tree, `GetEventFailureReason` appends an ancestor tree so you can see the hierarchy and which element is responsible. The responsible element is marked with `<---- THIS`:

```
The parent NineSliceRuntime does not raise its children's events, preventing Button named ConfirmButton from raising events
└-ContainerRuntime named Main Root
  └-NineSliceRuntime <---- THIS
    └-Panel
      └-Button named ConfirmButton
```

In the example above, the `NineSliceRuntime` has its `ExposeChildrenEvents` property set to `false`, so its descendants (including the button) cannot receive events. The tree makes it clear which ancestor to fix, even when the control is nested several levels deep.

{% hint style="info" %}
Controls used as Forms visuals often want `ExposeChildrenEvents = false` so the control absorbs clicks from its own children (e.g. a `Button` treats clicks on its inner text as clicks on the button). The diagnostic only flags ancestors, so these intentional cases do not produce false positives.
{% endhint %}

## Detecting Which Control is Under the Cursor

You can inspect the cursor's `VisualOver` property to see which control the cursor believes it is over. This is useful when the diagnostic suggests the wrong control is intercepting input. The following snippet writes the current visual under the cursor to the output window each frame:

```csharp
// Update
var cursor = GumUI.Cursor;
string visualOver = "<null>";
if (cursor.VisualOver != null)
{
    visualOver = $"{cursor.VisualOver.GetType().Name} with name {cursor.VisualOver.Name}";
}
System.Diagnostics.Debug.WriteLine($"Visual over: {visualOver}");
```

If you want the matching Forms control rather than the raw visual, use `FrameworkElementOver`:

```csharp
// Update
var frameworkElementOver = GumUI.Cursor.FrameworkElementOver;
System.Diagnostics.Debug.WriteLine($"Forms control over: {frameworkElementOver?.GetType().Name ?? "<null>"}");
```
