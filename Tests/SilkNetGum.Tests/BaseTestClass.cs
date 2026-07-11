using Gum;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;
using Keys = Gum.Forms.Input.Keys;

namespace SilkNetGum.Tests;

/// <summary>
/// Base for SilkNetGum tests. Mirrors the RaylibGum.Tests base: resets shared Forms static state
/// and sweeps any renderables a test leaked onto the shared layers so tests stay order-independent.
/// </summary>
public class BaseTestClass : IDisposable
{
    // Renderables present right after the assembly bootstrap (Root + Forms defaults). Anything found
    // beyond this set was added by a test and is swept in Dispose (#3066).
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

        // Remove any mock cursor a test installed.
        FrameworkElement.MainCursor = new Cursor();

        InteractiveGue.CurrentInputReceiver = null;
        InteractiveGue.ClearNextClickActions();

        GumService.Default.Root.Children!.Clear();
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
        // does). On Skia, flipping CacheTextures to false disposes cached textures — including the
        // shared V3 UISpriteSheet SKBitmap the default visuals hold — so a later control construction
        // would call SKImage.FromBitmap on a disposed bitmap and crash the host with an
        // AccessViolationException. Leaving the cache intact keeps the sprite sheet valid across tests.

        FileManager.CustomGetStreamFromFile = null;
    }
}
