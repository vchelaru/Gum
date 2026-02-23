---
name: bump-nuget-version
description: Bump the NuGet package versions for all 9 Gum library projects. Queries NuGet to check if a version exists for today, then sets the new version to YYYY.M.D.V where V increments from the latest published version today (or starts at 1). Creates a release branch named ReleaseCode_YYYY_M_D_V, commits the changes, and pushes. Run this before triggering the nuget release workflow.
disable-model-invocation: true
---

Invoking coder agent to bump NuGet package versions.

You are bumping the NuGet `<Version>` tag in all 9 Gum library `.csproj` files, then creating a release branch and pushing it. Follow these steps in order:

## Step 1: Get today's date

The current date is available in your system context. Use the year, month, and day as integers (no leading zeros) to form:
- The version prefix: `YYYY.M.D` (e.g., `2026.2.23`)

## Step 2: Query NuGet for today's highest V

Fetch the version list for `FlatRedBall.GumCommon` from the NuGet flat container API (package ID must be all-lowercase in the URL):

`https://api.nuget.org/v3-flatcontainer/flatredball.gumcommon/index.json`

The response has a `versions` array of strings. Filter for entries starting with `{today_prefix}.` (e.g., `2026.2.23.`). Parse the last segment of each match as an integer and find the maximum. The new version is `{today_prefix}.{max+1}`. If no versions for today exist, use `{today_prefix}.1`.

## Step 3: Create the release branch

From master, create and check out the new branch. The branch name includes the full version with underscores:
`ReleaseCode_YYYY_M_D_V` (e.g., `ReleaseCode_2026_2_23_2`)

```bash
git checkout master
git checkout -b ReleaseCode_YYYY_M_D_V
```

## Step 4: Update all 9 .csproj files

Read each file first, then use the Edit tool to replace the `<Version>...</Version>` line with the new version string.

The repo root is `C:\Users\vchel\Documents\GitHub\Gum`. The 9 files are:

1. `GumCommon\GumCommon.csproj`
2. `MonoGameGum\MonoGameGum.csproj`
3. `MonoGameGum\KniGum\KniGum.csproj`
4. `MonoGameGum\FnaGum\FnaGum.csproj`
5. `Runtimes\SkiaGum\SkiaGum.csproj`
6. `Runtimes\SkiaGum.Maui\SkiaGum.Maui.csproj`
7. `Runtimes\GumShapes\MonoGameGumShapes.csproj`
8. `Runtimes\GumShapes\KniGumShapes.csproj`
9. `Runtimes\RaylibGum\RaylibGum.csproj`

## Step 5: Commit and push the branch

Stage only the 9 csproj files and commit:

```bash
git add GumCommon/GumCommon.csproj MonoGameGum/MonoGameGum.csproj MonoGameGum/KniGum/KniGum.csproj MonoGameGum/FnaGum/FnaGum.csproj Runtimes/SkiaGum/SkiaGum.csproj Runtimes/SkiaGum.Maui/SkiaGum.Maui.csproj Runtimes/GumShapes/MonoGameGumShapes.csproj Runtimes/GumShapes/KniGumShapes.csproj Runtimes/RaylibGum/RaylibGum.csproj
```

Commit message should be `Bump version to {new_version}`.

Then push:

```bash
git push -u origin ReleaseCode_YYYY_M_D_V
```

## Step 6: Report

Print a summary:
- New version string
- Whether today already had a published version (and what the previous V was) or if this is the first release today
- Branch name

$ARGUMENTS
