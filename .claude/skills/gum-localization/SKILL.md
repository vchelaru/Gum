---
name: gum-localization
description: Reference guide for Gum's localization system — ILocalizationService, CSV/RESX loading in both the tool and runtime, Text vs TextNoTranslate paths, Forms control localization patterns, and gotchas.
---

# Gum Localization

## Architecture Overview

Localization is opt-in via a nullable static property. When set, text assigned through the `"Text"` property name is translated; text assigned through `"TextNoTranslate"` bypasses translation entirely.

**Entry point:** `CustomSetPropertyOnRenderable.LocalizationService` (static, nullable `ILocalizationService?`)

**Default initialization:** `SystemManagers` lazily creates a `LocalizationService` instance using `??=`, so assigning your own service *before* initialization preserves it.

**Access at runtime:** `GumService.Default.LocalizationService` forwards to the static property above.

## ILocalizationService

`GumCommon/Localization/ILocalizationService.cs` — five members:

- `CurrentLanguage` (int) — index into the translation arrays (0 = default/source language)
- `Languages` (`IReadOnlyList<string>`) — language names populated after loading; empty until a database is loaded
- `AddDatabase(Dictionary<string, string[]>, List<string>)` — loads translations; key = string ID, value = array where `[0]` is the ID and `[1..N]` are translations per language
- `Clear()` — resets the database and Languages list
- `Translate(string stringId)` — returns the translated string for `CurrentLanguage`

## LocalizationService (default implementation)

`GumCommon/Localization/LocalizationService.cs`

Translation logic in `TranslateForLanguage`:
1. If database is empty → return string as-is (no translation, no suffix)
2. If string ID is found → return `mStringDatabase[stringId][language]`
3. If string has no letters (numbers/punctuation/whitespace only) → return as-is (excluded from translation)
4. Otherwise → return `stringId + "(loc)"` — the "(loc)" suffix signals a missing translation key

## Loading Data — LocalizationServiceExtensions

`GumCommon/Localization/LocalizationServiceExtensions.cs` — extension methods on `ILocalizationService`:

**CSV:** `AddCsvDatabase(Stream)` — uses CsvHelper. First column = string ID, subsequent columns = translations. First row = language headers. Languages list populated from header row.

**RESX:** Two overloads:
- `AddResxDatabase(string baseResxFilePath)` — discovers satellite files by convention (e.g., `Strings.resx` + `Strings.es.resx`, `Strings.fr.resx`). Satellites are sorted alphabetically. Base file is labeled `"Default"` in the Languages list; satellites use their culture code (e.g., `"es"`, `"fr"`).
- `AddResxDatabase(IEnumerable<(string languageName, Stream stream)>)` — stream-based, for manual control over language order and names. Use this on mobile/web where `Directory.GetFiles` isn't available.

Both formats produce the same internal structure: `Dictionary<string, string[]>` where index 0 = string ID, 1+ = per-language translations.

## Gum Tool Localization Support

The tool stores a single `LocalizationFile` path (relative to the project) in `GumProjectSave`. Both `.csv` and `.resx` are supported; format is inferred from the file extension in `FileCommands.LoadLocalizationFile()`.

**RESX in the tool:** User points to the base file (`Strings.resx`). The tool calls `AddResxDatabase(string)` which auto-discovers all satellites in the same directory.

**File watching:** `FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(changedFile, baseFile)` determines whether a changed file should trigger a reload. For RESX, this returns true for the base file AND any sibling matching `{BaseName}.*.resx`. For CSV, only exact match.

**Language dropdown:** After loading, `ILocalizationService.Languages` is populated. `ProjectPropertiesViewModel.LanguageName` (string) replaces the raw `LanguageIndex` int in the UI. The plugin syncs `LanguageName` ↔ `LanguageIndex` via `IFileCommands.LocalizationLoaded` event (fired at the end of every `LoadLocalizationFile()` call).

**Variable grid refresh:** `LoadLocalizationFile()` calls `_guiCommands.RefreshVariables()` at the end, so the Text property displayer updates from plain textbox to localization combo box without requiring re-selection.

## Translation Flow in CustomSetPropertyOnRenderable

`Gum/Wireframe/CustomSetPropertyOnRenderable.cs`, `TrySetPropertyOnText` method:

When `SetProperty` is called with property name `"Text"` or `"TextNoTranslate"`:

1. If the raw value contains `[` → treated as BBCode markup, applied directly (stored as `StoredMarkupText`)
2. If property is `"Text"` AND `LocalizationService != null` → `rawText = LocalizationService.Translate(rawText)`
3. If the *translated* result contains `[` → treated as BBCode (translation can produce BBCode)
4. If property is `"TextNoTranslate"` → no translation call, value used as-is

**Key detail:** BBCode in the *original* string is checked first (step 1). If there's no BBCode in the original, translation runs, then BBCode is checked again on the result (step 3). This means a translated value can contain BBCode markup even if the string ID didn't.

## TextRuntime

`MonoGameGum/GueDeriving/TextRuntime.cs`:

- `Text` property (get/set) — calls `SetProperty("Text", value)` → goes through localization
- `SetTextNoTranslate(string?)` method — calls `SetProperty("TextNoTranslate", value)` → bypasses localization

`SetTextNoTranslate` is a method, not a property, because the underlying renderable only stores the final string — there's no way to distinguish translated from untranslated text after assignment, so a getter would be misleading.

## Forms Controls Pattern

All Forms controls with displayable text follow the same pattern:

| Control | Localized property | No-translate method |
|---|---|---|
| Button | `Text` | `SetTextNoTranslate()` |
| Label | `Text` | `SetTextNoTranslate()` |
| CheckBox | `Text` | `SetTextNoTranslate()` |
| RadioButton | `Text` | `SetTextNoTranslate()` |
| TextBox | `Text` | `SetTextNoTranslate()` |
| TextBoxBase | `Placeholder` | `SetPlaceholderNoTranslate()` |
| MenuItem | `Header` | `SetHeaderNoTranslate()` |

Internally, all no-translate methods call `SetProperty("TextNoTranslate", value)` on the underlying text component.

### Data-Driven Controls — Intentionally No Localization

**ComboBox** — `Text` property sets `coreTextObject.RawText` directly (bypasses `SetProperty` entirely). This is because ComboBox text comes from `SelectedItem.ToString()`, which is data-driven.

**ListBoxItem** — `UpdateToObject(object o)` sets `coreText.RawText = o?.ToString()` directly. Same reason: items come from a data collection.

To localize data-driven controls, pre-translate values before adding them to the `Items` collection.

### TextBox and PasswordBox — User Input

TextBox internally uses `SetTextNoTranslate` for all user-initiated editing: typing (`HandleCharEntered`), pasting, and deleting. This prevents accidental translation of user-typed content.

PasswordBox uses `TextNoTranslate` for mask characters (e.g., "●●●●") since those should never be translated.

## Gotchas

1. **"(loc)" suffix is intentional** — When a database is loaded but a string ID isn't found, `Translate()` appends "(loc)". This is a debugging feature, not a bug. Empty databases return strings unchanged (no suffix).

2. **Translation happens at assignment time, not read time** — The renderable stores only the final translated string. Changing `CurrentLanguage` after setting text does NOT retroactively update existing UI. You must re-assign `Text` to all controls.

3. **Null service = no localization** — If `LocalizationService` is null, all text passes through unchanged. This is the expected state when localization isn't needed.

4. **BBCode interaction** — If the original string contains `[`, BBCode is parsed *before* translation (and translation is skipped for that value). If the original has no BBCode but the translated result does, BBCode is parsed on the translated result. Be careful: a string ID with `[` in it won't be translated.

5. **CurrentLanguage is a raw array index** — No bounds checking. Index 0 in the translation array is the string ID itself (not a translation). Actual translations start at index 1. Setting `CurrentLanguage = 0` returns the string ID.

6. **RESX satellite ordering and naming** — Satellites are sorted alphabetically by file path, so `de` comes before `es` comes before `fr`. The base file is always first and labeled `"Default"`. If you need a specific order or names, use the stream-based overload.

7. **ShouldExcludeFromTranslation** — Strings with no letters (pure numbers, punctuation, whitespace, or empty) are silently excluded from translation and returned as-is, with no "(loc)" suffix. This prevents false positives on numeric display values.

## Key Files

- `GumCommon/Localization/ILocalizationService.cs` — interface (`CurrentLanguage`, `Languages`, `AddDatabase`, `Clear`, `Translate`)
- `GumCommon/Localization/LocalizationService.cs` — default implementation
- `GumCommon/Localization/LocalizationServiceExtensions.cs` — CSV/RESX loaders
- `Gum/Wireframe/CustomSetPropertyOnRenderable.cs` — static `LocalizationService` property and translation logic in `TrySetPropertyOnText`
- `Gum/Commands/FileCommands.cs` — `LoadLocalizationFile()` (CSV/RESX branch, `LocalizationLoaded` event)
- `Gum/Commands/IFileCommands.cs` — `LocalizationLoaded` event declaration
- `Gum/Managers/FileChangeReactionLogic.cs` — `IsLocalizationFileThatShouldTriggerReload()` (satellite matching)
- `Gum/Plugins/InternalPlugins/ProjectPropertiesWindowPlugin/` — Language dropdown UI
- `MonoGameGum/GueDeriving/TextRuntime.cs` — `Text` property and `SetTextNoTranslate` method
- `MonoGameGum/Forms/Controls/` — Forms control localization pattern
- `MonoGameGum.Tests/Localization/LocalizationServiceExtensionsTests.cs` — CSV/RESX loader tests
- `MonoGameGum.Tests/Localization/LocalizationServiceLanguagesTests.cs` — `ILocalizationService.Languages` interface contract tests
- `Tool/Tests/GumToolUnitTests/Managers/FileChangeReactionLogicTests.cs` — satellite matching tests
