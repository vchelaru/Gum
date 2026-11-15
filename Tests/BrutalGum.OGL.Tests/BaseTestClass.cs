using Gum.Forms;
using Gum.Input;
using SkiaHostTests;

namespace BrutalGum.Tests;

/// <summary>
/// Base for BrutalGum tests. The shared reset lives in <see cref="SkiaHostTestBase"/>; the input part is a
/// fresh device-less cursor, so one test's held buttons do not leak into the next.
/// </summary>
public class BaseTestClass : SkiaHostTestBase
{
    protected override void ResetInput() => FormsUtilities.SetCursor(new Cursor());
}
