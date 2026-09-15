using Avalonia;
using Avalonia.Headless.XUnit;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The head's <see cref="App"/> as Avalonia boots it.</summary>
public class AppTests
{
    // macOS names the app menu (its title, "Hide ...") from Application.Name, not the bundle.
    [AvaloniaFact]
    public void Name_IsGum()
    {
        Application.Current!.Name.ShouldBe("Gum");
    }
}
