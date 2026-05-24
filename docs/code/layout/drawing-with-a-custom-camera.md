# Drawing with a Custom Camera

Gum's render loop is driven by an internal camera (`SystemManagers.Default.Renderer.Camera`). The auto-fit helpers — [`EnableZoomToWindow` and `EnableExpandToWindow`](resizing-the-game-window.md) — are the easiest way to drive that camera, but games that already maintain their own camera state (for pan / zoom / cinematic effects, or because other content draws under the same camera) need a way to hand Gum that state directly.

The `Draw` method on `GumService` has platform-specific overloads that accept the platform's native per-frame camera type, copy the relevant fields onto Gum's internal camera, and then draw under it. This is the supported path for "render Gum under my own camera."

## raylib: `Draw(Camera2D)`

On raylib, `GumService.Draw` accepts a raylib `Camera2D`:

```csharp
using Raylib_cs;
using static Raylib_cs.Raylib;

GumService GumUI => GumService.Default;

Camera2D camera = new Camera2D
{
    Target = new Vector2(0, 0),
    Offset = new Vector2(0, 0),
    Zoom = 1f,
    Rotation = 0,
};

while (!WindowShouldClose())
{
    // ...handle input that mutates camera.Target / camera.Zoom...

    BeginDrawing();
    ClearBackground(Color.Black);

    BeginMode2D(camera);
    DrawWorld();       // your own world content under the same camera
    EndMode2D();

    GumUI.Update(GetTime());
    GumUI.Draw(camera); // Gum renders under the same Camera2D
    EndDrawing();
}
```

The overload copies `camera.Target.X`, `camera.Target.Y`, and `camera.Zoom` onto Gum's internal camera before drawing.

`camera.Offset` and `camera.Rotation` are intentionally **not** copied:

* **Offset** — Gum derives the rendering offset from `Camera.CameraCenterOnScreen` (`Center` or `TopLeft`). Set that separately on the camera if you need non-default placement; the offset on the passed `Camera2D` is ignored.
* **Rotation** — Gum's camera does not model rotation. A rotated `Camera2D` will not produce rotated UI from this call. If you need rotated UI input compensation, see [Cursor TransformMatrix](../gum-code-reference/cursor/transformmatrix.md).

Each call to `Draw(camera)` overwrites any previously-configured `X`, `Y`, and `Zoom` on Gum's internal camera for that frame, so this overload and the auto-fit helpers (`EnableZoomToWindow` / `EnableExpandToWindow`) should not be used together — the camera passed to `Draw` wins.

## Other platforms

Equivalent `Draw` overloads for MonoGame, KNI, FNA, and SkiaSharp are not yet shipped. The current workaround on those backends is the [RenderTarget approach](resizing-the-game-window.md#rendertargets-scaling-and-offsets) — render Gum into a `RenderTarget2D`, then composite it under your own transform. As the per-platform overloads land they will follow the same pattern: pass the platform's native per-frame transform to `GumUI.Draw`, no separate sync step.
