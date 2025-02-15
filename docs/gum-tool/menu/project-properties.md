# Project Properties

## Introduction

The project properties window allows you to modify properties that apply across the entire project (as opposed to a single screen or component).

To open the Project Properties tab, select Edit -> Properties.

![](<../../.gitbook/assets/image (10) (1) (1).png>)

## Canvas Width/Height

Gum allows you to change the canvas width and height of a project. This canvas width/height can both give you a sense of size when creating UIs, as well as provide a container for objects with no parents. In other words, objects that sit directly in a screen (as opposed to in another container) will be positioned and sized according to the canvas width/height.

The canvas width and height can be changed through the project properties page.

![](<../../.gitbook/assets/14_15 17 26.gif>)

## Localization

Localization can be added to a Gum project through the Localization File file picker. For more information see the [Localization page](../localization.md).

## Font Ranges

The Font Ranges setting controls which characters are included in default fonts. The Font Ranges value is ignored when using custom font (.fnt) files.

The default Font Ranges value is `32-126,160-255` which maps to the first page of the Bitmap font generator character set, labeled as **Latin + Latin Supplement**.

![](<../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png>)

Various websites provide a list of unicode character sets such as [https://unicode-table.com/en/blocks/](https://unicode-table.com/en/blocks/)

Additional characters can be added by modifying the Font Ranges character set. Individual characters can be added or entire ranges. For example, to add the **Latin Extended** A set, the Font Ranges value can be changed to `32-126,160-255,256-383`. Note that the last range of 256-383 could be merged with the previous to produce the following range: `32-126:160-383`. Note that the ranges are inclusive on both ends, so a range of 160-383 will include the characters 160 and 383 along with all values in between.

Changing the font ranges immediately refreshes the page. Note that all fonts are re-created so this operation can take some time.

The following animation shows the Ā character being included and excluded from the Font Range, causing it to appear and disappear in the displayed text.

![](<../../.gitbook/assets/14_16 04 36.gif>)

Note that expanding the character set results in larger font PNG files which can impact the size and performance of games using the Gum files.
