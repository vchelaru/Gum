# Adding XnaFiddle Links to Docs

## Criteria for including a link
- The example requires **no custom content files** (no PNG, font, .achx, etc.)
- The example is **self-contained** (no multi-file project, no helper classes in other files)

---

## The encoding tool

A pre-built Windows exe lives in the XnaFiddle repo:

```
C:\Users\vchel\Documents\GitHub\XnaFiddle\tools\xnafiddle-encode.exe
```

Or from the Gum repo root:

```
..\XnaFiddle\tools\xnafiddle-encode.exe
```

**Agents must use this tool. Do not write encoding logic by hand.**

It takes two arguments — the mode (`snippet` or `code`) and either an inline string or `--file <path>` — and prints a single line to stdout: the complete ready-to-paste URL.

```
xnafiddle-encode.exe snippet '<json>'
xnafiddle-encode.exe snippet --file mysnippet.json
xnafiddle-encode.exe code '<csharp>'
xnafiddle-encode.exe code --file MyGame.cs
```

**Use `--file` whenever the input contains quotes or newlines.** Shell quoting of inline strings is fragile and error-prone. Write the content to a temp file and pass the path instead.

---

## Two URL formats

### `#snippet=` — preferred for Gum examples

The input is a small JSON object. XnaFiddle wraps it in a full `Game` subclass automatically. Only provide the parts that are specific to the example.

### `#code=` — for complex examples

The input is a complete, compilable C# source file containing a `Game` subclass. Use this when the example has custom helper classes, complex members, or anything that doesn't fit the snippet scaffold.

| Situation | Use |
|---|---|
| Example only shows Gum control setup in `Initialize` | `#snippet=` with `IsGum: true` |
| Example has custom fields, `Update` logic, or draw code beyond Gum | `#snippet=` with extra fields |
| Example has multiple types or complex structure | `#code=` with a full class |

---

## Snippet format (`#snippet=`)

### JSON schema

```jsonc
{
  "IsGum": true,        // adds all Gum boilerplate — see table below

  "usings":    ["Ns"], // extra namespaces beyond the defaults
  "members":   "...",  // field/property declarations inside the class
  "initialize":"...",  // body placed after GumUI.Initialize()
  "loadContent":"...", // body of LoadContent()
  "update":    "...",  // body placed after GumUI.Update()
  "draw":      "..."   // body placed between Clear() and GumUI.Draw()
}
```

All fields are optional. Omitted fields produce empty method bodies (with preset boilerplate only).

### What `IsGum: true` generates automatically

You do **not** include any of the following — the scaffold adds them:

| Section | Generated |
|---|---|
| Usings | `using MonoGameGum;` `using Gum.Forms;` `using Gum.Forms.Controls;` |
| Member | `GumService GumUI => GumService.Default;` |
| Constructor | `new GraphicsDeviceManager(this)` + `HiDef` profile + `IsMouseVisible = true` + `AllowUserResizing = true` |
| Initialize | `base.Initialize();` → `GumUI.Initialize(this, DefaultVisualsVersion.V3);` → *your code* |
| Update | `GumUI.Update(gameTime);` → *your code* → `base.Update(gameTime);` |
| Draw | `GraphicsDevice.Clear(new Color(0.15f, 0.15f, 0.2f));` → *your code* → `GumUI.Draw();` → `base.Draw(gameTime);` |

### JSON escaping rules

Inside a JSON string value:
- Newlines → `\n`
- Double quotes → `\"`
- Backslashes → `\\`
- All other characters are literal

Example — this C# line:
```csharp
label.Text = $"Value: {slider.Value:0.0}";
```
becomes this in JSON:
```json
"label.Text = $\"Value: {slider.Value:0.0}\";"
```

Curly braces `{}` do **not** need escaping in JSON — only in shell strings.

### Recommended workflow for a snippet link

1. Write the initialize body (and any other sections needed) in a plain `.json` temp file — no shell escaping required:

```json
{
  "IsGum": true,
  "initialize": "int count = 0;\nvar label = new Label();\nlabel.Text = \"Clicks: 0\";\nlabel.AddToRoot();\nvar btn = new Button();\nbtn.Text = \"Click me!\";\nbtn.Width = 200;\nbtn.Click += (_, _) => label.Text = $\"Clicks: {++count}\";\nbtn.AddToRoot();"
}
```

2. Run the tool:

```bash
..\XnaFiddle\tools\xnafiddle-encode.exe snippet --file mysnippet.json
```

3. Capture the output URL and place it in the doc immediately after the closing triple-backtick:

```markdown
```csharp
// ... doc code snippet (not the full game class) ...
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAA..." target="_blank">Try on XnaFiddle.NET</a>
```

---

## Full code format (`#code=`)

### Recommended workflow for a `#code=` link

1. Write a complete, compilable C# source file with a `Game` subclass. Use this boilerplate as the shell:

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using Gum.Forms;
using Gum.Forms.Controls;

public class MyGame : Game
{
    GraphicsDeviceManager graphics;
    GumService GumUI => GumService.Default;

    public MyGame()
    {
        graphics = new GraphicsDeviceManager(this);
        graphics.GraphicsProfile = GraphicsProfile.HiDef;
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        GumUI.Initialize(this, DefaultVisualsVersion.V3);

        // example body here

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        GumUI.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(0.15f, 0.15f, 0.2f));
        GumUI.Draw();
        base.Draw(gameTime);
    }
}
```

2. Run the tool:

```bash
..\XnaFiddle\tools\xnafiddle-encode.exe code --file MyGame.cs
```

3. Place the output URL in the doc after the code block (same as snippet format above).

### Standard usings available without adding to `usings`

| Namespace | Provides |
|---|---|
| `Microsoft.Xna.Framework` | `Game`, `GameTime`, `Color`, `Vector2` |
| `Microsoft.Xna.Framework.Graphics` | `GraphicsDeviceManager`, `SpriteBatch` |
| `Microsoft.Xna.Framework.Input` | `Keyboard`, `Mouse`, `Keys` |
| `MonoGameGum` | `GumService` |
| `Gum.Forms` | `DefaultVisualsVersion` |
| `Gum.Forms.Controls` | `Button`, `Label`, `CheckBox`, `ComboBox`, `ListBox`, `RadioButton`, `ScrollViewer`, `Slider`, `StackPanel`, `TextBox` |

---

## Link format in Markdown

Place the link on the line **immediately after** the closing triple-backtick of the code block, before any `<figure>` or blank line:

```markdown
```csharp
// ... example code ...
```
<a href="https://xnafiddle.net/#snippet=<encoded>" target="_blank">Try on XnaFiddle.NET</a>

<figure>...
```

- Use an HTML `<a>` tag with `target="_blank"` — standard Markdown links cannot set this
- Link text is always **`Try on XnaFiddle.NET`** — the `.NET` suffix is intentional
- One link per code block; each link is self-contained

---

## One link per code block

Each code block gets its own self-contained fiddle. Do not combine multiple doc sections into one link — keep each focused so readers can make targeted edits.

---

## Rebuilding the exe

If the encoding logic in `UrlCodec.cs` changes, rebuild from the XnaFiddle repo:

```bash
dotnet publish XnaFiddle.Encoder/XnaFiddle.Encoder.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=none -o tools
```
