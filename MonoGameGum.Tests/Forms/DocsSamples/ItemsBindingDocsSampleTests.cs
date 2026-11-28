// Sample code is copied from the docs as written, and the docs samples do not use nullable annotations.
#nullable disable warnings
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Mvvm;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms.DocsSamples;

/// <summary>
/// Runs the samples on docs/code/binding-viewmodels/items-binding-listbox-combobox-itemscontrol.md.
/// </summary>
public class ItemsBindingDocsSampleTests : BaseTestClass
{
    public class ExampleViewModel : ViewModel
    {
        public ObservableCollection<DateTime> Dates
        {
            get;
            private set;
        } = new ObservableCollection<DateTime>();
    }

    public class TopLevelViewModel : ViewModel
    {
        public ObservableCollection<WeaponViewModel> Weapons
        {
            get;
            private set;
        } = new ObservableCollection<WeaponViewModel>();
    }

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

    public class ButtonWithSubtext : Button
    {
        public TextRuntime SubText { get; }

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
            SubText = subText;
        }
    }

    public class WeaponDisplay : ListBoxItem
    {
        public TextRuntime SubText { get; }
        public string? MainText => coreText?.RawText;

        public WeaponDisplay() : base()
        {
            Visual.Height = 10;

            var text = ((ListBoxItemVisual)this.Visual).TextInstance;
            text.Anchor(Gum.Wireframe.Anchor.TopLeft);

            // add a 2nd Text instance under the main text:
            var subText = new TextRuntime();
            subText.Y = 22;
            subText.Color = new Color(220, 220, 200);
            subText.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;

            this.AddChild(subText);

            // Each item's BindingContext is its WeaponViewModel.
            // The main text shows WeaponName through DisplayMemberPath.
            subText.SetBinding(
                nameof(subText.Text),
                nameof(WeaponViewModel.WeaponDetails));
            SubText = subText;
        }
    }

    [Fact]
    public void ItemsControl_BoundToDates_AddsItemPerDate()
    {
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

        itemsControl.FrameworkElementTemplate =
            new Gum.Forms.FrameworkElementTemplate(typeof(Button));

        viewModel.Dates.Add(System.DateTime.Now);
        viewModel.Dates.Add(System.DateTime.Now);

        itemsControl.Items.Count.ShouldBe(2);
        itemsControl.InnerPanel.Children.Count.ShouldBe(2);
        ((InteractiveGue)itemsControl.InnerPanel.Children[0]).FormsControlAsObject.ShouldBeOfType<Button>();
    }

    [Fact]
    public void ItemsControl_ButtonWithSubtextTemplate_BindsEachItem()
    {
        var viewModel = new TopLevelViewModel();

        var stackPanel = new StackPanel();
        stackPanel.AddToRoot();

        var itemsControl = new ItemsControl();
        stackPanel.AddChild(itemsControl);
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

        var button = (ButtonWithSubtext)((InteractiveGue)itemsControl.InnerPanel.Children[0]).FormsControlAsObject;
        button.Text.ShouldBe("Sword");
        button.SubText.Text.ShouldBe("A sharp blade used for cutting.");
    }

    [Fact]
    public void ComboBox_WeaponDisplayTemplate_ShowsNameAndDetails()
    {
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

        comboBox.IsDropDownOpen = true;
        var item = comboBox.ListBox.ListBoxItems.OfType<WeaponDisplay>().Single();
        item.MainText.ShouldBe("Sword");
        item.SubText.Text.ShouldBe("A sharp blade used for cutting.");
    }
}
