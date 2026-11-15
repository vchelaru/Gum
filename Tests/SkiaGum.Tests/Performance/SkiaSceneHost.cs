using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using SkiaSharp;

namespace SkiaGum.Tests.Performance;

/// <summary>
/// Owns an in-memory CPU raster surface wired to <see cref="GumService.Default"/> for allocation
/// tests, and removes everything the test added to the root when disposed.
/// </summary>
internal sealed class SkiaSceneHost : IDisposable
{
    private readonly SKSurface _surface;
    private double _totalSeconds;

    public SkiaSceneHost(int width = 640, int height = 480)
    {
        _surface = SKSurface.Create(new SKImageInfo(width, height));
        GumService.Default.Initialize(_surface.Canvas, width, height);
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    /// <summary>Advances the host one frame, as a game loop calls it before drawing.</summary>
    public void Update()
    {
        _totalSeconds += 1.0 / 60.0;
        GumService.Default.Update(_totalSeconds);
    }

    /// <summary>Draws the current frame into the surface.</summary>
    public void Draw()
    {
        GumService.Default.Draw();
    }

    /// <summary>Wipes the surface, which a draw does not do itself.</summary>
    public void Clear()
    {
        _surface.Canvas.Clear(SKColors.Transparent);
    }

    /// <summary>The surface's pixels, to compare two draws.</summary>
    public byte[] CaptureBytes()
    {
        using SKImage image = _surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }

    /// <summary>Counts non-transparent pixels on a coarse grid, to prove a draw produced output.</summary>
    public int CountPaintedSamples()
    {
        using SKImage image = _surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        int painted = 0;
        for (int y = 0; y < bitmap.Height; y += 4)
        {
            for (int x = 0; x < bitmap.Width; x += 4)
            {
                if (bitmap.GetPixel(x, y).Alpha > 0)
                {
                    painted++;
                }
            }
        }

        return painted;
    }

    public void Dispose()
    {
        GumService.Default.Root.Children.Clear();
        _surface.Dispose();
    }

    /// <summary>
    /// Describes <see cref="BuildRealisticFormsScene"/> for test output.
    /// </summary>
    public const string RealisticFormsSceneDescription =
        "StackPanel(3 Buttons, TextBox, ComboBox[4 items], ListBox[15 items]) + floating Window(2 Buttons)";

    /// <summary>
    /// Builds a typical game-UI Forms layout in code and adds it to the default root: a vertical
    /// <see cref="StackPanel"/> of three buttons, a text box, a combo box and a fifteen-item list box,
    /// plus a floating <see cref="Window"/> with two buttons. Every control carries real text. Returns
    /// the window so callers can change it to trigger a relayout.
    /// </summary>
    public static Window BuildRealisticFormsScene()
    {
        StackPanel panel = new StackPanel();
        panel.X = 10;
        panel.Y = 10;
        panel.Spacing = 4;
        panel.AddToRoot();

        foreach (string label in new[] { "Play", "Options", "Quit" })
        {
            Button button = new Button();
            button.Text = label;
            panel.AddChild(button);
        }

        TextBox textBox = new TextBox();
        textBox.Text = "Player One";
        panel.AddChild(textBox);

        ComboBox comboBox = new ComboBox();
        foreach (string item in new[] { "Easy", "Normal", "Hard", "Insane" })
        {
            comboBox.Items!.Add(item);
        }
        panel.AddChild(comboBox);

        ListBox listBox = new ListBox();
        listBox.Height = 200;
        for (int i = 0; i < 15; i++)
        {
            listBox.Items!.Add("Save Slot " + i);
        }
        panel.AddChild(listBox);

        Window window = new Window();
        window.X = 300;
        window.Y = 40;
        window.Width = 240;
        window.Height = 200;
        window.AddToRoot();

        Button okButton = new Button();
        okButton.Text = "OK";
        window.AddChild(okButton);

        Button cancelButton = new Button();
        cancelButton.Text = "Cancel";
        cancelButton.Y = 40;
        window.AddChild(cancelButton);

        return window;
    }
}
