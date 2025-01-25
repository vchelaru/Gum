# GetAbsoluteHeight

### Introduction

Returns the height of the calling GraphicalUiElement in pixels. This can be used to get the actual height of instance even if it is using a Height Units that is not pixels.

### Code Example

The following code gets the absolute height of a Text instance:

```csharp
// Assuming TextInstance is a valid instance:
var absoluteHeight = TextInstance.GetAbsoluteHeight();
```
