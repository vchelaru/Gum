# Common Component Types

## Introduction

This tutorial looks at some of the more common component types in Gum and how to work with them in code. This tutorial provides an introduction to common controls. For a deeper dive into each type of Forms control, see the [Gum Forms Controls](../../gum-forms/controls/) page.

If you've been following the previous tutorials, keep in mind that this tutorial begins with an empty TitleScreen. Since the previously-added buttons are being deleted, any code written must also be removed to no longer reference the buttons or you will get compile errors.



## StackPanel

StackPanels are used to contain other controls in Forms. StackPanels are similar to Containers, but they default to stacking their children top-to-bottom. StackPanels also provide access to their children as Forms, whereas Containers provide access to the visuals of their children rather than the forms object. Therefore, you should usually use a StackPanel to contain children, or a Panel if you do not want stacking.

By default StackPanels display a dotted outline in the Gum tool, but are invisible at runtime. We can add a stack pane to our screen by drag+dropping the StackPanel component into our Screen.

<figure><img src="../../../../.gitbook/assets/image (191).png" alt=""><figcaption><p>StackPanel in TitleScreen</p></figcaption></figure>

## Label

Label provides a way to display read-only strings to the user. A Label's Text property can be used to change the displayed string.

