# Has Events

The `Has Events` variable controls whether an object responds to the cursor (mouse and touch). When checked, the object counts as what the cursor is over, so its events such as Click, RollOver, and Push are raised, and it blocks clicks to objects behind it. When unchecked, the cursor passes through the object to whatever is behind it.

`Has Events` only affects cursor input. Keyboard and gamepad input do not use it.

Leave it checked for objects the player interacts with, such as a button. Uncheck it on objects that only display something. For example, uncheck it on a HUD container that covers the screen so it does not block clicks to the game or menus behind it.

`Has Events` has no effect in the Gum tool. It is only used at runtime.
