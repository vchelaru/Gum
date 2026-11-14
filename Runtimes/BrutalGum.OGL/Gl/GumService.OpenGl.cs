#if BRUTAL
#nullable enable

using Brutal.GlfwApi;
using Gum.Forms;
using System;
using Gum.Input;

namespace Gum;

public partial class GumService
{
    /// <summary>
    /// Initializes Gum for a BRUTAL application drawing with OpenGL 3.3, optionally loading a Gum project.
    /// Create the window with a 3.3 core context, make it current (<c>window.MakeContextCurrent()</c>) and make it
    /// resizable if you want Gum to follow its size. Gum creates its Skia context and render target at the
    /// framebuffer size, and <see cref="Draw"/> draws Gum straight into the window's default framebuffer, over whatever the game drew; call
    /// <c>window.SwapBuffers()</c> after it.
    /// </summary>
    /// <param name="window">The GLFW window, with its OpenGL context current on this thread.</param>
    /// <param name="gumProjectFile">An optional .gumx or .gumj project file to load.</param>
    public void Initialize(GlfwWindow window, string? gumProjectFile = null) =>
        Initialize(new BrutalGlfwInput(window), gumProjectFile);

    /// <summary>
    /// Initializes Gum like <see cref="Initialize(GlfwWindow, string?)"/> with an explicit set of default Forms visuals.
    /// Skia supports <see cref="DefaultVisualsVersion.V3"/> only; any other value throws <see cref="NotSupportedException"/>.
    /// </summary>
    /// <param name="window">The GLFW window, with its OpenGL context current on this thread.</param>
    /// <param name="visualsVersion">The default Forms visuals to register.</param>
    /// <param name="gumProjectFile">An optional .gumx or .gumj project file to load.</param>
    public void Initialize(GlfwWindow window, DefaultVisualsVersion visualsVersion, string? gumProjectFile = null) =>
        Initialize(new BrutalGlfwInput(window), visualsVersion, gumProjectFile);

    /// <summary>
    /// Initializes Gum for a BRUTAL application drawing with OpenGL 3.3 whose window and events are not
    /// GLFW's (SDL3, a custom loop). The host supplies cursor, keyboard, clipboard and gamepad state through
    /// <paramref name="input"/>; otherwise this behaves like <see cref="Initialize(GlfwWindow, string?)"/>:
    /// the host's OpenGL 3.3 core context must be current on this thread, and <see cref="Draw"/> draws into
    /// the default framebuffer.
    /// </summary>
    /// <param name="input">The host's window, mouse, keyboard, clipboard and gamepad state.</param>
    /// <param name="gumProjectFile">An optional .gumx or .gumj project file to load.</param>
    public void Initialize(IBrutalInput input, string? gumProjectFile = null) =>
        Initialize(input, new GlBackend(), gumProjectFile);

    /// <summary>
    /// Initializes Gum like <see cref="Initialize(IBrutalInput, string?)"/> with an explicit set of default Forms visuals.
    /// Skia supports <see cref="DefaultVisualsVersion.V3"/> only; any other value throws <see cref="NotSupportedException"/>.
    /// </summary>
    /// <param name="input">The host's window, mouse, keyboard, clipboard and gamepad state.</param>
    /// <param name="visualsVersion">The default Forms visuals to register.</param>
    /// <param name="gumProjectFile">An optional .gumx or .gumj project file to load.</param>
    public void Initialize(IBrutalInput input, DefaultVisualsVersion visualsVersion, string? gumProjectFile = null) =>
        Initialize(input, new GlBackend(), gumProjectFile, visualsVersion);
}
#endif
