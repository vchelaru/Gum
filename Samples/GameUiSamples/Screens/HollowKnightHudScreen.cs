using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using System;
using MonoGameGum;
using GameUiSamples.Components;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
namespace GameUiSamples.Screens;

partial class HollowKnightHudScreen : IUpdateScreen
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
        this.AddManaButton.Click += (_, _) => ManaOrbInstance.PercentFull += 10;
        this.SubtractManaButton.Click += (_, _) => ManaOrbInstance.PercentFull -= 10;

        this.RefillHealthButton.Click += (_, _) => CurrentHealth = 6;
        this.TakeDamageButton.Click += (_, _) => CurrentHealth--;

        this.AddMoneyButton.Click += (_, _) =>
            CurrencyInstance.AddMoney(100);

        this.ExitButton.Click += (_, _) =>
        {
            GumService.Default.Root.Children.Clear();
            var mainMenu = new MainMenu();
            mainMenu.AddToRoot();
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
        for (int i = 0; i < HealthContainer.Children.Count; i++)
        {
            var child = (HealthItem)HealthContainer.Children[i];

            var isFilled = i < CurrentHealth;

            child.FullEmptyCategoryState = isFilled
                ? HealthItem.FullEmptyCategory.Full
                : HealthItem.FullEmptyCategory.Empty;
        }
    }
}
