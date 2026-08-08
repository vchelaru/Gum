using Moq;
using Shouldly;
using System.Collections.Generic;
using WpfDataUi;
using WpfDataUi.DataTypes;
using WpfDataUi.EventArguments;

namespace GumToolUnitTests.Controls;

/// <summary>
/// Pins DataUiGrid.PropertyChange populating PropertyChangedArgs.OldValue (#4387). Previously OldValue
/// was always left at its default null, even though the value existed on the InstanceMember immediately
/// before the UI-driven assignment.
/// </summary>
public class DataUiGridPropertyChangeOldValueTests : BaseTestClass
{
    class SimplePropertyOwner
    {
        public bool Flag { get; set; }
    }

    [StaFact]
    public void PropertyChange_PopulatesOldValue_WhenValueSetThroughTrySetValueOnInstance()
    {
        var owner = new SimplePropertyOwner { Flag = true };
        var instanceMember = new InstanceMember(nameof(SimplePropertyOwner.Flag), owner);

        var category = new MemberCategory("Category");
        category.Members.Add(instanceMember);

        var grid = new DataUiGrid();
        grid.SetCategories(new List<MemberCategory> { category });

        PropertyChangedArgs? capturedArgs = null;
        grid.PropertyChange += (name, args) => capturedArgs = args;

        object? newValueOnUi = false;
        var mockDataUi = new Mock<IDataUi>();
        mockDataUi.SetupGet(d => d.InstanceMember).Returns(instanceMember);
        mockDataUi.Setup(d => d.TryGetValueOnUi(out newValueOnUi)).Returns(ApplyValueResult.Success);

        mockDataUi.Object.TrySetValueOnInstance();

        capturedArgs.ShouldNotBeNull();
        capturedArgs!.OldValue.ShouldBe(true);
        capturedArgs.NewValue.ShouldBe(false);
        capturedArgs.PropertyName.ShouldBe(nameof(SimplePropertyOwner.Flag));
    }
}
