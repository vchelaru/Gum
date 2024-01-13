# TextRuntime

### Introduction

The TextRuntime object is used to draw strings to the screen. It supports a variety of options for rendering text including alignment, fonts, coloring, and line wrapping.

### Example

To create a TextRuntime, instantiate it and add it to the managers as shown in the following code:

```csharp
var textInstance = new TextRuntime();
textInstance.Text = "Hello world";
text.AddToManagers(SystemManagers.Default, null);
```
