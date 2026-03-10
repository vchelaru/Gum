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
[Try on XnaFiddle.NET](https://xnafiddle.net/#code=<encoded>)

<figure>...
```

The link text is always **`Try on XnaFiddle.NET`** — the `.NET` suffix is intentional so readers learn the full domain name.

## One link per code block
Each code block gets its **own self-contained Game1 class**. Do not combine multiple sections into one link. This keeps URLs short and lets users make focused edits.

## Game1 boilerplate for MonoGame + Gum Forms
Every fiddle wraps the example body in this shell:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

## PowerShell encoding script
Use this function to produce an encoded URL from a complete Game1 source string:

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
