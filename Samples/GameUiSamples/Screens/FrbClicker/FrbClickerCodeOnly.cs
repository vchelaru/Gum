using GameUiSamples.Components.FrbClickerComponents;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameUiSamples.Screens.FrbClicker;
public class FrbClickerCodeOnly : BindableGue, IUpdateScreen
{
    FrbScreenViewModel ViewModel => (FrbScreenViewModel)BindingContext;
    Button ManualClickButton;
    ToolTip toolTip = new ToolTip();
    BallButtonRuntime BallButton;

    public FrbClickerCodeOnly() : base(new InvisibleRenderable())
    {
        this.Width = 0;
        this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        this.Height = 0;
        this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        this.BindingContext = new FrbScreenViewModel();

        CreateLeftColumn();

        CreateRightColumn();

        toolTip = new ToolTip();
        FrameworkElement.PopupRoot.Children.Add(toolTip);
    }

    private void CreateLeftColumn()
    {
        var leftPanel = new StackPanel();

        this.Children.Add(leftPanel.Visual);
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

    private void CreateRightColumn()
    {
        var scrollViewer = new ScrollViewer();
        this.Children.Add(scrollViewer.Visual);

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

        if(FormsUtilities.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Up))
        {
            int m = 3;
        }

        if(windowOver == BallButton)
        {
            toolTip.Visible = true;
            toolTip.Text = "Click to render a red ball manually";
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
