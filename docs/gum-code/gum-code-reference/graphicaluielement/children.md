# Children

### Introduction

The Children collection contains the direct descend children of the GraphicalUiElement. An instance's children will report the instance as their parent.

Note that Screen GraphicalUiElements have `null` for Children. The reason is because Screen GraphicalUiElements do not have a position or size - they are merely containers for children without providing any layout information. Therefore, to access the items that a Screen contains, see the ContainedElements property.
