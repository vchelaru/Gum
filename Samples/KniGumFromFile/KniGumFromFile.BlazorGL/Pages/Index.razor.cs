using System;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;

namespace KniGumFromFile.Pages
{
    public partial class Index
    {
        Game _game;
        int _ticks;
        bool _benching;

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                JsRuntime.InvokeAsync<object>("initRenderJS", DotNetObjectReference.Create(this));
            }
        }

        [JSInvokable]
        public void TickDotNet()
        {
            // SPIKE #6156: the game loop pauses while the benchmark runs.
            if (_benching) return;

            // init game
            if (_game == null)
            {
                _game = new KniGumFromFileGame();
                _game.Run();
            }

            // run gameloop
            _game.Tick();

            if (++_ticks == 30)
            {
                _benching = true;
                _ = RunBench();
            }
        }

        async System.Threading.Tasks.Task RunBench()
        {
            try
            {
                // Earlier spikes: PremultiplyBench.Run, WebLoadBench.RunE / RunD.
                PkgLoadBench.Run(JsRuntime, _game.GraphicsDevice, Console.WriteLine);
                await System.Threading.Tasks.Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.WriteLine("WEBBENCH FAILED " + e);
            }
            _benching = false;
        }
    }
}
