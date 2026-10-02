using System.Collections.Generic;
using System.Linq;
using HtmlToGumPlugin;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.HtmlToGum;

public class NodeResolverTests
{
    [Fact]
    public void Resolve_NodeOnPath_ReturnsThatNode()
    {
        FakeEnvironment env = new FakeEnvironment { PathVariable = "/usr/bin:/custom/bin" };
        env.Files.Add("/custom/bin/node");

        new NodeResolver(env).Resolve().ShouldBe("/custom/bin/node");
    }

    [Fact]
    public void Resolve_FinderLaunchWithMinimalPath_FindsHomebrewNode()
    {
        // launchd's PATH for an app launched from Finder.
        FakeEnvironment env = new FakeEnvironment { PathVariable = "/usr/bin:/bin:/usr/sbin:/sbin" };
        env.Files.Add("/opt/homebrew/bin/node");

        new NodeResolver(env).Resolve().ShouldBe("/opt/homebrew/bin/node");
    }

    [Fact]
    public void Resolve_OnlyNvmInstalls_ReturnsTheHighestVersion()
    {
        FakeEnvironment env = new FakeEnvironment { PathVariable = "/usr/bin" };
        env.Directories["/Users/me/.nvm/versions/node"] = ["v9.11.2", "v22.3.0", "v18.20.1"];
        env.Files.Add("/Users/me/.nvm/versions/node/v9.11.2/bin/node");
        env.Files.Add("/Users/me/.nvm/versions/node/v22.3.0/bin/node");
        env.Files.Add("/Users/me/.nvm/versions/node/v18.20.1/bin/node");

        new NodeResolver(env).Resolve().ShouldBe("/Users/me/.nvm/versions/node/v22.3.0/bin/node");
    }

    [Fact]
    public void Resolve_NothingInKnownLocations_AsksTheLoginShell()
    {
        FakeEnvironment env = new FakeEnvironment { PathVariable = "/usr/bin" };
        env.LoginShellOutput = "/Users/me/.local/share/fnm/bin/node";
        env.Files.Add("/Users/me/.local/share/fnm/bin/node");

        new NodeResolver(env).Resolve().ShouldBe("/Users/me/.local/share/fnm/bin/node");
    }

    [Fact]
    public void Resolve_LoginShellNamesAFileThatDoesNotExist_ReturnsNull()
    {
        FakeEnvironment env = new FakeEnvironment { PathVariable = "/usr/bin" };
        env.LoginShellOutput = "node: not found";

        new NodeResolver(env).Resolve().ShouldBeNull();
    }

    [Fact]
    public void Resolve_Windows_DoesNotProbeLoginShell()
    {
        FakeEnvironment env = new FakeEnvironment { IsWindows = true, PathVariable = @"C:\Windows" };
        env.LoginShellOutput = "/never/used/node";
        env.Files.Add("/never/used/node");

        new NodeResolver(env).Resolve().ShouldBeNull();
        env.LoginShellCalls.ShouldBe(0);
    }

    private class FakeEnvironment : INodeSearchEnvironment
    {
        public bool IsWindows { get; set; }
        public string HomeDirectory { get; set; } = "/Users/me";
        public string? PathVariable { get; set; }
        public HashSet<string> Files { get; } = [];
        public Dictionary<string, string[]> Directories { get; } = [];
        public string? LoginShellOutput { get; set; }
        public int LoginShellCalls { get; private set; }

        public bool FileExists(string path) => Files.Contains(path);

        public IEnumerable<string> GetSubdirectoryNames(string directory) =>
            Directories.TryGetValue(directory, out string[]? names) ? names : Enumerable.Empty<string>();

        public string? RunLoginShell(string command)
        {
            LoginShellCalls++;
            return LoginShellOutput;
        }
    }
}
