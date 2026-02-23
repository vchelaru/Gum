---
name: bump-nuget-version
description: Bump the NuGet package versions for all 9 Gum library projects. Queries NuGet to check if a version exists for today, then sets the new version to YYYY.M.D.V where V increments from the latest published version today (or starts at 1). Creates a release branch, commits the changes, pushes, and opens a PR. Run this before triggering the nuget release workflow.
disable-model-invocation: true
---

Invoking coder agent to bump NuGet package versions and open a release PR.

You are bumping the NuGet `<Version>` tag in all 9 Gum library `.csproj` files, then creating a release branch and PR. Follow these steps in order:

## Step 1: Get today's date

Run a bash command to capture year, month, and day as integers (no leading zeros):

```bash
YEAR=$(date +%Y)
MONTH=$(( 10#$(date +%m) ))
DAY=$(( 10#$(date +%d) ))
echo "$YEAR $MONTH $DAY"
```

You will use these values to form:
- The version prefix: `YYYY.M.D` (e.g., `2026.2.23`)
- The branch name: `ReleaseCode_YYYY_M_D` (e.g., `ReleaseCode_2026_2_23`)

## Step 2: Create the release branch

From master, create and check out the new branch:

```bash
git checkout master
git checkout -b ReleaseCode_YYYY_M_D
```

## Step 3: Query NuGet for today's highest V

Fetch the version list for `FlatRedBall.GumCommon` from the NuGet flat container API (package ID must be all-lowercase in the URL):

`https://api.nuget.org/v3-flatcontainer/flatredball.gumcommon/index.json`

The response has a `versions` array of strings. Filter for entries starting with `{today_prefix}.` (e.g., `2026.2.23.`). Parse the last segment of each match as an integer and find the maximum. The new version is `{today_prefix}.{max+1}`. If no versions for today exist, use `{today_prefix}.1`.

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
git add GumCommon/GumCommon.csproj
git add MonoGameGum/MonoGameGum.csproj
git add MonoGameGum/KniGum/KniGum.csproj
git add MonoGameGum/FnaGum/FnaGum.csproj
git add Runtimes/SkiaGum/SkiaGum.csproj
git add Runtimes/SkiaGum.Maui/SkiaGum.Maui.csproj
git add Runtimes/GumShapes/MonoGameGumShapes.csproj
git add Runtimes/GumShapes/KniGumShapes.csproj
git add Runtimes/RaylibGum/RaylibGum.csproj
```

Commit message should be `Bump version to {new_version}`.

Then push:

```bash
git push -u origin ReleaseCode_YYYY_M_D
```

## Step 6: Create a PR

Use the `gh` CLI to open a PR targeting `master`:

```bash
gh pr create \
  --title "Release {new_version}" \
  --base master \
  --body "Bumps all 9 NuGet package versions to {new_version} in preparation for release. After merging, trigger the **Build and Publish Runtime NuGet Packages** workflow with publish enabled."
```

## Step 7: Report

Print a summary:
- New version string
- Whether today already had a published version (and what the previous V was) or if this is the first release today
- Branch name and PR URL

$ARGUMENTS
