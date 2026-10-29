using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The full-startup layer: the real head executable, run unattended on a copy of a fixture project,
/// goes through the whole startup sequence (settings, plugins, canvas, project load, selection) and
/// exits cleanly. Composition tests prove the container resolves; only a real run proves the order.
/// </summary>
/// <remarks>
/// Needs a display and a GL driver for the canvas, so it is skipped on headless machines and on CI
/// runners unless <c>GUM_RUN_HEAD_PROCESS_TEST=1</c> opts in (the Linux CI job does, under Xvfb with
/// Mesa's software GL).
/// </remarks>
public class HeadProcessTests
{
    private const string SkipReason = "needs a display and a GL driver; set GUM_RUN_HEAD_PROCESS_TEST=1 to run it on CI";

    private static bool CanRunTheHead => TestEnvironment.CanUseDisplay("GUM_RUN_HEAD_PROCESS_TEST");

    private sealed record HeadRun(int ExitCode, string Output, string Errors, TimeSpan Elapsed);

    [SkippableFact]
    public async Task UnattendedRun_LoadsAProject_AndExitsOnceReady()
    {
        Skip.IfNot(CanRunTheHead, SkipReason);
        using ScratchProject scratch = ScratchProject.Create();

        // --exit-after is only the upper bound (#5170): the run ends once the project has loaded
        // and the canvas has drawn, well before the bound.
        HeadRun run = await RunHead(scratch.Project, "--exit-after", "90", "--screenshot", scratch.Screenshot, "--user-data", scratch.UserData);

        run.ExitCode.ShouldBe(0, run.Errors);
        run.Errors.ShouldNotContain("Startup failed");
        run.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(80), "the run should end once ready, not when --exit-after runs out");
        File.Exists(scratch.Screenshot).ShouldBeTrue();
        File.Exists(Path.Combine(scratch.UserData, "GeneralSettings.xml")).ShouldBeTrue("the run's settings must be written under its own folder");
    }

    [SkippableFact]
    public async Task UnattendedRun_NotReadyByExitAfter_FailsWithoutAScreenshot()
    {
        Skip.IfNot(CanRunTheHead, SkipReason);
        using ScratchProject scratch = ScratchProject.Create();

        // No startup loads plugins, the canvas and a project in 0.2 s.
        HeadRun run = await RunHead(scratch.Project, "--exit-after", "0.2", "--screenshot", scratch.Screenshot, "--user-data", scratch.UserData);

        run.ExitCode.ShouldNotBe(0, "a run that captured nothing must not look like a success");
        run.Errors.ShouldContain("was not ready within --exit-after");
        File.Exists(scratch.Screenshot).ShouldBeFalse("a screenshot taken before the project loaded is the bug (#5170)");
    }

    [SkippableFact]
    public async Task UnattendedRun_ZoomToFit_FramesContentFarFromTheOrigin()
    {
        Skip.IfNot(CanRunTheHead, SkipReason);
        using ScratchProject scratch = ScratchProject.Create();
        // A 200x100 rectangle at (2400, 1600): far outside the default top-left view (#5142).
        scratch.AddScreen("FarScreen", "RectangleInstance", "Rectangle", x: 2400, y: 1600, width: 200, height: 100);

        HeadRun run = await RunHead(scratch.Project, "--exit-after", "90", "--select", "FarScreen", "--zoom-to-fit",
            "--screenshot", scratch.Screenshot, "--user-data", scratch.UserData);

        run.ExitCode.ShouldBe(0, run.Errors);
        Match fit = Regex.Match(run.Output,
            @"Zoom to fit: FarScreen .* zoom (?<zoom>\d+)%, camera \((?<x>-?[\d.]+), (?<y>-?[\d.]+)\), view (?<w>\d+)x(?<h>\d+) px");
        fit.Success.ShouldBeTrue("the head reports the camera it applied: " + run.Output);
        float zoom = int.Parse(fit.Groups["zoom"].Value, CultureInfo.InvariantCulture) / 100f;
        float cameraX = float.Parse(fit.Groups["x"].Value, CultureInfo.InvariantCulture);
        float cameraY = float.Parse(fit.Groups["y"].Value, CultureInfo.InvariantCulture);
        float viewRight = cameraX + int.Parse(fit.Groups["w"].Value, CultureInfo.InvariantCulture) / zoom;
        float viewBottom = cameraY + int.Parse(fit.Groups["h"].Value, CultureInfo.InvariantCulture) / zoom;
        cameraX.ShouldBeLessThan(2400);
        cameraY.ShouldBeLessThan(1600);
        viewRight.ShouldBeGreaterThan(2600);
        viewBottom.ShouldBeGreaterThan(1700);
        File.Exists(scratch.Screenshot).ShouldBeTrue();
    }

    [SkippableFact]
    public async Task RebuildFontsWithoutAProject_ReportsUsageAndFails()
    {
        Skip.IfNot(CanRunTheHead, SkipReason);
        using ScratchProject scratch = ScratchProject.Create();

        HeadRun run = await RunHead("--user-data", scratch.UserData, "--rebuildfonts");

        run.ExitCode.ShouldBe(1, run.Errors);
        run.Errors.ShouldContain("--rebuildfonts requires a project file");
        run.Errors.ShouldNotContain("Startup failed");
    }

    // The tool's device caps texture size; a sprite whose image is within the 8192 px cap must load (#6838).
    [SkippableTheory]
    [InlineData(5000)]
    [InlineData(8192)]
    public async Task UnattendedRun_SpriteWithATextureUpTo8192Pixels_LoadsWithoutAnError(int textureWidth)
    {
        Skip.IfNot(CanRunTheHead, SkipReason);
        using ScratchProject scratch = ScratchProject.Create();
        scratch.AddSpriteScreen("BigTextureScreen", "SpriteInstance", "BigTexture.png", textureWidth, 8);

        HeadRun run = await RunHead(scratch.Project, "--exit-after", "90", "--select", "BigTextureScreen#SpriteInstance",
            "--screenshot", scratch.Screenshot, "--user-data", scratch.UserData);

        run.ExitCode.ShouldBe(0, run.Errors);
        run.Errors.ShouldNotContain("Error setting SourceFile");
    }

    // Above the cap the sprite cannot load, and the error names the file and the limit instead of failing silently (#6838).
    [SkippableFact]
    public async Task UnattendedRun_SpriteWithATextureOver8192Pixels_ReportsTheLimit()
    {
        Skip.IfNot(CanRunTheHead, SkipReason);
        using ScratchProject scratch = ScratchProject.Create();
        scratch.AddSpriteScreen("BigTextureScreen", "SpriteInstance", "BigTexture.png", 8193, 8);

        HeadRun run = await RunHead(scratch.Project, "--exit-after", "90", "--select", "BigTextureScreen#SpriteInstance",
            "--screenshot", scratch.Screenshot, "--user-data", scratch.UserData);

        run.ExitCode.ShouldBe(0, run.Errors);
        run.Errors.ShouldContain("BigTexture.png");
        run.Errors.ShouldContain("maximum Texture2D size of 8192");
    }

    private static async Task<HeadRun> RunHead(params string[] arguments)
    {
        string configuration = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).Parent!.Name;
        // The apphost, not "dotnet Gum.dll": no console window per run, and it is what agents launch.
        string head = Path.Combine(FindRepositoryRoot(), "Tool", "Gum.Avalonia", "bin", configuration, "net10.0",
            OperatingSystem.IsWindows() ? "Gum.exe" : "Gum");
        File.Exists(head).ShouldBeTrue(head);

        ProcessStartInfo startInfo = new ProcessStartInfo(head)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        using Process process = Process.Start(startInfo)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> errors = process.StandardError.ReadToEndAsync();
        using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("The head did not exit within three minutes, although --exit-after should force an exit well before then.");
        }
        stopwatch.Stop();
        return new HeadRun(process.ExitCode, await output, await errors, stopwatch.Elapsed);
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GumFull.sln")))
            {
                return directory.FullName;
            }
        }
        throw new InvalidOperationException("The test is not running inside the Gum repository.");
    }

    /// <summary>A copy of the fixture project in its own temp folder, deleted on dispose.</summary>
    private sealed class ScratchProject : IDisposable
    {
        private readonly string _workingDirectory;
        private readonly string _projectDirectory;

        private ScratchProject(string workingDirectory, string projectDirectory, string project)
        {
            _workingDirectory = workingDirectory;
            _projectDirectory = projectDirectory;
            Project = project;
        }

        public string Project { get; }

        public string Screenshot => Path.Combine(_workingDirectory, "run.png");

        // The run's own settings folder: loading the project records it as the last project, which
        // must not land in the user's settings once this folder is deleted.
        public string UserData => Path.Combine(_workingDirectory, "UserData");

        public static ScratchProject Create()
        {
            string workingDirectory = Path.Combine(Path.GetTempPath(), "GumHeadRun_" + Guid.NewGuid().ToString("N"));
            // Two folders deep, as in the repository: the project's code root is "..\..\", which
            // for a shallow copy is the machine's temp folder's parent, and the orphan code scan on
            // load walks all of it (#5140).
            string projectDirectory = Path.Combine(workingDirectory, "Content", "GumProject");
            string source = Path.Combine(FindRepositoryRoot(), "Tests", "CodeGen_Skia_ByReference", "Content", "GumProject");
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(projectDirectory, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }
            return new ScratchProject(workingDirectory, projectDirectory, Directory.GetFiles(projectDirectory, "*.gumx").Single());
        }

        /// <summary>Adds a screen holding one instance at the given position and size.</summary>
        public void AddScreen(string screenName, string instanceName, string baseType, float x, float y, float width, float height)
        {
            string Variable(string name, float value) =>
                $"""
                    <Variable>
                      <Type>float</Type>
                      <Name>{instanceName}.{name}</Name>
                      <Value xsi:type="xsd:float">{value.ToString(CultureInfo.InvariantCulture)}</Value>
                      <SetsValue>true</SetsValue>
                    </Variable>
                """;

            File.WriteAllText(Path.Combine(_projectDirectory, "Screens", screenName + ".gusx"),
                $"""
                <?xml version="1.0" encoding="utf-8"?>
                <ScreenSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                  <Name>{screenName}</Name>
                  <State>
                    <Name>Default</Name>
                {Variable("X", x)}
                {Variable("Y", y)}
                {Variable("Width", width)}
                {Variable("Height", height)}
                  </State>
                  <Instance>
                    <Name>{instanceName}</Name>
                    <BaseType>{baseType}</BaseType>
                    <DefinedByBase>false</DefinedByBase>
                  </Instance>
                </ScreenSave>
                """);

            AddScreenReference(screenName);
        }

        /// <summary>Adds a screen holding one Sprite that shows a flat-color PNG of the given size, written beside the project.</summary>
        public void AddSpriteScreen(string screenName, string instanceName, string textureFileName, int textureWidth, int textureHeight)
        {
            WriteFlatPng(Path.Combine(_projectDirectory, textureFileName), textureWidth, textureHeight);

            File.WriteAllText(Path.Combine(_projectDirectory, "Screens", screenName + ".gusx"),
                $"""
                <?xml version="1.0" encoding="utf-8"?>
                <ScreenSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                  <Name>{screenName}</Name>
                  <State>
                    <Name>Default</Name>
                    <Variable>
                      <IsFile>true</IsFile>
                      <Type>string</Type>
                      <Name>{instanceName}.SourceFile</Name>
                      <Value xsi:type="xsd:string">{textureFileName}</Value>
                      <SetsValue>true</SetsValue>
                    </Variable>
                  </State>
                  <Instance>
                    <Name>{instanceName}</Name>
                    <BaseType>Sprite</BaseType>
                    <DefinedByBase>false</DefinedByBase>
                  </Instance>
                </ScreenSave>
                """);

            AddScreenReference(screenName);
        }

        private void AddScreenReference(string screenName)
        {
            string gumx = File.ReadAllText(Project);
            File.WriteAllText(Project, gumx.Replace("<ScreenReference Name=\"TestScreen\" />",
                $"<ScreenReference Name=\"TestScreen\" />\n  <ScreenReference Name=\"{screenName}\" />"));
        }

        // An 8-bit RGB PNG of one color; it compresses to almost nothing, so any size is cheap to write.
        private static void WriteFlatPng(string path, int width, int height)
        {
            static void WriteChunk(Stream stream, string type, byte[] data)
            {
                byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
                byte[] length = new byte[4];
                System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
                stream.Write(length);
                stream.Write(typeBytes);
                stream.Write(data);
                uint crc = 0xFFFFFFFF;
                foreach (byte value in typeBytes.Concat(data))
                {
                    crc ^= value;
                    for (int bit = 0; bit < 8; bit++)
                    {
                        crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
                    }
                }
                byte[] crcBytes = new byte[4];
                System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(crcBytes, ~crc);
                stream.Write(crcBytes);
            }

            byte[] header = new byte[13];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), width);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), height);
            header[8] = 8;
            header[9] = 2;

            using MemoryStream compressed = new MemoryStream();
            using (System.IO.Compression.ZLibStream zlib = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
            {
                byte[] row = new byte[1 + width * 3];
                for (int x = 0; x < width; x++)
                {
                    row[1 + x * 3] = 200;
                }
                for (int y = 0; y < height; y++)
                {
                    zlib.Write(row);
                }
            }

            using FileStream file = File.Create(path);
            file.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            WriteChunk(file, "IHDR", header);
            WriteChunk(file, "IDAT", compressed.ToArray());
            WriteChunk(file, "IEND", Array.Empty<byte>());
        }

        public void Dispose()
        {
            try { Directory.Delete(_workingDirectory, recursive: true); } catch { }
        }
    }
}
