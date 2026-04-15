# Localization

## Introduction

Gum supports localization using CSV or RESX files. Adding a localization file to your Gum project is useful since it allows previewing your layout with different languages. Some languages have text which is longer than other languages. By adding your localization to your Gum project you can adjust your layout so that it properly handles longer text.

## Adding a Localization File

Both CSV and RESX formats are supported. Choose the format that best fits your project's existing localization pipeline.

To add a localization file:

1. Click on Edit -> Properties
2. Click the Add button
3. Select a CSV or .RESX - see below for details on each file type
4. If your project uses multiple .RESX files per language, repeat the process by adding additional .RESX files

<figure><img src="../.gitbook/assets/15_13 16 27.png" alt=""><figcaption><p>Project with 2 RESX files</p></figcaption></figure>

### CSV

Localization can be added using a CSV file. The first column in a localization file is the _string ID_, which is the key that is used to perform localization look-ups. These keys should suggest the text which is displayed, but should also have some kind of prefix or suffix to differentiate string IDs from untranslated text.

Each additional column can contain translations for a single language. The top row of the CSV is ignored by Gum, so you can add titles for the language to make your CSV more readable.

The following shows what a sample CSV might look like:

| String ID          | English | Spanish   |
| ------------------ | ------- | --------- |
| T\_OK              | OK      | OK        |
| T\_Cancel          | Cancel  | Cancelar  |
| T\_Submit          | Submit  | Entregar  |
| T\_Back            | Back    | Regresar  |
| T\_Next            | Next    | Siguiente |
| // This is ignored |         |           |

Notice the string IDs in the table above have the "T\_" prefix. This is not a requirement - you are free to use any string ID convention you choose.

Notice that comments using two forward slashes (similar to languages like C#) can be used to add comments which are ignored by Gum.

Once you have created a CSV with the desired entries, you can reference this by clicking the Add button to browse for the file in Gum.

### RESX

Localization can also be added using .NET RESX files. Point Gum to a single base resource file (e.g. `Strings.resx`). The tool automatically discovers satellite files in the same folder using the standard .NET naming convention — for example, `Strings.es.resx` and `Strings.fr.resx` are picked up alongside `Strings.resx` without any additional configuration. Satellite files are listed in alphabetical order in the Language dropdown. The base file's language is labeled **Default** in the tool.

After selecting the localization file, a **Language** dropdown appears. The dropdown lists the available languages by name — for example, "English" and "Spanish" for a CSV file, or "Default", "es", and "fr" for a RESX file. The dropdown is only shown once a localization file has been loaded. Selecting a language updates the preview immediately.

#### Multiple RESX Files

Gum supports loading multiple base RESX files into a single project. This is useful if your localization is organized across several files — for example when using [ResX Resource Manager](https://github.com/dotnet/ResXResourceManager), where strings may be split across `Strings.resx`, `Buttons.resx`, `Errors.resx`, etc., each with its own language satellites.

Use the list editor in Project Properties to add, remove, and reorder RESX files. Each entry is a base file; its satellites are auto-discovered as described above.

When multiple RESX files are loaded:

* The **Language** dropdown shows the union of languages across all files. If `Strings.resx` has an `.es.resx` satellite but `Buttons.resx` does not, "es" still appears — keys missing from a given file fall back to the string ID.
* If the same string ID appears in more than one file, the **last file in the list wins**. A warning is printed to the Output tab listing every prior source that defined the key. Collisions between a base file and its own satellite are silent (expected behavior).
* All files must be RESX. Mixing CSV and RESX — or loading multiple CSVs — is not supported; the tool reports an error in the Output tab if you try.

File changes on disk (edits, renames, or new satellites) are watched for every file in the list and trigger an automatic reload.

## Using Localization

Once you have added one or more localization files, Gum recognizes this and displays Text properties as an editable drop-down. You can type in a string ID, or you can use the drop-down to select from available options.

<figure><img src="../.gitbook/assets/14_06 20 15.png" alt=""><figcaption><p>The Text dropdown displaying available string IDs</p></figcaption></figure>

Localized text appears in Gum based on the selected ID. You can change the Language at any time to see localization applied immediately in your screens and components.

<figure><img src="../.gitbook/assets/14_06 21 49.gif" alt=""><figcaption><p>Changing Language updates displayed Texts immediately</p></figcaption></figure>

## Localization and Font Ranges

Localized games may need an extended font range. If using the Gum tool, see the [Project Properties](project-properties.md#font-ranges) page for information on font ranges.
