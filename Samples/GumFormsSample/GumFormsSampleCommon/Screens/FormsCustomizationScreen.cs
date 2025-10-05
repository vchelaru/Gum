using Gum.Wireframe;
using GumFormsSample.Components;
using GumFormsSample.CustomForms;
using GumFormsSample.CustomRuntimes;
using MonoGameGum;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GumFormsSample.Screens;

internal class FormsCustomizationScreen : ContainerRuntime
{
    StackPanel _mainStackPanel;
    public FormsCustomizationScreen()
    {
        this.Dock(Gum.Wireframe.Dock.Fill);

        _mainStackPanel = new StackPanel();
        this.AddChild(_mainStackPanel);
        _mainStackPanel.Dock(Gum.Wireframe.Dock.Fill);
        _mainStackPanel.Spacing = 5;
        _mainStackPanel.Visual.WrapsChildren = true;

        CreateCustomButton();

        CreateListBoxWithCustomVisuals();

        CreateListBoxWithCustomUpdateToObject();

        CreateColorPicker();
    }


    private void CreateCustomButton()
    {
        var button = new Button();
        _mainStackPanel.AddChild(button.Visual);

        var category = button.Visual.Categories["ButtonCategory"];
        var highlightedState = category.States.FirstOrDefault(item => item.Name == FrameworkElement.HighlightedState);

        highlightedState.Variables.Clear();
        // Add the new color:
        highlightedState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
        {
            Name = "ButtonBackground.Color",
            Value = Microsoft.Xna.Framework.Color.Yellow
        });

        button.Width = 100;
        button.Height = 42;
        button.Text = $"My highlight is yellow";
    }

    private void CreateListBoxWithCustomVisuals()
    {
        var listBox = new ListBox();
        _mainStackPanel.AddChild(listBox.Visual);
        listBox.Width = 220;
        listBox.Height = 200;

        // assign the template before adding new list items

        // this template could be used to create list box items purely in code:

        //listBox.VisualTemplate =
        //    new MonoGameGum.Forms.VisualTemplate(() =>
        //        new CustomListBoxItemRuntime(tryCreateFormsObject: false));

        // ... or we can make a visual templte using gum-defined components:
        //listBox.VisualTemplate = new MonoGameGum.Forms.VisualTemplate(typeof(FancyListBoxItemRuntime));
        listBox.VisualTemplate = new Gum.Forms.VisualTemplate(() => new FancyListBoxItemRuntime());

        for (int i = 0; i < 20; i++)
        {
            listBox.Items.Add($"Custom ListBoxItem [{i}]");
        }
    }

    private void CreateListBoxWithCustomUpdateToObject()
    {
        var listBox = new ListBox();
        _mainStackPanel.AddChild(listBox.Visual);

        listBox.Width = 220;
        listBox.Height = 200;

        listBox.ListBoxItemFormsType = typeof(DateDisplayingListBoxItem);

        for (int i = 0; i < 20; i++)
        {
            var date = DateTime.Now.AddDays(-20 + i);
            listBox.Items.Add(date);
        }
    }

    private void CreateColorPicker()
    {
        var picker = new ColorPicker();
        _mainStackPanel.AddChild(picker);


    }
}


class DateDisplayingListBoxItem : ListBoxItem
{
    public DateDisplayingListBoxItem(InteractiveGue gue) : base(gue) { }
    public override void UpdateToObject(object o)
    {
        var date = (DateTime)o;
        coreText.RawText = date.ToString("MMM d yyyy");
    }
}
