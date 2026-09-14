# Save-parity corpus

Projects that `ProjectSaveParityTests` loads and saves again, byte for byte, under several cultures
(`de-DE`, `en-US`, `tr-TR`) and on every OS CI runs. Both tool heads (WPF and Avalonia) save through
the same data-model layer (`GumProjectSave.Save`), so this corpus is the baseline both are held to;
see `Direction/avalonia-migration/phase-100-testing-and-parity.md`.

| Corpus | Source | Covers |
|---|---|---|
| `FormsXml` | `Tests/CodeGen_MonoGameForms_ByReference` (Gum data files only) | a version-1 XML project: 58 Forms components, states and categories, 30 behaviors, screens, standards |
| `SkiaXml` | `Tests/CodeGen_Skia_ByReference` | a current-version XML project in the compact (attribute) format |
| `SkiaJson` | `SkiaXml` converted with `gumcli convert-to-json` | the JSON project format (ADR-0013) |

Only the files the save touches are here (`.gumx/.gumj`, `.gusx/.gusj`, `.gucx/.gucj`,
`.gutx/.gutj`, `.behx`); images, fonts, animations and generated code are not.

Line endings follow the checkout (`.gitattributes` is `* text=auto`), and the serializers write the
platform's newline, so the comparison holds on Windows, macOS and Linux alike.

## Updating the baselines

Do this only when a change to the saved format is intended, and review the resulting diff as part of
that change:

```
GUM_UPDATE_PARITY_BASELINES=1 dotnet test Tests/Gum.ProjectServices.Tests --filter ProjectSaveParityTests
git diff Tests/Gum.ProjectServices.Tests/ParityCorpus
```

With the variable set, the test writes what the current code saves back over the corpus instead of
comparing. A failing parity test is otherwise a regression to fix, not a baseline to regenerate.
