using System.Diagnostics;
using Apos.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Xunit;

namespace MonoGameGum.IntegrationTests;

// TEMPORARY probe for #6078.
public class ShapeInitProbeTests
{
    [Fact]
    public void Probe()
    {
        Stopwatch sw = Stopwatch.StartNew();
        using ProbeGame game = new();
        game.RunOneFrame();
        TimingProbe.Mark("P device", sw);
        GraphicsDevice gd = game.GraphicsDevice;

        BasicEffect be = new BasicEffect(gd);
        TimingProbe.Mark("P BasicEffect ctor", sw);

        for (int i = 0; i < 3; i++)
        {
            ShapeBatch sb = new ShapeBatch(gd, (Effect?)null);
            TimingProbe.Mark("P ShapeBatch ctor #" + i, sw);
            for (int d = 0; d < 2; d++)
            {
                gd.Clear(Color.Black);
                sb.Begin();
                sb.DrawCircle(new Vector2(50, 50), 20, Color.Red, Color.Red, 1, 2);
                sb.End();
                TimingProbe.Mark("P circle draw #" + i + "." + d, sw);
            }
            gd.Clear(Color.Black);
            sb.Begin();
            sb.DrawRectangle(new Vector2(10, 10), new Vector2(30, 30), Color.Red, Color.Red, 1, 0, 0, 0);
            sb.End();
            TimingProbe.Mark("P rect draw #" + i, sw);
            sb.Dispose();
            TimingProbe.Mark("P dispose #" + i, sw);
        }
    }

    private class ProbeGame : Game
    {
        public ProbeGame()
        {
            new GraphicsDeviceManager(this) { GraphicsProfile = GraphicsProfile.HiDef };
        }
        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) { }
    }
}
