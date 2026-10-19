using Gum.Forms;
using Gum.Input;
using SkiaHostTests;

namespace KniSkiaGum.Tests;

/// <summary>
/// Base for KniSkiaGum tests. The shared reset lives in <see cref="SkiaHostTestBase"/>; KNI's input part
/// is a fresh cursor that reads no mouse, so one test's held buttons don't leak into the next and
/// Update never reaches KNI's static device read.
/// </summary>
public class BaseTestClass : SkiaHostTestBase
{
    protected override void ResetInput() => FormsUtilities.SetCursor(new Cursor { ReadMouse = () => default });
}
