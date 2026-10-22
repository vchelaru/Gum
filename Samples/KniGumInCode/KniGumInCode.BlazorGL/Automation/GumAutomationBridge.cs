// DEBUG/TEST ONLY. This bridge can find, read and set properties on ANY Gum object in the running
// game, so it must never ship in a player build. The whole file compiles to nothing unless the
// GUM_AUTOMATION constant is defined (see the README: build with -p:GumAutomation=true).
#if GUM_AUTOMATION
using System;
using System.Text.Json;
using Gum;
using Gum.Wireframe;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;

namespace KniGumInCode.Automation;

/// <summary>
/// Lets browser automation (Playwright, Selenium, the dev tools console) find named Gum elements,
/// read and set their variables, and get their on-screen bounds. The page sees one JS object,
/// <c>window.gumAutomation</c>, installed by <see cref="Install"/>.
/// </summary>
public static class GumAutomationBridge
{
    // The assembly name of the BlazorGL head (AssemblyName in the csproj). DotNet.invokeMethod
    // needs it to find the [JSInvokable] method below.
    const string AssemblyName = "KniGumInCode";

    static Game _game;

    // window.gumAutomation.call(command, args) runs a command and returns its result, or throws.
    // find, get, set and bounds are one-liners on top of call.
    //
    // bounds() returns CSS pixels in the page's viewport, which is what Playwright's mouse uses.
    // C# reports the element in canvas pixels (back buffer pixels, Gum camera zoom applied).
    // The canvas drawing buffer (canvas.width) is not necessarily the size the canvas is displayed
    // at (CSS layout, device pixel ratio), so the script scales canvas pixels by
    // getBoundingClientRect().width / canvas.width and adds the canvas's offset in the page.
    // Browser zoom needs no extra handling: getBoundingClientRect already reports CSS pixels,
    // and Playwright clicks in CSS pixels.
    const string Script = @"
(function () {
  const call = (command, args) => {
    const reply = JSON.parse(DotNet.invokeMethod('" + AssemblyName + @"', 'Execute', command, JSON.stringify(args || {})));
    if (!reply.ok) throw new Error(reply.error);
    return reply.result;
  };
  window.gumAutomation = {
    call,
    ready: () => call('ready'),
    find: (name) => call('find', { name }),
    get: (name, property) => call('get', { name, property }),
    set: (name, property, value) => call('set', { name, property, value }),
    bounds: (name) => {
      const b = call('bounds', { name });
      const canvas = document.getElementById('theCanvas');
      const rect = canvas.getBoundingClientRect();
      const sx = rect.width / canvas.width, sy = rect.height / canvas.height;
      const left = rect.left + b.left * sx, top = rect.top + b.top * sy;
      const width = b.width * sx, height = b.height * sy;
      return { left, top, width, height, centerX: left + width / 2, centerY: top + height / 2, visible: b.visible };
    }
  };
})();";

    /// <summary>
    /// Call once after the game object exists. Defines window.gumAutomation in the page.
    /// </summary>
    public static void Install(IJSRuntime js, Game game)
    {
        _game = game;
        // eval keeps the JS in this file, so a build without the constant leaves no trace in index.html.
        ((IJSInProcessRuntime)js).InvokeVoid("eval", Script);
    }

    /// <summary>
    /// Runs one command. Arguments and reply are JSON: {"ok":true,"result":...} or {"ok":false,"error":"..."}.
    /// </summary>
    [JSInvokable]
    public static string Execute(string command, string argsJson)
    {
        try
        {
            using JsonDocument args = JsonDocument.Parse(string.IsNullOrEmpty(argsJson) ? "{}" : argsJson);
            object result = Run(command, args.RootElement);
            return JsonSerializer.Serialize(new { ok = true, result });
        }
        catch (Exception e)
        {
            return JsonSerializer.Serialize(new { ok = false, error = e.Message });
        }
    }

    static object Run(string command, JsonElement args)
    {
        switch (command)
        {
            case "ready":
                // The game is created on the first frame, so the Gum roots exist a moment later.
                return GumService.Default.Root != null && _game.GraphicsDevice != null;
            case "find":
                return Describe(Require(args));
            case "bounds":
                return GetBounds(Require(args));
            case "get":
                string property = args.GetProperty("property").GetString();
                if (!Require(args).TryGetProperty(property, out object value))
                {
                    throw new InvalidOperationException($"Unknown property '{property}'");
                }
                return value is Enum ? value.ToString() : value;
            case "set":
                Require(args).SetProperty(args.GetProperty("property").GetString(), ToClrValue(args.GetProperty("value")));
                return true;
            default:
                throw new InvalidOperationException($"Unknown command '{command}'");
        }
    }

    static GraphicalUiElement Require(JsonElement args)
    {
        string name = args.GetProperty("name").GetString();
        return Find(name) ?? throw new InvalidOperationException($"No element named '{name}'");
    }

    /// <summary>
    /// Resolves "Name" or a dotted path such as "OptionsComboBox.TextInstance". Each segment is the
    /// shallowest descendant with that name under the previous one, so a name that repeats inside many
    /// components (TextInstance, Background) is reached through its named parent. The first segment is
    /// searched in the root, the popup root (drop-downs) and the modal root (dialogs), in that order.
    /// </summary>
    static GraphicalUiElement Find(string path)
    {
        string[] segments = path.Split('.');
        GumService gum = GumService.Default;
        foreach (GraphicalUiElement root in new GraphicalUiElement[] { gum.Root, gum.PopupRoot, gum.ModalRoot })
        {
            GraphicalUiElement current = root;
            foreach (string segment in segments)
            {
                current = current?.FindByName(segment);
            }
            if (current != null)
            {
                return current;
            }
        }
        return null;
    }

    static object Describe(GraphicalUiElement element) => new
    {
        name = element.Name,
        type = element.GetType().Name,
        visible = element.AbsoluteVisible,
    };

    // Canvas (back buffer) pixels. The camera maps Gum units to the screen with
    // (gum - camera position) * zoom, offset by the viewport. This is the inverse of what
    // Cursor.XRespectingGumZoomAndBounds does for mouse input. Rotation is ignored, so a rotated
    // element reports its unrotated box.
    static object GetBounds(GraphicalUiElement element)
    {
        Camera camera = SystemManagers.Default.Renderer.Camera;
        float zoom = camera.Zoom;
        Viewport viewport = _game.GraphicsDevice.Viewport;
        return new
        {
            left = (element.AbsoluteLeft - camera.X) * zoom + viewport.X,
            top = (element.AbsoluteTop - camera.Y) * zoom + viewport.Y,
            width = element.AbsoluteWidth * zoom,
            height = element.AbsoluteHeight * zoom,
            visible = element.AbsoluteVisible,
        };
    }

    // JSON has one number type and Gum variables are mostly float. SetProperty converts the float
    // to int, and enum names to enums, for typed properties.
    static object ToClrValue(JsonElement json) => json.ValueKind switch
    {
        JsonValueKind.Number => json.GetSingle(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String => json.GetString(),
        JsonValueKind.Null => null,
        _ => throw new InvalidOperationException("Value must be a number, string, boolean or null"),
    };
}
#endif
