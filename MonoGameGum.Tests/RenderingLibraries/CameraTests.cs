using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

public class CameraTests : BaseTestClass
{
    [Fact]
    public void ScreenToWorld_ShouldRoundTripBackCorrectly()
    {
        Camera _sut = new Camera();
        _sut.ClientWidth = 800;
        _sut.ClientHeight = 600;

        _sut.ScreenToWorld(100, 150, out float worldX, out float worldY);

        _sut.WorldToScreen(worldX, worldY, out float screenX, out float screenY);

        screenX.ShouldBe(100);
        screenY.ShouldBe(150);
    }

    [Fact]
    public void ScreenToWorld_WithZoom_ShouldRoundTripBackCorrectly()
    {
        Camera _sut = new Camera();
        _sut.ClientWidth = 800;
        _sut.ClientHeight = 600;
        _sut.Zoom = 2;

        _sut.ScreenToWorld(100, 150, out float worldX, out float worldY);

        _sut.WorldToScreen(worldX, worldY, out float screenX, out float screenY);

        screenX.ShouldBe(100);
        screenY.ShouldBe(150);
    }

    [Fact]
    public void ScreenToWorld_WithClientLeftTop_ShouldRoundTripBackCorrectly()
    {
        Camera _sut = new Camera();
        _sut.ClientWidth = 800;
        _sut.ClientHeight = 600;
        _sut.ClientLeft = 50;
        _sut.ClientTop = 75;

        _sut.ScreenToWorld(100, 150, out float worldX, out float worldY);

        _sut.WorldToScreen(worldX, worldY, out float screenX, out float screenY);

        screenX.ShouldBe(100);
        screenY.ShouldBe(150);
    }
}
