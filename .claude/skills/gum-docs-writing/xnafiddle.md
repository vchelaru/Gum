# XnaFiddle Links

## Criteria — only include a link when

- The example needs **no content files** (no PNG, font, .achx, etc.)
- The example is **self-contained** (all code fits in one file — custom classes are fine via `#code=` format, but no external file dependencies)

> **Custom files not supported:** If the example requires any custom content files (textures, fonts, animation chains, etc.), an XnaFiddle link cannot be created — skip the link entirely.

## Encoding

URLs are gzip + base64 encoded — **deterministic**: same input always produces the same URL. You can detect stale links by re-encoding the adjacent code block and comparing.

Always use the pre-built exe — never write encoding logic by hand:

```
C:\Users\vchel\Documents\GitHub\XnaFiddle\tools\xnafiddle-encode.exe
# or from Gum repo root:
..\XnaFiddle\tools\xnafiddle-encode.exe
```

Usage: `xnafiddle-encode.exe <mode> <input>` where mode is `snippet` or `code`, input is an inline string or `--file <path>`. **Always use `--file` when input contains quotes or newlines.** The tool prints one complete URL per item to stdout.

**Batch encoding (preferred):** Pass multiple mode+input pairs in a single call — the tool outputs one URL per line:

```bash
xnafiddle-encode.exe snippet --file a.json snippet --file b.json snippet --file c.json
```

When adding links to multiple code blocks, **always batch**: write all JSON files first, then encode all in one Bash call, capture the output lines, then insert all links. This avoids one tool call per snippet.

## Two URL formats

| Format | When to use |
|---|---|
| `#snippet=` | Preferred. Input is a JSON object; XnaFiddle wraps it in a full `Game` class automatically. |
| `#code=` | Complete, compilable C# source file. **Must include an explicit `Game1 : Game` class.** Use when the example has multiple types or complex structure. |

### `#code=` requirements

Every `#code=` file **must** contain a complete `Game1` class that inherits from `Microsoft.Xna.Framework.Game`, with `Initialize`, `Update`, and `Draw` overrides. XnaFiddle does not inject any boilerplate for `#code=` — unlike `#snippet=`, nothing is added automatically.

If the fiddle demonstrates a custom class (e.g. `TextInputDialog`), the file should contain:
1. The custom class definition(s)
2. A `Game1 : Game` class that instantiates and uses the custom class

Example skeleton:

**Rules for `#code=` files:**
- Always include `using MonoGameGum;` and `using Gum.Forms;` — never `MonoGameGum.Forms.*` (obsolete)
- Always include `using Gum.Forms.DefaultVisuals.V3;` — **never** `using Gum.Forms.DefaultVisuals;` (the non-V3 namespace is obsolete and must not be used)
- Always declare `GumService GumUI => GumService.Default;` as a property on `Game1`
- Always fully qualify `Anchor` and `Dock` arguments: `Gum.Wireframe.Anchor.Center`, `Gum.Wireframe.Dock.Fill`
- `GumUI.Update` takes only `gameTime` — **not** `this`

```csharp
using MonoGameGum;
using Gum.Forms;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using Gum.Forms.DefaultVisuals.V3;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class MyCustomControl : Panel
{
    // ...class body using Gum.Wireframe.Dock.Fill, Gum.Wireframe.Anchor.Center...
}

public class Game1 : Game
{
    GraphicsDeviceManager graphics;
    GumService GumUI => GumService.Default;

    public Game1() { graphics = new GraphicsDeviceManager(this); }

    protected override void Initialize()
    {
        GumUI.Initialize(this, DefaultVisualsVersion.V3);
        var control = new MyCustomControl();
        control.AddToRoot();
        control.Anchor(Gum.Wireframe.Anchor.Center);
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

## Snippet JSON schema

All fields optional: `IsGum`, `IsMonoGameExtended`, `IsAposShapes`, `usings`, `members`, `initialize`, `loadContent`, `update`, `draw`.

| Flag | Effect |
|---|---|
| `"IsGum": true` | Injects Gum usings, `GumUI` member, and init/update/draw boilerplate. Does **not** create any UI controls. |
| `"IsMonoGameExtended": true` | Injects `SpriteBatch _spriteBatch` and its `LoadContent` init. |
| `"IsAposShapes": true` | Injects `ShapeBatch _shapeBatch`, its init, and `Begin()`/`End()` wrappers. |

**Visual classes in snippets:** Classes like `WindowVisual`, `ButtonVisual`, etc. are defined in versioned namespaces. The `IsGum` flag injects common Gum usings but does **not** inject the visual namespace. If a snippet references any visual class (e.g. `WindowVisual`, `ListBoxVisual`), add `"usings": ["Gum.Forms.DefaultVisuals.V3"]` to the snippet JSON. All current visuals use V3. **Never** put `using` statements in the `initialize` or `members` fields — they must go in the `usings` array to appear at file scope.

**JSON escaping inside string values:** newlines → `\n`, double quotes → `\"`, backslashes → `\\`. Curly braces `{}` do **not** need escaping in JSON (only in shell strings).

## Implied variables

Doc code samples in a tutorial often reference variables that were established in a prior page or earlier in the same page (e.g. a container, a label used across multiple snippets). These are **implied variables** — present in the reader's running project but absent from the snippet itself.

Before encoding a fiddle, scan the code block for any variable that is used but never declared within that block. Every implied variable **must** be declared in the fiddle's `initialize` field or the fiddle will fail to compile.

Also evaluate whether the implied variable should be made explicit in the **doc sample itself**. If a reader could reasonably copy the snippet and be confused about where a variable comes from, add its declaration to the doc code block too.

## Workflow

**CRITICAL: Never type or regenerate encoded URLs from memory.** LLMs hallucinate single characters in long base64 strings, silently corrupting the URL. Always read the encoder output from a file and use that exact content.

1. Write **all** snippet JSON files (or `.cs` files for `#code=`) to temp files.
2. Encode all in **one Bash call**, redirecting output to a file:
   ```bash
   xnafiddle-encode.exe code --file a.cs code --file b.cs > urls.txt
   ```
3. **Read `urls.txt`** with the Read tool to bring the exact URLs into context.
4. Build the `<a href="...">` tag using the URL exactly as read — do not retype, abbreviate, or reconstruct it.
5. Insert the link into the doc using Edit (for adding a single link) or Write (for multiple links or large rewrites).
6. **Verify after insertion:** extract the URL from the doc file using grep and diff it against the encoder output file to confirm no corruption occurred:
   ```bash
   grep -o 'https://xnafiddle[^"]*' doc.md > check.txt
   diff <(tr -d '\r\n' < check.txt) <(tr -d '\r\n' < urls.txt)
   ```
   If they differ, fix immediately — even one character breaks the fiddle.

## Link format

```markdown
<a href="https://xnafiddle.net/#snippet=<encoded>" target="_blank">Try on XnaFiddle.NET</a>
```

- Use an HTML `<a>` tag with `target="_blank"` — standard Markdown links cannot set this. **Do not use `<iframe>` — GitBook does not render iframes.**
- Link text is always `Try on XnaFiddle.NET` (`.NET` suffix intentional).
- Place the link immediately after the closing triple-backtick, before any `<figure>` or blank line.
- **One link per code block** — keep each fiddle focused and self-contained.
