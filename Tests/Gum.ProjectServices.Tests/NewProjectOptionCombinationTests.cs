#pragma warning disable xUnit1031 // The helpers create projects synchronously; nothing here can deadlock (no synchronization context).
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>Every combination of the New Project options is either resolved or refused for its documented reason.</summary>
public class NewProjectOptionCombinationTests : NewProjectServiceTestBase
{
    [Fact]
    public void Validate_EveryOptionCombination_ResolvesOrIsRefusedForTheDocumentedReason()
    {
        NewProjectService service = CreateService();
        int checkedCount = 0;

        foreach (string? platform in new string?[] { null }.Concat(Platforms.Select(entry => entry.Name)))
        foreach (string template in new[] { "forms", "empty" })
        foreach (bool xml in new[] { false, true })
        foreach (bool sourceLinked in new[] { false, true })
        foreach (bool packages in new[] { false, true })
        foreach (bool generate in new[] { false, true })
        foreach (bool restore in new[] { false, true })
        {
            // Source-linked together with packages is the subject of #7328; the next two are covered by their own tests.
            if (sourceLinked && packages)
            {
                continue;
            }
            if (platform == "kniskia" && !sourceLinked)
            {
                continue;
            }

            string label = $"platform={platform} template={template} xml={xml} linked={sourceLinked} packages={packages} generate={generate} restore={restore}";
            NewProjectValidation validation = service.Validate(Request(platform, template: template, xml: xml,
                sourceLinked: sourceLinked, packages: packages, generate: generate, restore: restore));

            NewProjectErrorKind? expected =
                platform == null && sourceLinked ? NewProjectErrorKind.SourceLinkedNeedsPlatform
                : platform == null && packages ? NewProjectErrorKind.RecommendedPackagesNeedPlatform
                : platform == null && generate ? NewProjectErrorKind.GenerateCodeNeedsPlatform
                : null;

            ((NewProjectErrorKind?)validation.Error?.Kind).ShouldBe(expected, label + " " + validation.Error?.Message);
            (validation.Resolved != null).ShouldBe(expected == null, label);
            checkedCount++;
        }

        checkedCount.ShouldBeGreaterThan(100);
    }

    [Fact]
    public void Validate_GenerateCodeWithTheEmptyTemplate_IsAcceptedAndGeneratesNothing()
    {
        NewProjectValidation validation = CreateService().Validate(Request("monogame", template: "empty", generate: true));

        validation.Resolved.ShouldNotBeNull(validation.Error?.Message);
        Run(Request("monogame", template: "empty", generate: true, restore: true)).GetAwaiter().GetResult();
        _calls.ShouldNotContain(call => call.StartsWith("codegen"));
    }
}
