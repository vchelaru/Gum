using System.Collections.Generic;
using Gum;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.HostServices;

/// <summary>
/// Tests for <see cref="GumSnapshotExporter.FillUnresolvedTextureSourceFiles"/>: the platform-agnostic
/// dedupe/path-fill orchestration behind <c>ExportSnapshot</c>'s embedded-texture extraction. The actual
/// <c>Texture2D.SaveAsPng</c> call is XNALIKE-only and lives in <c>GumService.XnaLike.cs</c>; this covers
/// the headless logic around it.
/// </summary>
public class GumSnapshotExporterTests
{
    [Fact]
    public void FillUnresolvedTextureSourceFiles_ShouldDedupeByTextureAndFillPlaceholders()
    {
        // Two references to one shared texture instance plus one to a distinct texture: the shared texture
        // is saved once (deduped by reference) and both its placeholders get that same path; the distinct
        // texture gets its own. This mirrors "one UISpriteSheet.png shared by many NineSlices".
        object sharedTexture = new();
        object otherTexture = new();
        VariableSave first = new() { Name = "A.SourceFile" };
        VariableSave second = new() { Name = "B.SourceFile" };
        VariableSave third = new() { Name = "C.SourceFile" };
        List<UnresolvedTextureReference> references = new()
        {
            new UnresolvedTextureReference(sharedTexture, first),
            new UnresolvedTextureReference(sharedTexture, second),
            new UnresolvedTextureReference(otherTexture, third),
        };

        List<object> saved = new();
        GumSnapshotExporter.FillUnresolvedTextureSourceFiles(references, (texture, relativePath) =>
        {
            saved.Add(texture);
            return true;
        });

        // Saved once per distinct texture instance.
        saved.Count.ShouldBe(2);
        // The shared texture's two placeholders resolve to the same (non-null) path; the distinct one differs.
        first.Value.ShouldNotBeNull();
        first.Value.ShouldBe(second.Value);
        third.Value.ShouldNotBeNull();
        first.Value.ShouldNotBe(third.Value);
    }

    [Fact]
    public void FillUnresolvedTextureSourceFiles_WhenSaverDeclines_ShouldLeavePlaceholderUnset()
    {
        // A texture the saver cannot persist (e.g. SaveAsPng throws) must leave its placeholder null -- the
        // slice renders blank rather than dangling on a path to a file that was never written.
        object texture = new();
        VariableSave placeholder = new() { Name = "A.SourceFile" };
        List<UnresolvedTextureReference> references = new()
        {
            new UnresolvedTextureReference(texture, placeholder),
        };

        GumSnapshotExporter.FillUnresolvedTextureSourceFiles(references, (_, _) => false);

        placeholder.Value.ShouldBeNull();
    }
}
