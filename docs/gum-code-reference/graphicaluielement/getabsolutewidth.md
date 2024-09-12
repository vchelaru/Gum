# GetAbsoluteWidth

### Introduction

Returns the width of the calling GraphicalUiElement in pixels. This can be used to get the actual width of instance even if it is using a Width Units that is not pixels.

### Code Example

The following code gets the absolute width of a Text instance:

```csharp
// Assuming TextInstance is a valid instance:
var absoluteWidth = TextInstance.GetAbsoluteWidth();
```
