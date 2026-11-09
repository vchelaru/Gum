using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.GumPreview;

public class PreviewDefaultsTests
{
    private sealed class Recorder : IPreviewDefaultsTarget
    {
        public List<string> Calls { get; } = new();
        public void UseHiDefProfile() => Calls.Add("HiDefProfile");
        public void InitializeShapes() => Calls.Add("Shapes");
        public void UseKeyboardDefaults() => Calls.Add("KeyboardDefaults");
        public void UseGamepadDefaults() => Calls.Add("GamepadDefaults");
    }

    [Fact]
    public void Apply_BothPhases_AppliesEveryListedDefaultOnceInOrder()
    {
        Recorder recorder = new();
        PreviewDefaults.Apply(recorder, PreviewDefaultPhase.Constructor);
        PreviewDefaults.Apply(recorder, PreviewDefaultPhase.AfterGumInitialize);

        recorder.Calls.ShouldBe(PreviewDefaults.All.Select(item => item.Id).ToList());
    }

    [Fact]
    public void All_EveryDefault_HasExplanationAndCode()
    {
        foreach (PreviewDefault item in PreviewDefaults.All)
        {
            item.Explanation.ShouldNotBeNullOrWhiteSpace();
            item.CodeLines.ShouldNotBeEmpty();
        }
    }

    [Fact]
    public void All_ContainsKeyboardGamepadAndShapes()
    {
        PreviewDefaults.All.Select(item => item.Id)
            .ShouldBe(new[] { "HiDefProfile", "KeyboardDefaults", "GamepadDefaults", "Shapes" });
    }
}
