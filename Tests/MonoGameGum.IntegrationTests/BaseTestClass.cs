using Gum.Wireframe;
using Gum.Forms.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGameGum.Input;

namespace MonoGameGum.IntegrationTests;
public class BaseTestClass : IDisposable
{
    public virtual void Dispose()
    {
        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.ClickCombos.Clear();
        FrameworkElement.ClickCombos.Add(new KeyCombo
        {
            PushedKey = Microsoft.Xna.Framework.Input.Keys.Enter,
            HeldKey = null,
            IsTriggeredOnRepeat = false
        });

        FrameworkElement.TabKeyCombos.Clear();
        FrameworkElement.TabKeyCombos.Add(new KeyCombo
        {
            PushedKey = Microsoft.Xna.Framework.Input.Keys.Tab,
            HeldKey = null,
            IsTriggeredOnRepeat = true
        });

        FrameworkElement.TabReverseKeyCombos.Clear();
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo
        {
            PushedKey = Microsoft.Xna.Framework.Input.Keys.Tab,
            HeldKey = Microsoft.Xna.Framework.Input.Keys.LeftShift,
            IsTriggeredOnRepeat = true
        });
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo
        {
            PushedKey = Microsoft.Xna.Framework.Input.Keys.Tab,
            HeldKey = Microsoft.Xna.Framework.Input.Keys.RightShift,
            IsTriggeredOnRepeat = true
        });

        // just to remove any mocks:
        FrameworkElement.MainCursor = new Cursor();

        InteractiveGue.CurrentInputReceiver = null;

        GumService.Default.Root.Children.Clear();
        GumService.Default.ModalRoot?.Children.Clear();
        GumService.Default.PopupRoot?.Children.Clear();
    }
}
