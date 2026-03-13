---
name: gum-docs-writing
description: Reference guide for writing Gum documentation in GitBook markdown. Load when writing or editing docs/ files, adding pages to SUMMARY.md, using GitBook hints/figures, linking between pages, or adding images.
type: skill
---

# Gum Docs Writing Reference

## File Location and SUMMARY.md

All docs live under `docs/`. `docs/SUMMARY.md` is the master table of contents — GitBook navigation is built entirely from it. A file not listed in SUMMARY.md is unreachable from the nav even if it exists on disk.

Every new file must be added to SUMMARY.md with a 2-space indent per nesting level:

```
* [Section](gum-tool/section/README.md)
  * [Page Title](gum-tool/section/page.md)
    * [Sub-page](gum-tool/section/sub/page.md)
```

Paths in SUMMARY.md are relative to `docs/`.

## README.md as Section Landing Pages

A `README.md` inside a folder is the section's landing page. In SUMMARY.md, link it as `folder/README.md`. Sub-pages of that section are indented under it.

## Image Paths

All images live in `docs/.gitbook/assets/`. Reference them using a depth-relative path from the page file:

- From `docs/gum-tool/gum-elements/general-properties/x-units.md` → `../../../.gitbook/assets/filename.png`
- From `docs/gum-tool/gum-elements/skia-standard-elements/arc/README.md` → `../../../../.gitbook/assets/filename.png`

Image filename convention: date-time prefix like `15_06 10 06.png` (day_hour minute second) or `08_14 21 31.gif`. Spaces in filenames require wrapping the path in `<>` angle brackets in standard Markdown image syntax: `![](<../../../.gitbook/assets/15_06 10 06.png>)`. In `<figure>` tags the `src` attribute handles spaces without angle brackets.

Some images use a generic `image (N).png` naming pattern with numeric suffixes like `image (13) (1).png` — these are GitBook-uploaded files; don't rename them.

## GitBook-Specific Syntax

**Hint blocks** — must always have a closing `{% endhint %}` tag or the page breaks:

```
{% hint style="info" %}
Content here.
{% endhint %}

{% hint style="warning" %}
Content here.
{% endhint %}
```

**Figure blocks** — used for images with captions. Always include `alt` attribute and `<figcaption>`:

```
<figure><img src="../../../.gitbook/assets/filename.png" alt=""><figcaption><p>Caption text</p></figcaption></figure>
```

For inline images without captions, use standard Markdown: `![](<path>)`.

**HTML entities** — `&#x20;` (space) and `&#x64;` (`d`) appear in GitBook-generated content for spacing and special characters. Do not strip them; GitBook uses them intentionally.

## Internal Links

Always use relative paths — no absolute URLs. Link to a section landing page using its `README.md`:

```
[Container](../container/README.md)
```

Do not use anchor-only links to other files (e.g., `[foo](other-file.md#section)` is fine, but `[foo](#section)` only works within the same page).

## Tone and Style

- Second person ("you"), present tense, instructional tone.
- Use numbered lists for procedures; bullet lists for non-sequential items.
- **Bold** UI element names: menu items, button labels, tab names, property names shown in the tool.
- Backticks for variable names, property values, file names, and code: `Width Units`, `Relative To Children`.
- Avoid passive voice — prefer "Gum displays a label" over "a label is displayed."
- Screenshots (`.png`) for single states; animated GIFs (`.gif`) for multi-step interactions. Use them liberally.

## Code Sample Placement Comments

Every `\`\`\`csharp` block must have a placement comment as its first line so readers and agents know where the code belongs.

| Comment | Meaning |
|---|---|
| `// Initialize` | General initialization — could be `Initialize()`, `CustomInitialize()`, a constructor, etc. Use this by default. |
| `// Update` | Code that runs each frame in the update loop. |
| `// Draw` | Code that runs each frame in the draw loop. |
| `// Class scope` | Field or property declarations at class level. |
| `// In CustomInitialize` | Code specifically in a generated partial `CustomInitialize()` method. |

**When to omit the comment:**
- Tutorial pages that show a complete Game/app class — leave as-is.
- Non-game types shown as their own type definition (ViewModels, enums, partial generated classes).
- `using` statements — self-evident, no comment needed.

**Mixed-scope snippets** (class members + method bodies): use `// Class scope` for the member declarations, then show the actual method block(s) with full signatures and indented bodies — no outer class wrapper:

```csharp
// Class scope
int clickCount;

protected override void Initialize()
{
    var button = new Button();
    button.Click += (_, _) => clickCount++;
}
```

**Platform-agnostic by default:** prefer `// Initialize` over MonoGame-specific phrasing unless the page is explicitly MonoGame-only. This supports Raylib and other runtimes.

## Gotchas

- SUMMARY.md indentation is 2 spaces per level — wrong indentation breaks nesting visually in GitBook.
- A missing `{% endhint %}` closing tag breaks all content after it on the page.
- `<figure>` tags without `alt` and `<figcaption>` may not render correctly in GitBook.
- The `<figcaption>` content must be wrapped in `<p>` tags: `<figcaption><p>Caption</p></figcaption>`.
- Code blocks in docs use `csharp` language identifier: ` ```csharp `.
- Some README.md section pages contain only a brief intro paragraph and a figure — that is intentional; sub-pages provide detail.
- Page titles use `# Title` (H1) at the top. Sections within a page use `##` and `###` — avoid jumping heading levels.
