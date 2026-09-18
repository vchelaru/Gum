using CommunityToolkit.Mvvm.Messaging;
using Gum.Services;
using Moq;
using PerformanceMeasurementPlugin.ViewModels;
using Shouldly;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pinning tests for PerformanceViewModel, relocated out of Gum.csproj into the headless
/// Gum.Presentation assembly (ADR-0005, #3754). Its direct reads/writes of the XNALIKE-only
/// RenderingLibrary.Graphics.Renderer/SystemManagers were replaced with the injected
/// IRenderDiagnosticsService seam, mirroring how IUiTimer replaced the WPF DispatcherTimer field
/// (#3786) — the concrete implementation stays behind, in PerformanceMeasurementPlugin.Services,
/// the one project with the KniGum reference needed to compile against those types.
/// </summary>
public class PerformanceViewModelTests
{
    [Fact]
    public void Constructor_StartsUiTimerAt500MillisecondInterval()
    {
        Mock<IUiTimer> uiTimer = new();
        Mock<IRenderDiagnosticsService> renderDiagnostics = new();

        PerformanceViewModel viewModel = new(uiTimer.Object, renderDiagnostics.Object, new WeakReferenceMessenger());

        uiTimer.Verify(t => t.Start(TimeSpan.FromMilliseconds(500)), Times.Once);
    }

    [Fact]
    public void CullOffscreenWhenClipped_SetToDifferentValue_WritesThroughAndRaisesPropertyChanged()
    {
        Mock<IUiTimer> uiTimer = new();
        Mock<IRenderDiagnosticsService> renderDiagnostics = new();
        renderDiagnostics.SetupGet(r => r.CullOffscreenWhenClipped).Returns(false);
        PerformanceViewModel viewModel = new(uiTimer.Object, renderDiagnostics.Object, new WeakReferenceMessenger());
        bool raised = false;
        viewModel.PropertyChanged += (_, _) => raised = true;

        viewModel.CullOffscreenWhenClipped = true;

        renderDiagnostics.VerifySet(r => r.CullOffscreenWhenClipped = true, Times.Once);
        raised.ShouldBeTrue();
    }

    [Fact]
    public void RenderDepthFirst_SetTrue_TurnsOffSortByBatchKey()
    {
        Mock<IUiTimer> uiTimer = new();
        Mock<IRenderDiagnosticsService> renderDiagnostics = new();
        renderDiagnostics.SetupGet(r => r.SortByBatchKey).Returns(true);
        PerformanceViewModel viewModel = new(uiTimer.Object, renderDiagnostics.Object, new WeakReferenceMessenger());

        viewModel.RenderDepthFirst = true;

        renderDiagnostics.VerifySet(r => r.SortByBatchKey = false, Times.Once);
    }

    [Fact]
    public void SortByBatchKey_SetToDifferentValue_WritesThroughAndRaisesPropertyChanged()
    {
        Mock<IUiTimer> uiTimer = new();
        Mock<IRenderDiagnosticsService> renderDiagnostics = new();
        renderDiagnostics.SetupGet(r => r.SortByBatchKey).Returns(false);
        PerformanceViewModel viewModel = new(uiTimer.Object, renderDiagnostics.Object, new WeakReferenceMessenger());
        bool raised = false;
        viewModel.PropertyChanged += (_, _) => raised = true;

        viewModel.SortByBatchKey = true;

        renderDiagnostics.VerifySet(r => r.SortByBatchKey = true, Times.Once);
        raised.ShouldBeTrue();
    }

    [Fact]
    public void SortByBatchKey_SetToSameValue_DoesNotWriteOrRaisePropertyChanged()
    {
        Mock<IUiTimer> uiTimer = new();
        Mock<IRenderDiagnosticsService> renderDiagnostics = new();
        renderDiagnostics.SetupGet(r => r.SortByBatchKey).Returns(true);
        PerformanceViewModel viewModel = new(uiTimer.Object, renderDiagnostics.Object, new WeakReferenceMessenger());
        bool raised = false;
        viewModel.PropertyChanged += (_, _) => raised = true;

        viewModel.SortByBatchKey = true;

        renderDiagnostics.VerifySet(r => r.SortByBatchKey = It.IsAny<bool>(), Times.Never);
        raised.ShouldBeFalse();
    }

    [Fact]
    public void SortByBatchKey_SetToDifferentValue_SendsSiblingOrderingChangedMessage()
    {
        Mock<IRenderDiagnosticsService> renderDiagnostics = new();
        renderDiagnostics.SetupProperty(r => r.SortByBatchKey, false);
        WeakReferenceMessenger messenger = new();
        List<SiblingOrderingChangedMessage> received = new();
        messenger.Register<SiblingOrderingChangedMessage>(this, (_, message) => received.Add(message));
        PerformanceViewModel viewModel = new(Mock.Of<IUiTimer>(), renderDiagnostics.Object, messenger);

        viewModel.SortByBatchKey = true;

        received.ShouldHaveSingleItem().SortByBatchKey.ShouldBeTrue();
    }

    [Fact]
    public void SortByBatchKey_SetToSameValue_SendsNoMessage()
    {
        Mock<IRenderDiagnosticsService> renderDiagnostics = new();
        renderDiagnostics.SetupProperty(r => r.SortByBatchKey, true);
        WeakReferenceMessenger messenger = new();
        List<SiblingOrderingChangedMessage> received = new();
        messenger.Register<SiblingOrderingChangedMessage>(this, (_, message) => received.Add(message));
        PerformanceViewModel viewModel = new(Mock.Of<IUiTimer>(), renderDiagnostics.Object, messenger);

        viewModel.SortByBatchKey = true;

        received.ShouldBeEmpty();
    }

    [Fact]
    public void TotalRenderStateChanges_SumsSpriteAndShapeBatchBeginCounts()
    {
        Mock<IUiTimer> uiTimer = new();
        Mock<IRenderDiagnosticsService> renderDiagnostics = new();
        renderDiagnostics.SetupGet(r => r.SpriteBatchBeginCount).Returns(3);
        renderDiagnostics.SetupGet(r => r.ShapeBatchBeginCount).Returns(2);
        PerformanceViewModel viewModel = new(uiTimer.Object, renderDiagnostics.Object, new WeakReferenceMessenger());

        viewModel.TotalRenderStateChanges.ShouldBe(5);
    }

    [Fact]
    public void UiTimerTick_RaisesPropertyChangedForAllProperties()
    {
        Mock<IUiTimer> uiTimer = new();
        Mock<IRenderDiagnosticsService> renderDiagnostics = new();
        PerformanceViewModel viewModel = new(uiTimer.Object, renderDiagnostics.Object, new WeakReferenceMessenger());
        PropertyChangedEventArgs? raisedArgs = null;
        viewModel.PropertyChanged += (_, e) => raisedArgs = e;

        uiTimer.Raise(t => t.Tick += null);

        raisedArgs.ShouldNotBeNull();
        raisedArgs.PropertyName.ShouldBeNull();
    }
}
