using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe.Editors;
using Moq;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// The canvas handles follow the project's variable rules (#7720).
/// </summary>
public class EditHandleRestrictionsTests : BaseTestClass
{
    private readonly Mock<IProjectRestrictions> _restrictions = new Mock<IProjectRestrictions>();
    private readonly EditHandleRestrictions _sut;
    private readonly InstanceSave _instance = new InstanceSave { Name = "Player", BaseType = "Sprite" };

    public EditHandleRestrictionsTests()
    {
        _sut = new EditHandleRestrictions(_restrictions.Object);
    }

    [Fact]
    public void Apply_ShouldDisableOnlyTheHandlesForHiddenVariables()
    {
        EditorContext context = EditorContextTestHelper.Create();
        foreach (string name in new[] { "Width", "Height", "Rotation" })
        {
            _restrictions.Setup(x => x.IsVariableHidden(_instance, name)).Returns(true);
        }

        _sut.Apply(context, _instance);

        context.IsXMovementEnabled.ShouldBeTrue();
        context.IsYMovementEnabled.ShouldBeTrue();
        context.IsWidthChangeEnabled.ShouldBeFalse();
        context.IsHeightChangeEnabled.ShouldBeFalse();
        context.IsRotationEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Apply_ShouldLeaveEverythingEnabled_WhenNothingIsHidden()
    {
        EditorContext context = EditorContextTestHelper.Create();

        _sut.Apply(context, _instance);

        context.IsXMovementEnabled.ShouldBeTrue();
        context.IsWidthChangeEnabled.ShouldBeTrue();
        context.IsRotationEnabled.ShouldBeTrue();
    }

    [Fact]
    public void GetShouldShowOrigin_ShouldBeFalse_OnlyWhenBothOriginsAreHidden()
    {
        _sut.GetShouldShowOrigin(_instance).ShouldBeTrue();

        _restrictions.Setup(x => x.IsVariableHidden(_instance, "XOrigin")).Returns(true);
        _sut.GetShouldShowOrigin(_instance).ShouldBeTrue();

        _restrictions.Setup(x => x.IsVariableHidden(_instance, "YOrigin")).Returns(true);
        _sut.GetShouldShowOrigin(_instance).ShouldBeFalse();
    }
}
