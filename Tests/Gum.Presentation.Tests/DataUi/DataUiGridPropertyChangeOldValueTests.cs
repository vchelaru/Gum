using Moq;
using Shouldly;
using System.Collections.Generic;
using WpfDataUi;
using WpfDataUi.DataTypes;
using WpfDataUi.EventArguments;

namespace Gum.Presentation.Tests.DataUi;

/// <summary>
/// Pins DataUiGridModel.PropertyChange populating PropertyChangedArgs.OldValue (#4387). Previously OldValue
/// was always left at its default null, even though the value existed on the InstanceMember immediately
/// before the UI-driven assignment.
/// </summary>
public class DataUiGridPropertyChangeOldValueTests
{
    class SimplePropertyOwner
    {
        public bool Flag { get; set; }
    }

    [Fact]
    public void PropertyChange_PopulatesOldValue_WhenValueSetThroughTrySetValueOnInstance()
    {
        SimplePropertyOwner owner = new SimplePropertyOwner { Flag = true };
        InstanceMember instanceMember = new InstanceMember(nameof(SimplePropertyOwner.Flag), owner);

        MemberCategory category = new MemberCategory("Category");
        category.Members.Add(instanceMember);

        DataUiGridModel grid = new DataUiGridModel();
        grid.SetCategories(new List<MemberCategory> { category });

        PropertyChangedArgs? capturedArgs = null;
        grid.PropertyChange += (name, args) => capturedArgs = args;

        object? newValueOnUi = false;
        Mock<IDataUi> mockDataUi = new Mock<IDataUi>();
        mockDataUi.SetupGet(d => d.InstanceMember).Returns(instanceMember);
        mockDataUi.Setup(d => d.TryGetValueOnUi(out newValueOnUi)).Returns(ApplyValueResult.Success);

        mockDataUi.Object.TrySetValueOnInstance();

        capturedArgs.ShouldNotBeNull();
        capturedArgs!.OldValue.ShouldBe(true);
        capturedArgs.NewValue.ShouldBe(false);
        capturedArgs.PropertyName.ShouldBe(nameof(SimplePropertyOwner.Flag));
    }
}
