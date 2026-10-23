using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace KniSkiaGumImmediateMode.Screens;

/// <summary>
/// A page within the immediate-mode sample. Each screen owns its own Gum objects and draws them via a
/// shared <see cref="GumBatch"/>, and optionally straight onto the <see cref="SKCanvas"/>.
/// </summary>
public interface IImmediateModeScreen : IDisposable
{
    void Initialize();

    /// <summary>
    /// Gum roots owned by this screen that are not part of <c>GumService.Default.Root</c>
    /// (e.g. a container only ever drawn via <c>GumBatch</c>) but still need Forms
    /// interactivity (hover/click/drag). Game1 combines these with Root and runs them through
    /// a single <c>GumService.Default.Update(gameTime, roots)</c> call each frame.
    ///
    /// This must be combined into one call rather than calling Update a second time: Cursor's
    /// push/click detection is edge-triggered (compares this frame's raw mouse state against a
    /// snapshot taken by the previous Update call), so a second Update call in the same frame
    /// sees no edge and push/click silently never fire, even though hover keeps working.
    /// Default: none.
    /// </summary>
    IEnumerable<GraphicalUiElement> AdditionalUpdateRoots => Array.Empty<GraphicalUiElement>();

    void Draw(GumBatch gumBatch, SKCanvas canvas);
}
