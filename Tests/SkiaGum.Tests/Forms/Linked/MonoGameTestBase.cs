// Compiled into SkiaGum.Tests so Forms test files linked from MonoGameGum.Tests (which derive from
// MonoGameGum.Tests.BaseTestClass in namespace MonoGameGum.Tests.Forms) run unchanged against the
// shared Skia code. Linked files appear in the Skia assembly under the MonoGameGum.Tests namespace.
namespace MonoGameGum.Tests
{
    public class BaseTestClass : SkiaGum.Tests.Forms.Linked.SkiaFormsTestBase
    {
    }
}

// Stand-in for MonoGameGum's XNA-typed keyboard interface. The linked tests mock it only through
// its IInputReceiverKeyboard base (Mock.As), which is the surface shared with Skia.
namespace Gum.Input
{
    public interface IInputReceiverKeyboardMonoGame : Gum.Wireframe.IInputReceiverKeyboard
    {
    }
}

// Namespaces the linked files import but, on Skia, use nothing from. Declaring them lets the unused
// `using` directives compile; a test that really needs an XNA type still fails to compile and stays unlinked.
namespace Microsoft.Xna.Framework.Input
{
    internal static class UnusedNamespaceAnchor { }
}

namespace MonoGameGum.Tests.Input
{
    internal static class UnusedNamespaceAnchor { }
}
