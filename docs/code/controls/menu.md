# Menu

## Introduction

Menu is a top-level menu bar control that docks to the top of the screen. It inherits from [ItemsControl](itemscontrol.md) and contains [MenuItem](menuitem.md) instances.

## Code Example: Creating a Menu

The following code creates a Menu with File and Edit top-level items.

```csharp
// Initialize
var menu = new Menu();
menu.AddToRoot();

var fileMenuItem = new MenuItem();
fileMenuItem.Header = "File";
menu.Items.Add(fileMenuItem);

var editMenuItem = new MenuItem();
editMenuItem.Header = "Edit";
menu.Items.Add(editMenuItem);
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACqvm5VJQUPIsdi_NVbJSKCkqTdUBi2TmZZZkJuZkVqUChZXKEosUclPzShVsFfJSyxV8gUwNTeuYPJCYnmNKSkh-UH5-CVgoJg-kOC0zJxWkyrMkNRdJE4gLVoUsr-eRmpiSWgRUFqPkBhSPUYKZDJItBpmvgaweYUtqSmYJPluQ5ZFtcQWKY7MFWb2mtRIvVy0Ad6gyIB8BAAA" target="_blank">Try on XnaFiddle.NET</a>

For adding sub-items, handling activation events, and keyboard shortcuts, see the [MenuItem](menuitem.md) page.
