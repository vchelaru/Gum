# Orphaned Code Files (GUM0005)

Code generation writes `.cs` files to a folder outside your Gum project. When you delete, rename, or move an element, the files that were generated for it can be left behind. SDK-style `.csproj` files include every `.cs` file under the project folder, so a leftover file keeps compiling forever without any warning.

Gum scans for these files when you load a project, and on demand through **Content** > **Scan for Orphaned Code Files**. Anything it finds appears in the [Errors tab](../editor-tab.md) as a **GUM0005** entry with a **Delete File** button.

{% hint style="info" %}
Available in September 2026, or now if building Gum from source.
{% endhint %}

## What the Scan Reports

* **Generated code files** (`MyScreen.Generated.cs`) with no matching element. These are recreated exactly by regenerating the element, so deleting one loses nothing. The **Delete File** button moves it to the Recycle Bin immediately.
* **Custom code files** (`MyScreen.cs`) belonging to an orphaned generated file, in the same folder or in the Custom Code Folder at the matching path. These contain your own code, so Gum asks you to confirm before moving the file to the Recycle Bin.
* **Element code settings files** (`MyScreen.codsj`) alongside the element XML, holding the per-element settings shown on the **Code** tab. Gum confirms before removing these too.

## Renames, Moves and Deletes

Renaming an element, moving it to another folder in the Gum project, or changing its base type renames or moves its code files to match:

* The old `.Generated.cs` is deleted and regenerated at the new path.
* The custom `.cs` is moved, with its `namespace` and `partial class` lines rewritten to match. If a file is already at the destination, Gum asks before replacing it, and the replaced file goes to the Recycle Bin.
* The element's `.codsj` settings file moves with it.

When you delete an element, Gum removes its `.Generated.cs` and `.codsj` files. The custom `.cs` file has its own **Delete custom code file (contains your code)** checkbox, which is unchecked by default and only appears when the file holds code beyond the empty stub. Everything Gum removes goes to the Recycle Bin. Deleting several elements or a folder shows one dialog for the whole batch. Elements set to `NeverGenerate` keep their files in every case.

## Files Left at an Old Location

A code settings change (see [When a Folder Setting Changes](README.md#when-a-folder-setting-changes)), or an element you renamed outside Gum, can leave files at a path the current settings no longer use. If the element still exists, the entry offers **Migrate** instead of **Delete File**. Migrating moves the files to where the current settings put them, keeps a backup, and can be undone with **Content > Restore Last Code File Migration**.

## What the Scan Does Not Report

Detection is deliberately conservative, so it never flags a file Gum did not write:

* **Extra hand-written partial classes.** If you add your own `MyScreen.Input.cs` partial, Gum has no knowledge of it and never reports it, even after the element is deleted. Remove those files yourself.
* **Files with a Generation Behavior of `NeverGenerate`.** Those elements are hand-managed, so Gum leaves their files alone.
* **Elements whose source file is missing.** The element is still part of the project, so its code files are not orphans. That situation is reported separately as [GUM0004](../project-files/README.md).
* **Generated files written by other tools.** Gum only recognizes a `.Generated.cs` file that carries the header its own code generation writes.
* **Custom code you chose to keep.** Deleting an element removes its generated file, and leaves the custom `.cs` file whenever you leave **Delete custom code file (contains your code)** unchecked. Gum finds an orphaned custom file through its generated file, so once that generated file is gone the custom one is no longer reported. Delete it yourself if you later change your mind.

## Cleaning Up From the Command Line

`gumcli codegen <project.gumj> --prune` regenerates the project and then deletes generated files with no matching element. This is often the better option for a project already under source control: it is explicit, it handles every file at once, and the result shows up as a reviewable diff.

`--prune` never deletes custom code or `.codsj` settings files. It lists them so you can decide.
