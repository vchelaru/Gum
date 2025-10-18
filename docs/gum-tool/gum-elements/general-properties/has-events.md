# Has Events

## Introduction

The `Has Events` variable controls whether the selected instance supports UI-related events at runtime such as responding to a cursor click. If this value is false then events are not raised for this instance. If true, then cursor events are raised for this instance.

Usually instances of components should have this value set to true if the component can be interacted with at runtime, such as a button or text box.

This value has no affect in the Gum tool and is only used at runtime.

## Has Events and Cursor.WindowOver

If an instance has its `Has Events` value unchecked then it will not be eligible to be assigned to the Cursor's `WindowOver` property. For more information, see the [Cursor page](../../../code/gum-code-reference/cursor/).
