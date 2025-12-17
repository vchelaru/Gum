# Mouse and Touch Screen (Cursor)

## Introduction

Gum supports reading from the mouse and touch screen for events. Both the mouse and touch screen report their actions through the `Cursor` class. Controls which respond to click events (such as `Button` and `CheckBox`) automatically read from the `Cursor`.

## Code Example: Accessing the Cursor

GumService contains an instance of the `Cursor` class. The following code shows how to access the Cursor class and create rectangles when the `Cursor` detects a click.

```csharp
protected override void Update(GameTime gameTime)
{
    GumUI.Update(gameTime);

    Cursor cursor = GumUI.Cursor;

    if(cursor.PrimaryClick)
    {
        ColoredRectangleRuntime rectangle = new ();
        rectangle.AddToRoot();
        rectangle.X = cursor.XRespectingGumZoomAndBounds();
        rectangle.Y = cursor.YRespectingGumZoomAndBounds();
        rectangle.Color = Color.Red;
    }

    base.Update(gameTime);
}
```

<figure><img src="../../.gitbook/assets/17_04 24 51.gif" alt=""><figcaption><p>Rectangles created in response to <code>Cursor</code> clicks</p></figcaption></figure>

## Disabling the Cursor Globally

The Cursor instance reported by GumService can be replaced with a custom implementation of the `ICursor` interface. A custom `ICursor` class can be created to modify its behavior. For example, the following implementation disables all behavior:

```csharp
public class DisabledCursor : ICursor
{
    public Cursors? CustomCursor { get; set; }
    public InputDevice LastInputDevice => InputDevice.Mouse;
    // Return negative values which will be outside the screen
    public int X => -1000;
    public int Y => -1000;

    public double LastPrimaryPushTime => -1000;
    public double LastPrimaryClickTime => -1000;

    public int XChange => 0;
    public int YChange => 0;

    public int ScrollWheelChange => 0;
    public float ZVelocity => 0;

    public bool PrimaryPush => false;
    public bool PrimaryDown => false;
    public bool PrimaryClick => false;
    public bool PrimaryClickNoSlide => false;
    public bool PrimaryDoubleClick => false;
    public bool PrimaryDoublePush => false;

    public bool SecondaryPush => false;
    public bool SecondaryDown => false;
    public bool SecondaryClick => false;
    public bool SecondaryDoubleClick => false;

    public bool MiddlePush => false;
    public bool MiddleDown => false;
    public bool MiddleClick => false;
    public bool MiddleDoubleClick => false;

    public InteractiveGue WindowPushed { get; set; }
    public InteractiveGue VisualRightPushed { get; set; }
    public InteractiveGue WindowOver { get; set; }

    public void Activity(double currentGameTimeTotalSeconds){}

    public float XRespectingGumZoomAndBounds() => -1000;

    public float YRespectingGumZoomAndBounds() => -1000;
}
```

{% hint style="warning" %}
Future versions of Gum are likely to change the `ICursor` class by adding or renaming properties. Be aware that any custom `ICursor` implementation may need to be adjusted in response to these changes.
{% endhint %}

This `DisabledCursor` class can be assigned to disable all Cursor actions as shown in the following code:

```csharp
protected override void Initialize()
{
    GumUI.Initialize(this, Gum.Forms.DefaultVisualsVersion.V3);

    Button button = new();
    button.AddToRoot();
    button.Anchor(Anchor.Center);

    FormsUtilities.SetCursor(new DisabledCursor());

    base.Initialize();
}
```

<figure><img src="../../.gitbook/assets/17_04 48 28.gif" alt=""><figcaption><p><code>DisabledCursor</code> used to prevent UI interaction</p></figcaption></figure>
