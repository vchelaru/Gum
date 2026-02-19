using Gum.Forms.Data;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms.Data;

public class BinderHelpersTests
{
    [Fact]
    public void CanWritePath_ReturnsFalse_ForReadonlyProperty()
    {
        bool canWrite = BinderHelpers.CanWritePath(typeof(TestPathRoot), nameof(TestPathRoot.ReadonlyText));
        canWrite.ShouldBeFalse();
    }

    [Fact]
    public void CanWritePath_ReturnsTrue_ForWritableProperty()
    {
        bool canWrite = BinderHelpers.CanWritePath(typeof(TestPathRoot), nameof(TestPathRoot.Text));
        canWrite.ShouldBeTrue();
    }

    [Fact]
    public void CanWritePath_ReturnsFalse_ForReadonlyNestedProperty()
    {
        bool canWrite = BinderHelpers.CanWritePath(typeof(TestPathRoot), $"{nameof(TestPathRoot.Child)}.{nameof(TestPathChild.ReadonlyValue)}");
        canWrite.ShouldBeFalse();
    }

    [Fact]
    public void CanWritePath_ReturnsTrue_ForWritableNestedProperty()
    {
        bool canWrite = BinderHelpers.CanWritePath(typeof(TestPathRoot), $"{nameof(TestPathRoot.Child)}.{nameof(TestPathChild.Value)}");
        canWrite.ShouldBeTrue();
    }

    private class TestPathRoot
    {
        public string Text { get; set; } = string.Empty;
        public string ReadonlyText => Text;
        public TestPathChild Child { get; set; } = new();
    }

    private class TestPathChild
    {
        public int Value { get; set; }
        public int ReadonlyValue => Value;
    }
}
