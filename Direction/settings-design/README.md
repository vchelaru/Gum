# Settings window design language

How pages in Edit > Settings... are built, so one search-highlighting mechanism (#6096) covers them all.
The Code generation page (#6775) is the first page built this way. Mock: `mock-settings.html`
(`mock-a-code-generation-page.png`, `mock-b-row-kinds.png`); the shipped page is `code-generation-dark.png`
and `code-generation-light.png`.

## Page shape
- Title, one line saying where it is saved, then collapsible section bars (the ones Canvas, Fonts and Project General use). No flat lists.
- Sections group by what the user is thinking about (Output, Locations, Namespaces and usings), not by how the settings file is laid out.

## Row shape
- One setting per row, one label column (about 190px, set per member through `InstanceMember.FirstGridLength`, so other grids are untouched) so labels stay on one line.
- A one-line hint on every row (`InstanceMember.DetailText`): what it does or what it is relative to, never the label repeated.
- Row kinds: toggle, text/number, choice, path (field + browse + reveal), color, read-only (muted, hint names the source), list with Add, warning callout (`InstanceMember.IsDetailTextWarning`).
- Anything a user could get wrong (file moves, experimental) is a callout, not a hint.

## Search-ready
- A row's label and hint are its search terms (`SettingsPage.SearchTerms`), so a page declares them just by building rows this way.
- Match state (not built yet, #6096): accent bar on the row plus highlighted label and hint. Nothing is hidden.
- A section bar shows its match count and expands when it holds a match, so a match is never inside a collapsed section.
- Hand-built pages (General, Appearance, Fonts) and items-list pages (Keyboard shortcuts) get the same row shape.
