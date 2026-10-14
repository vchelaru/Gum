using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Input;

public class CursorShimTests
{
#pragma warning disable CS0618 // The shim is the subject under test.
    [Fact]
    public void LegacyCursor_ShouldDeriveFromGumInputCursor_AndBeObsolete()
    {
        Type legacyType = typeof(MonoGameGum.Input.Cursor);

        legacyType.BaseType.ShouldBe(typeof(Gum.Input.Cursor));
        Attribute.IsDefined(legacyType, typeof(ObsoleteAttribute), inherit: false).ShouldBeTrue();
    }

    [Fact]
    public void LegacyCursor_ShouldBeUsableWhereGumInputCursorIsExpected()
    {
        Gum.Input.Cursor cursor = new MonoGameGum.Input.Cursor(null);

        cursor.ShouldNotBeNull();
        cursor.ShouldBeAssignableTo<Gum.Wireframe.ICursor>();
    }
#pragma warning restore CS0618
}
