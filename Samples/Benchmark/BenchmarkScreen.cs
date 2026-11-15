#nullable enable
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;

namespace GumSamples.Benchmark;

/// <summary>
/// The one scene every runtime's benchmark draws (#8052). It has no windowing or rendering code, so any host
/// links this file. Three regions, sized for a 1280x720 window: a column of Forms controls (the kind of screen a
/// game really has), a ListBox of 200 text rows that <see cref="SetScroll"/> moves, and a grid of 100
/// NineSlice + Text pairs (the cost behind #7876/#7920). Shapes, shadows and gradients are deliberately absent.
/// </summary>
internal class BenchmarkScreen : FrameworkElement
{
    public const int ListRowCount = 200;
    public const int NineSlicePairCount = 100;

    private readonly ListBox _listBox;
    private readonly StackPanel _column;
    private readonly List<GraphicalUiElement> _pairs = new List<GraphicalUiElement>();

    public BenchmarkScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        // Left column: Forms controls.
        var column = new StackPanel();
        column.Visual.X = 8;
        column.Visual.Y = 8;
        column.Visual.Width = 380;
        column.Visual.Height = 700;
        column.Visual.StackSpacing = 6;
        _column = column;
        this.AddChild(column);

        var menu = new Menu();
        column.AddChild(menu.Visual);
        var file = new MenuItem { Header = "File" };
        file.Items!.Add(new MenuItem { Header = "New" });
        file.Items!.Add(new MenuItem { Header = "Open" });
        menu.Items!.Add(file);

        for (int i = 0; i < 6; i++)
        {
            var button = new Button { Text = "Button " + i };
            column.AddChild(button);
        }
        for (int i = 0; i < 6; i++)
        {
            var checkBox = new CheckBox { Text = "Checkbox " + i, IsChecked = i % 2 == 0 };
            checkBox.Width = 200;
            column.AddChild(checkBox);
        }
        for (int i = 0; i < 4; i++)
        {
            var slider = new Slider { Minimum = 0, Maximum = 100, Value = 20 * (i + 1) };
            slider.Width = 300;
            column.AddChild(slider);
        }
        for (int i = 0; i < 4; i++)
        {
            var textBox = new TextBox { Text = "TextBox " + i };
            textBox.Width = 300;
            column.AddChild(textBox);
        }
        var comboBox = new ComboBox();
        for (int i = 0; i < 5; i++)
        {
            comboBox.Items!.Add("Option " + i);
        }
        comboBox.Width = 200;
        column.AddChild(comboBox);
        for (int i = 0; i < 4; i++)
        {
            var label = new Label { Text = "Label " + i };
            column.AddChild(label);
        }

        // Middle: a ListBox of text rows.
        _listBox = new ListBox();
        _listBox.Visual.X = 400;
        _listBox.Visual.Y = 8;
        _listBox.Width = 300;
        _listBox.Height = 700;
        for (int i = 0; i < ListRowCount; i++)
        {
            _listBox.Items!.Add("List row " + i);
        }
        this.AddChild(_listBox);

        // Right: a grid of NineSlice + Text pairs.
        const int columns = 10;
        const int cellWidth = 56;
        const int cellHeight = 40;
        // SPIKE (#8098): GUM_PAIRS=N scales the grid; cells past 100 overlap the first 100 (wrap), so every pair is on screen.
        int pairCount = int.TryParse(System.Environment.GetEnvironmentVariable("GUM_PAIRS"), out int n) ? n : NineSlicePairCount;
        for (int i = 0; i < pairCount; i++)
        {
            var nineSlice = new NineSliceRuntime();
            nineSlice.SourceFileName = "Frame.png";
            nineSlice.X = 712 + (i % columns) * cellWidth;
            int cellIndex = i % 100;
            nineSlice.Y = 8 + (cellIndex / columns) * cellHeight;
            nineSlice.Width = cellWidth - 4;
            nineSlice.Height = cellHeight - 4;
            this.AddChild(nineSlice);
            _pairs.Add(nineSlice);

            var text = new TextRuntime();
            text.Text = i.ToString();
            text.X = nineSlice.X + 8;
            text.Y = nineSlice.Y + 8;
            this.AddChild(text);
            _pairs.Add(text);
        }
    }

    /// <summary>Shows only one region ("forms", "list", "pairs"; "empty" shows none; anything else shows all) so a run can attribute cost per region.</summary>
    public void ShowOnly(string? region)
    {
        bool all = region is not ("forms" or "list" or "pairs" or "empty");
        _column.Visual.Visible = all || region == "forms";
        _listBox.Visual.Visible = all || region == "list";
        foreach (GraphicalUiElement pair in _pairs)
        {
            pair.Visible = all || region == "pairs";
        }
    }

    /// <summary>Scrolls the ListBox to <paramref name="fraction"/> (0 to 1) of its range, the same for every runtime.</summary>
    public void SetScroll(double fraction)
    {
        _listBox.VerticalScrollBarValue = _listBox.VerticalScrollBarMaximum * fraction;
    }
}
