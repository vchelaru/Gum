using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using Gum.GueDeriving;
using System;
using Gum;
using Microsoft.Xna.Framework;
namespace GameUiSamples.Components;

partial class Currency
{
    int money = 0;
    int moneyToAdd = 0;
    TimeSpan lastAddTime;
    TimeSpan latestTotalTime;
    const int moneyToAddPerSecond = 400;
    const float secondsToPauseBeforeAdding = 0.5f;

    internal void AddMoney(int amount)
    {
        moneyToAdd += amount;
        // The Skia GumService has no GameTime, so this tracks the time of the latest Update.
        lastAddTime = latestTotalTime;
        UpdateTexts();
    }

    partial void CustomInitialize()
    {
        UpdateTexts();
    }


    public void Update(GameTime gameTime)
    {
        latestTotalTime = gameTime.TotalGameTime;

        if (moneyToAdd > 0 &&
            (gameTime.TotalGameTime - lastAddTime) >
                TimeSpan.FromSeconds(secondsToPauseBeforeAdding))
        {

            int amountToAdd = (int)(gameTime.ElapsedGameTime.TotalSeconds * moneyToAddPerSecond);

            if (amountToAdd > moneyToAdd)
            {
                amountToAdd = moneyToAdd;
            }

            moneyToAdd -= amountToAdd;
            money += amountToAdd;

            UpdateTexts();
        }
    }

    private void UpdateTexts()
    {
        ToAddTextInstance.Visible = moneyToAdd > 0;

        ToAddTextInstance.Text = $"+{moneyToAdd:N0}";
        TotalMoneyTextInstance.Text = $"{money:N0}";
    }
}
