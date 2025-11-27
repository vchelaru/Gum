# Migrating to 2025 November

## V3 Visuals

{% hint style="warning" %}
November 2025 has not yet been released. This document currently covers the November version if your project is linked to source.
{% endhint %}

The November 2025 release of Gum introduces an improvement to the default code-only controls. These new V3 visuals add the following improvements over V2:

* Simplified styling - new color properties can be assigned without needing to make changes to states.
* Better consistency with the Gum UI tool - code-only projects look even more similar to projects which use the Gum UI tool

### Upgrading to V3

To upgrade a project to V3, make the changes listed below.

First, find the initialize method and change it from V2 to V3:

```csharp
//GumUI.Initialize(this, DefaultVisualsVersion.V2);
GumUI.Initialize(this, DefaultVisualsVersion.V3);
```

Replace usage of the default Visuals namespace with V3:

```csharp
//using Gum.Forms.DefaultVisuals;
using Gum.Forms.DefaultVisuals.V3;
```

Replace any explicit qualification of Visuals. If you are relying on namespaces this is not necessary, but if you are using explicit visual types, you need to switch to the V3 version. For example, the following code shows how to change a ButtonVisual reference from V2 to V3:

```csharp
var button = new Button();
//var buttonVisual = (Gum.Forms.DefaultVisuals.ButtonVisual)button.Visual;
var buttonVisual = (Gum.Forms.DefaultVisuals.V3.ButtonVisual)button.Visual;
```

Optionally, your code can remove state assignments on visual colors since V3 now simplifies setting colors.

For example, the following code can be used to set a button's background:

```csharp
var button = new Button();
button.AddToRoot();
var buttonVisual = (ButtonVisual)button.Visual;
buttonVisual.BackgroundColor = Color.Red;
```

For more information on working with the new Visuals, see the [Code-Only  Styling](../../code/styling/code-only-styling/) section;

Adjust the dimensions, positions of controls if desired. Some of the controls have changed size to match the Gum UI tool more closely. Note that a few names have also been changed to match the naming of the visuals in the Gum tool.

**The following changes have been made to V3:**

<table><thead><tr><th width="256.7999267578125">Visual Element</th><th width="121.3997802734375">Variable</th><th width="115.400146484375">V2</th><th>V3</th></tr></thead><tbody><tr><td>CheckBox</td><td>Height</td><td>32</td><td>24</td></tr><tr><td>ListBoxItem</td><td>Height</td><td>0</td><td>6 (RelativeToChildren)</td></tr><tr><td>ListBox</td><td>Height</td><td>150</td><td>256</td></tr><tr><td>ListBox</td><td>Width</td><td>150</td><td>256</td></tr><tr><td>ListBox.FocusedIndicator</td><td>Y</td><td>-2</td><td>2 (fixed overlap bug)</td></tr><tr><td>MenuItem</td><td>Width</td><td>6</td><td>0 (still RelativeToChildren)</td></tr><tr><td>MenuItem</td><td>Height</td><td>6</td><td>0 (still RelativeToChildren)</td></tr><tr><td>MenuItem.TextInstance</td><td>X</td><td>2</td><td>0</td></tr><tr><td>MenuItem.TextInstance</td><td>Height</td><td>2</td><td>0 (still RelativeToChildren)</td></tr><tr><td>MenuItem<br>.SubmenuIndicatorInstance</td><td>Width</td><td>2</td><td>0</td></tr><tr><td>PasswordBox</td><td>Height</td><td>32</td><td>24</td></tr><tr><td>RadioButton</td><td>Height</td><td>32</td><td>24</td></tr><tr><td>RadioButton</td><td>Background</td><td>Background</td><td>Renamed to RadioBackground</td></tr><tr><td>RadioButton</td><td>InnerCheck</td><td>InnerCheck</td><td>Ranamed to Radio</td></tr><tr><td>TextBox</td><td>Width</td><td>100</td><td>256</td></tr></tbody></table>
