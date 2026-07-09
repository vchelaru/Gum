using Gum.Forms.Controls;
using Gum.Wireframe;
using Raylib_cs;
using Gum.Input;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using Keys = Gum.Forms.Input.Keys;

namespace RaylibGum.Tests;

public class BaseTestClass : IDisposable
{
    // #3066: renderables present right after the assembly bootstrap (Root + Forms defaults).
    // Anything found on a layer beyond this set was added by a test — typically via AddToManagers
    // without a matching RemoveFromManagers — and is swept in Dispose. Left in place, a leaked
    // renderable persists on the shared layer and corrupts later draw-call-count assertions (the
    // leak coalesces with other tests' content), which surfaced as order-dependent flaky failures.
    private static readonly HashSet<IRenderableIpso> BaselineRenderables = new();

    /// <summary>
    /// Records the renderables currently on the renderer's layers as the per-test baseline. Called
    /// once from the assembly bootstrap (and again after any Uninitialize/re-init) so Dispose can
    /// sweep anything a test leaks beyond it.
    /// </summary>
    internal static void CaptureRenderableBaseline()
    {
        BaselineRenderables.Clear();
        foreach (Layer layer in SystemManagers.Default.Renderer.Layers)
        {
            foreach (IRenderableIpso renderable in layer.Renderables)
            {
                BaselineRenderables.Add(renderable);
            }
        }
    }

    public BaseTestClass()
    {
        GumService.Default.InitializeForTesting();
    }

    public virtual void Dispose()
    {
        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.ClickCombos.Clear();
        FrameworkElement.ClickCombos.Add(new KeyCombo
        {
            PushedKey = Keys.Enter,
            HeldKey = null,
            IsTriggeredOnRepeat = false
        });

        FrameworkElement.TabKeyCombos.Clear();
        FrameworkElement.TabKeyCombos.Add(new KeyCombo
        {
            PushedKey = Keys.Tab,
            HeldKey = null,
            IsTriggeredOnRepeat = true
        });

        FrameworkElement.TabReverseKeyCombos.Clear();
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo
        {
            PushedKey = Keys.Tab,
            HeldKey = Keys.LeftShift,
            IsTriggeredOnRepeat = true
        });
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo
        {
            PushedKey = Keys.Tab,
            HeldKey = Keys.RightShift,
            IsTriggeredOnRepeat = true
        });

        // just to remove any mocks:
        FrameworkElement.MainCursor = new Cursor();

        InteractiveGue.CurrentInputReceiver = null;
        InteractiveGue.ClearNextClickActions();

        GumService.Default.Root.Children!.Clear();
        FrameworkElement.AdditionalPopupRootPairs.Clear();

        // #3066: sweep any renderables a test added straight to the renderer layers via
        // AddToManagers without a matching RemoveFromManagers. Clearing Root.Children above does not
        // reach those (they are not Root children), so without this they leak across tests. See
        // BaselineRenderables.
        foreach (Layer layer in SystemManagers.Default.Renderer.Layers)
        {
            List<IRenderableIpso> leaked = layer.Renderables.Where(r => !BaselineRenderables.Contains(r)).ToList();
            foreach (IRenderableIpso renderable in leaked)
            {
                layer.Remove(renderable);
            }
        }

        // Why aren't these available?
        //GumService.Default.ModalRoot.Children!.Clear();
        //GumService.Default.PopupRoot.Children!.Clear();

        //CustomSetPropertyOnRenderable.LocalizationService = null;

        RenderingLibrary.Content.LoaderManager.Self.CacheTextures = false;
        RenderingLibrary.Content.LoaderManager.Self.CacheTextures = true;

        FileManager.CustomGetStreamFromFile = null;

        //Text.Customizations.Clear();

        // The hidden Raylib window opened in TestAssemblyInitialize is intentionally
        // kept alive for the duration of the test run — closing and reopening it across
        // tests is unreliable on CI and makes Forms tests (which need LoadEmbeddedTexture2d
        // to succeed) prohibitively expensive to set up per test.
    }
}
