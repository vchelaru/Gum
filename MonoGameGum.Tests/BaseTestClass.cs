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
        // Dispose clears these too, but only for tests that derive from this class. Several test
        // classes in this project don't, so a global suspend flag one of them leaves set would
        // otherwise land on whatever runs next -- and both flags make font and layout work silently
        // no-op rather than fail, so the damage shows up as a confusing assertion somewhere else.
        // Clearing on the way in as well means a test starts from a known state no matter what ran
        // before it.
        GraphicalUiElement.IsAllLayoutSuspended = false;
        GraphicalUiElement.SuppressFontRegeneration = false;

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
        // Paired with IsAllLayoutSuspended: the same "silently skip font work" hazard, and the
        // tests that set it rely on their own finally alone until now.
        GraphicalUiElement.SuppressFontRegeneration = false;
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

        // Empty is the true default (opt-in, unlike Tab/Click above) -- just clear, don't re-seed.
        FrameworkElement.UpKeyCombos.Clear();
        FrameworkElement.DownKeyCombos.Clear();
        FrameworkElement.LeftKeyCombos.Clear();
        FrameworkElement.RightKeyCombos.Clear();

        // just to remove any mocks:
        FrameworkElement.MainCursor = new Cursor(null);

        InteractiveGue.CurrentInputReceiver = null;
        InteractiveGue.ClearNextClickActions();
        // A Menu popup left open at the end of a test (e.g. via MenuItem.IsSelected = true) queues
        // a pending push action (Menu.HandleNextPush) that isn't cleared by ClearNextClickActions.
        // Left uncleared, it fires against the next test's cursor state instead, closing over a
        // torn-down Menu whose Visual no longer resolves EffectiveManagers correctly.
        InteractiveGue.ClearNextPushActions();

        GumService.Default.Root.Children!.Clear();
        GumService.Default.ModalRoot.Children!.Clear();
        GumService.Default.PopupRoot.Children!.Clear();
        FrameworkElement.AdditionalPopupRootPairs.Clear();

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

        // Same singleton, same hazard: a test that loads a GumProjectSave (even just to construct one
        // in memory, never "Save"d to disk) leaves GumProjectSave.FullFileName possibly null. A later,
        // unrelated test resolving a relative TextRuntime.CustomFontFile goes through
        // CustomSetPropertyOnRenderable.ResolveFontFilePath, which -- when a project is loaded --
        // resolves relative to FileManager.GetDirectory(gumProject.FullFileName) instead of
        // FileManager.RelativeDirectory, throwing on a null FullFileName. Reset so only a test that
        // deliberately sets this up (and is inside its own try/finally) sees a project loaded.
        ObjectFinder.Self.GumProjectSave = null;
    }
}
