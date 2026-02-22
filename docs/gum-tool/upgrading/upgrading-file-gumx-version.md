# Upgrading File (GUMX) Version

## Introduction

Gum project files include a `Version` tag which is used by the Gum tool and Gum runtimes to determine how to handle the file.

Unless otherwise stated below, these version upgrades are optional - you can continue to use old versions.

## Upgrading Versions

{% hint style="danger" %}
Upgrading your Gum version can make changes to your project which cannot be undone. Before performing any upgrades, be sure to back up your project, or start with a clean commit in Git.
{% endhint %}

To upgrade to a new version:

1. If open, close the Gum tool.
2. Open your Gum project file (.gumx) in a text editor
3.  Search for the \<Version> XML tag. For example your file may have&#x20;

    ```xml
    <Version>1</Version>
    ```
4. Change the version number to the desired version.
5. Save the .gumx file
6. Open the Gum tool

Gum should detect the new version and if any changes are necessary to your project, Gum will perform them.

If the change modifies how the file is saved, you may need to use the **File** -> **Save All** command to forcefully save all files to disk.

## Version 1

The initial file version, which was used until February 2026.

## Version 2

This version uses XML Attributes to greatly reduce the size of XML files and to make manual editing and diffing of files easier.

✅No manual changes are needed to your project beyond changing the version number.

❗Be sure that your entire team is using the new version of the Gum (February 2026 or later) before making any upgrades, and once the upgrade has been made, make sure that no one on your team is still making Gum changes on V1. This can cause a "hybrid" file which is partially V2 and partially V1, which can break a project.

After changing the version number:

1. Open the Gum project in the Gum tool
2. Select File->Save All to save all files to disk using the new attribute format.

You should notice significant reduction in file size and number of lines. The following files will be affected by this change:

* Screen files
* Component files
* StandardElementSave files
* Main Gum project file

Before:

<figure><img src="../../.gitbook/assets/22_08 35 48.png" alt=""><figcaption></figcaption></figure>

After:

<figure><img src="../../.gitbook/assets/22_08 49 01.png" alt=""><figcaption></figcaption></figure>

Note that the file savings vary by project type and by content type.

The savings are both in file size, but also in line number which makes comparing files in diffs much easier than before. Here are some typical savings in line count. These were taken from the Cranky Chibi Cthulhu game ([https://store.steampowered.com/app/2631990/Cranky\_Chibi\_Cthulhu/](https://store.steampowered.com/app/2631990/Cranky_Chibi_Cthulhu/))

* GumProject .gumx file: 620 -> 180 lines
* PauseMenu Component: 513 -> 248 lines
* GameScreenGum Screen: 798 -> 381 lines
* SplashScreenGum Screen: 63 -> 37 lines
* PlayerHud Component: 2646 -> 1309 lines
* Circle Standard Element File: 165 -> 72 lines
