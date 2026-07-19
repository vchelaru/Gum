using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.TreeView;

// Characterization (pinning) tests for the icon-index decision logic extracted from
// ElementTreeViewManager. They assert against the named ImageIndex constants, so they pin the
// semantic type -> icon mapping and survive any renumbering of the underlying indices.
public class TreeNodeImageLogicTests
{
    private readonly TreeNodeImageLogic _logic = new TreeNodeImageLogic();

    [Fact]
    public void GetCreateImageIndex_MissingSource_ReturnsExclamation()
    {
        _logic.GetCreateImageIndex(isSourceFileMissing: true, defaultImageIndex: ElementTreeViewManager.ComponentImageIndex)
            .ShouldBe(ElementTreeViewManager.ExclamationIndex);
    }

    [Fact]
    public void GetCreateImageIndex_PresentSource_ReturnsDefault()
    {
        _logic.GetCreateImageIndex(isSourceFileMissing: false, defaultImageIndex: ElementTreeViewManager.ComponentImageIndex)
            .ShouldBe(ElementTreeViewManager.ComponentImageIndex);
    }

    [Fact]
    public void GetElementRefreshImageIndex_ComponentWithErrors_ReturnsExclamation()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };

        _logic.GetElementRefreshImageIndex(component, hasErrors: true)
            .ShouldBe(ElementTreeViewManager.ExclamationIndex);
    }

    [Fact]
    public void GetElementRefreshImageIndex_ScreenNoErrors_ReturnsScreenIcon()
    {
        ScreenSave screen = new ScreenSave { Name = "MyScreen" };

        _logic.GetElementRefreshImageIndex(screen, hasErrors: false)
            .ShouldBe(ElementTreeViewManager.ScreenImageIndex);
    }

    [Fact]
    public void GetImageIndexForInstance_KnownStandardType_ReturnsPerTypeInstanceIcon()
    {
        _logic.GetImageIndexForInstance("Sprite").ShouldBe(ElementTreeViewManager.SpriteInstanceImageIndex);
    }

    [Fact]
    public void GetImageIndexForInstance_UnknownType_ReturnsGenericInstanceIcon()
    {
        _logic.GetImageIndexForInstance("SomeComponent").ShouldBe(ElementTreeViewManager.InstanceImageIndex);
    }

    [Fact]
    public void GetImageIndexForStandardElement_KnownType_ReturnsPerTypeIcon()
    {
        _logic.GetImageIndexForStandardElement("Text").ShouldBe(ElementTreeViewManager.TextImageIndex);
    }

    [Fact]
    public void GetImageIndexForStandardElement_UnknownType_ReturnsGenericStandardIcon()
    {
        _logic.GetImageIndexForStandardElement("NotAStandard").ShouldBe(ElementTreeViewManager.StandardElementImageIndex);
    }

    [Fact]
    public void GetInstanceCreateImageIndex_InvalidBaseTypeNotTolerated_ReturnsExclamation()
    {
        InstanceSave instance = new InstanceSave { Name = "Broken", BaseType = "Missing" };

        _logic.GetInstanceCreateImageIndex(instance, baseTypeValid: false, tolerateMissingTypes: false)
            .ShouldBe(ElementTreeViewManager.ExclamationIndex);
    }

    [Fact]
    public void GetInstanceCreateImageIndex_LockedInstance_ReturnsLockIcon()
    {
        InstanceSave instance = new InstanceSave { Name = "Locked", BaseType = "Sprite", Locked = true };

        _logic.GetInstanceCreateImageIndex(instance, baseTypeValid: true, tolerateMissingTypes: false)
            .ShouldBe(ElementTreeViewManager.LockedInstanceImageIndex);
    }

    [Fact]
    public void GetInstanceCreateImageIndex_ValidUnlocked_ReturnsPerTypeIcon()
    {
        InstanceSave instance = new InstanceSave { Name = "Text", BaseType = "Text" };

        _logic.GetInstanceCreateImageIndex(instance, baseTypeValid: true, tolerateMissingTypes: false)
            .ShouldBe(ElementTreeViewManager.TextInstanceImageIndex);
    }

    [Fact]
    public void GetInstanceRefreshImageIndex_LockedInstanceWithValidBase_ReturnsLockIcon()
    {
        InstanceSave instance = new InstanceSave { Name = "Locked", BaseType = "Sprite", Locked = true };
        ComponentSave baseElement = new ComponentSave { Name = "Sprite" };

        _logic.GetInstanceRefreshImageIndex(instance, baseElement)
            .ShouldBe(ElementTreeViewManager.LockedInstanceImageIndex);
    }

    [Fact]
    public void GetInstanceRefreshImageIndex_MissingBaseElement_ReturnsExclamation()
    {
        InstanceSave instance = new InstanceSave { Name = "Orphan", BaseType = "Sprite" };

        _logic.GetInstanceRefreshImageIndex(instance, baseElement: null)
            .ShouldBe(ElementTreeViewManager.ExclamationIndex);
    }

    [Fact]
    public void GetInstanceRefreshImageIndex_ValidUnlocked_ReturnsPerTypeIcon()
    {
        InstanceSave instance = new InstanceSave { Name = "Sprite", BaseType = "Sprite" };
        ComponentSave baseElement = new ComponentSave { Name = "Sprite" };

        _logic.GetInstanceRefreshImageIndex(instance, baseElement)
            .ShouldBe(ElementTreeViewManager.SpriteInstanceImageIndex);
    }
}
