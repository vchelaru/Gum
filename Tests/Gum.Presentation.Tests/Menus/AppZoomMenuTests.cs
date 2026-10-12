using CommunityToolkit.Mvvm.Messaging;
using Gum.Menus;
using Gum.Services;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.Menus;

public class AppZoomMenuTests
{
    private sealed class FakeAppScale : IAppScaleProvider
    {
        public double BaseFontSize { get; set; } = 12;
    }

    [Fact]
    public void ViewZoomRow_StepsTheWholeAppsFontSize_AndShowsItAsAPercentOfTheDefault()
    {
        FakeAppScale scale = new FakeAppScale { BaseFontSize = 12 };
        MenuStepper zoom = BuildZoomRow(scale);

        zoom.Label.ShouldBe("Zoom");
        zoom.ValueText.ShouldBe("100%");

        zoom.Increase();
        scale.BaseFontSize.ShouldBe(13);
        zoom.ValueText.ShouldBe("108%");

        zoom.Decrease();
        zoom.Decrease();
        scale.BaseFontSize.ShouldBe(11);
        zoom.ValueText.ShouldBe("92%");

        zoom.Reset();
        scale.BaseFontSize.ShouldBe(12);
        zoom.ValueText.ShouldBe("100%");
    }

    [Fact]
    public void ViewZoomRow_StopsAtTheLargestAndSmallestFontSize()
    {
        FakeAppScale scale = new FakeAppScale { BaseFontSize = 24 };
        MenuStepper zoom = BuildZoomRow(scale);
        zoom.ValueText.ShouldBe("200%");

        zoom.Increase();
        scale.BaseFontSize.ShouldBe(24);
        zoom.ValueText.ShouldBe("200%");

        scale.BaseFontSize = 6;
        zoom.Decrease();
        scale.BaseFontSize.ShouldBe(6);
        zoom.Reset();
        zoom.Decrease();
        for (int i = 0; i < 10; i++)
        {
            zoom.Decrease();
        }
        scale.BaseFontSize.ShouldBe(6);
        zoom.ValueText.ShouldBe("50%");
    }

    [Fact]
    public void ViewZoomRow_FollowsAFontSizeChangedFromAnotherPlace()
    {
        // The hotkeys and the Theming dialog change the same setting.
        FakeAppScale scale = new FakeAppScale();
        AutoMocker mocker = new AutoMocker();
        WeakReferenceMessenger messenger = new WeakReferenceMessenger();
        UiSettingsService settings = new UiSettingsService(messenger, scale);
        mocker.Use<IMessenger>(messenger);
        mocker.Use<IUiSettingsService>(settings);
        MenuStepper zoom = FindZoomRow(mocker);

        settings.BaseFontSize = 15;

        zoom.ValueText.ShouldBe("125%");
    }

    [Fact]
    public void ViewZoomRow_HasPlainItemHeadersForAMenuThatCannotHostControls()
    {
        MenuStepper zoom = BuildZoomRow(new FakeAppScale());

        zoom.IncreaseHeader.ShouldBe("Zoom In");
        zoom.DecreaseHeader.ShouldBe("Zoom Out");
        zoom.ResetHeader.ShouldBe("Actual Size");
    }

    private static MenuStepper BuildZoomRow(FakeAppScale scale)
    {
        AutoMocker mocker = new AutoMocker();
        WeakReferenceMessenger messenger = new WeakReferenceMessenger();
        mocker.Use<IMessenger>(messenger);
        mocker.Use<IUiSettingsService>(new UiSettingsService(messenger, scale));
        return FindZoomRow(mocker);
    }

    private static MenuStepper FindZoomRow(AutoMocker mocker)
    {
        StandardMenuModelBuilder builder = mocker.CreateInstance<StandardMenuModelBuilder>();
        MenuItemModel row = builder.Build().GetItem("View")!.Items.Single(item => item.Stepper != null);
        return row.Stepper!;
    }
}
