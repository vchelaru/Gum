using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Gum.Input;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using RenderingLibrary.Graphics;
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
        // Same reasoning: a test outside this hierarchy that builds a GumProjectSave in memory and
        // never clears it changes how the next test resolves fonts and elements (see Dispose).
        ObjectFinder.Self.GumProjectSave = null;
        // Renderer.TextureFilter is process-wide; a test (or a project it loads) that sets Linear
        // would change how every later test's sprites sample.
        Renderer.TextureFilter = TextureFilter.Point;
        // The shared roots are laid out against the canvas, so restore it before laying them out.
        ResetCanvasAndRoots();

        Gum.GumService.Default.InitializeForTesting();
        CreateMockCursor();
    }

    /// <summary>
    /// Restores the canvas and the shared Root, PopupRoot and ModalRoot to their startup state.
    /// GumService.Update sizes the roots to the canvas, and Root is centered, so a test that grows
    /// the canvas, calls Update and restores the canvas leaves Root wider than the canvas. The next
    /// layout of Root then puts it at a negative position, and every element added under it lands
    /// off-screen (issue #6199).
    /// </summary>
    private static void ResetCanvasAndRoots()
    {
        GraphicalUiElement.CanvasWidth = 800;
        GraphicalUiElement.CanvasHeight = 600;

        Gum.GumService gumService = Gum.GumService.Default;
        gumService.Root.X = 0;
        gumService.Root.Y = 0;
        gumService.Root.Width = 0;
        gumService.Root.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        gumService.Root.Height = 0;
        gumService.Root.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        gumService.Root.UpdateLayout();

        // Popup and modal roots are left-origin and sized to the canvas by Update.
        foreach (InteractiveGue popupOrModalRoot in new InteractiveGue?[] { gumService.PopupRoot, gumService.ModalRoot })
        {
            if (popupOrModalRoot != null)
            {
                FormsUtilities.SetDimensionsToCanvas(popupOrModalRoot);
                popupOrModalRoot.UpdateLayout();
            }
        }
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
        Renderer.TextureFilter = TextureFilter.Point;

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

        Gum.GumService.Default.Root.Children!.Clear();
        Gum.GumService.Default.ModalRoot.Children!.Clear();
        Gum.GumService.Default.PopupRoot.Children!.Clear();
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

        // Same singleton, same hazard: a loaded project changes how a later, unrelated test resolves
        // font file paths (CustomSetPropertyOnRenderable.ResolveFontFilePath resolves against the
        // project's directory when it has a FullFileName) and looks up elements. Reset so only a test
        // that deliberately sets this up (and is inside its own try/finally) sees a project loaded.
        ObjectFinder.Self.GumProjectSave = null;

        // Last: the roots' layout depends on the canvas, and clearing their children above can lay them out.
        ResetCanvasAndRoots();
    }
}
