# XnaFiddle Links

## Criteria — only include a link when

- The example needs **no content files** (no PNG, font, .achx, etc.)
- The example is **self-contained** (single file, no helper classes)

## Encoding

URLs are gzip + base64 encoded — **deterministic**: same input always produces the same URL. You can detect stale links by re-encoding the adjacent code block and comparing.

Always use the pre-built exe — never write encoding logic by hand:

```
C:\Users\vchel\Documents\GitHub\XnaFiddle\tools\xnafiddle-encode.exe
# or from Gum repo root:
..\XnaFiddle\tools\xnafiddle-encode.exe
```

Usage: `xnafiddle-encode.exe <mode> <input>` where mode is `snippet` or `code`, input is an inline string or `--file <path>`. **Always use `--file` when input contains quotes or newlines.** The tool prints the complete URL to stdout.

## Two URL formats

| Format | When to use |
|---|---|
| `#snippet=` | Preferred. Input is a JSON object; XnaFiddle wraps it in a full `Game` class automatically. |
| `#code=` | Complete, compilable C# source file. Use when the example has multiple types or complex structure. |

## Snippet JSON schema

All fields optional: `IsGum`, `usings`, `members`, `initialize`, `loadContent`, `update`, `draw`.

`"IsGum": true` generates all Gum usings, the `GumUI` member, constructor setup, and wraps each method body with `GumUI.Initialize` / `GumUI.Update` / `GumUI.Draw` calls.

**JSON escaping inside string values:** newlines → `\n`, double quotes → `\"`, backslashes → `\\`. Curly braces `{}` do **not** need escaping in JSON (only in shell strings).

## Workflow

1. Write snippet JSON (or full `.cs` for `#code=`) to a temp file.
2. Run `xnafiddle-encode.exe snippet --file mysnippet.json` (or `code --file MyGame.cs`).
3. Place the output URL **immediately after** the closing triple-backtick, before any `<figure>` or blank line.

## Embed format

Always use the gate page with `?hover=true` — never a bare `<a>` link:

```html
<iframe src="https://xnafiddle.net/embed-gate.html?hover=true#snippet=<encoded>" width="600" height="400"></iframe>
```

- `embed-gate.html` shows a **"▶ Run Sample"** button and prefetches the ~4 MB WASM in the background. Once clicked on any page, future visits skip the button automatically.
- `?hover=true` throttles to 2 fps when the mouse is not over the canvas — essential when multiple iframes appear on one page.
- Place the `<iframe>` immediately after the closing triple-backtick, before any `<figure>` or blank line.
- **One iframe per code block** — keep each fiddle focused and self-contained.
