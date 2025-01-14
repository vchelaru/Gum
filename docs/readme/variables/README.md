# Variables

## Introduction

The Variables tab displays the variables for the currently-selected element, instance, or behavior. The variables tab displays the effective value of all variables for the selected object and provides controls for changing these variables.

<figure><img src="../../.gitbook/assets/VariablesTab.png" alt=""><figcaption><p>Variables for the ExitButton instance</p></figcaption></figure>

## Default and Explicitly Set Values

Gum helps you visualize which variables are explicitly set at the current selection level. If a variable is not explicitly set, then it inherits the value from the next level down.

Standard elements explicitly set their values on their Default state, which means all values have a white background.

<figure><img src="../../.gitbook/assets/14_09 17 15.png" alt=""><figcaption><p>All values explicitly set on a a Colored Rectangle</p></figcaption></figure>

By contrast, if a ColoredRectangle is dropped into a Screen or Component, most of its variables are not explicitly set so they show up with a green background.

<figure><img src="../../.gitbook/assets/DefaultValues.png" alt=""><figcaption><p>Most variables are not explicitly set, so they have a green background</p></figcaption></figure>

If a variable is set, then its value is updated and its background changes from green to white.

<figure><img src="../../.gitbook/assets/14_09 21 28.gif" alt=""><figcaption><p>Setting values changes the background from green to white</p></figcaption></figure>
