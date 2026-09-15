# Plugin compatibility notice (per ADR-0018)

**Published 2026-09-14** on the plugin docs landing page (`docs/gum-tool/plugins/README.md`, which
is now the maintained copy). Still owed: the Breaking Changes entry in the first Avalonia
release's notes (the `gum-monthly-release` skill carries the reminder) and the Discord post.
Versions: the cutover release is the first release after September 2, 2026; the last WPF release
is Release September 2, 2026.

---

## Gum plugins and the Avalonia tool

Starting with Gum **[cutover version]**, the Gum tool runs on Avalonia and ships for Windows,
macOS, and Linux. The tool no longer uses WPF or Windows Forms, so a plugin that references those
frameworks does not load in it. The tool reports such a plugin in the Output tab with the assembly
it references, and skips it.

If you maintain a plugin outside the Gum repository, here is what to change.

### Menus

Replace `AddMenuItem`, which returned a WPF `MenuItem`, with `AddMenuEntry`, which takes the click
action first and then the menu path:

```csharp
// Before
var item = AddMenuItem(new[] { "My Plugin", "Do Thing" });
item.Click += (_, _) => DoThing();

// After
var entry = AddMenuEntry(DoThing, "My Plugin", "Do Thing");
entry.Header = "Do Thing…";   // the returned model drives the rendered item
```

`AddMenuItem` still exists in Gum **[last WPF version]** with an obsolete warning, so you can
switch before the cutover.

### Tabs

`CreateTab(object content, string title, TabLocation location)` accepts either a control the tool
can show or a ViewModel. Under Avalonia, hand it an Avalonia control or a ViewModel that your plugin
registers a view for. A WPF `UserControl` no longer works.

### Dialogs

Use the injected `IDialogService` (`ShowMessage`, `GetUserString`, `OpenFile`, `SaveFile`, and
`Show<TViewModel>()` for your own dialogs). `System.Windows.MessageBox` and WPF `Window`s do not
exist in the tool.

### Project file

Target `net10.0` rather than `net10.0-windows`, drop `<UseWPF>` and `<UseWindowsForms>`, and
reference `Gum.Presentation` instead of `Gum.csproj`. If you want the compiler to flag Windows-only
API calls before you ship, add the `Microsoft.CodeAnalysis.BannedApiAnalyzers` package and the
`BannedSymbols.CrossPlatform.txt` file from the Gum repository as an `AdditionalFiles` item, the
way `Gum/ConvertToJsonPlugin/ConvertToJsonPlugin.csproj` does.

### If you cannot migrate yet

Gum **[last WPF version]** remains available for download and keeps loading WPF plugins. It does
not receive new features after the cutover.
