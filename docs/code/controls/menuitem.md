# MenuItem

## MenuItem

MenuItem is a control which is added to Menu instances, or as a child of other MenuItems. MenuItems can be created automatically or explicitly when working with a Menu.

<figure><img src="../../.gitbook/assets/13_09 30 56.gif" alt=""><figcaption><p>MenuItems in a Menu</p></figcaption></figure>

## Code Example: Creating MenuItems

The following code shows how to create menu items. Notice that MenuItems can be added to a Menu, or as children of other MenuItems.

```csharp
// Initialize
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
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACn1RsW7CMBDd8xUnT4mCUFkbOjDQFol2QIxeovqQTkpsKThFKuLfexeCfCDAy9nvvXvvbB8zALPaf_SteYXY9TgRgDxFqhv6Q0bNb91Bi76HN_B4gC_e5kVlvWDThXPbsAkhDpD1It5Rg6JaRWxVkxwHleann1g77FhmzTvj1lychd2Lf671KYVtHwWMlPb-xsNgfZWdIsaO5N6E2m3wB328F5BYnbFmFM7ws6zUfI7bhS4nziE2eam4zGEmtSwL64_WAy8ZiR7cVnjhbp-SC5RAo0KNnEaRNrE4Xa6NjuKzr9O8zlsyfu_rtL6oTHbK_gHw63H0cgIAAA" target="_blank">Try on XnaFiddle.NET</a>

## IsSelected

IsSelected controls whether the MenuItem displays its Selected state. If this value is true, the MenuItem displays its contained Items.

Typically this property is set to true by clicking on a MenuItem, or by hovering over a MenuItem after its parent is selected, but it can also be explicitly set to true.

The following code shows how to explicitly set IsSelected to true based on keyboard input:

```csharp
// Update
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
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACp1Sy2rDMBC85yuETjYUfUBLD4HErSmB0vTQgy-KtSYishT0iGmD_71axzYKmB6qi9iZ2ZnVouuKEFq6l9DSR-JtgAcEWmgPYF2E6A50KD20pJAKpuKp0jO-FdLPOB3apZZeciV_AB0u3JI2Csgz0dAR1GZ5dECMrYX4NB_G-AGqdJqSNGA5KFKevQIXYKOsoohXdHJF1qF3lupvCem8SwkpnyYgvpSQ6vNxAeEsuJ8ff4Lvg-FWRJu45z3Yi6yBbaDhQXn2NrLDcLLJJjUr3Vr5jel0XulrpUk8KR373oM7gsh2srbGmcazL81ZYXkLnbEnVupzGPwdK_Jogg6jEZ67VZZuDwpqDzgl_oM4Dor62wXKwf_Ttwvpd2v-I72nq371C1TJ1z6mAgAA" target="_blank">Try on XnaFiddle.NET</a>

By default MenuItems in a Menu are not selected unless the user clicks on the MenuItem. Once an item is selected, the user can hover over any menu item (either in the Menu itself, or a child MenuItem) to automatically expand the MenuItem.

## Handling Activation

MenuItem provides two events:

* `Clicked` fires when the user clicks a MenuItem. Use this to respond when the user picks a menu item.
* `Selected` fires when `IsSelected` becomes true, which also opens the MenuItem's submenu. This is useful for reacting to submenu expansion rather than user activation.

For leaf items (items with no children), `Clicked` is typically the event you want.

The following code shows how to handle `Clicked` on leaf MenuItems to update a label.

```csharp
// Initialize
var statusLabel = new Label();
statusLabel.Y = 40;
statusLabel.AddToRoot();
statusLabel.Text = "Select a menu item...";

var menu = new Menu();
menu.AddToRoot();

var fileMenuItem = new MenuItem();
fileMenuItem.Header = "File";
menu.Items.Add(fileMenuItem);

var newItem = new MenuItem();
newItem.Header = "New";
newItem.Clicked += (_, _) => statusLabel.Text = "New clicked";
fileMenuItem.Items.Add(newItem);

var openItem = new MenuItem();
openItem.Header = "Open";
openItem.Clicked += (_, _) => statusLabel.Text = "Open clicked";
fileMenuItem.Items.Add(openItem);

var exitItem = new MenuItem();
exitItem.Header = "Exit";
exitItem.Clicked += (_, _) => statusLabel.Text = "Exit clicked";
fileMenuItem.Items.Add(exitItem);
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACo2QT0vDQBDF74V-h2FPCZbFgydLBClVC_4B7UUIlDUZYXGzkWRji-J3d3bSNGs1kNvmvTfv_cjXdAIgVvV1U4hzcFWDM1a01U4roz-RZPGhKqidck19q17QQAIWt8DvKJ6nNvDkM7lnp0fiZZ6vy8eydH_ia9w5ukjFExrMHCgo0DagHRZSylRQPLV-n-V2-I6eXOS1o-42_KoN-tSKaoIj_8mp0Jc3qHKsGOKKdN7kZu_Wvj8K8_0K1Q4N7K2w-x63XN1ZC6OzN8zhJIFoM4NNDMkF_Ptr6BKyNs0Nv-h7yH1xz1e-ox0C7LyQ8IE0HjiY4xn97RjIrrqnxJ12Q5SdF1IuSeOFgzme0t-Ooeyq47mYTr5_AOKUkc0lAwAA" target="_blank">Try on XnaFiddle.NET</a>
