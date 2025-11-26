# Troubleshooting Events

## Introduction

Gum forms controls provide built-in responses to user actions such as clicks, typing, and dragging. These responses may be purely visual, such as a `Button` highlighting when the cursor moves over it, or they may raise events.

If a control is not responding to input, its events may be disabled for a variety of reasons.

## GetEventFailureReason

Controls may fail to raise events for many reasons. To help diagnose the problem, the `Cursor` class includes a diagnostic extension method `GetFailureReason` which can be called to identify why an object is not raising its events.

Since event failure may be caused by `Cursor` and control position, it's best to call this method in an Update call while attempting to interact with the element with the cursor.

The following example shows how to use `GetEventFailureReason`:

```csharp
using MonoGameGum.Input; // Adds GetEventFailureReason extension method

Button button;
protected override void Initialize()
{
    GumUI.Initialize(this, DefaultVisualsVersion.V2);

    button = new Button();
    button.AddToRoot();
    button.IsVisible = false;

    base.Initialize();
}

protected override void Update(GameTime gameTime)
{
    GumUI.Update(gameTime);

    var failureReason =
        GumUI.Cursor.GetEventFailureReason(button);
    System.Diagnostics.Debug.WriteLine(failureReason);

    base.Update(gameTime);
}
```

In this case, the button has its `IsVisible` property set to false. The output for this provides a hint as to how to solve the problem:

```
The argument ButtonVisual is invisible so it will not raise events
```

Improved diagnostics can be provided if names are given to controls. For example, changing the Button's name changes the output as shown in the following code block:

<pre class="language-csharp"><code class="lang-csharp">using MonoGameGum.Input; // Adds GetEventFailureReason extension method

Button button;
protected override void Initialize()
{
    GumUI.Initialize(this, DefaultVisualsVersion.V2);

    button = new Button();
    button.AddToRoot();
<strong>    button.Name = "TestButton";
</strong>    button.IsVisible = false;

    base.Initialize();
}

protected override void Update(GameTime gameTime)
{
    GumUI.Update(gameTime);

    var failureReason =
        GumUI.Cursor.GetEventFailureReason(button);
    System.Diagnostics.Debug.WriteLine(failureReason);

    base.Update(gameTime);
}
</code></pre>

```
The argument ButtonVisual named TestButton is invisible so it will not raise events
```

## Detecting Which Control is Under the Cursor

You can check the Cursor.WindowOver property to see what the Cursor believes it is over. This can tell you if the Cursor is over the object that you expect it to be over. The following code can be used to output the CursorOver to Visual Studio's output window:

```csharp
string windowOver = "<null>";
var cursor = GumUI.Cursor;
if(cursor.WindowOver != null)
{
    windowOver = 
        $"{cursor.WindowOver.GetType().Name} with name {cursor.WindowOver.Name}";
}
System.Diagnostics.Debug.WriteLine($"Window over: {windowOver}");
```
