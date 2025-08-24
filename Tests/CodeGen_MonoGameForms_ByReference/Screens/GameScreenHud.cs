using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGenProject.Screens;

partial class GameScreenHud
{
    public int CurrentHealth
    {
        set => this.HealthLabel.Text = 
                $"Current Health: {value}";
    }

    public bool IsLowHealth
    {
        set
        {
            var textInstance = HealthLabel.GetVisual<TextRuntime>();
            textInstance.Color = value ? Microsoft.Xna.Framework.Color.Red : Microsoft.Xna.Framework.Color.White;
        }
    }

    partial void CustomInitialize()
    {


    }
}
