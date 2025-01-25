# RaiseChildrenEventsOutsideOfBounds

### Introduction

RaiseChildrenEventsOutsideOfBounds determines whether InteractiveGue instances check all children for events even if the children fall outside of the bounds of the instance. This is false by default for performance reasons, but is needed if a parent has children which are intentionally placed outside of its bounds.
