# Items Binding (ListBox, ComboBox, ItemsControl)

## Introduction

Some controls support the dynamic creation of children when their Items property is bound. These include `ListBox`, `ComboBox`, and `ItemsControl`. This page discusses how to dynamically create these items using the Items property.

## Items Binding Concepts

Controls which have an Items property automatically create children when an object is added to the Items property. Adding items can be explicitly performed by calling `Items.Add` or can be performed by binding to an observable collection and changing that collection.

Although binding can be performed to any collection, `ObservableCollection` is commonly used so that the collection can change dynamically, automatically updating the displayed controls. All operations on `ObservableCollection` are supported including adding, removing, clearing, and reordering.

Binding can be performed to any type which implements `INotifyCollectionChanged`, so developers familiar with MVVM can use any view model implementation. This document uses Gum's `ViewModel` for simplicity.

## ItemsControl Binding

`ItemsControl` is similar to `ListBox`, but it does not have any restrictions on visual types since `ItemControl` does not support the concept of selection. This makes `ItemsControl` suitable for general usage.

The following code shows how to bind an `ItemsControl` to a `ViewModel`'s `ObservableCollection`:

```csharp
public class ExampleViewModel : ViewModel
{
    public ObservableCollection<DateTime> Dates
    {
        get;
        private set;
    } = new ObservableCollection<DateTime>();
}

//------------------------------------------

var viewModel = new ExampleViewModel();

var stackPanel = new StackPanel();
stackPanel.AddToRoot();
stackPanel.Anchor(Anchor.Center);

var itemsControl = new ItemsControl();
stackPanel.AddChild(itemsControl);
itemsControl.Width = 200;
itemsControl.BindingContext = viewModel;
itemsControl.SetBinding(
    nameof(itemsControl.Items),
    nameof(viewModel.Dates));

var addButton = new Button();
stackPanel.AddChild(addButton);
addButton.Text = "Add Date";
addButton.Click += (_, _) =>
{
    viewModel.Dates.Add(DateTime.Now);
};
```

<figure><img src="../../.gitbook/assets/19_06 21 40.gif" alt=""><figcaption><p>Items added through binding</p></figcaption></figure>

The `FrameworkElementTemplate` template can be modified to support creating custom FrameworkElement types as shown in the following code block:

<pre class="language-csharp"><code class="lang-csharp">public class ExampleViewModel : ViewModel
{
    public ObservableCollection&#x3C;DateTime> Dates
    {
        get;
        private set;
    } = new ObservableCollection&#x3C;DateTime>();
}

//------------------------------------------

var viewModel = new ExampleViewModel();

var stackPanel = new StackPanel();
stackPanel.AddToRoot();
stackPanel.Anchor(Anchor.Center);

var itemsControl = new ItemsControl();
stackPanel.AddChild(itemsControl);
itemsControl.Width = 200;
itemsControl.BindingContext = viewModel;
itemsControl.SetBinding(
    nameof(itemsControl.Items),
    nameof(viewModel.Dates));

<strong>itemsControl.FrameworkElementTemplate = 
</strong><strong>    new Gum.Forms.FrameworkElementTemplate(typeof(Button));
</strong>
var addButton = new Button();
stackPanel.AddChild(addButton);
addButton.Text = "Add Date";
addButton.Click += (_, _) =>
{
    viewModel.Dates.Add(DateTime.Now);
};

</code></pre>

<figure><img src="../../.gitbook/assets/19_06 55 10.gif" alt=""><figcaption></figcaption></figure>

Each item created through the `FrameworkElementTemplate` is bound to a corresponding item in the Items collection. This means that each `FrameworkElement` can be further customized through its own binding.

For example, we can create a top-level view model which contains a collection of weapons. Each weapon is displayed with a custom button.

The following code shows the two view models:

```csharp
// This is the top-level ViewModel for the example.
public class TopLevelViewModel : ViewModel
{
    public ObservableCollection<WeaponViewModel> Weapons
    {
        get;
        private set;
    } = new ObservableCollection<WeaponViewModel>();
}

// This is the view model representing a single weapon.
// Each instance of WeaponViewModel results in an item
// added to the ItemsControl.
public class WeaponViewModel : ViewModel
{
    public string WeaponName
    {
        get => Get<string>();
        set => Set(value);
    }
    public string WeaponDetails
    {
        get => Get<string>();
        set => Set(value);
    }
}
```

Each `WeaponViewModel` instance is displayed with a new Button instance called `ButtonWithSubtext` as shown in the following code block:

```csharp
// A special button which includes text and subtext
public class ButtonWithSubtext : Button
{
    public ButtonWithSubtext()
    {
        // Existing properties can be bound...
        this.SetBinding(
            nameof(this.Text),
            nameof(WeaponViewModel.WeaponName));

        Visual.Dock(Gum.Wireframe.Dock.FillHorizontally);

        var mainText = ((ButtonVisual)Visual).TextInstance;
        mainText.Dock(Gum.Wireframe.Dock.Top);

        // ... or new visuals with their own bound properties
        // can also be added
        var subText = new TextRuntime();
        subText.Color = new Color(220,220,200);
        this.AddChild(subText);
        subText.Dock(Gum.Wireframe.Dock.Top);
        subText.Y = 20;
        subText.SetBinding(
            nameof(subText.Text),
            nameof(WeaponViewModel.WeaponDetails));
    }
}
```

Finally, these view models and custom button can be used to display weapons in an `ItemsControl` as shown in the following code block:

```csharp
var viewModel = new TopLevelViewModel();

var stackPanel = new StackPanel();
stackPanel.AddToRoot();
stackPanel.Anchor(Anchor.Center);

itemsControl = new ItemsControl();
stackPanel.AddChild(itemsControl);
//itemsControl.Height = 70;
itemsControl.Width = 250;
itemsControl.BindingContext = viewModel;
itemsControl.SetBinding(
    nameof(itemsControl.Items),
    nameof(viewModel.Weapons));

itemsControl.FrameworkElementTemplate = 
    new Gum.Forms.FrameworkElementTemplate(typeof(ButtonWithSubtext));


viewModel.Weapons.Add(new WeaponViewModel
{
    WeaponName = "Sword",
    WeaponDetails = "A sharp blade used for cutting."
});

viewModel.Weapons.Add(new WeaponViewModel
{
    WeaponName = "Bow",
    WeaponDetails = "A ranged weapon that shoots arrows."
});

viewModel.Weapons.Add(new WeaponViewModel
{
    WeaponName = "Staff",
    WeaponDetails = "A magical staff used for casting spells."
});

viewModel.Weapons.Add(new WeaponViewModel
{
    WeaponName = "Dagger",
    WeaponDetails = "A small, sharp blade used for stabbing."
});
```

<figure><img src="../../.gitbook/assets/19_08 26 46.gif" alt=""><figcaption><p>ItemsControl displaying weapons using a customized Button</p></figcaption></figure>

## Code Example: Binding Combo Box Items

The following example shows how to bind a ComboBox to a ViewModel which contains a list of weapons.

```csharp
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Mvvm;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using System.Collections.ObjectModel;

namespace MonoGameAndGum;

// This is the top-level ViewModel for the example.
public class TopLevelViewModel : ViewModel
{
    public ObservableCollection<WeaponViewModel> Weapons
    {
        get;
        private set;
    } = new ObservableCollection<WeaponViewModel>();
}

// This is the view model representing a single weapon.
// Each instance of WeaponViewModel results in an item
// added to the ItemsControl.
public class WeaponViewModel : ViewModel
{
    public string WeaponName
    {
        get => Get<string>();
        set => Set(value);
    }
    public string WeaponDetails
    {
        get => Get<string>();
        set => Set(value);
    }
}

// A special button which includes text and subtext
public class WeaponDisplay : ListBoxItem
{
    TextRuntime subText;

    public WeaponDisplay() : base()
    {
        Visual.Height = 10;

        var text = ((ListBoxItemVisual)this.Visual).TextInstance;
        text.Anchor(Gum.Wireframe.Anchor.TopLeft);

        // add a 2nd Text instance under the main text:
        subText = new TextRuntime();
        subText.Y = 22;
        subText.Color = new Color(220, 220, 200);
        subText.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.AddChild(subText);
    }

    public override void UpdateToObject(object o)
    {
        var asWeaponViewModel = o as WeaponViewModel;

        if(asWeaponViewModel != null)
        {
            coreText.RawText = asWeaponViewModel.WeaponName;
            subText.Text = asWeaponViewModel.WeaponDetails;
        }
    }
}



public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    GumService GumUI => GumService.Default;
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GumUI.Initialize(this, DefaultVisualsVersion.V3);

        var viewModel = new TopLevelViewModel();

        var comboBox = new ComboBox();
        comboBox.AddToRoot();
        comboBox.Anchor(Anchor.Center);
        comboBox.Width = 300;
        comboBox.BindingContext = viewModel;
        comboBox.SetBinding(
            nameof(comboBox.Items),
            nameof(viewModel.Weapons));
        comboBox.DisplayMemberPath = nameof(WeaponViewModel.WeaponName);

        comboBox.FrameworkElementTemplate =
            new Gum.Forms.FrameworkElementTemplate(typeof(WeaponDisplay));


        viewModel.Weapons.Add(new WeaponViewModel
        {
            WeaponName = "Sword",
            WeaponDetails = "A sharp blade used for cutting."
        });

        viewModel.Weapons.Add(new WeaponViewModel
        {
            WeaponName = "Bow",
            WeaponDetails = "A ranged weapon that shoots arrows."
        });

        viewModel.Weapons.Add(new WeaponViewModel
        {
            WeaponName = "Staff",
            WeaponDetails = "A magical staff used for casting spells."
        });

        viewModel.Weapons.Add(new WeaponViewModel
        {
            WeaponName = "Dagger",
            WeaponDetails = "A small, sharp blade used for stabbing."
        });

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        GumUI.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        GumUI.Draw();

        base.Draw(gameTime);
    }
}

```

<figure><img src="../../.gitbook/assets/04_22 25 18.gif" alt=""><figcaption><p>ComboBox with custom ListBoxItems and binding</p></figcaption></figure>
