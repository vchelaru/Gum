# Launching Gum with a Project

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

Pass a project file to the Gum executable (`Gum.exe` on Windows, `Gum` on macOS and Linux) to open that project on startup:

```sh
Gum MyProject/MyProject.gumx
```

Add a screen or component file to also select it once the project loads. The project file is optional when the element file sits inside a project's folder:

```sh
Gum MyProject/MyProject.gumx MyProject/Screens/MainMenu.gusx
Gum MyProject/Screens/MainMenu.gusx
```

## When Gum Is Already Running

If another Gum already has the same project open, that Gum comes to the front and selects the screen, and the new launch exits. If another Gum has a different project open, Gum starts a second window. Nothing is closed, replaced, or asked about.

This lets a script or shortcut send you to a screen in Gum without piling up windows.

## Starting a New Gum Anyway

`--no-handoff` always starts a new Gum, even when another Gum has the project open. Use it in scripts and automated runs where several launches open the same project and none should reuse another's window:

```sh
Gum MyProject/MyProject.gumx --no-handoff
```
