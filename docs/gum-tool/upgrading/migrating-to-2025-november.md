# Migrating to 2025 November

## V3 Visuals

{% hint style="warning" %}
November 2025 has not yet been released. This document currently covers the November version if you your project is linked to source.
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

Adjust the dimensions, positions of contorls if desired. Some of the controls have changed size to match the Gum UI tool more closely. Note that a few names have also been changed to match the naming of the visuals in the Gum tool.

The following changes have been made to V3:

* CheckBox Height 32 ⇒ 24
* ListBoxItem Height 0 ⇒ 6 (RelativeToChildren)
* ListBox Height 150 ⇒ 256
* ListBox Width 150 ⇒ 256
* ListBox FocusedIndicator Y -2 ⇒ 2 (fixed overlap bug)
* MenuItem Width 6 ⇒ 0 (still RelativeToChildren)
* MenuItem Height 6 ⇒ 0 (still RelativeToChildren)
* MenuItem TextInstance X 2 ⇒ 0&#x20;
* MenuItem TextInstance Height 2 ⇒ 0 (still RelativeToChildren)
* MenuItem SubmenuIndicatorInstance Width 2 ⇒ 0
* PasswordBox Width 100 ⇒ 256
* RadioButton Height 32 ⇒ 24
* RadioButton Background renamed to RadioBackground
* RadioButton InnerCheck Renamed to Radio
* TextBox Width 100 ⇒ 256
