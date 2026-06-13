using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Gum.Analyzers.Tests;

/// <summary>
/// Verifies that GUM003 flags an XNA <c>Buttons</c> value passed to a Gum gamepad query method and
/// that the code fix rewrites it to <c>GamepadButton</c>. The migrated call is itself a CS1503
/// type-mismatch after the break, so these tests disable compiler-diagnostic verification
/// (<see cref="CompilerDiagnostics.None"/>) and assert only the analyzer behavior.
/// </summary>
public class GamepadButtonMigrationCodeFixTests
{
    // Minimal stand-ins for the real types, so the analyzer's namespace/name checks resolve without
    // referencing MonoGame or GumCommon from the analyzer test project.
    private const string Stubs = @"
namespace Gum.Input
{
    public enum GamepadButton { A, B, DPadUp }
    public interface IGamePad { }
    public class GamePad : IGamePad
    {
        public bool ButtonDown(GamepadButton button) => false;
        public bool ButtonRepeatRate(GamepadButton button, double a = 0.35, double b = 0.12) => false;
    }
}

namespace Microsoft.Xna.Framework.Input
{
    public enum Buttons { A, B, DPadUp }
}
";

    private static async Task VerifyAsync(string testCode, string? fixedCode = null)
    {
        var test = new CSharpCodeFixTest<GamepadButtonMigrationAnalyzer, GamepadButtonMigrationCodeFixProvider, DefaultVerifier>
        {
            TestCode = testCode,
            // The point of GUM003 is that the migrated call no longer type-checks (CS1503);
            // ignore compiler diagnostics and verify only the analyzer + fix.
            CompilerDiagnostics = CompilerDiagnostics.None,
        };

        if (fixedCode != null)
        {
            test.FixedCode = fixedCode;
        }

        await test.RunAsync();
    }

    [Fact]
    public async Task ButtonDown_WithXnaButtons_RaisesGum003AndRewrites()
    {
        string testCode = @"
using Gum.Input;
using Microsoft.Xna.Framework.Input;

namespace TestProject
{
    class MyClass
    {
        void M(GamePad gamepad)
        {
            bool down = gamepad.ButtonDown({|GUM003:Buttons.A|});
        }
    }
}
" + Stubs;

        string fixedCode = @"
using Gum.Input;
using Microsoft.Xna.Framework.Input;

namespace TestProject
{
    class MyClass
    {
        void M(GamePad gamepad)
        {
            bool down = gamepad.ButtonDown(GamepadButton.A);
        }
    }
}
" + Stubs;

        await VerifyAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task ButtonRepeatRate_WithXnaButtons_RaisesGum003OnButtonArgOnly()
    {
        string testCode = @"
using Gum.Input;
using Microsoft.Xna.Framework.Input;

namespace TestProject
{
    class MyClass
    {
        void M(GamePad gamepad)
        {
            bool repeat = gamepad.ButtonRepeatRate({|GUM003:Buttons.DPadUp|}, 0.5, 0.2);
        }
    }
}
" + Stubs;

        string fixedCode = @"
using Gum.Input;
using Microsoft.Xna.Framework.Input;

namespace TestProject
{
    class MyClass
    {
        void M(GamePad gamepad)
        {
            bool repeat = gamepad.ButtonRepeatRate(GamepadButton.DPadUp, 0.5, 0.2);
        }
    }
}
" + Stubs;

        await VerifyAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task GamepadButtonArgument_IsNotFlagged()
    {
        // Already-migrated code must not be flagged.
        string testCode = @"
using Gum.Input;

namespace TestProject
{
    class MyClass
    {
        void M(GamePad gamepad)
        {
            bool down = gamepad.ButtonDown(GamepadButton.A);
        }
    }
}
" + Stubs;

        await VerifyAsync(testCode);
    }

    [Fact]
    public async Task ButtonDown_OnNonGamepadReceiver_IsNotFlagged()
    {
        // A same-named method on an unrelated type that legitimately takes XNA Buttons must be left alone.
        string testCode = @"
using Microsoft.Xna.Framework.Input;

namespace TestProject
{
    class NotAGamePad
    {
        public bool ButtonDown(Buttons button) => false;
    }

    class MyClass
    {
        void M(NotAGamePad thing)
        {
            bool down = thing.ButtonDown(Buttons.A);
        }
    }
}
" + Stubs;

        await VerifyAsync(testCode);
    }
}
