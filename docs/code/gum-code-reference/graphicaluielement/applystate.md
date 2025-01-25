# ApplyState

### Introduction

ApplyState can be used to apply a state (StateSave) to a GraphicalUiElement. States can be set by direct StateSave reference, or by unqualified name. Direct StateSave assignment supports states defined in Gum or dynamically created states.

### Code Example - Setting States by Reference

```csharp
var stateToSet = setMeInCode.ElementSave.Categories
    .FirstOrDefault(item => item.Name == "RightSideCategory")
    .States.Find(item => item.Name == "Blue");
setMeInCode.ApplyState(stateToSet);
```

### Code Example - Setting States by Name

```csharp
setMeInCode.ApplyState("Green");
```

### Code Example - Creating Dynamic States

```csharp
var dynamicState = new StateSave();
dynamicState.Variables.Add(new VariableSave()
{
    Value = 300f,
    Name = "Width",
    Type = "float",
    // values can exist on a state but be "disabled"
    SetsValue = true
});
dynamicState.Variables.Add(new VariableSave()
{
    Value = 250f,
    Name = "Height",
    Type = "float",
    SetsValue = true
});
setMeInCode.ApplyState(dynamicState);
```
