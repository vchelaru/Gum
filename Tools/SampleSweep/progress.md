# Sample sweep progress (#5117)

Interrupted run, 2026-09-26: the sweep was paused because the head's windows took focus. Resume with `pwsh Tools/SampleSweep/sweep.ps1 -Resume -Reference -OutRoot <same folder>` once `--exit-after` runs open off-screen. Delete this file when the sweep is complete.

- 378 elements across 12 distinct sample projects (SokolGumFromFile has the same .gumx as MonoGameGumFromFile and is skipped); 37 are byte-identical to an element already swept, 341 to run.
- Run: 309, all exit 0 with a screenshot, no flagged Output lines. Screenshots not yet read.
- gumcli MonoGame reference renders: not run yet.

## Not yet run (32)

- MVVM_GumProject: Component `Elements/DividerHorizontal`
- MVVM_GumProject: Component `Elements/DividerVertical`
- MVVM_GumProject: Component `Elements/Icon`
- MVVM_GumProject: Component `Elements/Label`
- MVVM_GumProject: Component `Elements/PercentBar`
- MVVM_GumProject: Component `Elements/PercentBarIcon`
- MVVM_GumProject: Component `Elements/VerticalLines`
- MVVM_GumProject: Standard `Circle`
- MVVM_GumProject: Standard `ColoredRectangle`
- MVVM_GumProject: Standard `Component`
- MVVM_GumProject: Standard `Container`
- MVVM_GumProject: Standard `NineSlice`
- MVVM_GumProject: Standard `Polygon`
- MVVM_GumProject: Standard `Rectangle`
- MVVM_GumProject: Standard `Sprite`
- MVVM_GumProject: Standard `Text`
- SilkNetGum_SilkNetGumSample_GumProject: Screen `GeneralScreen`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `Arc`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `Canvas`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `Circle`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `ColoredCircle`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `ColoredRectangle`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `Component`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `Container`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `LottieAnimation`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `NineSlice`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `Polygon`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `Rectangle`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `RoundedRectangle`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `Sprite`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `Svg`
- SilkNetGum_SilkNetGumSample_GumProject: Standard `Text`

## Done (309)

- FnaGum_FnaSample_GumProject: 51
- GameUiSamples_GumProject: 80
- GumFormsSample_MonoGameGumFormsSample_FormsGumProject: 27
- GumFromZipFile_GumProject: 10
- KniGumFromFile_KniGumFromFileContent: 21
- MauiSkiaGum_GumProject: 16
- MonoGameGumCodeGeneration_GumProject: 6
- MonoGameGumFromFile_MonoGameGumFromFile: 23
- MonoGameGumFromFile_MonoGameGumFromFileAndroid: 21
- MonoGameGumFromFile_MonoGameGumFromFileDX: 20
- MVVM_GumProject: 34

