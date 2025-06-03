# FrameworkElement

## Introduction

FrameworkElement is the base class for all Gum Forms controls. Gum Forms controls are a collection of controls which often are used to build up UI such as [Button](../button.md), [ListBox](../listbox.md), [Label](../label.md), and [TextBox](../textbox.md). FrameworkElement provides much of the common functionality across all controls.

FrameworkElements have a Visual property which is of type InteractiveGue. FrameworkElements do not contain any visual properties, although they do provide shortcut methods for accessing some common Visual properties such as X, Y, Width, Height. They also provide access to the Anchor and Dock methods.

The FrameworkElement class is usually not directly instantiated. Rather, derived types such as Button and TextBox are created.
