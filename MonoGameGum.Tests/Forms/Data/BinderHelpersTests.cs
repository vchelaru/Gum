using Gum.Forms.Data;
using Xunit;

namespace MonoGameGum.Tests.Forms.Data;

public class BinderHelpersTests
{
    [Fact]
    public void CanWritePath_ReturnsFalse_ForReadonlyProperty()
    {
        bool canWrite = BinderHelpers.CanWritePath(typeof(TestPathRoot), nameof(TestPathRoot.ReadonlyText));
        Assert.False(canWrite);
    }

    [Fact]
    public void CanWritePath_ReturnsTrue_ForWritableProperty()
    {
        bool canWrite = BinderHelpers.CanWritePath(typeof(TestPathRoot), nameof(TestPathRoot.Text));
        Assert.True(canWrite);
    }

    [Fact]
    public void CanWritePath_ReturnsFalse_ForReadonlyNestedProperty()
    {
        bool canWrite = BinderHelpers.CanWritePath(typeof(TestPathRoot), $"{nameof(TestPathRoot.Child)}.{nameof(TestPathChild.ReadonlyValue)}");
        Assert.False(canWrite);
    }

    [Fact]
    public void CanWritePath_ReturnsTrue_ForWritableNestedProperty()
    {
        bool canWrite = BinderHelpers.CanWritePath(typeof(TestPathRoot), $"{nameof(TestPathRoot.Child)}.{nameof(TestPathChild.Value)}");
        Assert.True(canWrite);
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
