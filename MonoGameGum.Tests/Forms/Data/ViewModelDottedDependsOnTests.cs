using Gum.Mvvm;
using GumRuntime;
using MonoGameGum.TestsCommon;
using Shouldly;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;

namespace MonoGameGum.Tests.Forms.Data;

/// <summary>
/// <c>[DependsOn("Game.Gold")]</c>: a dependent property follows a dotted path into another
/// view model and is raised when any segment of that path changes.
/// </summary>
public class ViewModelDottedDependsOnTests : BaseTestClass
{
    private readonly Action<string>? _previousReporter = ElementSaveExtensions.ReportPropertyAssignmentError;
    private readonly List<string> _reports = new List<string>();

    public ViewModelDottedDependsOnTests()
    {
        ElementSaveExtensions.ReportPropertyAssignmentError = _reports.Add;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.ReportPropertyAssignmentError = _previousReporter;
        base.Dispose();
    }

    public class GameViewModel : ViewModel
    {
        public int Gold { get => Get<int>(); set => Set(value); }
        public GameViewModel? Partner { get => Get<GameViewModel?>(); set => Set(value); }
    }

    public class ShopItemViewModel : ViewModel
    {
        private GameViewModel? _game;

        public int Price { get => Get<int>(); set => Set(value); }
        public GameViewModel? Game { get => _game; set => Set(ref _game, value); }

        [DependsOn(nameof(Price))]
        [DependsOn("Game.Gold")]
        public bool CanAfford => Game != null && Game.Gold >= Price;
    }

    public class SessionViewModel : ViewModel
    {
        public ShopItemViewModel? Item { get => Get<ShopItemViewModel?>(); set => Set(value); }

        [DependsOn("Item.Game.Gold")]
        public int ItemGold => Item?.Game?.Gold ?? -1;
    }

    // The root is assigned once in the constructor without a notification.
    public class ReadOnlyRootViewModel : ViewModel
    {
        public ReadOnlyRootViewModel(GameViewModel game)
        {
            Game = game;
        }

        public GameViewModel Game { get; }

        [DependsOn("Game.Gold")]
        public int Gold => Game.Gold;
    }

    public class PlainNpc : INotifyPropertyChanged
    {
        private int _gold;

        public int Gold
        {
            get => _gold;
            set
            {
                _gold = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Gold)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public int HandlerCount => PropertyChanged?.GetInvocationList().Length ?? 0;
    }

    public class PlainRootViewModel : ViewModel
    {
        public PlainNpc? Wallet { get => Get<PlainNpc?>(); set => Set(value); }

        [DependsOn("Wallet.Gold")]
        public int Gold => Wallet?.Gold ?? 0;
    }

    // Each side's Mirror depends on the other side's Mirror: a dependency cycle.
    public class MirrorViewModel : ViewModel
    {
        public MirrorViewModel? Other { get => Get<MirrorViewModel?>(); set => Set(value); }

        [DependsOn("Other.Mirror")]
        public int Mirror => 0;

        public void RaiseMirror() => NotifyPropertyChanged(nameof(Mirror));
    }

    public class InvalidPathViewModel : ViewModel
    {
        public GameViewModel? Game { get => Get<GameViewModel?>(); set => Set(value); }

        [DependsOn("Game.Silver")]
        public int Silver => 0;
    }

    private static List<string> Record(INotifyPropertyChanged source)
    {
        List<string> raised = new List<string>();
        source.PropertyChanged += (_, e) => raised.Add(e.PropertyName!);
        return raised;
    }

    [Fact]
    public void LeafChange_RaisesDependent()
    {
        GameViewModel game = new GameViewModel();
        ShopItemViewModel item = new ShopItemViewModel { Price = 10, Game = game };
        List<string> raised = Record(item);

        game.Gold = 20;

        raised.ShouldBe(new[] { nameof(ShopItemViewModel.CanAfford) });
        item.CanAfford.ShouldBeTrue();
    }

    [Fact]
    public void LeafChange_RaisesDependent_WhenRootWasSetAfterSubscribing()
    {
        GameViewModel game = new GameViewModel();
        ShopItemViewModel item = new ShopItemViewModel { Price = 10 };
        List<string> raised = Record(item);
        item.Game = game;
        raised.Clear();

        game.Gold = 20;

        raised.ShouldBe(new[] { nameof(ShopItemViewModel.CanAfford) });
    }

    [Fact]
    public void RootReplaced_RaisesDependent_AndOldRootIsUnsubscribed()
    {
        GameViewModel oldGame = new GameViewModel();
        GameViewModel newGame = new GameViewModel { Gold = 50 };
        ShopItemViewModel item = new ShopItemViewModel { Price = 10, Game = oldGame };
        List<string> raised = Record(item);
        oldGame.GetPropertyChangeCount().ShouldBe(1);

        item.Game = newGame;

        raised.ShouldContain(nameof(ShopItemViewModel.CanAfford));
        oldGame.GetPropertyChangeCount().ShouldBe(0);
        raised.Clear();
        oldGame.Gold = 100;
        raised.ShouldBeEmpty();
        newGame.Gold = 0;
        raised.ShouldBe(new[] { nameof(ShopItemViewModel.CanAfford) });
    }

    [Fact]
    public void RootNulled_RaisesDependent_AndUnsubscribes()
    {
        GameViewModel game = new GameViewModel();
        ShopItemViewModel item = new ShopItemViewModel { Price = 10, Game = game };
        List<string> raised = Record(item);

        item.Game = null;

        raised.ShouldContain(nameof(ShopItemViewModel.CanAfford));
        item.CanAfford.ShouldBeFalse();
        game.GetPropertyChangeCount().ShouldBe(0);
    }

    [Fact]
    public void DeepPath_RaisesDependent_ForLeafAndEachIntermediate()
    {
        GameViewModel game = new GameViewModel();
        ShopItemViewModel item = new ShopItemViewModel { Game = game };
        SessionViewModel session = new SessionViewModel { Item = item };
        List<string> raised = Record(session);

        game.Gold = 5;
        raised.ShouldBe(new[] { nameof(SessionViewModel.ItemGold) });

        raised.Clear();
        GameViewModel otherGame = new GameViewModel { Gold = 7 };
        item.Game = otherGame;
        raised.ShouldBe(new[] { nameof(SessionViewModel.ItemGold) });
        session.ItemGold.ShouldBe(7);

        raised.Clear();
        game.Gold = 99;
        raised.ShouldBeEmpty();

        raised.Clear();
        session.Item = null;
        raised.ShouldContain(nameof(SessionViewModel.ItemGold));
        session.ItemGold.ShouldBe(-1);
    }

    [Fact]
    public void PlainINotifyPropertyChangedIntermediate_RaisesDependent()
    {
        PlainNpc wallet = new PlainNpc();
        PlainRootViewModel root = new PlainRootViewModel { Wallet = wallet };
        List<string> raised = Record(root);

        wallet.Gold = 3;

        raised.ShouldBe(new[] { nameof(PlainRootViewModel.Gold) });
    }

    [Fact]
    public void RootAssignedWithoutNotification_IsWatchedOnceSubscribed()
    {
        GameViewModel game = new GameViewModel();
        ReadOnlyRootViewModel root = new ReadOnlyRootViewModel(game);
        List<string> raised = Record(root);

        game.Gold = 4;

        raised.ShouldBe(new[] { nameof(ReadOnlyRootViewModel.Gold) });
    }

    [Fact]
    public void DependencyCycleAcrossViewModels_DoesNotRecurseForever()
    {
        MirrorViewModel first = new MirrorViewModel();
        MirrorViewModel second = new MirrorViewModel();
        first.Other = second;
        second.Other = first;
        List<string> firstRaised = Record(first);
        List<string> secondRaised = Record(second);

        first.RaiseMirror();

        firstRaised.ShouldContain(nameof(MirrorViewModel.Mirror));
        secondRaised.ShouldContain(nameof(MirrorViewModel.Mirror));
    }

    [Fact]
    public void ViewModelsReferencingEachOther_RaiseOnce()
    {
        GameViewModel first = new GameViewModel();
        GameViewModel second = new GameViewModel();
        first.Partner = second;
        second.Partner = first;
        ShopItemViewModel item = new ShopItemViewModel { Game = first };
        List<string> raised = Record(item);

        first.Gold = 1;

        raised.ShouldBe(new[] { nameof(ShopItemViewModel.CanAfford) });
    }

    [Fact]
    public void DroppedDependentViewModel_IsCollected_WhileReferencedViewModelLives()
    {
        GameViewModel game = new GameViewModel();
        WeakReference itemReference = CreateSubscribedItem(game);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        itemReference.IsAlive.ShouldBeFalse();
        // The stale weak listener removes itself on the next event.
        game.Gold = 1;
        game.GetPropertyChangeCount().ShouldBe(0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateSubscribedItem(GameViewModel game)
    {
        ShopItemViewModel item = new ShopItemViewModel { Game = game };
        item.PropertyChanged += (_, _) => { };
        game.GetPropertyChangeCount().ShouldBe(1);
        return new WeakReference(item);
    }

    public class MultiPathViewModel : ViewModel
    {
        public GameViewModel? Game { get => Get<GameViewModel?>(); set => Set(value); }
        public GameViewModel? Rival { get => Get<GameViewModel?>(); set => Set(value); }

        // Two dotted paths with different roots on one property.
        [DependsOn("Game.Gold")]
        [DependsOn("Rival.Gold")]
        public int Lead => (Game?.Gold ?? 0) - (Rival?.Gold ?? 0);

        // The same path on a second property.
        [DependsOn("Game.Gold")]
        public int GameGold => Game?.Gold ?? 0;

        // An undotted dependent of a dotted dependent.
        [DependsOn(nameof(Lead))]
        public bool IsAhead => Lead > 0;

        // Two paths sharing a root.
        [DependsOn("Game.Partner")]
        public bool HasPartner => Game?.Partner != null;

        public MultiPathViewModel CloneIt() => Clone<MultiPathViewModel>();
    }

    // A view model that depends on another view model's dotted dependent.
    public class OuterViewModel : ViewModel
    {
        public ShopItemViewModel? Item { get => Get<ShopItemViewModel?>(); set => Set(value); }

        [DependsOn("Item.CanAfford")]
        public bool ItemCanAfford => Item?.CanAfford ?? false;
    }

    public class CollectionHolderViewModel : ViewModel
    {
        public System.Collections.ObjectModel.ObservableCollection<GameViewModel>? Games
        {
            get => Get<System.Collections.ObjectModel.ObservableCollection<GameViewModel>?>();
            set => Set(value);
        }

        // An indexed root segment.
        [DependsOn("Games[0].Gold")]
        public int FirstGold => Games != null && Games.Count > 0 ? Games[0].Gold : -1;
    }

    public class NestedCollectionViewModel : ViewModel
    {
        public CollectionHolderViewModel? Holder { get => Get<CollectionHolderViewModel?>(); set => Set(value); }

        // An indexed intermediate segment.
        [DependsOn("Holder.Games[1].Gold")]
        public int SecondGold => Holder?.Games != null && Holder.Games.Count > 1 ? Holder.Games[1].Gold : -1;
    }

    [Fact]
    public void UndottedDependsOn_OnSameProperty_StillRaises()
    {
        ShopItemViewModel item = new ShopItemViewModel { Game = new GameViewModel() };
        List<string> raised = Record(item);

        item.Price = 3;

        raised.ShouldBe(new[] { nameof(ShopItemViewModel.Price), nameof(ShopItemViewModel.CanAfford) });
    }

    [Fact]
    public void TwoDottedPaths_DifferentRoots_EachRaise()
    {
        GameViewModel game = new GameViewModel();
        GameViewModel rival = new GameViewModel();
        MultiPathViewModel viewModel = new MultiPathViewModel { Game = game, Rival = rival };
        List<string> raised = Record(viewModel);

        rival.Gold = 2;

        raised.ShouldBe(new[] { nameof(MultiPathViewModel.Lead), nameof(MultiPathViewModel.IsAhead) });
    }

    [Fact]
    public void SamePath_OnTwoProperties_RaisesBoth_AndChainsUndottedDependent()
    {
        GameViewModel game = new GameViewModel();
        MultiPathViewModel viewModel = new MultiPathViewModel { Game = game };
        List<string> raised = Record(viewModel);

        game.Gold = 2;

        raised.ShouldBe(new[] { nameof(MultiPathViewModel.Lead), nameof(MultiPathViewModel.IsAhead), nameof(MultiPathViewModel.GameGold) },
            ignoreOrder: true);
        game.GetPropertyChangeCount().ShouldBe(2);
    }

    [Fact]
    public void TwoPathsSharingARoot_RaiseOnlyTheirOwnDependents()
    {
        GameViewModel game = new GameViewModel();
        MultiPathViewModel viewModel = new MultiPathViewModel { Game = game };
        List<string> raised = Record(viewModel);

        game.Partner = new GameViewModel();

        raised.ShouldBe(new[] { nameof(MultiPathViewModel.HasPartner) });
    }

    [Fact]
    public void DependsOnAnotherViewModelsDottedDependent_Raises()
    {
        GameViewModel game = new GameViewModel();
        ShopItemViewModel item = new ShopItemViewModel { Game = game, Price = 1 };
        OuterViewModel outer = new OuterViewModel { Item = item };
        List<string> raised = Record(outer);

        game.Gold = 1;

        raised.ShouldBe(new[] { nameof(OuterViewModel.ItemCanAfford) });
        outer.ItemCanAfford.ShouldBeTrue();
    }

    [Fact]
    public void IndexedRootSegment_RaisesOnItemChangeAndCollectionChange()
    {
        GameViewModel first = new GameViewModel();
        CollectionHolderViewModel holder = new CollectionHolderViewModel
        {
            Games = new System.Collections.ObjectModel.ObservableCollection<GameViewModel> { first },
        };
        List<string> raised = Record(holder);

        first.Gold = 4;
        raised.ShouldContain(nameof(CollectionHolderViewModel.FirstGold));

        raised.Clear();
        GameViewModel replacement = new GameViewModel { Gold = 9 };
        holder.Games!.Insert(0, replacement);
        raised.ShouldContain(nameof(CollectionHolderViewModel.FirstGold));
        holder.FirstGold.ShouldBe(9);

        raised.Clear();
        replacement.Gold = 10;
        raised.ShouldContain(nameof(CollectionHolderViewModel.FirstGold));
    }

    [Fact]
    public void IndexedIntermediateSegment_RaisesOnItemChangeAndCollectionChange()
    {
        CollectionHolderViewModel holder = new CollectionHolderViewModel
        {
            Games = new System.Collections.ObjectModel.ObservableCollection<GameViewModel> { new GameViewModel() },
        };
        NestedCollectionViewModel nested = new NestedCollectionViewModel { Holder = holder };
        List<string> raised = Record(nested);

        GameViewModel second = new GameViewModel { Gold = 3 };
        holder.Games!.Add(second);
        raised.ShouldContain(nameof(NestedCollectionViewModel.SecondGold));
        nested.SecondGold.ShouldBe(3);

        raised.Clear();
        second.Gold = 8;
        raised.ShouldContain(nameof(NestedCollectionViewModel.SecondGold));
    }

    [Fact]
    public void Clone_WatchesItsOwnRoot()
    {
        GameViewModel originalGame = new GameViewModel();
        MultiPathViewModel original = new MultiPathViewModel { Game = originalGame };
        MultiPathViewModel clone = original.CloneIt();
        GameViewModel cloneGame = new GameViewModel();
        List<string> originalRaised = Record(original);
        clone.Game = cloneGame;
        List<string> cloneRaised = Record(clone);
        originalRaised.Clear();

        cloneGame.Gold = 1;
        originalRaised.ShouldBeEmpty();
        cloneRaised.ShouldContain(nameof(MultiPathViewModel.GameGold));

        cloneRaised.Clear();
        originalGame.Gold = 1;
        originalRaised.ShouldContain(nameof(MultiPathViewModel.GameGold));
        cloneRaised.ShouldBeEmpty();
    }

    [Fact]
    public void SameGameValueSetAgain_DoesNotDoubleSubscribe()
    {
        GameViewModel game = new GameViewModel();
        ShopItemViewModel item = new ShopItemViewModel { Game = game };
        Record(item);

        item.Game = null;
        item.Game = game;
        item.Game = game;

        game.GetPropertyChangeCount().ShouldBe(1);
    }

    [Fact]
    public void UnsubscribedListener_DoesNotStopWatching_ForLaterListeners()
    {
        GameViewModel game = new GameViewModel();
        ShopItemViewModel item = new ShopItemViewModel { Game = game };
        System.ComponentModel.PropertyChangedEventHandler handler = (_, _) => { };
        item.PropertyChanged += handler;
        item.PropertyChanged -= handler;
        item.GetPropertyChangeCount().ShouldBe(0);
        List<string> raised = Record(item);

        game.Gold = 1;

        raised.ShouldBe(new[] { nameof(ShopItemViewModel.CanAfford) });
    }

    [Fact]
    public void DottedPath_AddsNoListenerOnTheViewModelItself()
    {
        ShopItemViewModel item = new ShopItemViewModel { Game = new GameViewModel() };

        Record(item);

        item.GetPropertyChangeCount().ShouldBe(1);
    }

    [Fact]
    public void ValidPath_WithNullRoot_ReportsNothing()
    {
        ShopItemViewModel item = new ShopItemViewModel();
        Record(item);
        item.Game = new GameViewModel();
        item.Game = null;

        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void InvalidSegment_ReportsOnce()
    {
        InvalidPathViewModel viewModel = new InvalidPathViewModel { Game = new GameViewModel() };
        Record(viewModel);
        viewModel.Game = new GameViewModel();

        _reports.Count.ShouldBe(1);
        _reports[0].ShouldContain("Game.Silver");
        _reports[0].ShouldContain(nameof(InvalidPathViewModel));
    }
}
