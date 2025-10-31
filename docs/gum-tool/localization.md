# Localization

## Introduction

Gum supports localization using a localization CSV. Adding a localization file to your Gum project is useful since it allows previewing your layout with different languages. Some languages have text which is longer than other languages. By adding your localization to your Gum project you can adjust your layout so that it properly handles longer text.

## Adding a CSV

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

<figure><img src="../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Selecting localization file</p></figcaption></figure>

After selecting the localization file, you can choose which language index is being displayed. Note that this is a 0-based index, with the left-most column being 0. For example, using the table above a Language Index of 1 would result in the English column being displayed.

Once you have added a localization file, Gum recognizes this and displays Text properties as an editable drop-down. You can type in a string ID, or you can use the drop-down to select from available options.

<figure><img src="../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>The Text dropdown displaying available string IDs</p></figcaption></figure>

Localized text appears in Gum based on the selected ID. You can change the Language Index at any time to see localization applied immediately in your screens and components.

<figure><img src="../.gitbook/assets/22_12 04 32 (1) (1).gif" alt=""><figcaption><p>Changing Language Index updates displayed Texts immediately</p></figcaption></figure>
