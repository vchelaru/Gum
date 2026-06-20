# Building and Releasing Gum

This page describes how to publish Gum's NuGet packages and how to cut a new release of the Gum tool. It is intended for maintainers. The two processes are independent — publishing NuGet packages does not require a tool release, and vice versa.

## NuGet Package Publishing

The Gum repository builds and uploads its NuGet packages through a GitHub Actions workflow. To publish new packages:

1. Open the [dotnet-nuget workflow](https://github.com/vchelaru/Gum/actions/workflows/dotnet-nuget.yaml).
2. Trigger the workflow manually (**Run workflow**).
3. Select the target branch (typically `main`) and enable both publishing checkboxes.
4. Specify the version using the format `year.month.day.build`, where `build` increments only if multiple releases occur on the same day. For preview releases, append `-preview.1`.

Reference the existing versions on [nuget.org](https://www.nuget.org/packages/Gum.MonoGame/#versions-body-tab) when choosing the next version number.

## Gum Tool Release

A tool release is several coordinated steps: generating the release notes, running the release build, and announcing it.

### 1. Generate the release notes

Run the `/gum-monthly-release` skill in Claude Code from the Gum repository. It drafts the release notes from the PRs and commits since the previous release and asks for three inputs up front:

* **Release tag** — following the pattern `Release_<Month>_<DD>_<YYYY>` (for example, `Release_May_31_2026`).
* **Previous-release boundary** — the previous release tag or URL to diff `main` against.
* **Breaking-changes migration doc URL** — the [Upgrading](../gum-tool/upgrading/README.md) page for this release, or confirmation that there are no breaking changes.

The skill writes a draft to `temp/` and walks through any open questions with you. It only produces the notes draft — it does not bump versions, create the tag, or trigger the release workflow.

### 2. Run the release

1. Create screenshots (and GIFs) for the highlighted features.
2. Run the [Build and Release Gum Tool workflow](https://github.com/vchelaru/Gum/actions/workflows/build-and-release.yml) with full release settings.
3. Add the generated notes and screenshots to the GitHub release. Fill in the **Full Changelog** compare link once the tag exists.
4. Create or update the [migration documentation](../gum-tool/upgrading/README.md) if there are breaking changes.
5. Announce the release across the community channels: FRB Discord, MonoGame Discord, MGE Discord, Kni Discord, Twitter, Bluesky, and the MonoGame community forum.
