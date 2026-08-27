using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using KernSmith.Gum;
using RaylibGum.Renderables;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// Issue #4515 — end-to-end cover for a project-referenced <c>.ttf</c> that ships inside a
/// <c>.gumpkg</c> instead of on disk: setting <see cref="TextRuntime.Font"/> to it must rasterize
/// through the in-memory font creator rather than raising a property-assignment error and falling
/// back to the default font.
/// </summary>
public class TextRuntimeBundledFontTests : BaseTestClass
{
    [Fact]
    public void SettingFontToATtfServedOnlyByAGumpkg_RasterizesWithoutAPropertyAssignmentError()
    {
        // A name unique to this test: LoaderManager's font cache is a process-wide static, and a
        // cache hit would return before the font is ever read.
        string fontFileName = "Bundled_" + Guid.NewGuid().ToString("N") + ".ttf";
        string relativeFontPath = "Fonts/" + fontFileName;
        byte[] fontBytes = File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Orbitron-Black.ttf"));

        string temporaryDirectory = Path.Combine(Path.GetTempPath(), "TextRuntimeBundledFontTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(temporaryDirectory);

        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        Func<string, Stream>? savedHook = FileManager.CustomGetStreamFromFile;
        GumProjectSave? savedProject = ObjectFinder.Self.GumProjectSave;
        List<string> errors = new List<string>();
        void CaptureError(string message) => errors.Add(message);
        CustomSetPropertyOnRenderable.PropertyAssignmentError += CaptureError;

        try
        {
            string gumpkgPath = Path.Combine(temporaryDirectory, "Project.gumpkg");
            using (FileStream output = File.Create(gumpkgPath))
            {
                GumBundleWriter.Write(output, new (string, byte[])[]
                {
                    ("Project.gumx", Encoding.UTF8.GetBytes("<GumProjectSave />")),
                    (relativeFontPath, fontBytes),
                });
            }

            ProjectResolution resolution = GumBundleLoader.Resolve(gumpkgPath);
            ObjectFinder.Self.GumProjectSave = new GumProjectSave { FullFileName = resolution.ResolvedGumxPath };
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            TextRuntime textRuntime = new TextRuntime();
            textRuntime.FontSize = 24;
            textRuntime.Font = relativeFontPath;

            errors.ShouldBeEmpty();
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= CaptureError;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
            FileManager.CustomGetStreamFromFile = savedHook;
            ObjectFinder.Self.GumProjectSave = savedProject;
            try { Directory.Delete(temporaryDirectory, recursive: true); } catch { /* best-effort */ }
        }
    }
}
