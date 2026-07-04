using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using MonoGameGum.Input;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace MonoGameGum.Tests;
public class BaseTestClass : IDisposable
{

    public BaseTestClass()
    {
        GumService.Default.InitializeForTesting();
        CreateMockCursor();
    }


    private void CreateMockCursor()
    {
        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.PrimaryClick).Returns(true);
        FormsUtilities.SetCursor(cursor.Object);
        cursor.SetupProperty(x => x.VisualOver);
        cursor.SetupProperty(x => x.WindowPushed);
        cursor.Setup(x => x.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(x => x.PrimaryPush).Returns(true);

    }

    public virtual void Dispose()
    {
        GraphicalUiElement.IsAllLayoutSuspended = false;
        GraphicalUiElement.CanvasWidth = 800;
        GraphicalUiElement.CanvasHeight = 600;
        GraphicalUiElement.GlobalFontScale = 1;

        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.Clear();
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
        InteractiveGue.ClearNextClickActions();

        GumService.Default.Root.Children!.Clear();
        GumService.Default.ModalRoot.Children!.Clear();
        GumService.Default.PopupRoot.Children!.Clear();

        CustomSetPropertyOnRenderable.LocalizationService = null;

        RenderingLibrary.Content.LoaderManager.Self.CacheTextures = false;
        RenderingLibrary.Content.LoaderManager.Self.CacheTextures = true;

        FileManager.CustomGetStreamFromFile = null;

        RenderingLibrary.Graphics.Text.Customizations.Clear();
        RenderingLibrary.Graphics.Text.ContextCustomizations.Clear();

        // RenderableRegistry holds static per-capability factories. Anything a test
        // (or production code path exercised by a test) registers must be cleared so
        // it doesn't leak into the next test. Module-initializer registrations from
        // optional packages (e.g. MonoGameGumShapes) re-run at assembly load only —
        // not after Reset — so this Reset is intended for test-introduced state.
        RenderableRegistry.Reset();

        // ObjectFinder.Self is a cross-test singleton (see ObjectFinderTests). Any Standard Element
        // registered as a fallback via RegisterFallbackStandardElements must be cleared the same way,
        // or it leaks into unrelated tests.
        ObjectFinder.Self.ClearFallbackStandardElements();
    }
}
