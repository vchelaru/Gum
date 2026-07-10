using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using System;

namespace MonoGameGum.Tests.V2;

/// <summary>
/// Verifies that the runtime GumService exposes the platform-agnostic
/// <see cref="IGumService"/> contract so engine code can take a dependency on
/// the interface rather than the concrete runtime.
/// </summary>
public class GumServiceInterfaceTests
{
    [Fact]
    public void GumService_ImplementsIGumService()
    {
        GumService.Default.ShouldBeAssignableTo<IGumService>();
    }

    [Fact]
    public void IGumService_CanvasWidth_ForwardsToService()
    {
        IGumService service = GumService.Default;
        float original = service.CanvasWidth;

        try
        {
            service.CanvasWidth = 1234f;
            GumService.Default.CanvasWidth.ShouldBe(1234f);
        }
        finally
        {
            service.CanvasWidth = original;
        }
    }

    [Fact]
    public void IGumService_CanvasHeight_ForwardsToService()
    {
        IGumService service = GumService.Default;
        float original = service.CanvasHeight;

        try
        {
            service.CanvasHeight = 567f;
            GumService.Default.CanvasHeight.ShouldBe(567f);
        }
        finally
        {
            service.CanvasHeight = original;
        }
    }

    [Fact]
    public void IGumService_Initialize_OnXnaLikeRuntime_Throws()
    {
        IGumService service = GumService.Default;

        Should.Throw<NotSupportedException>(() => service.Initialize());
    }

    [Fact]
    public void IGumService_InitializeWithProjectPath_OnXnaLikeRuntime_Throws()
    {
        IGumService service = GumService.Default;

        Should.Throw<NotSupportedException>(() => service.Initialize("some.gumx"));
    }

    [Fact]
    public void IGumService_CreateCursor_OnXnaLikeRuntime_ReturnsNonNullCursor()
    {
        // The MonoGame GumService overrides the IGumService.CreateCursor default (which returns
        // null on render-only hosts). FormsUtilities.InitializeDefaults creates the cursor through
        // this service path, so this pins that the XNALIKE override is wired and non-null.
        IGumService service = GumService.Default;

        ICursor? cursor = service.CreateCursor();

        cursor.ShouldNotBeNull();
    }

    [Fact]
    public void IGumService_Cursor_ReturnsServiceCursor()
    {
        IGumService service = GumService.Default;

        ICursor cursor = service.Cursor;

        cursor.ShouldBeSameAs(GumService.Default.Cursor);
    }

    [Fact]
    public void IGumService_Default_CanBeSetAndCleared()
    {
        IGumService? original = IGumService.Default;
        try
        {
            IGumService.Default = GumService.Default;
            IGumService.Default.ShouldBeSameAs(GumService.Default);

            IGumService.Default = null;
            IGumService.Default.ShouldBeNull();
        }
        finally
        {
            IGumService.Default = original;
        }
    }
}
