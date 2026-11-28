using System;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// When an intermediate object on a dotted path is replaced, the binding stops listening to the old one, even
/// when the root itself raises no PropertyChanged (a plain class holding view models).
/// </summary>
public class NestedPathReplacedIntermediateTests : BaseTestClass
{
    private class CountingConverter : IValueConverter
    {
        public int ConvertCount;

        public object? Convert(object? value, Type targetType, object? parameter)
        {
            ConvertCount++;
            return value;
        }

        public object? ConvertBack(object? value, Type sourceType, object? parameter) => value;
    }

    private class StatsViewModel : ViewModel
    {
        public string? Health
        {
            get => Get<string?>();
            set => Set(value);
        }
    }

    private class PlayerViewModel : ViewModel
    {
        public StatsViewModel? Stats
        {
            get => Get<StatsViewModel?>();
            set => Set(value);
        }
    }

    private class PlainRoot
    {
        public PlayerViewModel Player { get; } = new();
    }

    private class NotifyingRoot : ViewModel
    {
        public PlayerViewModel Player { get; } = new();
    }

    [Fact]
    public void PlainRoot_IntermediateReplaced_OldIntermediateChange_DoesNotUpdateTarget()
    {
        PlainRoot root = new();
        StatsViewModel oldStats = new() { Health = "10" };
        root.Player.Stats = oldStats;
        CountingConverter converter = new();
        Label label = new() { BindingContext = root };
        label.SetBinding(nameof(Label.Text), new Binding("Player.Stats.Health") { Converter = converter });

        root.Player.Stats = new StatsViewModel { Health = "20" };
        int countAfterReplace = converter.ConvertCount;
        oldStats.Health = "0";

        label.Text.ShouldBe("20");
        converter.ConvertCount.ShouldBe(countAfterReplace);
    }

    [Fact]
    public void NotifyingRoot_IntermediateReplaced_OldIntermediateChange_DoesNotUpdateTarget()
    {
        NotifyingRoot root = new();
        StatsViewModel oldStats = new() { Health = "10" };
        root.Player.Stats = oldStats;
        CountingConverter converter = new();
        Label label = new() { BindingContext = root };
        label.SetBinding(nameof(Label.Text), new Binding("Player.Stats.Health") { Converter = converter });

        root.Player.Stats = new StatsViewModel { Health = "20" };
        int countAfterReplace = converter.ConvertCount;
        oldStats.Health = "0";

        label.Text.ShouldBe("20");
        converter.ConvertCount.ShouldBe(countAfterReplace);
    }

    [Fact]
    public void PlainRoot_NewIntermediateChange_UpdatesTarget()
    {
        PlainRoot root = new();
        root.Player.Stats = new StatsViewModel { Health = "10" };
        Label label = new() { BindingContext = root };
        label.SetBinding(nameof(Label.Text), "Player.Stats.Health");

        StatsViewModel newStats = new() { Health = "20" };
        root.Player.Stats = newStats;
        newStats.Health = "15";

        label.Text.ShouldBe("15");
    }

    [Fact]
    public void IntermediateReplacedRepeatedly_EachReplaceConvertsOnce()
    {
        PlainRoot root = new();
        root.Player.Stats = new StatsViewModel { Health = "10" };
        CountingConverter converter = new();
        Label label = new() { BindingContext = root };
        label.SetBinding(nameof(Label.Text), new Binding("Player.Stats.Health") { Converter = converter });

        root.Player.Stats = new StatsViewModel { Health = "20" };
        int countAfterFirstReplace = converter.ConvertCount;
        root.Player.Stats = new StatsViewModel { Health = "30" };

        label.Text.ShouldBe("30");
        converter.ConvertCount.ShouldBe(countAfterFirstReplace + 1);
    }
}
