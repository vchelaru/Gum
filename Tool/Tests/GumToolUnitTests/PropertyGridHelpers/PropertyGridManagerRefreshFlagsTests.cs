using Gum.DataTypes;
using Gum.Managers;
using Shouldly;
using WpfDataUi.DataTypes;

namespace GumToolUnitTests.PropertyGridHelpers;

public class PropertyGridManagerRefreshFlagsTests : BaseTestClass
{
    [Fact]
    public void DetermineRefreshFlags_InstanceCountChanged_ReturnsFullRebuild()
    {
        InstanceSave instance = new InstanceSave();
        List<InstanceSave> oldInstances = new List<InstanceSave> { instance };
        List<InstanceSave> newInstances = new List<InstanceSave> { instance, new InstanceSave() };

        (bool hasChangedObjectShowing, bool instanceIdentityChanged) =
            PropertyGridManager.DetermineRefreshFlags(structuralChange: false, newInstances, oldInstances);

        hasChangedObjectShowing.ShouldBeTrue();
        instanceIdentityChanged.ShouldBeFalse();
    }

    [Fact]
    public void DetermineRefreshFlags_MultiSelectInstanceChanged_ReturnsFullRebuild()
    {
        InstanceSave oldInstanceA = new InstanceSave();
        InstanceSave oldInstanceB = new InstanceSave();
        InstanceSave newInstanceB = new InstanceSave();
        List<InstanceSave> oldInstances = new List<InstanceSave> { oldInstanceA, oldInstanceB };
        List<InstanceSave> newInstances = new List<InstanceSave> { oldInstanceA, newInstanceB };

        (bool hasChangedObjectShowing, bool instanceIdentityChanged) =
            PropertyGridManager.DetermineRefreshFlags(structuralChange: false, newInstances, oldInstances);

        hasChangedObjectShowing.ShouldBeTrue();
        instanceIdentityChanged.ShouldBeFalse();
    }

    [Fact]
    public void DetermineRefreshFlags_NothingChanged_ReturnsNoOp()
    {
        InstanceSave instance = new InstanceSave();
        List<InstanceSave> instances = new List<InstanceSave> { instance };

        (bool hasChangedObjectShowing, bool instanceIdentityChanged) =
            PropertyGridManager.DetermineRefreshFlags(structuralChange: false, instances, instances);

        hasChangedObjectShowing.ShouldBeFalse();
        instanceIdentityChanged.ShouldBeFalse();
    }

    [Fact]
    public void DetermineRefreshFlags_SingleInstanceIdentityChanged_ReturnsDiffPathFlag()
    {
        InstanceSave oldInstance = new InstanceSave();
        InstanceSave newInstance = new InstanceSave();
        List<InstanceSave> oldInstances = new List<InstanceSave> { oldInstance };
        List<InstanceSave> newInstances = new List<InstanceSave> { newInstance };

        (bool hasChangedObjectShowing, bool instanceIdentityChanged) =
            PropertyGridManager.DetermineRefreshFlags(structuralChange: false, newInstances, oldInstances);

        hasChangedObjectShowing.ShouldBeFalse();
        instanceIdentityChanged.ShouldBeTrue();
    }

    [Fact]
    public void DetermineRefreshFlags_StructuralChange_ReturnsFullRebuild()
    {
        InstanceSave instance = new InstanceSave();
        List<InstanceSave> instances = new List<InstanceSave> { instance };

        (bool hasChangedObjectShowing, bool instanceIdentityChanged) =
            PropertyGridManager.DetermineRefreshFlags(structuralChange: true, instances, instances);

        hasChangedObjectShowing.ShouldBeTrue();
        instanceIdentityChanged.ShouldBeFalse();
    }

    [Fact]
    public void DoCategoriesDiffer_DifferentMemberNames_ReturnsTrue()
    {
        List<InstanceMember> first = new List<InstanceMember> { new InstanceMember { Name = "X" } };
        List<InstanceMember> second = new List<InstanceMember> { new InstanceMember { Name = "Y" } };

        bool result = PropertyGridManager.DoCategoriesDiffer(first, second);

        result.ShouldBeTrue();
    }

    [Fact]
    public void DoCategoriesDiffer_SameMemberNames_ReturnsFalse()
    {
        List<InstanceMember> first = new List<InstanceMember> { new InstanceMember { Name = "X" }, new InstanceMember { Name = "Y" } };
        List<InstanceMember> second = new List<InstanceMember> { new InstanceMember { Name = "Y" }, new InstanceMember { Name = "X" } };

        bool result = PropertyGridManager.DoCategoriesDiffer(first, second);

        result.ShouldBeFalse();
    }
}
