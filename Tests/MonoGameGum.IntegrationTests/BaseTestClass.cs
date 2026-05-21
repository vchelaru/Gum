using Gum.GueDeriving;
using Gum.Managers;
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
            PushedKey = Gum.Forms.Input.Keys.Enter,
            HeldKey = null,
            IsTriggeredOnRepeat = false
        });

        FrameworkElement.TabKeyCombos.Clear();
        FrameworkElement.TabKeyCombos.Add(new KeyCombo
        {
            PushedKey = Gum.Forms.Input.Keys.Tab,
            HeldKey = null,
            IsTriggeredOnRepeat = true
        });

        FrameworkElement.TabReverseKeyCombos.Clear();
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo
        {
            PushedKey = Gum.Forms.Input.Keys.Tab,
            HeldKey = Gum.Forms.Input.Keys.LeftShift,
            IsTriggeredOnRepeat = true
        });
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo
        {
            PushedKey = Gum.Forms.Input.Keys.Tab,
            HeldKey = Gum.Forms.Input.Keys.RightShift,
            IsTriggeredOnRepeat = true
        });

        // just to remove any mocks:
        FrameworkElement.MainCursor = new Cursor(null);

        InteractiveGue.CurrentInputReceiver = null;

        GumService.Default.Root.Children.Clear();
        GumService.Default.ModalRoot?.Children.Clear();
        GumService.Default.PopupRoot?.Children.Clear();

        // ObjectFinder.Self is a process-wide singleton. If a previous test loaded
        // a .gumx, the project sticks around and pollutes tests that assert on the
        // "no project loaded" state (e.g. LoadAnimations_ThrowsException_WhenNoProjectLoaded).
        ObjectFinder.Self.GumProjectSave = null;

        // Clear any per-capability factories a test registered into the static
        // RenderableRegistry so they don't leak into the next test.
        RenderableRegistry.Reset();
    }
}
