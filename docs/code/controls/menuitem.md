# MenuItem

## MenuItem

MenuItem is a control which is added to Menu instances, or as a child of other MenuItems. MenuItems can be created automatically or explicitly when working with a Menu.

<figure><img src="../../.gitbook/assets/13_09 30 56.gif" alt=""><figcaption><p>MenuItems in a Menu</p></figcaption></figure>

## Code Example: Creating MenuItems

The following code shows how to create menu items. Notice that MenuItems can be added to a Menu, or as children of other MenuItems.

```csharp
var menu = new Menu();
menu.AddToRoot();

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

## IsSelected

IsSelected controls whether the MenuItem displays its Selected state. If this value is true, the MenuItem displays its contained Items.

Typically this property is set to true by clicking on a MenuItem, or by hovering over a MenuItem after its parent is selected, but it can also be explicitly set to true.

The following code shows how to explicitly set IsSelected to true based on keyboard input:

```csharp
var keyboard = GumService.Default.Keyboard;

if(keyboard.IsAltDown)
{
    if(keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.F))
    {
        FileMenuItem.IsSelected = true;
    }
    else if(keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.E))
    {
        EditMenuItem.IsSelected = true;
    }
}

```

By default MenuItems in a Menu are not selected unless the user clicks on the MenuItem. Once an item is selected, the user can hover over any menu item (either in the Menu itself, or a child MenuItem) to automatically expand the MenuItem.
