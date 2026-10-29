using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Plugins;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using RenderingLibrary.Content;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// A reload of content (#7027) drops the path-keyed cache even when no element is shown, so the next
/// selection loads from disk instead of drawing what was cached before the file changed.
/// </summary>
public class WireframeObjectManagerReloadTexturesTests : BaseTestClass
{
    private sealed class Cached : IDisposable
    {
        public bool Disposed;
        public void Dispose() => Disposed = true;
    }

    private static WireframeObjectManager Build(ElementSave? selected)
    {
        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.Setup(x => x.SelectedElements).Returns(selected == null ? new List<ElementSave>() : new List<ElementSave> { selected });
        selectedState.Setup(x => x.SelectedElement).Returns(selected);

        return new WireframeObjectManager(
            Mock.Of<IFontManager>(),
            selectedState.Object,
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>(),
            new LocalizationService(),
            Mock.Of<IPluginManager>(),
            Mock.Of<IProjectState>());
    }

    [Fact]
    public void RefreshAll_DropsTheCache_WhenReloadingTexturesWithNothingSelected()
    {
        Cached cached = new Cached();
        LoaderManager.Self.DisposeAndClear();
        LoaderManager.Self.AddDisposable("Photo.png", cached);
        try
        {
            Build(null).RefreshAll(forceLayout: true, forceReloadTextures: true);

            cached.Disposed.ShouldBeTrue();
            LoaderManager.Self.CachedDisposables.ShouldNotContainKey("Photo.png");
        }
        finally
        {
            LoaderManager.Self.DisposeAndClear();
        }
    }

    [Fact]
    public void RefreshAll_DropsTheCache_WhenReloadingTexturesAndTheElementsFileIsMissing()
    {
        ScreenSave screen = new ScreenSave { Name = "Gone", IsSourceFileMissing = true };
        Cached cached = new Cached();
        LoaderManager.Self.DisposeAndClear();
        LoaderManager.Self.AddDisposable("Photo.png", cached);
        try
        {
            Build(screen).RefreshAll(forceLayout: true, forceReloadTextures: true);

            cached.Disposed.ShouldBeTrue();
        }
        finally
        {
            LoaderManager.Self.DisposeAndClear();
        }
    }

    [Fact]
    public void RefreshAll_KeepsTheCache_WhenNotReloadingTextures()
    {
        Cached cached = new Cached();
        LoaderManager.Self.DisposeAndClear();
        LoaderManager.Self.AddDisposable("Photo.png", cached);
        try
        {
            Build(null).RefreshAll(forceLayout: true);

            cached.Disposed.ShouldBeFalse();
        }
        finally
        {
            LoaderManager.Self.DisposeAndClear();
        }
    }
}
