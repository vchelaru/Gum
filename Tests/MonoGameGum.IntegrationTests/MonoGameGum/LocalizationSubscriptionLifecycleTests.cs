using Gum.Forms;
using Gum.Localization;
using Shouldly;
using System;
using System.Collections.Generic;


namespace MonoGameGum.IntegrationTests.MonoGameGum;

// The enclosing MonoGameGum namespace holds an obsolete GumService shim; bind to the real type.
using GumService = global::Gum.GumService;


/// <summary>
/// Every <see cref="GumService"/> listens for language changes on the process-wide localization service
/// so it can re-translate its live text. A service that has been uninitialized has no live text and must
/// stop listening: otherwise each Initialize/Uninitialize cycle (every lazily recreated
/// <c>GumService.Default</c>) leaves one more service, and the UI tree it holds, subscribed for the life of
/// the process, and a language change refreshes the shared popup roots once per leaked service (#6066).
/// </summary>
public class LocalizationSubscriptionLifecycleTests : BaseTestClass
{
    private sealed class CountingLocalizationService : ILocalizationService
    {
        private Action? _changed;
        public int SubscriberCount { get; private set; }

        public int CurrentLanguage { get; set; }
        public IReadOnlyList<string> Languages { get; } = new List<string> { "English" };

        public event Action? CurrentLanguageChanged
        {
            add { _changed += value; SubscriberCount++; }
            remove { _changed -= value; SubscriberCount--; }
        }

        public void RaiseLanguageChanged() => _changed?.Invoke();
        public void AddDatabase(Dictionary<string, string[]> entryDictionary, List<string> headerList) { }
        public void Clear() { }
        public string Translate(string stringId) => stringId;
    }

    private readonly ILocalizationService? _previousService = Gum.Wireframe.CustomSetPropertyOnRenderable.LocalizationService;
    private readonly CountingLocalizationService _localization = new();

    public LocalizationSubscriptionLifecycleTests()
    {
        Gum.Wireframe.CustomSetPropertyOnRenderable.LocalizationService = _localization;
    }

    public override void Dispose()
    {
        Gum.Wireframe.CustomSetPropertyOnRenderable.LocalizationService = _previousService;
        base.Dispose();
    }

    [Fact]
    public void Uninitialize_StopsTheServiceListeningForLanguageChanges()
    {
        using ServiceTestGame game = new();
        game.Start();
        int before = _localization.SubscriberCount;
        GumService service = game.CreateService();
        service.Initialize(game, DefaultVisualsVersion.V3);
        _localization.SubscriberCount.ShouldBe(before + 1);

        service.Uninitialize();

        _localization.SubscriberCount.ShouldBe(before);
    }

    [Fact]
    public void Initialize_AfterUninitialize_ListensForLanguageChangesAgain()
    {
        using ServiceTestGame game = new();
        game.Start();
        int before = _localization.SubscriberCount;
        GumService service = game.CreateService();
        service.Initialize(game, DefaultVisualsVersion.V3);
        service.Uninitialize();

        service.Initialize(game, DefaultVisualsVersion.V3);

        _localization.SubscriberCount.ShouldBe(before + 1);
    }

    [Fact]
    public void InitializeAndUninitialize_RepeatedOnNewServices_DoNotAccumulateSubscribers()
    {
        using ServiceTestGame game = new();
        game.Start();
        int before = _localization.SubscriberCount;

        for (int i = 0; i < 3; i++)
        {
            GumService service = game.CreateService();
            service.Initialize(game, DefaultVisualsVersion.V3);
            service.Uninitialize();
        }

        _localization.SubscriberCount.ShouldBe(before);
    }
}
