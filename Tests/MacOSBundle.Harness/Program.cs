using System;
using System.IO;
using ToolsUtilities;

// End-to-end check for issue #731: when launched from inside a macOS .app bundle, Gum must resolve
// loose content that ships in Contents/Resources/ even though the executable lives in Contents/MacOS/.
// The macOS CI job publishes this harness, packages it into a real .app, and runs it from
// Contents/MacOS/. It returns 0 when content is resolved correctly and non-zero otherwise, so the CI
// step asserts purely on the process exit code.

const string relativeContentPath = "Content/macos-bundle-test.txt";
const string expectedContents = "gum-bundle-ok";

Console.WriteLine($"[macOS bundle harness] ExeLocation = {FileManager.ExeLocation}");

if (!OperatingSystem.IsMacOS())
{
    return Fail("expected to run on macOS.");
}

// The exe-relative path must NOT exist on disk; otherwise the bundle layout isn't being exercised
// and a pass would prove nothing.
string exeRelativeAbsolute = FileManager.ExeLocation + relativeContentPath.Replace('/', Path.DirectorySeparatorChar);
if (File.Exists(exeRelativeAbsolute))
{
    return Fail($"content unexpectedly present next to the executable at '{exeRelativeAbsolute}'; " +
        "the bundle layout is not being tested.");
}

// FileExists must rebase onto Contents/Resources/ and find the file there.
if (!FileManager.FileExists(relativeContentPath))
{
    return Fail($"FileManager.FileExists could not find '{relativeContentPath}' in the bundle's Resources directory.");
}

// FromFileText goes through GetStreamForFile, the same path a custom .fnt load uses.
string actualContents;
try
{
    actualContents = FileManager.FromFileText(relativeContentPath).Trim();
}
catch (Exception e)
{
    return Fail($"FileManager.FromFileText threw resolving '{relativeContentPath}': {e}");
}

if (actualContents != expectedContents)
{
    return Fail($"content mismatch. Expected '{expectedContents}', got '{actualContents}'.");
}

Console.WriteLine("[macOS bundle harness] PASS: resolved content from Contents/Resources/ via Gum's FileManager.");
return 0;

static int Fail(string message)
{
    Console.Error.WriteLine($"[macOS bundle harness] FAIL: {message}");
    return 1;
}
