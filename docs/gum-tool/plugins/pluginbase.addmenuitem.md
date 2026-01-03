# AddMenuItem

## Introduction

The AddMenuItem function allows adding menu items to Gum. The AddMenuItem accepts an enumerable of strings which allow you to embed menu items under a root menu item.

## Code Example

The following shows how to create a menu item called "My Plugin" which contains two items: First and Second. Clicking each item results in a message box appearing. Add the following to your plugin's **StartUp** function.

```
// Add startup logic here:
var firstMenuItem = 
    AddMenuItem(new List<string> { "My Plugin", "First" });

firstMenuItem.Click += (args, sender) => 
    System.Windows.Forms.MessageBox.Show("You clicked first");

var secondMenuItem =
    AddMenuItem(new List<string> { "My Plugin", "Second" });

secondMenuItem.Click += (args, sender) =>
    System.Windows.Forms.MessageBox.Show("You clicked second");
```

![](<../../.gitbook/assets/BeforeClickingFirstGum (1).png>)
