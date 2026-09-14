using System.Reflection;
using Gum.Services;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>The About dialog's version text, from whichever stamp the head carries.</summary>
public class ToolVersionTests
{
    [Fact]
    public void Describe_PrefersTheBuildVersionStamp()
    {
        Attribute[] attributes =
        {
            new AssemblyInformationalVersionAttribute("1.0.0+abc123"),
            new AssemblyMetadataAttribute("BuildVersion", "2026.09.02"),
        };

        ToolVersion.Describe(attributes).ShouldBe("2026.09.02");
    }

    [Fact]
    public void Describe_FallsBackToTheInformationalVersion_WithoutTheSourceRevision()
    {
        Attribute[] attributes = { new AssemblyInformationalVersionAttribute("2026.09.11+9f3c1e2") };

        ToolVersion.Describe(attributes).ShouldBe("2026.09.11");
    }

    [Fact]
    public void Describe_ReportsUnknown_WhenNothingIsStamped()
    {
        ToolVersion.Describe(Array.Empty<Attribute>()).ShouldBe(ToolVersion.Unknown);
        ToolVersion.Describe((Assembly?)null).ShouldBe(ToolVersion.Unknown);
    }
}
