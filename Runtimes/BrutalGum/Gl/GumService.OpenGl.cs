#if BRUTAL
#nullable enable

using Brutal.GlfwApi;
using Gum.Input;

namespace Gum;

public partial class GumService
{
    /// <summary>
    /// Initializes Gum for a BRUTAL application drawing with OpenGL 3.3, optionally loading a Gum project.
    /// Create the window with a 3.3 core context, make it current (<c>window.MakeContextCurrent()</c>) and make it
    /// resizable if you want Gum to follow its size. Gum creates its Skia context and render target at the
    /// framebuffer size, and <see cref="Draw"/> blits the frame to the window's default framebuffer; call
    /// <c>window.SwapBuffers()</c> after it.
    /// </summary>
    /// <param name="window">The GLFW window, with its OpenGL context current on this thread.</param>
    /// <param name="gumProjectFile">An optional .gumx or .gumj project file to load.</param>
    public void Initialize(GlfwWindow window, string? gumProjectFile = null) =>
        Initialize(new BrutalGlfwInput(window), new GlBackend(), gumProjectFile);
}
#endif
