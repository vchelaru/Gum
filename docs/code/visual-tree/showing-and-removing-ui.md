# Showing and Removing UI

## Introduction

Most games show different UI over time: a title screen, then an options screen, then a HUD during gameplay. This page covers how to add UI so it appears, how to remove it, how to switch between screens, and what happens to UI after you remove it.

The same methods work whether you build UI entirely in code or load screens from a Gum project with generated code. The sections below cover each case.

## Showing and Removing UI

`AddToRoot` adds a control or visual to `GumUI.Root`. Anything under `Root` is drawn, receives input, and updates its layout. Children added to an element on the root appear with it.

Gum gives you two ways to remove UI:

* `RemoveFromRoot()` removes one element and everything inside it. Other elements on the root stay.
* `GumUI.Root.Children.Clear()` removes everything on the root.

Use `RemoveFromRoot` when other top-level UI should stay, such as a HUD that is visible across screens. Use `GumUI.Root.Children.Clear()` when you want a clean slate.

Neither one affects `PopupRoot` or `ModalRoot`, so popups and modal dialogs stay until you remove them separately. See [ModalRoot and PopupRoot](../gum-code-reference/gumservice-gumui/modalroot-and-popuproot.md) for more information.

If the focused control (for example a `TextBox` the user is typing into) is inside the removed UI, Gum clears the focus so keyboard input does not keep going to a control that is no longer visible.

## Code-Only UI

When you build UI in code, give each screen its own class. A screen class inherits from `Panel`, fills the whole canvas, and holds the screen's controls. This matches how screens from the Gum tool work, and filling the canvas lets you anchor controls to the center or to an edge of the screen.

Each screen raises an event when the user asks to leave it, rather than creating the next screen itself. This keeps screens independent of each other:

```csharp
public class MainMenuScreen : Panel
{
    public event System.EventHandler? OptionsRequested;

    public MainMenuScreen()
    {
        Dock(Gum.Wireframe.Dock.Fill);

        StackPanel menu = new();
        menu.Anchor(Gum.Wireframe.Anchor.Center);
        AddChild(menu);

        Label title = new();
        title.Text = "Main Menu";
        menu.AddChild(title);

        Button optionsButton = new();
        optionsButton.Text = "Options";
        optionsButton.Click += (_, _) => OptionsRequested?.Invoke(this, System.EventArgs.Empty);
        menu.AddChild(optionsButton);
    }
}

public class OptionsScreen : Panel
{
    public event System.EventHandler? BackRequested;

    public OptionsScreen()
    {
        Dock(Gum.Wireframe.Dock.Fill);

        StackPanel menu = new();
        menu.Anchor(Gum.Wireframe.Anchor.Center);
        AddChild(menu);

        Label title = new();
        title.Text = "Options";
        menu.AddChild(title);

        CheckBox musicCheckBox = new();
        musicCheckBox.Text = "Music";
        menu.AddChild(musicCheckBox);

        Button backButton = new();
        backButton.Text = "Back";
        backButton.Click += (_, _) => BackRequested?.Invoke(this, System.EventArgs.Empty);
        menu.AddChild(backButton);
    }
}
```

Your game creates the screens and switches between them when they raise their events:

```csharp
// Initialize
MainMenuScreen mainMenuScreen = new();
OptionsScreen optionsScreen = new();

mainMenuScreen.OptionsRequested += (_, _) =>
{
    mainMenuScreen.RemoveFromRoot();
    optionsScreen.AddToRoot();
};
optionsScreen.BackRequested += (_, _) =>
{
    optionsScreen.RemoveFromRoot();
    mainMenuScreen.AddToRoot();
};

mainMenuScreen.AddToRoot();
```
[Try on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAACt1VTY_aMBC951dYnIK6slT11hVd8bFQpKJWsEt7W5lkAAvHprYDu13x39dOQhLnY0HtrbkknnmeeeN5nsSK8g2axNGtF58_8VjISNUMeCi4loI1eEawJjHTS6piwhRefjpDZjSQQom1xr84wWNJIjgKubvgxhNJ9lsamEzePl4xGqCAEaXQjFA-Ax4vAgnA0Wf0g3Bg3quHzJMh4QBco8WL0hDhe7v4SnjIQN6h73tNBVdz-B2DcYcmfGmjG9zvJr40tH1GItj5tuSfVMLaUsXWhMeUsW4WyT4LTYJdwgtFJhrqIQ5H3yDOAGvFfR5shazES414aDiDLO3oh-FwS1no263lXN_IyqTRVDOo50nM-AGetfF1bHXIltepMjkHT_Dl6INYa8GRSE8tW9XSOO48XXbUnTbc0Jz4Dn3oIf_pBj11Ue9LrTt3eMoPYge-3lJ143S0LzcK30d7_dJtq8ZJl6FO3qkiqCznX-ppYDrdIiYn8P-mpXpzLyhpuIVgNxDPKDL3PshX9XLK7kK61tqazNnTIN-VOcI27Ra-PJntaacR0SBZRwD_ptciUatYJ6avH41I7TvT6HlQjuBAA5gRTjYg0SYfnwkmjhYgrd9-Pk4t8cJ2nt2ufJNUfhe95rHSw2tOmFTcvTWU0xhSaAjMiSBxAClpCOggaIimnGpKGP0DtQuREMMlQHqG7n9lCVIZ3ZnfS7nNlX9C5C5rLXdvfDYlquCiR040XB1RjhryTUVZDSHmEJlTGUsRzYXQZWb2cfhYcTyIKuxUG6kZ2hHjNczc7e8Tq1TRxsy7Bl-6XArKTc-V_66MHvch0eBbiT7QCNAm-2jWVIbOQeWrbbI3uy8wGElyvCK_c1XM-AAi_aFgdioLyddMHEEOWFzmlHJO4leZJsYaz5P3BpfLbijCCQAA)

This example creates each screen once and reuses it, so the options screen keeps its state (such as the **Music** check box) when the user comes back to it. To start fresh each time instead, create a new screen in the event handler.

## Screens From a Gum Project

When your project uses code generation, each screen in the Gum project has a generated class with the same name, plus a custom code file where you add your own code. As with code-only screens, each screen raises an event when the user asks to leave it. Declare the event in the custom code file and raise it from a click handler:

```csharp
partial class TitleScreen
{
    public event System.EventHandler? OptionsRequested;

    partial void CustomInitialize()
    {
        OptionsButton.Click += (_, _) => OptionsRequested?.Invoke(this, System.EventArgs.Empty);
    }
}
```

`OptionsScreen` declares a `BackRequested` event the same way. Your game creates the screens and switches between them:

```csharp
// Class scope
void ShowTitleScreen()
{
    GumUI.Root.Children.Clear();
    TitleScreen titleScreen = new();
    titleScreen.OptionsRequested += (_, _) => ShowOptionsScreen();
    titleScreen.AddToRoot();
}

void ShowOptionsScreen()
{
    GumUI.Root.Children.Clear();
    OptionsScreen optionsScreen = new();
    optionsScreen.BackRequested += (_, _) => ShowTitleScreen();
    optionsScreen.AddToRoot();
}

protected override void Initialize()
{
    // GumUI.Initialize goes here
    ShowTitleScreen();
}
```

This example creates a new screen on each switch, so every screen starts with the values set in the Gum tool. To keep a screen's state between visits, create it once and reuse it as shown in the code-only example above.

For a full walkthrough, see the Multiple Screens tutorial for [Forms projects](../getting-started/tutorials/gum-project-forms-tutorial/multiple-screens.md) or [.gumx projects](../getting-started/tutorials/gum-project-.gumx-tutorial/multiple-screens.md).

## What Happens to Removed UI

Removing UI does not destroy it. A removed element is an ordinary C# object with no parent:

* Gum controls, visuals, and screens do not implement `IDisposable`, so there is nothing to dispose.
* The element keeps its properties, its children, and any event handlers you attached, so you can call `AddToRoot` again to show it as it was.
* Once your code holds no references to it, the .NET garbage collector frees it like any other object.

The last point means removed UI stays in memory as long as something references it. Fields that hold a screen keep it alive, which is what you want when reusing it. A less obvious reference is an event subscription on a longer-lived object. If a screen subscribes to an event on a static class or a game-wide service, that object holds a reference to the screen, so unsubscribe when you are done with the screen.

## Hiding Instead of Removing

To hide UI briefly, such as a pause menu that toggles on and off, set `IsVisible` (Forms controls) or `Visible` (visuals) to `false` instead of removing it. Hidden elements stay in the visual tree, do not draw, and do not receive input.

```csharp
// Initialize
pauseMenu.IsVisible = false;
```

Removing is a better fit when the user is leaving that UI, such as switching screens.
