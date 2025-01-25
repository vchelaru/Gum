# Variables

## Introduction

The Variables tab displays the variables for the currently-selected element, instance, or behavior. The variables tab displays the effective value of all variables for the selected object and provides controls for changing these variables.

<figure><img src="../../.gitbook/assets/VariablesTab.png" alt=""><figcaption><p>Variables for the ExitButton instance</p></figcaption></figure>

## Default and Explicitly Set Values

Gum helps you visualize which variables are explicitly set at the current selection level. If a variable is not explicitly set, then it inherits the value from the next level down.

Standard elements explicitly set their values on their Default state, which means all values have a white background. The following image shows default values for the ColoredRectangle standard element.

<figure><img src="../../.gitbook/assets/14_09 17 15.png" alt=""><figcaption><p>All values explicitly set on a a ColoredRectangle</p></figcaption></figure>

By contrast, if a ColoredRectangle is dropped into a Screen or Component, this creates an instance. Most of its variables on the instance are not explicitly set so they show up with a green background. For example, the ColoredRectangleInstance in the following image has its `Width` and `Height` variables set to `50`. This is the default value for `Width` and `Height` obtained from the ColoredRectangle standard element.

<figure><img src="../../.gitbook/assets/15_04 19 28.png" alt=""><figcaption><p>Most variables are not explicitly set, so they have a green background</p></figcaption></figure>

If a variable is set, then its value is updated and its background changes from green to white. Notice that variables can be set by typing in values, clicking on icons, dragging over the labels, or changing the value in the editor tab.

<figure><img src="../../.gitbook/assets/15_04 22 36.gif" alt=""><figcaption><p>Setting values changes the background from green to white</p></figcaption></figure>

## Setting Variables to Default

A changed variable can be restored to default.

Any change can be undone with the CTRL+Z key combination if the variable was previously set to its default value. The undo history can be viewed in the History tab which updates as changes are made or as undo's are applied.

<figure><img src="../../.gitbook/assets/15_04 27 13.gif" alt=""><figcaption><p>If a variable was previously its default value, an undo restores the default value</p></figcaption></figure>

Variables can be set to default by right-clicking on the variable in the Variables tab and selecting the **Make Default** option.

<figure><img src="../../.gitbook/assets/15_04 29 04.gif" alt=""><figcaption><p>Make Default right click option restores a variable to its default value</p></figcaption></figure>

