using System.Diagnostics;
using System.Reflection;
using Gum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;

namespace GlyphBench;

/// <summary>
/// One measurement of real Gum text drawing: 200 letters wrapped in a 300 px Text, Orbitron-Black 16 pt from
/// KernSmith, CharacterByCharacter, hidden MonoGame DesktopGL window. The same measurement as
/// MonoGameGum.IntegrationTests' ColdVersusWarm_SingleCase "plain-warm" (1 s warmup, best of 9 batches of
/// 200 frames). Prints one RESULT line on stdout.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        int warmupMs = args.Length > 0 ? int.Parse(args[0]) : 1000;
        int frames = args.Length > 1 ? int.Parse(args[1]) : 200;
        int batches = args.Length > 2 ? int.Parse(args[2]) : 9;

        // RoundToInt(float) lives in GumCommon.dll; the runner swaps in a copy built with a different body.
        // The IL size tells the runner which body this process really ran.
        MethodInfo roundToInt = typeof(MathFunctions).GetMethod(nameof(MathFunctions.RoundToInt), new[] { typeof(float) })!;
        int ilBytes = roundToInt.GetMethodBody()!.GetILAsByteArray()!.Length;

        using HostGame game = new HostGame();
        game.RunOneFrame();
        Gum.GumService.Default.Initialize(game, Gum.Forms.DefaultVisualsVersion.Newest);
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(game.GraphicsDevice);
        Text.TextRenderingMode = TextRenderingMode.CharacterByCharacter;

        TextRuntime text = new TextRuntime();
        text.UseCustomFont = true;
        text.CustomFontFile = Path.Combine(AppContext.BaseDirectory, "Orbitron-Black.ttf");
        text.FontSize = 16;
        text.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        text.Width = 300;
        text.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        text.X = 10;
        text.Y = 10;
        text.Text = BuildText(200);
        text.AddToRoot();

        Gum.GumService.Default.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0)));

        // Liveness: something must be drawn, or the numbers mean nothing.
        long before = game.GraphicsDevice.Metrics.SpriteCount;
        Gum.GumService.Default.Draw();
        long draws = game.GraphicsDevice.Metrics.SpriteCount - before;
        if (draws <= 0)
        {
            Console.Error.WriteLine("Nothing was drawn.");
            return 2;
        }

        long warmupEnd = Stopwatch.GetTimestamp() + Stopwatch.Frequency * warmupMs / 1000;
        for (int i = 0; i < 100 || Stopwatch.GetTimestamp() < warmupEnd; i++)
        {
            Gum.GumService.Default.Draw();
        }

        List<double> perFrame = new List<double>();
        for (int batch = 0; batch < batches; batch++)
        {
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < frames; i++)
            {
                Gum.GumService.Default.Draw();
            }
            perFrame.Add((Stopwatch.GetTimestamp() - start) * 1_000_000.0 / Stopwatch.Frequency / frames);
        }
        perFrame.Sort();

        double min = perFrame[0] / draws;
        double median = perFrame[perFrame.Count / 2] / draws;
        Console.WriteLine($"RESULT\t{min:F4}\t{median:F4}\t{draws}\t{ilBytes}");
        return 0;
    }

    private static string BuildText(int characters)
    {
        const string words = "lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor ";
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        while (builder.Length < characters)
        {
            builder.Append(words);
        }
        builder.Length = characters;
        if (builder[builder.Length - 1] == ' ')
        {
            builder[builder.Length - 1] = 'x';
        }
        return builder.ToString();
    }

    private sealed class HostGame : Game
    {
        public HostGame()
        {
            // The window is created hidden and stays hidden: only RunOneFrame is used, never Run.
            _ = new GraphicsDeviceManager(this) { GraphicsProfile = GraphicsProfile.HiDef };
        }

        protected override void Update(GameTime gameTime) { }

        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);
    }
}
