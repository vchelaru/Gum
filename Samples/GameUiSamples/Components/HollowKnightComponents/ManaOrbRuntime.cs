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
namespace GameUiSamples.Components;

partial class ManaOrbRuntime : ContainerRuntime
{
    float offset;
    private float percentFull;

    public float PercentFull
    {
        get => percentFull;
        set
        {
            percentFull = Math.Clamp(value, 0, 100);
            UpdateToPercentFull();
        }
    }

    const float wavePixelsPerSecond = 35;

    partial void CustomInitialize()
    {
        PercentFull = 50;
    }
    internal void Update(GameTime gameTime)
    {
        offset -= (float)(wavePixelsPerSecond * gameTime.ElapsedGameTime.TotalSeconds);

        if(offset < -WaveTop.Texture.Width)
        {
            offset += WaveTop.Texture.Width;
        }

        WaveTop.X = offset;
    }

    private void UpdateToPercentFull()
    {
        var fullState = this.ElementSave.AllStates.First(item => item.Name == "Full");
        var emptyState = this.ElementSave.AllStates.First(item => item.Name == "Empty");
        this.InterpolateBetween(emptyState, fullState, PercentFull / 100f);
    }
}
