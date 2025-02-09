using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using GameUiSamples.Components;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using Microsoft.Xna.Framework;
using System;
using MonoGameGum.Forms;
using GumRuntime;
using RenderingLibrary;
namespace GameUiSamples.Screens;

partial class HollowKnightHudScreenRuntime : Gum.Wireframe.BindableGue, IUpdateScreen
{
    const int MaxHealth = 6;

    int currentHealth;
    public int CurrentHealth
    {
        get => currentHealth;
        set
        {
            currentHealth = Math.Clamp(value, 0, MaxHealth);
            UpdateCurrentHealth();
        }
    }

    partial void CustomInitialize()
    {
        this.AddManaButton.FormsControl.Click += (_, _) => ManaOrbInstance.PercentFull += 10; 
        this.SubtractManaButton.FormsControl.Click += (_, _) => ManaOrbInstance.PercentFull -= 10; 

        this.RefillHealthButton.FormsControl.Click += (_, _) =>  CurrentHealth = 6; 
        this.TakeDamageButton.FormsControl.Click += (_, _) => CurrentHealth--; 

        this.AddMoneyButton.FormsControl.Click += (_, _) => 
            CurrencyInstance.AddMoney(100);

        this.ExitButton.FormsControl.Click += (_, _) =>
        {
            Game1.Root.RemoveFromManagers();
            Game1.Root = ObjectFinder.Self.GetScreen("MainMenu")
                .ToGraphicalUiElement(SystemManagers.Default, addToManagers: true);
        };
            

        CurrentHealth = MaxHealth;
    }

    public void Update(GameTime gameTime)
    {
        this.ManaOrbInstance.Update(gameTime);
        this.CurrencyInstance.Update(gameTime);
    }
    private void UpdateCurrentHealth()
    {
        for(int i = 0; i < HealthContainer.Children.Count; i++)
        {
            var child = (HealthItemRuntime)HealthContainer.Children[i];

            var isFilled = i < CurrentHealth;

            child.FullEmptyCategoryState = isFilled 
                ? HealthItemRuntime.FullEmptyCategory.Full 
                : HealthItemRuntime.FullEmptyCategory.Empty;
        }
    }
}
