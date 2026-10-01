# Building and Releasing Gum

This page describes how to publish Gum's NuGet packages and how to cut a new release of the Gum tool. It is intended for maintainers. The two processes are independent: publishing NuGet packages does not require a tool release, and vice versa.

## Release Branches

Pull requests always target `main`. When a release is ready to stabilize, a maintainer cuts a release branch from `main`, so feature work on `main` never has to stop for a release.

1. Create the branch from `main`, named `release/<YYYY-MM>`:

    ```sh
    git fetch origin
    git push origin origin/main:refs/heads/release/2026-10
    ```
2. Tag the commit the branch was cut from, so `main`'s history shows where each release split off:

    ```sh
    git tag Branched_October_01_2026 origin/main
    git push origin Branched_October_01_2026
    ```
3. Fix bugs found during stabilization on `main` first, through a normal pull request. After it merges, cherry-pick the squashed commit onto the release branch. The `-x` flag records which `main` commit it came from:

    ```sh
    git checkout --detach origin/release/2026-10
    git cherry-pick -x <commit on main>
    git push origin HEAD:refs/heads/release/2026-10
    ```

    Pushes to `release/*` run the same CI as `main`, so the cherry-pick is built and tested on the release branch.
4. Run both the NuGet and the tool release workflows from the release branch, as described below.

Commits only move from `main` to the release branch, never back. The release tag lands on the release branch commit that was built, so once a fix has been cherry-picked, no commit on `main` matches the release exactly. The tag is the record of what shipped.

## NuGet Package Publishing

The Gum repository builds and uploads its NuGet packages through a GitHub Actions workflow. To publish new packages:

1. Open the [dotnet-nuget workflow](https://github.com/vchelaru/Gum/actions/workflows/dotnet-nuget.yaml).
2. Trigger the workflow manually (**Run workflow**).
3. Select the release branch (`release/<YYYY-MM>`) as the branch to run from, and enable both publishing checkboxes. For a NuGet-only publish outside a tool release, `main` is fine.
4. Specify the version using the format `year.month.day.build`, where `build` increments only if multiple releases occur on the same day. For preview releases, append `-preview.1`.

Reference the existing versions on [nuget.org](https://www.nuget.org/packages/Gum.MonoGame/#versions-body-tab) when choosing the next version number.

## Gum Tool Release

A tool release is several coordinated steps: generating the release notes, running the release build, and announcing it.

### 1. Generate the release notes

Run the `/gum-monthly-release` skill in Claude Code from the Gum repository. It drafts the release notes from the PRs and commits since the previous release and asks for three inputs up front:

* **Release tag**: following the pattern `Release_<Month>_<DD>_<YYYY>` (for example, `Release_May_31_2026`).
* **Previous-release boundary**: the previous release tag or URL to diff the release branch against.
* **Breaking-changes migration doc URL**: the [Upgrading](../gum-tool/upgrading/README.md) page for this release, or confirmation that there are no breaking changes.

The skill writes a draft to `temp/` and walks through any open questions with you. It only produces the notes draft. It does not bump versions, create the tag, or trigger the release workflow.

### 2. Run the release

1. Create screenshots (and GIFs) for the highlighted features.
2. Run the [Build and Release Gum Tool workflow](https://github.com/vchelaru/Gum/actions/workflows/build-and-release.yml) from the release branch with **Type** set to `test`. This builds every package into a hidden draft release. Download the build for your OS and run it.
3. If the test build works, delete the test draft and run the workflow again from the release branch with **Type** set to `release`. It tags the commit it built and builds and attaches the native Avalonia packages for Windows, macOS, and Linux (`Gum-<rid>.zip`/`.tar.xz` with their `.sha256` files, see [Setup](../gum-tool/setup/README.md)); the WPF tool no longer ships, so a failed package build fails the release.
4. Add the generated notes and screenshots to the GitHub release, replacing the list GitHub generates. Fill in the **Full Changelog** compare link once the tag exists.
5. Create or update the [migration documentation](../gum-tool/upgrading/README.md) if there are breaking changes.
6. Announce the release across the community channels: FRB Discord, MonoGame Discord, MGE Discord, Kni Discord, Twitter, Bluesky, and the MonoGame community forum.

### Hotfixes

A hotfix is an emergency tool release for a bad bug, cut outside the normal release cadence. It skips anything not required to ship the fix:

* Skip `/gum-monthly-release`, since that skill is built for monthly-scale PR volume. Write a short, plain note describing the bug and the fix instead.
* Fix the bug on `main` first, then cherry-pick the fix onto the release branch of the release you are patching, as in [Release Branches](#release-branches) step 3. Release from that branch, so the hotfix carries nothing else from `main`.
* Skip screenshots/GIFs and skip step 6 (community announcements).
* A hotfix should never carry a breaking change, so confirm that before tagging.
* Still run the Build and Release Gum Tool workflow (steps 2 and 3) and fill in the GitHub release notes and Full Changelog link.
