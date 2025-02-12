using Gum.Mvvm;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameUiSamples.Screens.FrbClicker;
internal class FrbScreenViewModel : ViewModel
{
    public BigInteger CurrentBalls
    {
        get => Get<BigInteger>();
        set
        {
            if (Set(value))
            {
                foreach(var item in BuildingViewModels)
                {
                    item.CurrentBalls = value;
                }
            }
        }
    }

    decimal CurrentBallsDecimal;

    [DependsOn(nameof(CurrentBalls))]
    public string CurrentBallsDisplay => CurrentBalls.ToString("N0") + " FRBs";

    public List<BuildingViewModel> BuildingViewModels { get; set; } = new List<BuildingViewModel>();

    public decimal EarningsPerSecond =>
        BuildingViewModels.Sum(item => item.Earnings);

    [DependsOn(nameof(EarningsPerSecond))]
    public string EarningsPerSecondDisplay =>
        (EarningsPerSecond).ToString() + " FRBs per second";

    public event PropertyChangedEventHandler BuildingPropertyChanged;

    public FrbScreenViewModel()
    {
        foreach(var item in BuildingDefs.AllBuildings)
        {
            var innerVm = new BuildingViewModel();
            innerVm.BackingData = item;
            innerVm.PropertyChanged += HandleInnerPropertyChanged;
            BuildingViewModels.Add(innerVm);
        }
    }

    private void HandleInnerPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        BuildingPropertyChanged?.Invoke(sender, e);
    }

    public void DoManualClick()
    {
        CurrentBalls++;
    }

    public void Update(GameTime gameTime)
    {
        CurrentBallsDecimal +=
            EarningsPerSecond * (decimal)gameTime.ElapsedGameTime.TotalSeconds;

        var cookiesToAdd = (int)CurrentBallsDecimal;

        CurrentBalls += cookiesToAdd;
        CurrentBallsDecimal -= cookiesToAdd;

    }

    internal void TryBuy(BuildingDef building)
    {
        var vm= BuildingViewModels.Find(item => item.BackingData == building);
        var cost = vm.NextCost;

        var hasEnough = CurrentBalls >= cost;

        if(hasEnough)
        {
            CurrentBalls -= cost;
            vm.Count++;
            NotifyPropertyChanged(nameof(EarningsPerSecond));
        }
    }

}

#region BuildingViewModel

public class BuildingViewModel : ViewModel
{
    public BuildingDef BackingData { get; set; }

    public int Count { get => Get<int>(); set =>Set(value); }

    [DependsOn(nameof(Count))]
    public BigInteger NextCost => GetCostFor(BackingData);

    [DependsOn(nameof(Count))]    
    public string CountDisplay => "x" + Count.ToString();

    public BigInteger CurrentBalls
    {
        get => Get<BigInteger>(); 
        set => Set(value);
    }

    [DependsOn(nameof(CurrentBalls))]
    [DependsOn(nameof(NextCost))]
    public bool HasEnoughToBuy => CurrentBalls >= NextCost;

    private BigInteger GetCostFor(BuildingDef building)
    {
        var multiplier =
            System.Math.Pow((double)building.CostScaleMultiplier, Count);

        if (multiplier < 1000)
        {
            // should be fine as decimal:
            var costAsDouble = (double)building.InitialCost * multiplier;

            return (BigInteger)(costAsDouble);
        }
        else
        {
            return building.InitialCost * (BigInteger)multiplier;

        }
    }



    public decimal Earnings => Count * BackingData.EarningsPerSecond;
}

#endregion