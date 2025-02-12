using GameUiSamples.Components.FrbClickerComponents;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
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
    public FrbClickerCodeOnly() : base(new InvisibleRenderable())
    {
        this.Width = 0;
        this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        this.Height = 0;
        this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        this.BindingContext = new FrbScreenViewModel();

        CreateLeftColumn();

        CreateRightColumn();
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
        leftPanel.AddChild(label);

        var perSecondLabel = new Label();
        perSecondLabel.SetBinding(nameof(Label.Text), nameof(ViewModel.EarningsPerSecondDisplay));
        leftPanel.AddChild(perSecondLabel);

        var button = new Button();
        leftPanel.AddChild(button);
        button.Text = "Render Ball";
        button.Click += (_, _) => ViewModel.DoManualClick();

    }

    private void CreateRightColumn()
    {
        var scrollViewer = new ScrollViewer();
        this.Children.Add(scrollViewer.Visual);
        scrollViewer.Visual.Width = 33;
        scrollViewer.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Percentage;
        scrollViewer.Visual.Height = 0;
        scrollViewer.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        scrollViewer.Visual.XOrigin = HorizontalAlignment.Right;
        scrollViewer.Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;

        foreach(var item in ViewModel.BuildingViewModels)
        {
            AddBuildingButton(item);
        }

        void AddBuildingButton(BuildingViewModel buildingVm)
        {
            var buildingButton = new BuildingButton();
            buildingButton.BuildingName = buildingVm.BackingData.Name;
            buildingButton.SetBinding(nameof(buildingButton.Cost), nameof(buildingVm.NextCost));
            buildingButton.SetBinding(nameof(buildingButton.Amount), nameof(buildingVm.CountDisplay));

            buildingButton.BindingContext = buildingVm;
            buildingButton.Click += (_, _) => ViewModel.TryBuy(buildingVm.BackingData);



            scrollViewer.AddChild(buildingButton.FormsControl);
        }
    }



    public void Update(GameTime gameTime)
    {
        ViewModel.Update(gameTime);
    }

}
