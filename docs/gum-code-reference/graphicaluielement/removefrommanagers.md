# RemoveFromManagers

### Introduction

RemoveFromManagers removes the calling GraphicalUiElement from the SystemManagers. This method should be called on GraphicalUiElements which need to be destroyed but do not have parents. To remove a GraphicalUiElement which has parents, remove it from its Parent's Children.
