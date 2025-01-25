# MenuItem

## MenuItem

MenuItem is a control which is added to Menu instances, or as a child of other MenuItems. MenuItems can be created automatically or explicitly when working with a Menu.

<figure><img src="../../../../.gitbook/assets/20_15 41 52.gif" alt=""><figcaption><p>MenuItems in a Menu</p></figcaption></figure>

## Code Example - Creating MenuItems

The following code shows how to create menu items. Notice that MenuItems can be added to a Menu, or as children of other MenuItems.

```csharp
var menu = new Menu();
Root.Children.Add(menu.Visual);

var fileMenuItem = new MenuItem();
fileMenuItem.Header = "File";
menu.Items.Add(fileMenuItem);

var newItem = new MenuItem();
newItem.Header = "New";
fileMenuItem.Items.Add(newItem);

var loadRecent = new MenuItem();
loadRecent.Header = "Load Recent";
fileMenuItem.Items.Add(loadRecent);

for(int i = 0; i < 10; i++)
{
    var item = new MenuItem();
    item.Header = "File " + i;
    loadRecent.Items.Add(item);
}

var editMenuItem = new MenuItem();
editMenuItem.Header = "Edit";
menu.Items.Add(editMenuItem);
```
