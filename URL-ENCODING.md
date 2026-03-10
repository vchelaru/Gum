# Adding XnaFiddle Links to Docs

## Criteria for including a link
- The example requires **no custom content files** (no PNG, font, .achx, etc.)
- The example is **self-contained in a single Game class** (no multi-file project)

## Link format in Markdown
Place the link on the line **immediately after** the closing triple-backtick of the code block, before any `<figure>` or blank line:

```markdown
```csharp
// ... example code ...
```
<a href="https://xnafiddle.net/#code=<encoded>" target="_blank">Try on XnaFiddle.NET</a>

<figure>...
```

Use an HTML `<a>` tag with `target="_blank"` so the link opens in a new tab — standard Markdown links have no way to set this. The link text is always **`Try on XnaFiddle.NET`** — the `.NET` suffix is intentional so readers learn the full domain name.

## One link per code block
Each code block gets its **own self-contained Game1 class**. Do not combine multiple sections into one link. This keeps URLs short and lets users make focused edits.

## Standard usings

| Namespace | Provides |
|---|---|
| `Microsoft.Xna.Framework` | `Game`, `GameTime`, `Color` |
| `Microsoft.Xna.Framework.Graphics` | `GraphicsDeviceManager` |
| `MonoGameGum` | `GumService`, `AddToRoot` extension method |
| `Gum.Forms` | `DefaultVisualsVersion` |
| `Gum.Forms.Controls` | `StackPanel`, `Button`, `Label`, `CheckBox`, `ComboBox`, `ListBox`, `RadioButton`, `ScrollViewer`, `Slider`, `TextBox` |

Add more namespaces here as new ones are discovered to be needed.

## Game1 boilerplate for MonoGame + Gum Forms
Every fiddle wraps the example body in this shell:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using Gum.Forms;
using Gum.Forms.Controls;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    GumService GumUI => GumService.Default;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GumUI.Initialize(this, DefaultVisualsVersion.V3);

        var mainPanel = new StackPanel();
        mainPanel.AddToRoot();

        // *** example body goes here ***

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        GumUI.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumUI.Draw();
        base.Draw(gameTime);
    }
}
```

If an example references a `label` variable (for event feedback), add a `Label` and `mainPanel.AddChild(label)` before the example body — the doc snippets assume it already exists in context.

## Encoding tool (preferred)

Use the pre-built exe at `tools/xnafiddle-encode.exe` in the XnaFiddle repo. Pass `code` or `snippet` as the first argument:

```bash
tools/xnafiddle-encode.exe code --file MyGame.cs
tools/xnafiddle-encode.exe snippet '{"IsGum":true,"initialize":"..."}'
```

Output is a single line with the complete URL. See the **Snippet format** section below for full details.

## PowerShell encoding script (fallback)
Only use this if the exe is unavailable. Produces an encoded string for `#code=` links:

```powershell
function Encode-ForXnaFiddle($code) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($code)
    $ms = New-Object System.IO.MemoryStream
    $gz = New-Object System.IO.Compression.GZipStream($ms, [System.IO.Compression.CompressionLevel]::Optimal)
    $gz.Write($bytes, 0, $bytes.Length)
    $gz.Close()
    $base64 = [Convert]::ToBase64String($ms.ToArray())
    return $base64.Replace('+', '-').Replace('/', '_').TrimEnd('=')
}

$url = "https://xnafiddle.net/#code=$(Encode-ForXnaFiddle $code)"
```

---

## Snippet format (`#snippet=`) — preferred for Gum examples

For examples that only need Gum UI setup (no custom draw loop, no extra members), use the **snippet format** instead of encoding a full Game class. The snippet is a small JSON object; XnaFiddle wraps it in the full `Game` scaffold automatically.

**URL format:** `https://xnafiddle.net/#snippet=<encoded>`

The encoding is identical (GZip + Base64url) but the input is JSON, not C#.

### JSON schema

```jsonc
{
  "IsGum": true,           // inject Gum boilerplate (see below)

  "usings":   ["MyNs"],    // extra using namespaces beyond defaults
  "members":  "...",       // field/property declarations inside the class
  "initialize": "...",     // body of Initialize(), after GumUI.Initialize()
  "loadContent": "...",    // body of LoadContent()
  "update":   "...",       // body of Update(), after GumUI.Update()
  "draw":     "..."        // body of Draw(), between Clear() and GumUI.Draw()
}
```

All fields are optional. Use `\n` for newlines and `\"` for quotes inside JSON strings.

### What `IsGum: true` provides automatically

You do **not** need to include any of the following — the scaffold adds them:

| Section | Generated code |
|---|---|
| Usings | `using MonoGameGum;` `using Gum.Forms;` `using Gum.Forms.Controls;` |
| Member | `GumService GumUI => GumService.Default;` |
| Constructor | `graphics = new GraphicsDeviceManager(this);` + `HiDef` + `IsMouseVisible` + `AllowUserResizing` |
| Initialize | `base.Initialize();` then `GumUI.Initialize(this, DefaultVisualsVersion.V3);` |
| Update | `GumUI.Update(gameTime);` then `base.Update(gameTime);` |
| Draw | `GraphicsDevice.Clear(new Color(0.15f, 0.15f, 0.2f));` then `GumUI.Draw();` then `base.Draw(gameTime);` |

The user's `initialize` code is inserted **after** `GumUI.Initialize(...)`, so Gum controls can be created immediately.

### Minimal example

A button that counts clicks:

```json
{"IsGum":true,"initialize":"int count = 0;\nvar label = new Label();\nlabel.Text = \"Clicks: 0\";\nlabel.AddToRoot();\nvar btn = new Button();\nbtn.Text = \"Click me!\";\nbtn.Width = 200;\nbtn.Click += (_, _) => label.Text = $\"Clicks: {++count}\";\nbtn.AddToRoot();"}
```

That 268-character JSON is the entire input. The encoder produces the `#snippet=` value.

### When to use `#snippet=` vs `#code=`

| Situation | Use |
|---|---|
| Example only shows Gum control setup | `#snippet=` with `IsGum: true` |
| Example has custom draw, members, or update logic beyond Gum | `#snippet=` with extra fields, or `#code=` if it's complex |
| Example has custom helper classes or multiple types | `#code=` with a full Game class |

### Encoding tool

A pre-built Windows exe lives at `tools/xnafiddle-encode.exe` in the XnaFiddle repo. **Agents should use this instead of writing encoding logic manually.** Invoke it directly — no installation, no dotnet required at runtime.

```bash
# Snippet from an inline JSON string
tools/xnafiddle-encode.exe snippet '{"IsGum":true,"initialize":"var btn = new Button();\nbtn.AddToRoot();"}'

# Snippet from a file
tools/xnafiddle-encode.exe snippet --file mysnippet.json

# Full code from a file
tools/xnafiddle-encode.exe code --file MyGame.cs
```

Output is always a single line — the complete URL — ready to paste into Markdown:

```
https://xnafiddle.net/#snippet=H4sIAAAAAAAACqtW8i...
```

#### Rebuilding the exe

If `UrlCodec` encoding logic changes, rebuild with:

```bash
dotnet publish XnaFiddle.Encoder/XnaFiddle.Encoder.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=none -o tools
```

### Markdown link format for snippets

Same as `#code=` links — `target="_blank"`, same link text:

```markdown
<a href="https://xnafiddle.net/#snippet=<encoded>" target="_blank">Try on XnaFiddle.NET</a>
```
