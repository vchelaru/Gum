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
| `#code=` | Complete, compilable C# source file. Use when the example has multiple types or complex structure. |

## Snippet JSON schema

All fields optional: `IsGum`, `IsMonoGameExtended`, `IsAposShapes`, `members`, `initialize`, `loadContent`, `update`, `draw`.

| Flag | Effect |
|---|---|
| `"IsGum": true` | Injects Gum usings, `GumUI` member, and init/update/draw boilerplate. Does **not** create any UI controls. |
| `"IsMonoGameExtended": true` | Injects `SpriteBatch _spriteBatch` and its `LoadContent` init. |
| `"IsAposShapes": true` | Injects `ShapeBatch _shapeBatch`, its init, and `Begin()`/`End()` wrappers. |

**JSON escaping inside string values:** newlines → `\n`, double quotes → `\"`, backslashes → `\\`. Curly braces `{}` do **not** need escaping in JSON (only in shell strings).

## Implied variables

Doc code samples in a tutorial often reference variables that were established in a prior page or earlier in the same page (e.g. a container, a label used across multiple snippets). These are **implied variables** — present in the reader's running project but absent from the snippet itself.

Before encoding a fiddle, scan the code block for any variable that is used but never declared within that block. Every implied variable **must** be declared in the fiddle's `initialize` field or the fiddle will fail to compile.

Also evaluate whether the implied variable should be made explicit in the **doc sample itself**. If a reader could reasonably copy the snippet and be confused about where a variable comes from, add its declaration to the doc code block too.

## Workflow

1. Write **all** snippet JSON files (or `.cs` files for `#code=`) to temp files.
2. Encode all in **one Bash call**: `xnafiddle-encode.exe snippet --file a.json snippet --file b.json ...` — captures all URLs at once.
3. Place each output URL **immediately after** the closing triple-backtick of its code block, before any `<figure>` or blank line.
4. Prefer a single `Write` to rewrite the whole doc over multiple `Edit` calls when inserting many links.

## Link format

```markdown
<a href="https://xnafiddle.net/#snippet=<encoded>" target="_blank">Try on XnaFiddle.NET</a>
```

- Use an HTML `<a>` tag with `target="_blank"` — standard Markdown links cannot set this. **Do not use `<iframe>` — GitBook does not render iframes.**
- Link text is always `Try on XnaFiddle.NET` (`.NET` suffix intentional).
- Place the link immediately after the closing triple-backtick, before any `<figure>` or blank line.
- **One link per code block** — keep each fiddle focused and self-contained.
