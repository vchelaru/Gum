using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using GameUiSamples.Components;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using System;
using ToolsUtilities;
using RenderingLibrary;
using Microsoft.Xna.Framework;
using MonoGameGum;
namespace GameUiSamples.Components
{
    partial class CurrencyRuntime : ContainerRuntime
    {
        int money = 0;
        int moneyToAdd = 0;
        TimeSpan lastAddTime;
        const int moneyToAddPerSecond = 400;
        const float secondsToPauseBeforeAdding = 0.5f;

        internal void AddMoney(int amount)
        {
            moneyToAdd += amount;
            lastAddTime = GumService.Default.GameTime.TotalGameTime;
            UpdateTexts();
        }

        partial void CustomInitialize()
        {
            UpdateTexts();
        }

        public void Update(GameTime gameTime)
        {
            if (moneyToAdd > 0 && 
                (gameTime.TotalGameTime - lastAddTime) > 
                    TimeSpan.FromSeconds(secondsToPauseBeforeAdding))
            {

                int amountToAdd = (int)(gameTime.ElapsedGameTime.TotalSeconds * moneyToAddPerSecond);

                if(amountToAdd > moneyToAdd)
                {
                    amountToAdd = moneyToAdd;
                }

                moneyToAdd-= amountToAdd;
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
}
