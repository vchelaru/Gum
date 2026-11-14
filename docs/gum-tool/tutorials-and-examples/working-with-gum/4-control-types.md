# 4 - Control Types

### Introduction

By default Gum projects include typical controls for game UI including Button, CheckBox, TextBox, and ListBox. This page covers some of the most common types of controls and explains how to work with them. This page does not provide exhaustive coverage of all controls, so you are encouraged to explore and try them out in your own project.

Controls are added to your Controls folder under Components. Note that if you used a different theme, the folder names may differ, but most of the controls will be the same.

Label and CheckBox, which are used later in this series, are in the same folder.

<figure><img src="../../../.gitbook/assets/13_12_48_44.png" alt=""><figcaption><p>Controls in the Project tab</p></figcaption></figure>

### Button

The ButtonStandard control is one of the most common controls. It reacts to the cursor hovering over it and clicking it.

<figure><img src="../../../.gitbook/assets/13_12_58_49.png" alt=""><figcaption><p>ButtonStandard Component</p></figcaption></figure>

By default Buttons contain text that line wraps, and the height of a button is relative to its text. Adding additional text makes it wrap onto more lines, which grows the size of the button.

<figure><img src="../../../.gitbook/assets/13_13_06_04.png" alt=""><figcaption><p>Buttons with different text</p></figcaption></figure>

Buttons can also be disabled, which changes their appearance and makes them not respond to hover or click events.

1. Select the button instance.
2. In the **Behavior** category of the Variables tab, clear **Is Enabled**.

The button changes to its disabled appearance.

<figure><img src="../../../.gitbook/assets/13_13_07_18.png" alt=""><figcaption><p>Enabled and Disabled button</p></figcaption></figure>

### TextBox

Gum provides a fully-functional TextBox component which accepts typing, selection, copy/paste, and multi-line editing.

<figure><img src="../../../.gitbook/assets/13_13_11_59.png" alt=""><figcaption><p>TextBox in Gum</p></figcaption></figure>

TextBox-specific variables are all organized under the Behaviors category. TextBox has far more varaibles controlling its behavior compared to Button.

<figure><img src="../../../.gitbook/assets/13_13_13_19.png" alt=""><figcaption></figcaption></figure>

#### Multi-line TextBox

TextBoxes use a combination of variables to support multi-line editing. You can set any combination of these to customize how a TextBox behaves.

The Text Wrapping varaible controls whether text wraps or stays on one line. To see multi-line text easily, you may also want to adjust a TextBox's Height.

<figure><img src="../../../.gitbook/assets/13_13_15_53.png" alt=""><figcaption><p>TextBox with text wrapping</p></figcaption></figure>

You can test this behavior using Gum Preview.

<figure><img src="../../../.gitbook/assets/13_13_17_17.gif" alt=""><figcaption><p>TextBox with wrapping</p></figcaption></figure>

If Accepts Return is checked, then pressing the Enter/Return key inserts a new line.

### ListBox

ListBoxes can contain any number of ListBoxItem instances. These can be single or multi-selected. If enough items are displayed, a scroll bar appears when a game runs.

<figure><img src="../../../.gitbook/assets/13_13_21_50.png" alt=""><figcaption><p>ListBox in Gum</p></figcaption></figure>

ListBoxItem instances can be added in the tool or in game. Most of the time ListBoxes are populated in game, but Gum does support adding ListBoxItems manually.

To add items to a ListBox, drag+drop ListBoxItem instances onto the ListBox.

<figure><img src="../../../.gitbook/assets/13_13_22_48.gif" alt=""><figcaption><p>Adding items to ListBox</p></figcaption></figure>

Each item's text can be set by selecting it and changing its Text property.

<figure><img src="../../../.gitbook/assets/13_13_29_23.png" alt=""><figcaption><p>ListBoxItem with changed text</p></figcaption></figure>

If enough items are added, then the ListBox displays a scroll bar in game.

<figure><img src="../../../.gitbook/assets/13_13_30_18.png" alt=""><figcaption><p>ListBox with a scroll bar</p></figcaption></figure>

### Conclusion

This page shows how to work with some of the most common controls. As mentioned earlier, Gum has lots of controls so you'll want to try out some of the other controls in your screen and in the Preview window to get a feel for what is available.

The next tutorial discusses how to customize control appearance.
