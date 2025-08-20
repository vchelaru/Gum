# VerticalScrollBarVisibility

## Introduction

VerticalScrollBarVisibility controls the visibility behavior of the vertical ScrollBar. The available values are:

* Auto - the ScrollBar is only visible if enough items are added to require scrolling
* Hidden - the ScrollBar is never visible
* Visible - the ScrollBar is always visible

## Code Example: Setting a ScrollViewer's VerticalScrollBarVisibility

The following code sets the visibility to never show:

```csharp
ScrollViewerInstance.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
```
