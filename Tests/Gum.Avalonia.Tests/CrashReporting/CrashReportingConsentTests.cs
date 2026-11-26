using Gum.Avalonia.Diagnostics.CrashReporting;
using Gum.Menus;
using Gum.Services.Dialogs;
using Gum.Settings;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests.CrashReporting;

public class CrashReportingConsentTests
{
    private readonly MemorySettings _settings = new();
    private readonly Mock<IDialogService> _dialogService = new();

    private CrashReportingConsent Create() => new CrashReportingConsent(_settings, _dialogService.Object);

    private void AnswerPromptWith(MessageDialogResult result) =>
        _dialogService.Setup(x => x.ShowMessage(CrashReportingConsent.PromptMessage, CrashReportingConsent.PromptTitle,
                It.Is<MessageDialogStyle>(style => style.AffirmativeText == CrashReportingConsent.SendText
                    && style.NegativeText == CrashReportingConsent.DontSendText)))
            .Returns(result);

    [Theory]
    [InlineData(MessageDialogResult.Affirmative, true)]
    [InlineData(MessageDialogResult.Negative, false)]
    [InlineData(MessageDialogResult.Canceled, false)]
    public void PromptIfUnset_Unset_AsksOnceAndSavesTheAnswer(MessageDialogResult answer, bool expected)
    {
        AnswerPromptWith(answer);
        CrashReportingConsent consent = Create();

        consent.PromptIfUnset(isUnattended: false);
        consent.PromptIfUnset(isUnattended: false);

        _settings.CurrentValue.SendCrashReports.ShouldBe(expected);
        consent.IsSendingAllowed.ShouldBe(expected);
        _dialogService.Verify(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PromptIfUnset_AlreadyAnswered_DoesNotAsk(bool saved)
    {
        _settings.CurrentValue.SendCrashReports = saved;

        Create().PromptIfUnset(isUnattended: false);

        _dialogService.VerifyNoOtherCalls();
        _settings.CurrentValue.SendCrashReports.ShouldBe(saved);
    }

    [Fact]
    public void PromptIfUnset_Unattended_DoesNotAskOrSave()
    {
        Create().PromptIfUnset(isUnattended: true);

        _dialogService.VerifyNoOtherCalls();
        _settings.CurrentValue.SendCrashReports.ShouldBeNull();
    }

    [Fact]
    public void AddServices_EmptyDsn_RegistersNoConsent()
    {
        ServiceCollection services = new ServiceCollection();

        CrashReportingConsent.AddServices(services, "");

        services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(ICrashReportingConsent));
    }

    [Fact]
    public void AddServices_Dsn_RegistersTheConsent()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<IWritableOptions<CrashReportingSettings>>(_settings);
        services.AddSingleton(_dialogService.Object);

        CrashReportingConsent.AddServices(services, "https://key@example.invalid/1");

        services.BuildServiceProvider().GetService<ICrashReportingConsent>().ShouldBeOfType<CrashReportingConsent>();
    }

    [Fact]
    public void HelpMenuItem_ShowsAndTogglesTheSavedChoice()
    {
        _settings.CurrentValue.SendCrashReports = true;
        MenuModel menu = new MenuModel();
        menu.TopLevelItems.Add(new MenuItemModel("Help"));

        Create().AddHelpMenuItem(menu);
        MenuItemModel item = menu.GetItem("Help")!.Items.Single(x => x.Header == CrashReportingConsent.MenuHeader);

        item.IsCheckable.ShouldBeTrue();
        item.IsChecked.ShouldBeTrue();
        item.Invoke();
        _settings.CurrentValue.SendCrashReports.ShouldBe(false);
        item.Invoke();
        _settings.CurrentValue.SendCrashReports.ShouldBe(true);
    }

    private sealed class MemorySettings : IWritableOptions<CrashReportingSettings>
    {
        public CrashReportingSettings CurrentValue { get; } = new CrashReportingSettings();
        public CrashReportingSettings Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<CrashReportingSettings, string?> listener) => null;
        public void Update(Action<CrashReportingSettings> applyChanges) => applyChanges(CurrentValue);
    }
}
