using Gum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Android/iOS load content through TitleContainer, which is case-sensitive, so the file an inline
// BBCode font run asks for must keep the mixed case of the FontCache file name (#5461).
public class TextRuntimeBbCodeMobileFontPathTests : BaseTestClass
{
    [Fact]
    public void Text_WithBbCodeBoldRun_OnMobile_ShouldRequestTheFontFileWithItsCasePreserved()
    {
        bool? savedIsMobileOverride = FileManager.IsMobileOverride;
        Func<string, Stream>? savedStreamHook = FileManager.CustomGetStreamFromFile;
        string savedRelativeDirectory = FileManager.RelativeDirectory;
        try
        {
            // A unique content folder keeps the font-cache key unique, so no earlier cache hit can
            // skip the file lookup under test.
            string contentFolder = "Content_" + Guid.NewGuid().ToString("N");
            string expectedRequest = contentFolder + "/FontCache/Font12Garet_Bold.fnt";

            FileManager.IsMobileOverride = true;
            FileManager.RelativeDirectory = contentFolder + "/";
            List<string> requestedFiles = new List<string>();
            // Stands in for the TitleContainer hook, which strips the "./" marker before the OS call.
            // Returning null reports the file as missing.
            FileManager.CustomGetStreamFromFile = fileName =>
            {
                string normalized = fileName.Replace('\\', '/');
                if (normalized.StartsWith("./", StringComparison.Ordinal))
                {
                    normalized = normalized.Substring(2);
                }
                requestedFiles.Add(normalized);
                return null!;
            };

            TextRuntime textRuntime = new();
            textRuntime.Font = "Garet";
            textRuntime.FontSize = 12;
            textRuntime.Text = "normal [IsBold=true]bold[/IsBold]";

            requestedFiles.ShouldContain(expectedRequest);
        }
        finally
        {
            FileManager.IsMobileOverride = savedIsMobileOverride;
            FileManager.CustomGetStreamFromFile = savedStreamHook;
            FileManager.RelativeDirectory = savedRelativeDirectory;
        }
    }
}
