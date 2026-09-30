using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace UnityGum.Tests;

/// <summary>
/// Resets the shared Forms and input state after each test and sweeps renderables a test left on the
/// layers, so tests stay order-independent. Mirrors SilkNetGum.Tests' base.
/// </summary>
public class BaseTestClass : IDisposable
{
    private static readonly HashSet<IRenderableIpso> BaselineRenderables = new();

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

    public virtual void Dispose()
    {
        FrameworkElement.KeyboardsForUiControl.Clear();

        // A fresh pushed cursor/keyboard so one test's held buttons and keys don't leak into the next.
        FormsUtilities.SetCursor(new Cursor());
        FormsUtilities.SetKeyboard(new Keyboard());

        InteractiveGue.CurrentInputReceiver = null;
        InteractiveGue.ClearNextClickActions();

        GumService.Default.Root.Children!.Clear();
        GumService.Default.ModalRoot.Children!.Clear();
        GumService.Default.PopupRoot.Children!.Clear();
        FrameworkElement.AdditionalPopupRootPairs.Clear();

        foreach (Layer layer in SystemManagers.Default.Renderer.Layers)
        {
            List<IRenderableIpso> leaked = layer.Renderables.Where(r => !BaselineRenderables.Contains(r)).ToList();
            foreach (IRenderableIpso renderable in leaked)
            {
                layer.Remove(renderable);
            }
        }
    }
}
