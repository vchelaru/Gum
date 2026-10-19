using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using ToolsUtilities;
using Keys = Gum.Forms.Input.Keys;

namespace SkiaHostTests;

/// <summary>
/// Base for the interactive Skia host test classes. Resets shared Forms static state after each test
/// and sweeps any renderables a test leaked onto the shared layers, so tests stay order-independent.
/// A host's own <c>BaseTestClass</c> derives from this and overrides <see cref="ResetInput"/> for the
/// part that is specific to its input devices.
/// </summary>
public abstract class SkiaHostTestBase : IDisposable
{
    // Renderables present right after the assembly bootstrap (Root + Forms defaults). Anything found
    // beyond this set was added by a test and is swept in Dispose (#3066).
    private static readonly HashSet<IRenderableIpso> BaselineRenderables = new();

    /// <summary>
    /// The service the host's bootstrap initialized.
    /// </summary>
    protected static GumServiceSkiaBase Service => (GumServiceSkiaBase)IGumService.Default!;

    /// <summary>
    /// Records the renderables currently on the renderer's layers as the per-test baseline. Called
    /// once from the assembly bootstrap so Dispose can sweep anything a test leaks beyond it.
    /// </summary>
    public static void CaptureRenderableBaseline()
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

    /// <summary>
    /// Puts the host's input devices back to a clean state. Runs after the shared Forms state is reset
    /// and before the roots are cleared. Install a fresh <c>Cursor</c> with
    /// <c>FormsUtilities.SetCursor</c>: that, unlike assigning <c>FrameworkElement.MainCursor</c>, also
    /// backs the host's typed <c>GumService.Default.Cursor</c>, which casts to the concrete type, so a
    /// mock cursor left installed would make that cast return null for every later test (#3652).
    /// </summary>
    protected abstract void ResetInput();

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

        ResetInput();

        InteractiveGue.CurrentInputReceiver = null;
        InteractiveGue.ClearNextClickActions();

        Service.Root.Children!.Clear();
        Service.ModalRoot.Children!.Clear();
        Service.PopupRoot.Children!.Clear();
        FrameworkElement.AdditionalPopupRootPairs.Clear();

        // Sweep any renderables a test added straight to the renderer layers via AddToManagers
        // without a matching RemoveFromManagers (Clearing Root.Children above does not reach those).
        foreach (Layer layer in SystemManagers.Default.Renderer.Layers)
        {
            List<IRenderableIpso> leaked = layer.Renderables.Where(r => !BaselineRenderables.Contains(r)).ToList();
            foreach (IRenderableIpso renderable in leaked)
            {
                layer.Remove(renderable);
            }
        }

        // NOTE: deliberately NOT toggling LoaderManager.Self.CacheTextures here (the RaylibGum base
        // does). On Skia, flipping CacheTextures to false disposes cached textures, including the
        // shared V3 UISpriteSheet SKBitmap the default visuals hold, so a later control construction
        // would call SKImage.FromBitmap on a disposed bitmap and crash the host with an
        // AccessViolationException. Leaving the cache intact keeps the sprite sheet valid across tests.

        FileManager.CustomGetStreamFromFile = null;
    }
}
