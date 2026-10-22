using System;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;

namespace KniGumInCode.Pages
{
    public partial class Index
    {
        Game _game;

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
            // init game
            if (_game == null)
            {
                _game = new KniGumInCodeGame();
                _game.Run();
#if GUM_AUTOMATION
                KniGumInCode.Automation.GumAutomationBridge.Install(JsRuntime, _game);
#endif
            }

            // run gameloop
            _game.Tick();
        }

    }
}
