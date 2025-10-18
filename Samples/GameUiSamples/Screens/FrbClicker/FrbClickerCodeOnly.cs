using GameUiSamples.Components.FrbClickerComponents;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Gum.Forms;
using Gum.Forms.Controls;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameUiSamples.Screens.FrbClicker;
public class FrbClickerCodeOnly : Panel, IUpdateScreen
{
    FrbScreenViewModel ViewModel => (FrbScreenViewModel)BindingContext;
    Button ManualClickButton;
    ToolTip toolTip = new ToolTip();
    BallButtonRuntime BallButton;

    List<StackPanel> imagePanels = new List<StackPanel>();

    public FrbClickerCodeOnly()
    {
        this.Visual.Width = 0;
        this.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        this.Visual.Height = 0;
        this.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        CreateViewModel();

        CreateLeftColumn();

        CreateCenterColumn();
        // uncomment to cheat:
        //ViewModel.CurrentBalls = 30000000;
        CreateRightColumn();

        toolTip = new ToolTip();
        FrameworkElement.PopupRoot.Children.Add(toolTip);
    }

    private void CreateViewModel()
    {
        var vm = new FrbScreenViewModel();
        vm.BuildingPropertyChanged += HandleBuildingPropertyChanged;
        this.BindingContext = vm;
    }

    private void HandleBuildingPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(BuildingViewModel.Count))
        {
            var senderAsVm = sender as BuildingViewModel;

            var matchingPanel = imagePanels.First(item => item.BindingContext == sender);

            while(matchingPanel.Children.Count < senderAsVm.Count)
            {
                var image = new Image();
                var relativeFile = senderAsVm.BackingData.Icon;
                image.Source = $"Components/FrbClickerComponents/{relativeFile}";
                matchingPanel.AddChild(image);
            }
        }
    }

    private void CreateLeftColumn()
    {
        var leftPanel = new StackPanel();

        this.AddChild(leftPanel);
        leftPanel.Visual.Width = 33;
        leftPanel.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Percentage;
        leftPanel.Visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
        leftPanel.Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        leftPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        leftPanel.Visual.Height = 0;

        var label = new Label();
        label.SetBinding(nameof(Label.Text), nameof(ViewModel.CurrentBallsDisplay));
        label.Visual.XOrigin = HorizontalAlignment.Center;
        label.Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        leftPanel.AddChild(label);

        var perSecondLabel = new Label();
        perSecondLabel.SetBinding(nameof(Label.Text), nameof(ViewModel.EarningsPerSecondDisplay));
        perSecondLabel.Visual.XOrigin = HorizontalAlignment.Center;
        perSecondLabel.Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        leftPanel.AddChild(perSecondLabel);

        BallButton = new BallButtonRuntime();
        leftPanel.AddChild(BallButton.FormsControl);
        BallButton.FormsControl.Click += (_, _) => ViewModel.DoManualClick();


    }

    private void CreateCenterColumn()
    {
        var centerPanel = new ScrollViewer();
        centerPanel.Visual.X = 34;
        centerPanel.Visual.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        centerPanel.Visual.Width = 32;
        centerPanel.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Percentage;
        centerPanel.Visual.Y = 4;
        centerPanel.Visual.Height = -8;
        centerPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        this.AddChild(centerPanel);

        foreach(var item in ViewModel.BuildingViewModels)
        {
            var panel = new StackPanel();
            centerPanel.AddChild(panel);
            panel.Orientation = Orientation.Horizontal;
            panel.Visual.WrapsChildren = true;
            panel.Visual.Width = 0;
            panel.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            panel.Visual.Height = 0;
            panel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            panel.BindingContext = item;
            imagePanels.Add(panel);
        }
    }

    private void CreateRightColumn()
    {
        var scrollViewer = new ScrollViewer();
        this.AddChild(scrollViewer);

        scrollViewer.Visual.Width = 33;
        scrollViewer.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Percentage;
        scrollViewer.Visual.Height = 0;
        scrollViewer.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        scrollViewer.X = 0;
        scrollViewer.Visual.XOrigin = HorizontalAlignment.Right;
        scrollViewer.Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        scrollViewer.InnerPanel.StackSpacing = 5;

        foreach (var item in ViewModel.BuildingViewModels)
        {
            AddBuildingButton(item);
        }


        void AddBuildingButton(BuildingViewModel buildingVm)
        {
            var buildingButton = new BuildingButtonRuntime();
            buildingButton.BuildingName = buildingVm.BackingData.Name;
            buildingButton.SetBinding(
                nameof(buildingButton.Cost), nameof(buildingVm.NextCost));
            buildingButton.SetBinding(
                nameof(buildingButton.Amount), nameof(buildingVm.CountDisplay));
            buildingButton.SetBinding(
                nameof(buildingButton.IsEnabled), nameof(buildingVm.HasEnoughToBuy));

            buildingButton.BindingContext = buildingVm;
            buildingButton.Click += (_, _) => ViewModel.TryBuy(buildingVm.BackingData);



            scrollViewer.AddChild(buildingButton.FormsControl);
        }
    }



    public void Update(GameTime gameTime)
    {
        ViewModel.Update(gameTime);

        UpdateToolTip();
    }

    private void UpdateToolTip()
    {
        var cursor = FormsUtilities.Cursor;
        var windowOver = cursor.WindowOver;

        if(windowOver == BallButton)
        {
            toolTip.Visible = true;
            toolTip.Text = ViewModel.ClickOverlayText;
        }
        else if(windowOver != null && 
            windowOver is BuildingButtonRuntime buildingButtonOver)
        {
            toolTip.Visible = true;
            var vm = (buildingButtonOver.BindingContext as BuildingViewModel);
            var showTooltip = vm.HasEnoughToBuy || vm.Count > 0;
            if (showTooltip)
            {
                toolTip.Text = vm.BackingData.Description;
            }
            else
            {
                toolTip.Text = "????";
            }
        }
        else
        {
            toolTip.Visible = false;
        }

        if(toolTip.Visible)
        {
            toolTip.X = cursor.X;
            toolTip.Y = cursor.Y + 16;

            if(toolTip.AbsoluteRight > GraphicalUiElement.CanvasWidth)
            {
                toolTip.X = GraphicalUiElement.CanvasWidth - toolTip.GetAbsoluteWidth();
            }
        }
    }
}
