using System.Collections.Generic;
using Gum.ExtensionMethods;
using Shouldly;

namespace SkiaGum.Tests.ExtensionMethods;

/// <summary>
/// <c>GraphicalUiElement.Children</c> is an <see cref="IReadOnlyList{T}"/>, which has no
/// <c>IndexOf</c>. Game code ported from the XNA-family runtimes calls it through
/// <c>Gum.ExtensionMethods</c>, so every Skia host must expose that namespace.
/// </summary>
public class IReadOnlyListExtensionMethodsTests
{
    private sealed class ReadOnlyOnlyList : IReadOnlyList<string>
    {
        private readonly string[] _items;

        public ReadOnlyOnlyList(params string[] items) => _items = items;

        public string this[int index] => _items[index];
        public int Count => _items.Length;
        public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)_items).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }

    [Fact]
    public void IndexOf_ShouldReturnPositionOfElement()
    {
        IReadOnlyList<string> list = new ReadOnlyOnlyList("a", "b", "c");

        list.IndexOf("c").ShouldBe(2);
    }

    [Fact]
    public void IndexOf_ShouldReturnMinusOne_WhenElementIsMissingOrListIsEmpty()
    {
        IReadOnlyList<string> list = new ReadOnlyOnlyList("a", "b");
        IReadOnlyList<string> empty = new ReadOnlyOnlyList();

        list.IndexOf("z").ShouldBe(-1);
        empty.IndexOf("a").ShouldBe(-1);
    }
}
