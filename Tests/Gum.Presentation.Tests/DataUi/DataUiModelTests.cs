using System.Collections.Generic;
using System.Linq;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.DataUi;

/// <summary>
/// The neutral grid model's behaviors the two heads rely on: category visibility and menus,
/// context-menu entries and Make Default, multi-select grouping, and header colors.
/// </summary>
public class DataUiModelTests
{
    private sealed class Target
    {
        public float Width { get; set; }
        public float Height { get; set; }
    }

    private sealed class DefaultableMember : InstanceMember
    {
        private bool _isDefault;

        public DefaultableMember(string name, object instance) : base(name, instance)
        {
        }

        public int ResetCount { get; private set; }

        public override bool IsDefault
        {
            get => _isDefault;
            set
            {
                _isDefault = value;
                ResetCount++;
            }
        }
    }

    private sealed class RecordingDataUi : IDataUi, ISetDefaultable
    {
        public InstanceMember? InstanceMember { get; set; }
        public bool SuppressSettingProperty { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int RefreshCount { get; private set; }
        public bool WasSetToDefault { get; private set; }

        public void Refresh(bool forceRefreshEvenIfFocused = false) => RefreshCount++;

        public ApplyValueResult TryGetValueOnUi(out object? result)
        {
            result = null;
            return ApplyValueResult.Success;
        }

        public ApplyValueResult TrySetValueOnUi(object value) => ApplyValueResult.Success;

        public void SetToDefault() => WasSetToDefault = true;
    }

    [Fact]
    public void MemberCategory_IsVisible_TracksWhetherItHasMembers()
    {
        MemberCategory category = new MemberCategory("Position");
        List<string> changed = new List<string>();
        category.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        category.IsVisible.ShouldBeFalse();
        category.Members.Add(new InstanceMember("X", new Target()));

        category.IsVisible.ShouldBeTrue();
        changed.ShouldContain(nameof(MemberCategory.IsVisible));
    }

    [Fact]
    public void MemberCategoryContextMenuItem_RaiseCanExecuteChanged_NotifiesBoundMenus()
    {
        bool canPaste = false;
        MemberCategoryContextMenuItem item = new MemberCategoryContextMenuItem("Paste Values", () => { }, () => canPaste);
        int notifications = 0;
        item.CanExecuteChanged += (_, _) => notifications++;

        item.CanExecute(null).ShouldBeFalse();
        canPaste = true;
        item.RaiseCanExecuteChanged();

        notifications.ShouldBe(1);
        item.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public void GetContextMenuEntries_ListsMakeDefaultThenTheMembersEvents()
    {
        InstanceMember member = new InstanceMember("Width", new Target());
        object? sender = null;
        member.ContextMenuEvents.Add("Copy Qualified Variable Name", (s, _) => sender = s);
        RecordingDataUi dataUi = new RecordingDataUi { InstanceMember = member };

        List<DataUiContextMenuEntry> entries = dataUi.GetContextMenuEntries();

        entries.Select(e => e.Header).ShouldBe(new[] { "Make Default", "Copy Qualified Variable Name" });
        entries[1].Execute();
        sender.ShouldBe(member);
    }

    [Fact]
    public void GetContextMenuEntries_OmitsMakeDefault_WhenTheMemberDoesNotSupportIt()
    {
        InstanceMember member = new InstanceMember("Width", new Target()) { SupportsMakeDefault = false };
        RecordingDataUi dataUi = new RecordingDataUi { InstanceMember = member };

        dataUi.GetContextMenuEntries().ShouldBeEmpty();
    }

    [Fact]
    public void MakeDefault_ResetsRefreshesAndReportsAUiSet()
    {
        DefaultableMember member = new DefaultableMember("Width", new Target());
        int afterSetCount = 0;
        member.AfterSetByUi += (_, _) => afterSetCount++;
        RecordingDataUi dataUi = new RecordingDataUi { InstanceMember = member };

        dataUi.MakeDefault();

        member.ResetCount.ShouldBe(1);
        dataUi.RefreshCount.ShouldBe(1);
        dataUi.WasSetToDefault.ShouldBeTrue();
        afterSetCount.ShouldBe(1);
    }

    [Fact]
    public void SetMultipleCategoryLists_GroupsSameNamedMembersIntoOneEditor()
    {
        Target first = new Target { Width = 5 };
        Target second = new Target { Width = 5 };
        MemberCategory firstCategory = new MemberCategory("Dimensions") { HeaderColor = System.Drawing.Color.Red };
        firstCategory.Members.Add(new InstanceMember(nameof(Target.Width), first));
        MemberCategory secondCategory = new MemberCategory("Dimensions");
        secondCategory.Members.Add(new InstanceMember(nameof(Target.Width), second));
        DataUiGridModel grid = new DataUiGridModel();

        grid.SetMultipleCategoryLists(new List<List<MemberCategory>>
        {
            new List<MemberCategory> { firstCategory },
            new List<MemberCategory> { secondCategory },
        });

        grid.Categories.Count.ShouldBe(1);
        grid.Categories[0].HeaderColor.ShouldBe(System.Drawing.Color.Red);
        MultiSelectInstanceMember multi = grid.Categories[0].Members.Single().ShouldBeOfType<MultiSelectInstanceMember>();
        multi.SetValue(9f, SetPropertyCommitType.Full);
        first.Width.ShouldBe(9f);
        second.Width.ShouldBe(9f);
    }

    [Fact]
    public void Instance_ReflectsPublicPropertiesIntoAnUncategorizedCategory()
    {
        DataUiGridModel grid = new DataUiGridModel();
        grid.MembersToIgnore.Add(nameof(Target.Height));

        grid.Instance = new Target();

        grid.Categories.Single().Name.ShouldBe("Uncategorized");
        grid.Categories.Single().Members.Select(m => m.Name).ShouldBe(new[] { nameof(Target.Width) });
    }

    [Theory]
    [InlineData("#204300FF", 0x20, 0x43, 0x00, 0xFF)]
    [InlineData("#4300FF", 0xFF, 0x43, 0x00, 0xFF)]
    public void VariableCategoryDescriptor_GetHeaderColor_ParsesArgbAndRgbHex(string hex, int a, int r, int g, int b)
    {
        VariableCategoryDescriptor descriptor = new VariableCategoryDescriptor("General") { HeaderColorHex = hex };

        descriptor.GetHeaderColor().ShouldBe(System.Drawing.Color.FromArgb(a, r, g, b));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    public void VariableCategoryDescriptor_GetHeaderColor_ReturnsNullForMissingOrInvalidHex(string? hex)
    {
        VariableCategoryDescriptor descriptor = new VariableCategoryDescriptor("General") { HeaderColorHex = hex };

        descriptor.GetHeaderColor().ShouldBeNull();
    }
}
