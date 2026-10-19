using Gum.Forms;
using Gum.Input;
using SkiaHostTests;

namespace StrideGum.Tests;

/// <summary>
/// Base for StrideGum tests. The shared reset lives in <see cref="SkiaHostTestBase"/>; Stride's input
/// part is just a fresh device-less cursor, so one test's held buttons don't leak into the next.
/// </summary>
public class BaseTestClass : SkiaHostTestBase
{
    protected override void ResetInput() => FormsUtilities.SetCursor(new Cursor());
}
