using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.CustomForms;
public class ColorComponentSlider : Slider
{
    SpriteRuntime _gradientSprite;
    Texture2D _gradientTexture;

    int? _forcedRed;
    public int? ForcedRed 
    {
        get => _forcedRed;
        set
        {
            if (_forcedRed != value)
            {
                _forcedRed = value;
                UpdateGradient();
            }
        }
    }

    int? _forcedGreen;
    public int? ForcedGreen
    {
        get => _forcedGreen;
        set
        {
            if (_forcedGreen != value)
            {
                _forcedGreen = value;
                UpdateGradient();
            }
        }
    }

    int? _forcedBlue;
    public int? ForcedBlue
    {
        get => _forcedBlue;
        set
        {
            if (_forcedBlue != value)
            {
                _forcedBlue = value;
                UpdateGradient();
            }
        }
    }


    public ColorComponentSlider()
    {
        var topLevelContainer = new ContainerRuntime();
        topLevelContainer.Width = 250;
        topLevelContainer.Height = 32;

        var track = new ContainerRuntime();
        topLevelContainer.AddChild(track);
        track.Name = "TrackInstance";
        track.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        track.Width = -51f;
        track.Dock(Gum.Wireframe.Dock.Left);

        _gradientSprite = new SpriteRuntime();
        track.AddChild(_gradientSprite);
        _gradientSprite.Name = "GradientSprite";
        _gradientSprite.Dock(Gum.Wireframe.Dock.Fill);

        var thumbInstance = new ContainerRuntime();
        thumbInstance.Name = "ThumbInstance";
        track.AddChild(thumbInstance);
        thumbInstance.Width = 3;
        thumbInstance.Height = 4;
        thumbInstance.Anchor(Gum.Wireframe.Anchor.Bottom);
        thumbInstance.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;

        var thumbInstanceVisual = new ColoredRectangleRuntime();
        thumbInstance.AddChild(thumbInstanceVisual);
        thumbInstanceVisual.Dock(Gum.Wireframe.Dock.Fill);
        // todo - set the thumb Sprite image

        var textBox = new TextBox();
        topLevelContainer.AddChild(textBox);
        textBox.Dock(Gum.Wireframe.Dock.Right);
        textBox.Placeholder = string.Empty;
        textBox.Width = -201;
        textBox.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;

        UpdateGradient();

        // todo - if we care, add categories and states for diabled, etc
        this.Visual = topLevelContainer;

        IsMoveToPointEnabled = true;

        textBox.TextChanged += (sender, args) =>
        {
            if (float.TryParse(textBox.Text, out var value))
            {
                Value = Math.Clamp(value, this.Minimum, this.Maximum);
            }
        };

        this.ValueChanged += (_, _) =>
        {
            textBox.Text = Value.ToString("F0");
        };
        textBox.Text = Value.ToString("F0");

    }

    public void UpdateGradient()
    {
        if(_gradientTexture == null)
        {
            _gradientTexture = new Texture2D(GumService.Default.SystemManagers.Renderer.GraphicsDevice, 255, 1);
        }

        var colorData = new Color[255];
        if(ForcedRed == null)
        {
            var green = ForcedGreen ?? 0;
            var blue = ForcedBlue ?? 0;
            for (int i = 0; i < colorData.Length; i++)
            {
                colorData[i] = new Color(i, green, blue, 255); 
            }

        }
        else if(ForcedGreen == null) 
        {
            var red = ForcedRed ?? 0;
            var blue = ForcedBlue ?? 0;
            for (int i = 0; i < colorData.Length; i++)
            {
                colorData[i] = new Color(red, i, blue, 255);
            }
        }
        else if(ForcedBlue == null)
        {
            var red = ForcedRed ?? 0;
            var green = ForcedGreen ?? 0;
            for (int i = 0; i < colorData.Length; i++)
            {
                colorData[i] = new Color(red, green, i, 255);
            }
        }

        _gradientTexture.SetData(colorData);
        _gradientSprite.Texture = _gradientTexture;
    }
}
