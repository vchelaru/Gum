using Gum.Bundle;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ToolsUtilities;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Loading a <c>.gumpkg</c> replaces <see cref="FileManager.CustomGetStreamFromFile"/> with a hook
/// composed over the previous one (#6066 C3). Uninitialize must put the previous hook back, unless
/// a later service has installed its own hook on top.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class BundleStreamHookLifecycleTests : BaseTestClass
{
    private readonly string _directory;
    private readonly Func<string, Stream>? _hookBefore;

    public BundleStreamHookLifecycleTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "GumBundleHook_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _hookBefore = FileManager.CustomGetStreamFromFile;
    }

    public override void Dispose()
    {
        FileManager.CustomGetStreamFromFile = _hookBefore;
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
        base.Dispose();
    }

    [Fact]
    public void Uninitialize_AfterGumpkgLoad_ShouldRestorePreviousStreamHook()
    {
        using TwoServiceGame game = new(WriteBundle("A"), pathB: null);
        game.RunOneFrame();
        FileManager.CustomGetStreamFromFile.ShouldNotBe(_hookBefore);

        game.ServiceA.Uninitialize();

        FileManager.CustomGetStreamFromFile.ShouldBe(_hookBefore);
    }

    [Fact]
    public void Uninitialize_OfEarlierService_ShouldNotClobberLaterServicesHook()
    {
        using TwoServiceGame game = new(WriteBundle("A"), WriteBundle("B"));
        game.RunOneFrame();
        Func<string, Stream>? hookB = FileManager.CustomGetStreamFromFile;

        game.ServiceA.Uninitialize();

        FileManager.CustomGetStreamFromFile.ShouldBe(hookB);
    }

    [Fact]
    public void Uninitialize_AfterHostReplacedTheHook_LeavesTheHostsHookAlone()
    {
        using ServiceTestGame game = new();
        game.Start();
        Gum.GumService service = game.CreateService();
        service.Initialize(game, WriteBundle("A"));
        Func<string, Stream> hostHook = path => throw new FileNotFoundException(path);
        FileManager.CustomGetStreamFromFile = hostHook;

        service.Uninitialize();

        FileManager.CustomGetStreamFromFile.ShouldBe(hostHook,
            "the host installed this hook after Initialize; restoring the earlier one would discard it");
    }

    [Fact]
    public void Uninitialize_InReverseOfInstallOrder_RestoresEachPreviousHookInTurn()
    {
        using ServiceTestGame game = new();
        game.Start();
        Gum.GumService serviceA = game.CreateService();
        serviceA.Initialize(game, WriteBundle("A"));
        Func<string, Stream>? hookA = FileManager.CustomGetStreamFromFile;
        Gum.GumService serviceB = game.CreateService();
        serviceB.Initialize(game, WriteBundle("B"));
        FileManager.CustomGetStreamFromFile.ShouldNotBe(hookA);

        serviceB.Uninitialize();
        FileManager.CustomGetStreamFromFile.ShouldBe(hookA, "B's teardown puts back the hook it composed over");
        serviceA.Uninitialize();

        FileManager.CustomGetStreamFromFile.ShouldBe(_hookBefore);
    }

    [Fact]
    public void InitializeAndUninitialize_RepeatedOnOneService_DoNotStackHooks()
    {
        using ServiceTestGame game = new();
        game.Start();
        Gum.GumService service = game.CreateService();
        string bundle = WriteBundle("A");

        for (int cycle = 0; cycle < 3; cycle++)
        {
            service.Initialize(game, bundle);
            service.CurrentProjectResolution!.PreviousHook.ShouldBe(_hookBefore,
                "each load composes over the original hook, not over the previous cycle's bundle hook");

            service.Uninitialize();

            FileManager.CustomGetStreamFromFile.ShouldBe(_hookBefore);
        }
    }

    [Fact]
    public void Initialize_WhenTheBundledProjectFailsToLoad_RestoresPreviousStreamHook()
    {
        using ServiceTestGame game = new();
        game.Start();
        Gum.GumService service = game.CreateService();
        string path = Path.Combine(_directory, "Broken.gumpkg");
        using (FileStream stream = File.Create(path))
        {
            GumBundleWriter.Write(stream, new List<(string, byte[])>
            {
                ("Broken.gumx", Encoding.UTF8.GetBytes("this is not a gum project")),
            });
        }

        Should.Throw<Exception>(() => service.Initialize(game, path));

        FileManager.CustomGetStreamFromFile.ShouldBe(_hookBefore);
    }

    private string WriteBundle(string name)
    {
        string path = Path.Combine(_directory, name + ".gumpkg");
        using FileStream stream = File.Create(path);
        GumBundleWriter.Write(stream, new List<(string, byte[])>
        {
            (name + ".gumx", Encoding.UTF8.GetBytes("<GumProjectSave />")),
        });
        return path;
    }

    private sealed class TwoServiceGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private readonly string _pathA;
        private readonly string? _pathB;

        public Gum.GumService ServiceA { get; } = new();
        public Gum.GumService ServiceB { get; } = new();

        public TwoServiceGame(string pathA, string? pathB)
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            _pathA = pathA;
            _pathB = pathB;
        }

        protected override void Initialize()
        {
            base.Initialize();
            ServiceA.Initialize(this, _pathA);
            if (_pathB != null)
            {
                ServiceB.Initialize(this, _pathB);
            }
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(XnaColor.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (ServiceB.IsInitialized) { ServiceB.Uninitialize(); }
            if (ServiceA.IsInitialized) { ServiceA.Uninitialize(); }
            LoaderManager.Self?.DisposeAndClear();
            SystemManagers.Default = null!;
            base.Dispose(disposing);
        }
    }
}
