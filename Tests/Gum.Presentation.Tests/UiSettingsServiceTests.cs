using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Services;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="UiSettingsService"/>'s behavior after it moved off the WPF-typed
/// <c>AppScale</c>/<c>Application.Current.Resources</c> read to the <see cref="IAppScaleProvider"/>
/// seam (ADR-0005, issue #3754).
/// </summary>
public class UiSettingsServiceTests
{
    private readonly WeakReferenceMessenger _messenger;
    private readonly Mock<IAppScaleProvider> _appScaleProvider;
    private readonly UiSettingsService _service;
    private readonly List<UiBaseFontSizeChangedMessage> _receivedMessages;

    public UiSettingsServiceTests()
    {
        _messenger = new WeakReferenceMessenger();
        _appScaleProvider = new Mock<IAppScaleProvider>();
        _service = new UiSettingsService(_messenger, _appScaleProvider.Object);

        _receivedMessages = new List<UiBaseFontSizeChangedMessage>();
        _messenger.Register<UiBaseFontSizeChangedMessage>(this, (_, message) => _receivedMessages.Add(message));
    }

    [Fact]
    public void BaseFontSize_Get_ReturnsAppScaleProviderValue()
    {
        _appScaleProvider.SetupGet(x => x.BaseFontSize).Returns(14);

        double result = _service.BaseFontSize;

        result.ShouldBe(14);
    }

    [Fact]
    public void BaseFontSize_Set_WithValueInRange_UpdatesAppScaleProvider()
    {
        _service.BaseFontSize = 16;

        _appScaleProvider.VerifySet(x => x.BaseFontSize = 16, Times.Once);
    }

    [Fact]
    public void BaseFontSize_Set_WithValueInRange_SendsUiBaseFontSizeChangedMessage()
    {
        _service.BaseFontSize = 16;

        _receivedMessages.Count.ShouldBe(1);
        _receivedMessages[0].Size.ShouldBe(16);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(25)]
    public void BaseFontSize_Set_WithValueOutOfRange_DoesNotUpdateAppScaleProvider(double value)
    {
        _service.BaseFontSize = value;

        _appScaleProvider.VerifySet(x => x.BaseFontSize = It.IsAny<double>(), Times.Never);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(25)]
    public void BaseFontSize_Set_WithValueOutOfRange_DoesNotSendMessage(double value)
    {
        _service.BaseFontSize = value;

        _receivedMessages.ShouldBeEmpty();
    }
}
