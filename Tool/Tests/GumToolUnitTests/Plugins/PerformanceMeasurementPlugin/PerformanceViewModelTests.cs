using Gum.Services;
using Moq;
using PerformanceMeasurementPlugin.ViewModels;
using Shouldly;
using System;
using System.ComponentModel;
using Xunit;

namespace GumToolUnitTests.Plugins.PerformanceMeasurementPlugin;

/// <summary>
/// Pinning tests for PerformanceViewModel's <see cref="IUiTimer"/> wiring. The WPF DispatcherTimer
/// field it previously owned directly was replaced with the injected IUiTimer seam built for
/// ElementAnimationsViewModel (#3786), enabling this test. The VM itself stays in Gum.csproj
/// (not moved to Gum.Presentation): it reads the XNALIKE-only RenderingLibrary.Graphics.Renderer,
/// which is compiled only into the runtime backends (KniGum etc.), not the headless assembly.
/// </summary>
public class PerformanceViewModelTests
{
    [Fact]
    public void Constructor_StartsUiTimerAt500MillisecondInterval()
    {
        Mock<IUiTimer> uiTimer = new();

        PerformanceViewModel viewModel = new(uiTimer.Object);

        uiTimer.Verify(t => t.Start(TimeSpan.FromMilliseconds(500)), Times.Once);
    }

    [Fact]
    public void UiTimerTick_RaisesPropertyChangedForAllProperties()
    {
        Mock<IUiTimer> uiTimer = new();
        PerformanceViewModel viewModel = new(uiTimer.Object);
        PropertyChangedEventArgs? raisedArgs = null;
        viewModel.PropertyChanged += (_, e) => raisedArgs = e;

        uiTimer.Raise(t => t.Tick += null);

        raisedArgs.ShouldNotBeNull();
        raisedArgs.PropertyName.ShouldBeNull();
    }
}
