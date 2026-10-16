using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Content;
using System.Collections.Generic;
using ToolsUtilities;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// A bare <see cref="Game"/> for tests that create and initialize their own <see cref="Gum.GumService"/>
/// instances (#6066). Services passed to <see cref="Track"/> are uninitialized in reverse order on
/// dispose, and the process-wide defaults are reset, so a test that fails midway cannot leak a live
/// service into the next class.
/// </summary>
internal sealed class ServiceTestGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly List<Gum.GumService> _tracked = new();

    public ServiceTestGame()
    {
        LoaderManager.Self?.DisposeAndClear();
        _graphics = new GraphicsDeviceManager(this);
    }

    /// <summary>Creates the graphics device; call before initializing any service.</summary>
    public void Start() => RunOneFrame();

    /// <summary>Registers a service for cleanup and returns it.</summary>
    public Gum.GumService Track(Gum.GumService service)
    {
        _tracked.Add(service);
        return service;
    }

    /// <summary>Creates a new, tracked, uninitialized service.</summary>
    public Gum.GumService CreateService() => Track(new Gum.GumService());

    protected override void Update(GameTime gameTime) { }

    protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

    protected override void Dispose(bool disposing)
    {
        for (int i = _tracked.Count - 1; i >= 0; i--)
        {
            if (_tracked[i].IsInitialized)
            {
                _tracked[i].Uninitialize();
            }
        }
        LoaderManager.Self?.DisposeAndClear();
        SystemManagers.Default = null!;
        ISystemManagers.Default = null!;
        IGumService.Default = null;
        FileManager.RelativeDirectory = "Content/";
        base.Dispose(disposing);
    }
}
