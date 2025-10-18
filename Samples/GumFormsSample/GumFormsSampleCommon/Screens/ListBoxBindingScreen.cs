using Gum.Mvvm;
using Gum.Wireframe;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.Screens;
//internal class ListBoxBindingScreen : InteractiveGue
internal class ListBoxBindingScreen : ContainerRuntime
{
    public ListBoxBindingScreen()
    {
        this.ExposeChildrenEvents = true;

        this.Width = 0;
        this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        this.Height = 0;
        this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;


        var listBox = new ListBox();

        var listBoxVisual = listBox.Visual;

        listBoxVisual.X = 50;
        listBoxVisual.Y = 50;

        listBoxVisual.Width = 350;
        listBoxVisual.Height = 250;

        this.Children.Add(listBoxVisual);

        //listBox.VisualTemplate =
        //    new MonoGameGum.Forms.VisualTemplate((vm) =>
        //    {
        //        return new ListBoxRuntime();
        //    }
        //    );

        listBox.VisualTemplate = new Gum.Forms.VisualTemplate(typeof(BoundedListBoxItemRuntime));

        var viewModel = new ListBoxBindingViewModel();
        this.BindingContext = viewModel;
        listBox.SetBinding(nameof(ListBox.Items), nameof(ListBoxBindingViewModel.Items));

        for (int i = 0; i < 10; i++)
        {
            var itemVm = new SaveGameViewModel();

            itemVm.Name = "Save game " + i;
            itemVm.LastPlayed = DateTime.Now.AddHours(-1);
            itemVm.PlayerLevel = i * 10;

            viewModel.Items.Add(itemVm);
        }


        var button = new Button();
        var buttonVisual = button.Visual;
        button.X = 400;
        button.Text = "Modify Profile";
        button.Click += (_, _) =>
        {
            var saveGame = viewModel.Items[0];
            saveGame.Name = "Modified";
            saveGame.LastPlayed = DateTime.Now;
        };
        this.Children.Add(buttonVisual);

        var deleteProfileButton = new Button();

        deleteProfileButton.X = 400;
        deleteProfileButton.Y = 30;
        deleteProfileButton.Text = "Delete first profile";
        deleteProfileButton.Click += (_, _) =>
        {
            viewModel.Items.RemoveAt(0);
        };
        this.Children.Add(deleteProfileButton.Visual);

    }
}



public class ListBoxBindingViewModel : ViewModel
{
    public ObservableCollection<SaveGameViewModel> Items
    {
        get => Get<ObservableCollection<SaveGameViewModel>>();
        set => Set(value);
    }

    public ListBoxBindingViewModel()
    {
        Items = new ObservableCollection<SaveGameViewModel>();
    }
}


public class SaveGameViewModel : ViewModel
{
    public string Name
    {
        get => Get<string>();
        set => Set(value);
    }
    public DateTime LastPlayed
    {
        get => Get<DateTime>();
        set => Set(value);
    }

    [DependsOn(nameof(Name))]
    [DependsOn(nameof(LastPlayed))]
    public string LastPlayedDate => $"{Name} last played on:{LastPlayed}";

    public int PlayerLevel
    {
        get => Get<int>();
        set => Set(value);
    }
}

public class BoundedListBoxItemRuntime : ContainerRuntime
{
    public BoundedListBoxItemRuntime()
    {
        this.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        CreateTextItem().SetBinding(nameof(TextRuntime.Text), nameof(SaveGameViewModel.Name));
        CreateTextItem().SetBinding(nameof(TextRuntime.Text), nameof(SaveGameViewModel.LastPlayedDate));
        CreateTextItem().SetBinding(nameof(TextRuntime.Text), nameof(SaveGameViewModel.PlayerLevel));

        this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.Height = 15;
    }

    private TextRuntime CreateTextItem()
    {
        var runtime = new TextRuntime();
        runtime.Text = "Hello";
        runtime.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        runtime.Width = 0;
        runtime.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        runtime.Height = 0;
        this.Children.Add(runtime);
        return runtime;
    }
}

