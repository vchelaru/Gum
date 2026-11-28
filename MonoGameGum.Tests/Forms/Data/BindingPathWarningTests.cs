using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.GueDeriving;
using Gum.Mvvm;
using GumRuntime;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Forms.Data;

/// <summary>
/// A string binding path that names a member the BindingContext's type does not have is reported once per
/// binding through the [Gum] PropertyAssignmentError channel.
/// </summary>
public class BindingPathWarningTests : BaseTestClass
{
    private readonly Action<string>? _previousReporter;
    private readonly List<string> _reports;

    public BindingPathWarningTests()
    {
        _previousReporter = ElementSaveExtensions.ReportPropertyAssignmentError;
        _reports = new List<string>();
        ElementSaveExtensions.ReportPropertyAssignmentError = _reports.Add;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.ReportPropertyAssignmentError = _previousReporter;
        base.Dispose();
    }

    [Fact]
    public void FrameworkElementSetBinding_MisspelledPath_ReportsOnce()
    {
        Label label = new Label();
        GameViewModel viewModel = new GameViewModel();
        label.BindingContext = viewModel;

        label.SetBinding(nameof(Label.Text), "GoldDisplayy");
        viewModel.Gold = 5;
        viewModel.Gold = 6;
        label.BindingContext = new GameViewModel();

        _reports.ShouldBe(new[]
        {
            "Binding path 'GoldDisplayy' for Label.Text does not resolve: GameViewModel has no property 'GoldDisplayy' (BindingContext: GameViewModel)."
        });
    }

    [Fact]
    public void FrameworkElementSetBinding_MissingNestedSegment_ReportsSegmentAndItsType()
    {
        Label label = new Label();
        label.BindingContext = new GameViewModel();

        label.SetBinding(nameof(Label.Text), new Binding("Player.Nmae"));

        _reports.ShouldBe(new[]
        {
            "Binding path 'Player.Nmae' for Label.Text does not resolve: PlayerViewModel has no property 'Nmae' (BindingContext: GameViewModel)."
        });
    }

    [Fact]
    public void FrameworkElementSetBinding_NullContext_WaitsAndReportsWhenContextArrives()
    {
        Label label = new Label();

        label.SetBinding(nameof(Label.Text), "GoldDisplayy");
        int reportsWithoutContext = _reports.Count;
        label.BindingContext = new GameViewModel();

        reportsWithoutContext.ShouldBe(0);
        _reports.Count.ShouldBe(1);
    }

    [Fact]
    public void FrameworkElementSetBinding_ValidPaths_NeverReport()
    {
        Label flat = new Label();
        Label nested = new Label();
        Label indexed = new Label();
        Label untyped = new Label();
        GameViewModel viewModel = new GameViewModel();
        flat.BindingContext = viewModel;
        nested.BindingContext = viewModel;
        indexed.BindingContext = viewModel;
        untyped.BindingContext = viewModel;

        flat.SetBinding(nameof(Label.Text), nameof(GameViewModel.GoldDisplay));
        nested.SetBinding(nameof(Label.Text), new Binding("Player.Name"));
        indexed.SetBinding(nameof(Label.Text), new Binding("Players[0].Name"));
        untyped.SetBinding(nameof(Label.Text), new Binding("Untyped.Name"));
        viewModel.Gold = 5;
        viewModel.Player = null;

        _reports.ShouldBeEmpty();
        flat.Text.ShouldBe("5 gold");
    }

    [Fact]
    public void GraphicalUiElementSetBinding_MisspelledPath_ReportsOnce()
    {
        TextRuntime text = new TextRuntime();
        GameViewModel viewModel = new GameViewModel();
        text.BindingContext = viewModel;

        text.SetBinding(nameof(TextRuntime.Text), "GoldDisplayy");
        viewModel.Gold = 5;
        text.BindingContext = null;
        text.BindingContext = viewModel;

        _reports.ShouldBe(new[]
        {
            "Binding path 'GoldDisplayy' for TextRuntime.Text does not resolve: GameViewModel has no property 'GoldDisplayy' (BindingContext: GameViewModel)."
        });
    }

    [Fact]
    public void GraphicalUiElementSetBinding_InheritedContextOfOtherTypeThenOwnContext_ReportsOwnContextMiss()
    {
        ContainerRuntime screen = new ContainerRuntime();
        TextRuntime text = new TextRuntime();
        screen.AddChild(text);
        text.SetBinding(nameof(TextRuntime.Text), "Nmae");

        // The parent's context reaches the child first, as when a screen sets its own context and then gives
        // each child component its view model.
        screen.BindingContext = new GameViewModel();
        text.BindingContext = new PlayerViewModel();

        _reports.ShouldBe(new[]
        {
            "Binding path 'Nmae' for TextRuntime.Text does not resolve: GameViewModel has no property 'Nmae' (BindingContext: GameViewModel).",
            "Binding path 'Nmae' for TextRuntime.Text does not resolve: PlayerViewModel has no property 'Nmae' (BindingContext: PlayerViewModel)."
        }, ignoreOrder: false,
            customMessage: "because the miss against the view model the binding is meant for must not be hidden by a report against a transient inherited context");
    }

    [Fact]
    public void FrameworkElementSetBinding_InheritedContextOfOtherTypeThenOwnContext_ReportsOwnContextMiss()
    {
        StackPanel screen = new StackPanel();
        Label label = new Label();
        screen.AddChild(label);
        label.SetBinding(nameof(Label.Text), "Nmae");

        screen.BindingContext = new GameViewModel();
        label.BindingContext = new PlayerViewModel();

        _reports.ShouldBe(new[]
        {
            "Binding path 'Nmae' for Label.Text does not resolve: GameViewModel has no property 'Nmae' (BindingContext: GameViewModel).",
            "Binding path 'Nmae' for Label.Text does not resolve: PlayerViewModel has no property 'Nmae' (BindingContext: PlayerViewModel)."
        }, ignoreOrder: false,
            customMessage: "because the miss against the view model the binding is meant for must not be hidden by a report against a transient inherited context");
    }

    [Fact]
    public void GraphicalUiElementSetBinding_MissingNestedSegment_ReportsSegmentAndItsType()
    {
        TextRuntime text = new TextRuntime();
        text.BindingContext = new GameViewModel();

        text.SetBinding(nameof(TextRuntime.Text), "Player.Nmae");

        _reports.ShouldBe(new[]
        {
            "Binding path 'Player.Nmae' for TextRuntime.Text does not resolve: PlayerViewModel has no property 'Nmae' (BindingContext: GameViewModel)."
        });
    }

    [Fact]
    public void GraphicalUiElementSetBinding_MisspelledBindingContextPath_ReportsOnce()
    {
        ContainerRuntime parent = new ContainerRuntime();
        ContainerRuntime child = new ContainerRuntime();
        parent.AddChild(child);
        GameViewModel viewModel = new GameViewModel();
        parent.BindingContext = viewModel;

        child.SetBinding(nameof(ContainerRuntime.BindingContext), "Playr");
        parent.BindingContext = new GameViewModel();

        _reports.ShouldBe(new[]
        {
            "Binding path 'Playr' for ContainerRuntime.BindingContext does not resolve: GameViewModel has no property 'Playr' (BindingContext: GameViewModel)."
        });
    }

    [Fact]
    public void GraphicalUiElementSetBinding_NullContext_WaitsAndReportsWhenContextArrives()
    {
        TextRuntime text = new TextRuntime();

        text.SetBinding(nameof(TextRuntime.Text), "GoldDisplayy");
        int reportsWithoutContext = _reports.Count;
        text.BindingContext = new GameViewModel();

        reportsWithoutContext.ShouldBe(0);
        _reports.Count.ShouldBe(1);
    }

    [Fact]
    public void GraphicalUiElementSetBinding_ValidPaths_NeverReport()
    {
        TextRuntime flat = new TextRuntime();
        TextRuntime nested = new TextRuntime();
        TextRuntime indexed = new TextRuntime();
        TextRuntime untyped = new TextRuntime();
        GameViewModel viewModel = new GameViewModel();
        flat.BindingContext = viewModel;
        nested.BindingContext = viewModel;
        indexed.BindingContext = viewModel;
        untyped.BindingContext = viewModel;

        flat.SetBinding(nameof(TextRuntime.Text), nameof(GameViewModel.GoldDisplay));
        nested.SetBinding(nameof(TextRuntime.Text), "Player.Name");
        indexed.SetBinding(nameof(TextRuntime.Text), "Players[0].Name");
        untyped.SetBinding(nameof(TextRuntime.Text), "Untyped.Name");
        viewModel.Gold = 5;
        viewModel.Player = null;

        _reports.ShouldBeEmpty();
        flat.Text.ShouldBe("5 gold");
    }

    [Fact]
    public void SetBinding_BaseTypedIntermediate_MemberOnRuntimeType_NeverReports()
    {
        GameViewModel viewModel = new GameViewModel { Champion = new ChampionViewModel { Title = "Legendary" } };
        Label label = new Label { BindingContext = viewModel };
        TextRuntime text = new TextRuntime { BindingContext = viewModel };

        label.SetBinding(nameof(Label.Text), new Binding("Champion.Title"));
        text.SetBinding(nameof(TextRuntime.Text), "Champion.Title");

        _reports.ShouldBeEmpty();
        label.Text.ShouldBe("Legendary");
        text.Text.ShouldBe("Legendary");
    }

    [Fact]
    public void SetBinding_BaseTypedIntermediate_MemberMissingOnRuntimeType_ReportsRuntimeType()
    {
        GameViewModel viewModel = new GameViewModel { Champion = new ChampionViewModel() };
        Label label = new Label { BindingContext = viewModel };
        TextRuntime text = new TextRuntime { BindingContext = viewModel };

        label.SetBinding(nameof(Label.Text), new Binding("Champion.Titel"));
        text.SetBinding(nameof(TextRuntime.Text), "Champion.Titel");

        _reports.ShouldBe(new[]
        {
            "Binding path 'Champion.Titel' for Label.Text does not resolve: ChampionViewModel has no property 'Titel' (BindingContext: GameViewModel).",
            "Binding path 'Champion.Titel' for TextRuntime.Text does not resolve: ChampionViewModel has no property 'Titel' (BindingContext: GameViewModel)."
        });
    }

    [Fact]
    public void SetBinding_NullIntermediate_ReportsOnlyWhenNoRuntimeTypeCouldHaveMember()
    {
        GameViewModel viewModel = new GameViewModel { Champion = null };
        Label baseTyped = new Label { BindingContext = viewModel };
        Label sealedTyped = new Label { BindingContext = viewModel };

        baseTyped.SetBinding(nameof(Label.Text), new Binding("Champion.Title"));
        sealedTyped.SetBinding(nameof(Label.Text), new Binding("Referee.Title"));

        _reports.ShouldBe(new[]
        {
            "Binding path 'Referee.Title' for Label.Text does not resolve: RefereeViewModel has no property 'Title' (BindingContext: GameViewModel)."
        });
    }

    [Fact]
    public void FrameworkElementSetBinding_MisspelledRootPath_ShowsFallbackValue()
    {
        Label label = new Label { BindingContext = new GameViewModel() };

        label.SetBinding(nameof(Label.Text), new Binding("GoldDisplayy") { FallbackValue = "fallback" });

        label.Text.ShouldBe("fallback");
    }

    private sealed class RefereeViewModel : ViewModel
    {
    }

    private class ChampionViewModel : PlayerViewModel
    {
        public string Title
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    private class GameViewModel : ViewModel
    {
        public PlayerViewModel? Champion
        {
            get => Get<PlayerViewModel?>();
            set => Set(value);
        }

        public RefereeViewModel? Referee
        {
            get => Get<RefereeViewModel?>();
            set => Set(value);
        }

        public int Gold
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(Gold))]
        public string GoldDisplay => $"{Gold} gold";

        public PlayerViewModel? Player
        {
            get => Get<PlayerViewModel?>();
            set => Set(value);
        }

        public List<PlayerViewModel> Players { get; } = new List<PlayerViewModel> { new PlayerViewModel() };

        public object Untyped { get; } = new PlayerViewModel();

        public GameViewModel()
        {
            Player = new PlayerViewModel();
        }
    }

    private class PlayerViewModel : ViewModel
    {
        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }
    }
}
