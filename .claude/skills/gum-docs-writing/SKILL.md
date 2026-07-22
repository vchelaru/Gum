---
name: gum-docs-writing
description: Writing Gum docs in GitBook markdown. Triggers: editing docs/, SUMMARY.md, GitBook hints/figures, cross-page links, doc images.
type: skill
---

# Gum Docs Writing Reference

## Verify Behavior Claims Before Writing Them

Before writing a claim like "X isn't available on backend Y" or "X requires package Z," verify it against the relevant domain skill (e.g. `gum-runtime-fonts`, `gum-kernsmith-integrations`, `gum-cross-platform-unification`) or source — this skill covers GitBook mechanics only, not product behavior. A missing per-platform package is not proof of a capability gap: the platform may get the capability another way (Silk.NET has no KernSmith package because it renders through SkiaSharp, which rasterizes fonts natively — parity, not a gap).

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

## Tool Docs vs Code Docs

The docs have two top-level sections — **Gum Tool** (`docs/gum-tool/`) and **Code** (`docs/code/`). These must never be mixed:

- **Tool docs** (`gum-tool/`) describe the Gum UI tool — properties, editors, menus, and workflows within the tool. They should not contain C# code samples or reference runtime APIs.
- **Code docs** (`docs/code/`) describe using Gum at runtime in game code — C# APIs, Forms controls, layout in code, etc. They should not describe how to use the Gum tool UI.

When writing a tool doc page (e.g., a property page under `gum-elements/`), focus entirely on the tool experience: what the property does visually, where it appears in the UI, and screenshots showing the effect. Do not add code examples showing how to set the property in C#. Conversely, code doc pages should not walk through the tool's UI.

## Cross-Cutting Topics (Binding, Localization, Theming)

Some features cut across many controls — binding, localization, theming, code generation — and individual controls have control-specific behaviors that intersect with them. For example, `TextBox.Text` can be bound to a numeric ViewModel property and the binding implicitly converts string ↔ number; `ListBox` interacts with `BindingContext` differently than other controls; localized labels behave differently in `PasswordBox`.

The convention for these:

- **Document the system-level behavior in the feature's own section.** Someone hitting the feature for the first time will look there. Binding rules go in `docs/code/binding-viewmodels/`, localization rules in the localization section, etc. This is the single source of truth.
- **Add a brief note on the control's page** with a short example and any control-specific gotchas, then **link back to the feature page** for the general rule (e.g. `[Implicit Type Conversion](../binding-viewmodels/advanced-binding-options.md#implicit-type-conversion)`). Do not duplicate the system-level explanation — trust the reader to follow the link.

The control-page note exists so that a reader troubleshooting "why doesn't my int binding take" while reading the TextBox page finds what they need without bouncing around. The feature-page section exists so that a reader learning the binding system isn't surprised later. Both audiences are real; the split avoids both blind spots without duplicating content.

## Date-Specific Content Goes in Hint Blocks

Anything that is only true relative to a release date — "as of <month> <year>", "before <date>", "in <version> X changed" — must live in a `{% hint %}` block, not in the body paragraphs of a section. Phrase the surrounding prose in terms of current behavior so it stays correct after the dated note is removed.

Why: dated notes eventually become noise once enough time has passed (everyone reading the docs is on the new behavior). Keeping them in hint blocks gives a single, visually obvious anchor to delete later instead of forcing the author to re-read every paragraph hunting for embedded dates.

Pattern:

```
The Font Generator project property controls ... There are two options:

* **KernSmith** — ... This is the default for new projects.
* **BMFont** — ... a separate external tool.

{% hint style="info" %}
**As of May 2026:** New projects default to **KernSmith**. Before this, the default was **BMFont**. Existing projects keep their current setting.
{% endhint %}
```

Not this — date leaks into body bullets, so removing the dated context later means rewriting prose:

```
* **KernSmith** — ... Default for new projects starting in May 2026.
* **BMFont** — ... Was the default before May 2026.
```

This applies to: "as of <date>", "starting in <date>", "before <date>", "in version X", "the May 2026 release", and any "what changed in <release>" section heading. When in doubt: if deleting the sentence later would leave the surrounding paragraph still accurate, it's safe in the body; if deleting it would leave a contradiction or dangling reference, lift it into a hint.

## Tone and Style

- Second person ("you"), present tense, instructional tone.
- Use numbered lists for procedures; bullet lists for non-sequential items.
- **Bold** UI element names: menu items, button labels, tab names, category names shown in the tool (e.g., **Source** category, **Variables** tab).
- Backticks for property names, variable names, property values, file names, and code: `Width Units`, `Relative To Children`, `Is Tiling Middle Sections`.
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

**Irreducible per-platform syntax → tabs, don't ask.** First try to collapse a sample to a single platform-neutral form. When the syntax genuinely *cannot* collapse — most often because a member's **type** differs per backend (e.g. `KeyCombo.PushedKey` / `KeyEventArgs.Key` is XNA `Microsoft.Xna.Framework.Input.Keys` on MonoGame but `Gum.Forms.Input.Keys` on Raylib, same names/values but distinct types; or a host skeleton that's a `Game` subclass vs a `Program.Main` loop) — wrap just those blocks in GitBook `{% tabs %}` with one `{% tab %}` per platform (e.g. `MonoGame` / `Raylib`). This is the standard, expected treatment for irreducible divergence; use it without asking. Keep the surrounding prose shared above/below the tabs — only the code that truly differs goes inside.

## XnaFiddle Links

For adding or updating XnaFiddle interactive links, see [xnafiddle.md](xnafiddle.md).

## Code Sample Rules

- **One-time initialization goes first, never mid-sample** — put all startup/registration calls (e.g. `GumUI.UseKeyboardDefaults()`, `GumUI.UseGamepadDefaults()`, input-device registration, service configuration) at the **top** of the sample, right under the `// Initialize` comment where `GumUI` is set up — before the feature code. Readers copy a sample as one block, so a setup call buried between feature lines reads as part of the feature logic and is confusing. Group setup first, then the feature it enables. Bad: create the control, add items, *then* `UseKeyboardDefaults()`, then wire focus. Good: `UseKeyboardDefaults()` first, then create the control and wire focus.
- **No reflection** — never use `GetType()`, `typeof()`, `MemberInfo`, or any `System.Reflection` APIs in doc code samples. Use `is` pattern matching instead: `if (device is Gum.Input.Keyboard)`.
- **Fully qualify non-Gum types** — any type that is not part of the Gum API must use its fully qualified name. For example, use `System.DateTime` not `DateTime`. Do not rely on implicit `using` statements for non-Gum types.
- **Prefer visible output over console output** — by default, surface a sample's result in the UI itself (update a visible label, change a property on a displayed object) rather than logging it. This keeps the sample XnaFiddle-runnable and lets a reader see the effect — e.g. show "You clicked the button" in a `Label` instead of writing it to the output window. Avoid `System.Console.WriteLine` (requires opening the diagnostics window) and `System.Diagnostics.Debug.WriteLine` (breaks in NetFiddle) on any page intended to be fiddle-able. The one exception is a page whose subject *is* debugging — where the sample's output is a developer-facing diagnostic that nobody will fiddle (e.g. the events troubleshooting page). There, a breakpoint-and-inspect or `Debug.WriteLine` is the natural tool and is acceptable; visible-label output would be contrived.

## Gotchas

- SUMMARY.md indentation is 2 spaces per level — wrong indentation breaks nesting visually in GitBook.
- A missing `{% endhint %}` closing tag breaks all content after it on the page.
- `<figure>` tags without `alt` and `<figcaption>` may not render correctly in GitBook.
- The `<figcaption>` content must be wrapped in `<p>` tags: `<figcaption><p>Caption</p></figcaption>`.
- Code blocks in docs use `csharp` language identifier: ` ```csharp `.
- Some README.md section pages contain only a brief intro paragraph and a figure — that is intentional; sub-pages provide detail.
- Page titles use `# Title` (H1) at the top. Sections within a page use `##` and `###` — avoid jumping heading levels.
- **Image/gif placeholders must be visible** — never use HTML comments (`<!-- TODO -->`) for placeholders because they are invisible in the rendered docs. Instead use a warning hint block so the placeholder is obvious when browsing the docs.
