# Localization

## Introduction

Gum supports localization using CSV or RESX files. Adding a localization file to your Gum project is useful since it allows previewing your layout with different languages. Some languages have text which is longer than other languages. By adding your localization to your Gum project you can adjust your layout so that it properly handles longer text.

## Adding a Localization File

Both CSV and RESX formats are supported. Choose the format that best fits your project's existing localization pipeline.

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

Once you have created a CSV with the desired entries, you can reference this by clicking the ... button to browse for the file in Gum.

### RESX

Localization can also be added using .NET RESX files. Point Gum to a single base resource file (e.g. `Strings.resx`). The tool automatically discovers satellite files in the same folder using the standard .NET naming convention — for example, `Strings.es.resx` and `Strings.fr.resx` are picked up alongside `Strings.resx` without any additional configuration. Satellite files are listed in alphabetical order in the Language dropdown. The base file's language is labeled **Default** in the tool.

<figure><img src="../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Selecting localization file</p></figcaption></figure>

After selecting the localization file, a **Language** dropdown appears. The dropdown lists the available languages by name — for example, "English" and "Spanish" for a CSV file, or "Default", "es", and "fr" for a RESX file. The dropdown is only shown once a localization file has been loaded. Selecting a language updates the preview immediately.

Once you have added a localization file, Gum recognizes this and displays Text properties as an editable drop-down. You can type in a string ID, or you can use the drop-down to select from available options.

<figure><img src="../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>The Text dropdown displaying available string IDs</p></figcaption></figure>

Localized text appears in Gum based on the selected ID. You can change the Language at any time to see localization applied immediately in your screens and components.

<figure><img src="../.gitbook/assets/22_12 04 32 (1) (1).gif" alt=""><figcaption><p>Changing Language updates displayed Texts immediately</p></figcaption></figure>

## Localization and Font Ranges

Localized games may need an extended font range. If using the Gum tool, see the [Project Properties](project-properties.md#font-ranges) page for information on font ranges.
