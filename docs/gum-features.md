# Gum Features

Gum is a UI toolkit for C# game developers. This page is a tour of what it can do, with each feature linking to its full documentation. To start using Gum, see the [Setup](gum-tool/setup/README.md) page.

### [Design Visually or Write Code](gum-tool/editor-tab.md)

Build your UI in the visual editor, in C# only, or a mix of both.

{% tabs %}
{% tab title="Editor" %}
<figure><img src=".gitbook/assets/15_09 45 30.png" alt=""><figcaption><p>A screen built in the Gum editor</p></figcaption></figure>
{% endtab %}

{% tab title="Code" %}
```csharp
// Initialize
var panel = new StackPanel();
panel.Spacing = 8;
panel.Visual.X = 16;
panel.Visual.Y = 90;
panel.AddToRoot();

var button = new Button();
button.Text = "Click Me";
button.Width = 200;
panel.AddChild(button);

var textBox = new TextBox();
textBox.Placeholder = "Enter text here...";
textBox.Width = 200;
panel.AddChild(textBox);

var checkBox = new CheckBox();
checkBox.Text = "Check me";
panel.AddChild(checkBox);

var listBox = new ListBox();
listBox.Visual.Width = 200;
listBox.Visual.Height = 120;
for (int i = 0; i < 10; i++)
    listBox.Items.Add($"Item {i}");
panel.AddChild(listBox);
```
{% endtab %}
{% endtabs %}

### [Runs Everywhere](code/about/runtimes.md)

Run the same UI on desktop, mobile, and web, with an editor for Windows, macOS, and Linux.

<figure><img src=".gitbook/assets/15_10 48 05.webp" alt=""><figcaption><p>The same game UI on a TV, laptop, and tablet</p></figcaption></figure>

### [Full UI Controls, Themed](code/controls/README.md)

Use ready-made controls such as Button, ListBox, TextBox, and Slider, restyle them with a built-in theme or your own styles, and operate them with a mouse, touch screen, keyboard, or gamepad.

<figure><img src=".gitbook/assets/15_10 02 11.png" alt=""><figcaption><p>Forms controls in the Hazard theme</p></figcaption></figure>

### [Live Preview and Hot Reload](gum-tool/editor-tab.md#preview)

Open any screen or component in its own window running the real runtime, where buttons click, lists scroll, and text boxes take input, and see your running game update when you save a change in the editor.

<figure><img src=".gitbook/assets/15_14 38 24.gif" alt=""><figcaption><p>The Preview window runs the real runtime next to the editor</p></figcaption></figure>

### [Variable References](gum-tool/gum-elements/general-properties/variable-references.md)

Point any variable at another variable, like a spreadsheet formula, to keep sizes and colors consistent across a project.

<figure><img src=".gitbook/assets/15_15 03 44.png" alt=""><figcaption><p>Variable references drive each square's size, position, and rotation from its index</p></figcaption></figure>

### [Components with Has-A and Is-A](gum-tool/gum-elements/component/README.md)

Build a component once and place customized instances of it inside other components, or extend it with a base type and expose the variables each instance can change.

<figure><img src=".gitbook/assets/30_06 54 43.png" alt=""><figcaption><p>Components in a Gum project</p></figcaption></figure>

### [States and Animations](gum-tool/gum-elements/states/README.md)

Save sets of variable values as named states, then switch between them or animate between them on a timeline.

<figure><img src=".gitbook/assets/15_14 52 15.gif" alt=""><figcaption><p>Scrubbing a GrowIn animation between the Small and Big states in the Animations tab</p></figcaption></figure>

### [Layouts That Adapt](gum-tool/gum-elements/general-properties/x-units.md)

Position and size objects in pixels, percentages, or relative to their children, then stack, wrap, or arrange them in a grid so the layout fits any resolution.

<figure><img src=".gitbook/assets/30_06 50 51.png" alt=""><figcaption><p>Position and size units</p></figcaption></figure>

### [Gradients, Drop Shadows, and Rounded Rectangles](gum-tool/gum-elements/rectangle.md)

Draw rectangles and circles with fills, outlines, gradients, drop shadows, and rounded corners.

<figure><img src=".gitbook/assets/30_05 03 56.png" alt=""><figcaption><p>Rectangles with outline, corner radius, and gradient</p></figcaption></figure>

### [Shaders and Effects](code/standard-visuals/containerruntime.md#render-target-shaders-rendertargeteffect)

Render a container to its own texture, then fade, blend, or mask it or apply a shader to everything inside it at once.

<figure><img src=".gitbook/assets/15_15 22 07.png" alt=""><figcaption><p>A post-process outline shader applied to a container and its contents</p></figcaption></figure>

### [Vector or Raster Art](gum-tool/gum-elements/skia-standard-elements/README.md)

Show sprites and NineSlice panels from image files, or SVG and Lottie vector art through SkiaSharp.

<figure><img src=".gitbook/assets/15_15 31 40.png" alt=""><figcaption><p>An SVG logo displayed in the Gum editor</p></figcaption></figure>

### [Rich Text and Fonts](code/standard-visuals/textruntime/bbcode.md)

Style individual words with BBCode tags, and generate fonts at runtime from a font name and size.

<figure><img src=".gitbook/assets/ThisIsBiggerText.png" alt=""><figcaption><p>Text sized with a BBCode tag</p></figcaption></figure>

### [Localization](gum-tool/localization.md)

Link a CSV or RESX file to your project and switch languages in the editor to check how longer text fits your layout.

<figure><img src=".gitbook/assets/15_13 16 27.png" alt=""><figcaption><p>Localization files and language selection</p></figcaption></figure>

### [Full Code Generation](gum-tool/code-tab/README.md)

Generate C# classes for your screens and components so your code works with typed properties instead of strings.

<figure><img src=".gitbook/assets/09_06 39 27.png" alt=""><figcaption><p>Generated code in the Code tab</p></figcaption></figure>

### [Binding and MVVM](code/binding-viewmodels/README.md)

Bind controls to a view model so your UI stays in sync with your game data.

<figure><img src=".gitbook/assets/BubblegumThemeScreenshot.png" alt=""><figcaption><p>A settings screen in the Bubblegum theme</p></figcaption></figure>

### [Native AOT Ready](code/performance-and-optimization/native-aot-and-trimming.md)

Publish with Native AOT or trimming after converting your project to the JSON file format.

### [Customizable Down to the Renderer](code/styling/code-only-styling/adding-and-removing-visual-children.md)

Replace the visuals behind any control, down to adding your own renderable types, when styling is not enough.

<figure><img src=".gitbook/assets/Legends2.jpg" alt=""><figcaption><p>The UI of Champions of Anteria</p></figcaption></figure>

### [Tooling](cli/README.md)

Create projects, check for errors, and generate code from the command line with GumCli, extend the editor with plugins, and give an AI assistant the Gum documentation.

<figure><img src=".gitbook/assets/15_15 45 12.png" alt=""><figcaption><p>Creating a new Gum project with GumCli</p></figcaption></figure>
