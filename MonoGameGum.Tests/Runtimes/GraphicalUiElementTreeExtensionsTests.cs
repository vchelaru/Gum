using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class GraphicalUiElementTreeExtensionsTests : BaseTestClass
{
    private class SpecialContainer : ContainerRuntime { }

    #region Descendants

    [Fact]
    public void Descendants_ReturnsEmpty_WhenNoChildren()
    {
        GraphicalUiElement sut = new();

        IEnumerable<GraphicalUiElement> result = sut.Descendants();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Descendants_ReturnsDirectChildrenAndDeeper()
    {
        ContainerRuntime root = new() { Name = "root" };
        ContainerRuntime child = new() { Name = "child" };
        ContainerRuntime grandchild = new() { Name = "grandchild" };
        root.AddChild(child);
        child.AddChild(grandchild);

        List<GraphicalUiElement> result = root.Descendants().ToList();

        result.ShouldBe(new GraphicalUiElement[] { child, grandchild });
    }

    [Fact]
    public void Descendants_EnumeratesShallowestFirst()
    {
        // Tree:
        //     root
        //    /    \
        //   a      b
        //   |
        //   a1
        // Shallowest-first: a, b, a1 (NOT a, a1, b which would be depth-first).
        ContainerRuntime root = new() { Name = "root" };
        ContainerRuntime a = new() { Name = "a" };
        ContainerRuntime b = new() { Name = "b" };
        ContainerRuntime a1 = new() { Name = "a1" };
        root.AddChild(a);
        root.AddChild(b);
        a.AddChild(a1);

        List<GraphicalUiElement> result = root.Descendants().ToList();

        result.ShouldBe(new GraphicalUiElement[] { a, b, a1 });
    }

    [Fact]
    public void Descendants_IsLazy_DoesNotEnumerateUntilIterated()
    {
        // Cycle would StackOverflow if eagerly walked, so building a deep tree
        // and asserting that Take(1) doesn't traverse the whole thing proves laziness.
        ContainerRuntime root = new();
        ContainerRuntime current = root;
        for (int i = 0; i < 1000; i++)
        {
            ContainerRuntime next = new() { Name = $"n{i}" };
            current.AddChild(next);
            current = next;
        }

        GraphicalUiElement first = root.Descendants().First();

        first.Name.ShouldBe("n0");
    }

    #endregion

    #region DescendantsAndSelf

    [Fact]
    public void DescendantsAndSelf_StartsWithSelf()
    {
        ContainerRuntime root = new() { Name = "root" };
        ContainerRuntime child = new() { Name = "child" };
        root.AddChild(child);

        List<GraphicalUiElement> result = root.DescendantsAndSelf().ToList();

        result.ShouldBe(new GraphicalUiElement[] { root, child });
    }

    #endregion

    #region Ancestors

    [Fact]
    public void Ancestors_ReturnsEmpty_WhenNoParent()
    {
        GraphicalUiElement sut = new();

        IEnumerable<GraphicalUiElement> result = sut.Ancestors();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Ancestors_WalksParentChain_NearestFirst()
    {
        ContainerRuntime grandparent = new() { Name = "grandparent" };
        ContainerRuntime parent = new() { Name = "parent" };
        GraphicalUiElement child = new() { Name = "child" };
        grandparent.AddChild(parent);
        parent.AddChild(child);

        List<GraphicalUiElement> result = child.Ancestors().ToList();

        result.ShouldBe(new GraphicalUiElement[] { parent, grandparent });
    }

    #endregion

    #region AncestorsAndSelf

    [Fact]
    public void AncestorsAndSelf_StartsWithSelf()
    {
        ContainerRuntime parent = new() { Name = "parent" };
        GraphicalUiElement child = new() { Name = "child" };
        parent.AddChild(child);

        List<GraphicalUiElement> result = child.AncestorsAndSelf().ToList();

        result.ShouldBe(new GraphicalUiElement[] { child, parent });
    }

    #endregion

    #region Find<T>

    [Fact]
    public void FindOfT_ReturnsFirstShallowestMatch()
    {
        // Two SpriteRuntimes — one shallow, one deep. BFS must return the shallow one.
        ContainerRuntime root = new();
        ContainerRuntime middle = new();
        SpriteRuntime deep = new() { Name = "deep" };
        SpriteRuntime shallow = new() { Name = "shallow" };
        root.AddChild(middle);
        root.AddChild(shallow);
        middle.AddChild(deep);

        SpriteRuntime? result = root.Find<SpriteRuntime>();

        result.ShouldBe(shallow);
    }

    [Fact]
    public void FindOfT_MatchesSubclasses()
    {
        // is-T semantics: searching for ContainerRuntime should find a SpecialContainer.
        ContainerRuntime root = new();
        SpecialContainer special = new() { Name = "special" };
        root.AddChild(special);

        ContainerRuntime? result = root.Find<ContainerRuntime>();

        result.ShouldBe(special);
    }

    [Fact]
    public void FindOfT_ReturnsNull_WhenNoMatch()
    {
        ContainerRuntime root = new();
        root.AddChild(new ContainerRuntime());

        SpriteRuntime? result = root.Find<SpriteRuntime>();

        result.ShouldBeNull();
    }

    [Fact]
    public void FindOfT_DoesNotMatchSelf()
    {
        // Sugar walks descendants only, not self — matches legacy GetChild*Recursively behavior.
        SpriteRuntime sut = new();

        SpriteRuntime? result = sut.Find<SpriteRuntime>();

        result.ShouldBeNull();
    }

    #endregion

    #region FindByName

    [Fact]
    public void FindByName_ReturnsFirstShallowestMatch()
    {
        // Two children share the name "Target" — the shallower one wins.
        // This mirrors the legacy GetChildByNameRecursively behavior that ListBox/FocusedIndicator depends on.
        ContainerRuntime root = new();
        ContainerRuntime shallow = new() { Name = "Target" };
        ContainerRuntime middle = new();
        ContainerRuntime deep = new() { Name = "Target" };
        root.AddChild(shallow);
        root.AddChild(middle);
        middle.AddChild(deep);

        GraphicalUiElement? result = root.FindByName("Target");

        result.ShouldBe(shallow);
    }

    [Fact]
    public void FindByName_ReturnsNull_WhenNoMatch()
    {
        ContainerRuntime root = new();
        root.AddChild(new ContainerRuntime { Name = "Other" });

        GraphicalUiElement? result = root.FindByName("Missing");

        result.ShouldBeNull();
    }

    #endregion

    #region Find<T>(name)

    [Fact]
    public void FindOfTByName_RequiresBothTypeAndName()
    {
        // A ContainerRuntime named "Target" should be skipped when searching for a SpriteRuntime named "Target".
        ContainerRuntime root = new();
        ContainerRuntime decoy = new() { Name = "Target" };
        SpriteRuntime match = new() { Name = "Target" };
        root.AddChild(decoy);
        root.AddChild(match);

        SpriteRuntime? result = root.Find<SpriteRuntime>("Target");

        result.ShouldBe(match);
    }

    [Fact]
    public void FindOfTByName_ReturnsNull_WhenNoMatch()
    {
        ContainerRuntime root = new();
        root.AddChild(new SpriteRuntime { Name = "Other" });

        SpriteRuntime? result = root.Find<SpriteRuntime>("Target");

        result.ShouldBeNull();
    }

    #endregion
}
