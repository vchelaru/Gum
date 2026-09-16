# Upgrading

## Introduction

The Gum tool and Gum NuGet packages are updated frequently. Most updates to both require no changes in your project. This page explains how to upgrade. See the subsections for upgrading between different versions to make sure you can upgrade safely.

## How to Upgrade

If you are upgrading to a new version, you should read the upgrade documentation for all versions up to the version you are upgrading. For example, if you are upgrading from October 2025 to January 2026, then you should read the following documents:

* November 2025
* December 2025
* January 2025

Of course, you do not need to actually upgrade to each individual month along the way, but each month may introduce changes which are important for your project.

## Upgrading the Gum Tool

The Gum tool is distributed as an archive (a `.zip` on Windows, a `.tar.xz` on macOS and Linux) which is not installed on your machine. Therefore, to upgrade Gum, you need to delete the folder that contains the extracted files, download the latest archive for your operating system, and extract it. The archives and the per-OS steps are on the [Setup](../setup/) page.

Releases from September 2026 on are the native cross-platform tool (`Gum.Avalonia.exe` on Windows, `Gum.app` on macOS, `Gum.Avalonia` on Linux). Releases up to September 2, 2026 were the Windows-only WPF tool (`Gum.zip` with `Gum.exe`). Both open the same project files, save the same files, and generate the same code, so upgrading from one to the other needs no project changes; only plugins written against the WPF tool need to be migrated (see [Plugins](../plugins/README.md)).

The latest releases, including pre-releases, can be found on the [GitHub Releases](https://github.com/vchelaru/Gum/releases) page. All previous releases can be found on the GitHub Releases page in case you need to run an old version.

## Upgrading NuGet Packages

Gum frequently upgrades the NuGet packages with enhancements and bug fixes. To upgrade your project, upgrade the latest NuGet package through Visual Studio or the command line.
