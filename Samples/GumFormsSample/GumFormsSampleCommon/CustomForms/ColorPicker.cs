using Gum.Forms.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.CustomForms;
public class ColorPicker : StackPanel
{
    ColorComponentSlider _redSlider;
    ColorComponentSlider _greenSlider;
    ColorComponentSlider _blueSlider;

    int _red;
    public int Red
    {
        get => _red;
        set
        {
            _red = value;
            _redSlider.Value = _red;
            _greenSlider.ForcedRed = _red;
            _blueSlider.ForcedRed = _red;
        }
    }

    int _green;
    public int Green
    {
        get => _green;
        set
        {
            _green = value;
            _greenSlider.Value = _green;
            _blueSlider.ForcedGreen = _green;
            _redSlider.ForcedGreen = _green;
        }
    }

    int _blue;
    public int Blue
    {
        get => _blue;
        set
        {
            _blue = value;
            _blueSlider.Value = _blue;
            _redSlider.ForcedBlue = _blue;
            _greenSlider.ForcedBlue = _blue;
        }
    }

    public ColorPicker()
    {
        Spacing = 2;
        _redSlider = new ColorComponentSlider();
        _redSlider.ValueChanged += (_, _) => Red = (int)_redSlider.Value;
        this.AddChild(_redSlider);

        _greenSlider = new ColorComponentSlider();
        _greenSlider.ValueChanged += (_, _) => Green = (int)_greenSlider.Value;
        this.AddChild(_greenSlider);

        _blueSlider = new ColorComponentSlider();
        _blueSlider.ValueChanged += (_, _) => Blue = (int)_blueSlider.Value;
        this.AddChild(_blueSlider);

        _redSlider.Minimum = 0;
        _redSlider.Maximum = 255;
        _redSlider.TicksFrequency = 1;
        _redSlider.IsSnapToTickEnabled = true;

        _greenSlider.Minimum = 0;
        _greenSlider.Maximum = 255;
        _greenSlider.TicksFrequency = 1;
        _greenSlider.IsSnapToTickEnabled = true;

        _blueSlider.Minimum = 0;
        _blueSlider.Maximum = 255;
        _blueSlider.TicksFrequency = 1;
        _blueSlider.IsSnapToTickEnabled = true;


        Red = 0;
        Green = 0;
        Blue = 0;
    }
}
