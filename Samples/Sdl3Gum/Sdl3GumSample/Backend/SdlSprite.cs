using RenderingLibrary;
using RenderingLibrary.Graphics;
using SDL;
using static SDL.SDL3;

namespace Sdl3GumSample.Backend;

/// <summary>
/// The renderable behind a Gum Sprite: draws a source rectangle of a texture into the layout box.
/// </summary>
/// <remarks>
/// Gum's layout (<c>GraphicalUiElement</c>) owns this object and writes X, Y, Width and Height
/// (relative to the parent renderable) plus <see cref="SourceRectangle"/> (through
/// <see cref="ITextureCoordinate"/>). The renderable only has to draw what it is told.
/// </remarks>
public unsafe class SdlSprite : RenderableBase, ITextureCoordinate
{
    private string? _sourceFile;

    /// <summary>The texture to draw, or null to draw nothing.</summary>
    public SdlTexture? Texture { get; set; }

    /// <summary>
    /// A .gumx project's SourceFile variable lands here by name: with no custom
    /// <c>GraphicalUiElement.SetPropertyOnRenderable</c>, Gum assigns variables to the renderable's
    /// public properties through reflection. Code can call <c>SetProperty("SourceFile", ...)</c> too.
    /// </summary>
    public string? SourceFile
    {
        get => _sourceFile;
        set
        {
            _sourceFile = value;
            Texture = SdlTexture.Load(value);
        }
    }

    /// <inheritdoc/>
    public System.Drawing.Rectangle? SourceRectangle { get; set; }

    bool ITextureCoordinate.Wrap { get; set; }

    /// <inheritdoc/>
    public float? TextureWidth => Texture?.Width;

    /// <inheritdoc/>
    public float? TextureHeight => Texture?.Height;

    /// <summary>
    /// Gum's own draw entry point, which its renderers call with their SystemManagers. This backend
    /// walks the tree itself (<see cref="SdlGum.Draw"/>) and calls <see cref="Draw"/> instead.
    /// </summary>
    public override void Render(ISystemManagers managers)
    {
    }

    /// <summary>Draws this renderable at its absolute position.</summary>
    public virtual void Draw(SDL_Renderer* renderer)
    {
        if (Texture == null)
        {
            return;
        }

        SDL_FRect source = GetSourceRect();
        SDL_FRect destination = new SDL_FRect
        {
            x = this.GetAbsoluteLeft(),
            y = this.GetAbsoluteTop(),
            w = Width,
            h = Height
        };
        SDL_RenderTexture(renderer, Texture.Handle, &source, &destination);
    }

    /// <summary>The region of <see cref="Texture"/> to draw: the source rectangle, or the whole texture.</summary>
    protected SDL_FRect GetSourceRect()
    {
        if (SourceRectangle is System.Drawing.Rectangle rect)
        {
            return new SDL_FRect { x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height };
        }
        return new SDL_FRect { x = 0, y = 0, w = Texture!.Width, h = Texture.Height };
    }
}
