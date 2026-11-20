using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using Xunit;

namespace Gum.ProjectServices.Tests;

public class CodeOutputSettingsJsonWarmupTests
{
    [Fact]
    public void WarmUp_RoundTripsTheSettingsThroughJson()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            RootNamespace = "WarmUpNamespace",
            OutputLibrary = OutputLibrary.GumForms,
        };

        CodeOutputProjectSettings roundTripped = new CodeOutputSettingsJsonWarmup().WarmUp(settings);

        // A copy read back from the written text, so both the write and the read path ran.
        roundTripped.ShouldNotBeSameAs(settings);
        roundTripped.RootNamespace.ShouldBe("WarmUpNamespace");
        roundTripped.OutputLibrary.ShouldBe(OutputLibrary.GumForms);
    }
}
