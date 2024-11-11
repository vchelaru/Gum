# Project Properties

## Introduction

The project properties window allows you to modify properties that apply across the entire project (as opposed to a single screen or component).

To open the Project Properties tab, select Edit -> Properties.

![](<../.gitbook/assets/image (10) (1).png>)

## Canvas Width/Height

Gum allows you to change the canvas width and height of a project. This canvas width/height can both give you a sense of size when creating UIs, as well as provide a container for objects with no parents. In other words, objects that sit directly in a screen (as opposed to in another container) will be positioned and sized according to the canvas width/height.

The canvas width and height can be changed through the project properties page.

![](<../.gitbook/assets/14\_15 17 26.gif>)

## Localization

Gum supports localization using a localization CSV. Adding a localization file to your Gum project is useful since it allows previewing your layout with different languages. Some languages have text which is longer than other languages. By checking your localization in Gum you can adjust your layout so that it properly handles longer text.

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

<figure><img src="../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Selecting localization file</p></figcaption></figure>

After selecting the localization file, you can choose which language index is being displayed. Note that this is a 0-based index, with the left-most column being 0. For example, using the table above a Language Index of 1 would result in the English column being displayed.

Once you have added a localization file, Gum recognizes this and displays Text properties as an editable drop-down. You can type in a string ID, or you can use the drop-down to select from available options.&#x20;

<figure><img src="../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>The Text dropdown displaying available string IDs</p></figcaption></figure>

Localized text appears in Gum based on the selected ID. You can change the Language Index at any time to see localization applied immediately in your screens and components.

<figure><img src="../.gitbook/assets/22_12 04 32.gif" alt=""><figcaption><p>Changing Language Index updates displayed Texts immediately</p></figcaption></figure>

## Font Ranges

The Font Ranges setting controls which characters are included in default fonts. The Font Ranges value is ignored when using custom font (.fnt) files.

The default Font Ranges value is `32-126,160-255` which maps to the first page of the Bitmap font generator character set, labeled as **Latin + Latin Supplement**.

![](<../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png>)

Various websites provide a list of unicode character sets such as [https://unicode-table.com/en/blocks/](https://unicode-table.com/en/blocks/)

Additional characters can be added by modifying the Font Ranges character set. Individual characters can be added or entire ranges. For example, to add the **Latin Extended** A set, the Font Ranges value can be changed to `32-126,160-255,256-383`. Note that the last range of 256-383 could be merged with the previous to produce the following range: `32-126:160-383`. Note that the ranges are inclusive on both ends, so a range of 160-383 will include the characters 160 and 383 along with all values in between.

Changing the font ranges immediately refreshes the page. Note that all fonts are re-created so this operation can take some time.

The following animation shows the Ā character being included and excluded from the Font Range, causing it to appear and disappear in the displayed text.

![](<../.gitbook/assets/14\_16 04 36.gif>)

Note that expanding the character set results in larger font PNG files which can impact the size and performance of games using the Gum files.&#x20;
