# AddToManagers

### Introduction

The AddToMangers methods adds the calling GraphicalUiElement to the SystemManagers. This should be called if the GraphicalUiElement needs to be treated as a root-most object. This is automatically called if an ElementSave's ToGraphicalUiElement passes `true` for its `addToManagers` parameter.&#x20;
