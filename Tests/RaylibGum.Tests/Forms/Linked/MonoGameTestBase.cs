using Gum.Forms;
using RenderingLibrary;

// Compiled into RaylibGum.Tests so Forms test files linked from MonoGameGum.Tests (which derive from
// MonoGameGum.Tests.BaseTestClass) run unchanged against raylib. Raylib's V2 default templates do not
// register TextBox, so this base also registers the V3 visuals, as RaylibGum.Tests.Forms.TextBoxTests does.
namespace MonoGameGum.Tests
{
    public class BaseTestClass : RaylibGum.Tests.BaseTestClass
    {
        public BaseTestClass()
        {
            FormsUtilities.InitializeDefaults(SystemManagers.Default, DefaultVisualsVersion.V3);
        }
    }
}
