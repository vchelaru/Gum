using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Unusual inputs and states for the shared creation layer behind <c>gumcli new</c> (#7208).
/// </summary>
public class NewProjectEdgeCaseTests : BaseTestClass
{
    private readonly string _folder;
    private readonly string _checkout;

    public NewProjectEdgeCaseTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "GumNewProjectEdge_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _checkout = GumSourceLocator.Find(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("These tests run from a Gum checkout.");
    }

    public override void Dispose()
    {
        base.Dispose();
        MakeWritable(_folder);
        Directory.Delete(_folder, recursive: true);
    }

    private static void MakeWritable(string folder)
    {
        if (!OperatingSystem.IsWindows())
        {
            foreach (string directory in Directory.GetDirectories(folder, "*", SearchOption.AllDirectories))
            {
                File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private NewProjectService CreateService(params string[] gumSourceSearchDirectories)
    {
        FormsThemeLocator locator = new FormsThemeLocator(new[]
        {
            Path.Combine(_checkout, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes")
        });
        return new NewProjectService(locator, new NoRestoreRunner(), gumSourceSearchDirectories.Length == 0
            ? new[] { _checkout }
            : gumSourceSearchDirectories);
    }

    private static NewProjectRequest Request(string path, string? platform = null) =>
        new NewProjectRequest { Path = path, Platform = platform, Template = "empty", Restore = false };

    private static List<string> RelativeFiles(string root) =>
        Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

    #region Names

    [Theory]
    [InlineData("My Game")]
    [InlineData("My.Game.v2")]
    [InlineData("Jeu é")]
    [InlineData("日本語")]
    public async Task Execute_GumOnlyNameWithSpacesDotsOrUnicode_CreatesTheProjectAndThePlanMatches(string name)
    {
        NewProjectService service = CreateService();
        NewProjectRequest request = Request(Path.Combine(_folder, name));

        NewProjectPlan? plan = service.GetPlan(request);
        NewProjectResult result = await service.ExecuteAsync(request, null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        File.Exists(Path.Combine(_folder, name, name + ".gumj")).ShouldBeTrue();
        plan.ShouldNotBeNull();
        plan.Files.ToList().ShouldBe(RelativeFiles(Path.Combine(_folder, name)));
    }

    [Theory]
    [InlineData("My Game")]
    [InlineData("Jeu_é")]
    [InlineData("1Game")]
    public void Validate_PlatformNameThatIsNotAnIdentifierLikeName_IsRefusedAsInvalidName(string name)
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(Request(Path.Combine(_folder, name), "raylib"));

        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.InvalidProjectName);
        validation.Error.Message.ShouldContain(name);
    }

    [Theory]
    [InlineData("My-Game")]
    [InlineData("My.Game")]
    [InlineData("class")]
    [InlineData("_under")]
    public async Task Execute_PlatformNameNeedingEscaping_GeneratesAValidNamespace(string name)
    {
        NewProjectService service = CreateService();

        NewProjectResult result = await service.ExecuteAsync(Request(Path.Combine(_folder, name), "monogame"), null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string game1 = File.ReadAllText(Path.Combine(_folder, name, "Game1.cs"));
        Match match = Regex.Match(game1, @"^namespace\s+(\S+?);", RegexOptions.Multiline);
        match.Success.ShouldBeTrue();
        SyntaxFacts.IsValidIdentifier(match.Groups[1].Value.TrimStart('@')).ShouldBeTrue(match.Groups[1].Value);
    }

    [Fact]
    public async Task Execute_UppercaseProjectExtension_CreatesAJsonProjectAtThatPath()
    {
        NewProjectService service = CreateService();
        string path = Path.Combine(_folder, "Upper", "Game.GUMJ");

        NewProjectResult result = await service.ExecuteAsync(Request(path), null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        File.ReadAllText(path).TrimStart().ShouldStartWith("{");
    }

    #endregion

    #region Existing files and folders

    [Theory]
    [InlineData(null)]
    [InlineData("raylib")]
    public async Task Execute_TargetFolderIsAFile_FailsWithAMessageAndKeepsTheFile(string? platform)
    {
        NewProjectService service = CreateService();
        string taken = Path.Combine(_folder, "Taken");
        File.WriteAllText(taken, "mine");

        NewProjectResult result = await service.ExecuteAsync(Request(taken, platform), null, CancellationToken.None);

        result.Files.Success.ShouldBeFalse();
        result.Files.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        File.ReadAllText(taken).ShouldBe("mine");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("raylib")]
    public async Task Execute_ParentOfTargetIsAFile_FailsWithAMessageAndKeepsTheFile(string? platform)
    {
        NewProjectService service = CreateService();
        string parent = Path.Combine(_folder, "ParentFile");
        File.WriteAllText(parent, "mine");

        NewProjectResult result = await service.ExecuteAsync(Request(Path.Combine(parent, "Game"), platform), null, CancellationToken.None);

        result.Files.Success.ShouldBeFalse();
        result.Files.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        File.ReadAllText(parent).ShouldBe("mine");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("raylib")]
    public async Task Execute_ReadOnlyParent_FailsWithAMessageAndLeavesNothing(string? platform)
    {
        if (OperatingSystem.IsWindows() || Environment.UserName == "root")
        {
            return;
        }

        NewProjectService service = CreateService();
        string parent = Path.Combine(_folder, "ReadOnly");
        Directory.CreateDirectory(parent);
        File.SetUnixFileMode(parent, UnixFileMode.UserRead | UnixFileMode.UserExecute);

        NewProjectResult result = await service.ExecuteAsync(Request(Path.Combine(parent, "Game"), platform), null, CancellationToken.None);

        result.Files.Success.ShouldBeFalse();
        result.Files.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        Directory.GetFileSystemEntries(parent).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("raylib")]
    public async Task Execute_NameLongerThanTheFileSystemAllows_FailsWithAMessageAndLeavesNothing(string? platform)
    {
        NewProjectService service = CreateService();

        NewProjectResult result = await service.ExecuteAsync(Request(Path.Combine(_folder, new string('a', 300)), platform), null, CancellationToken.None);

        result.Files.Success.ShouldBeFalse();
        result.Files.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        Directory.GetFileSystemEntries(_folder).ShouldBeEmpty();
    }

    [Fact]
    public async Task Execute_DeeplyNestedNewFolders_CreatesTheProject()
    {
        NewProjectService service = CreateService();
        string deep = _folder;
        for (int i = 0; i < 30; i++)
        {
            deep = Path.Combine(deep, "d" + i);
        }

        NewProjectResult result = await service.ExecuteAsync(Request(Path.Combine(deep, "Game")), null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        File.Exists(Path.Combine(deep, "Game", "Game.gumj")).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_TheSameRequestTwice_RefusesTheSecondAndLeavesTheFirstUntouched()
    {
        NewProjectService service = CreateService();
        NewProjectRequest request = Request(Path.Combine(_folder, "Twice"), "raylib");
        NewProjectResult first = await service.ExecuteAsync(request, null, CancellationToken.None);
        first.Files.Success.ShouldBeTrue(first.Files.ErrorMessage);
        Dictionary<string, byte[]> before = RelativeFiles(Path.Combine(_folder, "Twice"))
            .ToDictionary(file => file, file => File.ReadAllBytes(Path.Combine(_folder, "Twice", file)));

        NewProjectResult second = await service.ExecuteAsync(request, null, CancellationToken.None);

        second.Files.Success.ShouldBeFalse();
        second.Files.ErrorMessage.ShouldContain("already exists");
        RelativeFiles(Path.Combine(_folder, "Twice")).ShouldBe(before.Keys.OrderBy(file => file, StringComparer.Ordinal).ToList());
        foreach (KeyValuePair<string, byte[]> file in before)
        {
            File.ReadAllBytes(Path.Combine(_folder, "Twice", file.Key)).ShouldBe(file.Value);
        }
    }

    [Theory]
    [InlineData("Proj.gumx", "Proj", false)]
    [InlineData("Proj.gumj", "Proj", true)]
    [InlineData("Proj.gumx", "Proj/Proj.gumj", false)]
    [InlineData("Proj.gumj", "Proj/Proj.gumx", false)]
    public async Task Validate_GumOnlyNextToAProjectOfTheOtherFormat_IsRefusedAsAlreadyExisting(
        string existingFile, string requestPath, bool useXml)
    {
        NewProjectService service = CreateService();
        string existing = Path.Combine(_folder, "Proj", existingFile);
        NewProjectResult first = await service.ExecuteAsync(Request(existing), null, CancellationToken.None);
        first.Files.Success.ShouldBeTrue(first.Files.ErrorMessage);

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = Path.Combine(_folder, requestPath.Replace('/', Path.DirectorySeparatorChar)),
            Template = "empty",
            UseXmlFormat = useXml
        });

        validation.Error.ShouldNotBeNull();
        validation.Error.Kind.ShouldBe(NewProjectErrorKind.ProjectAlreadyExists);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Validate_PlatformFolderHoldingAProjectOfEitherFormat_IsRefusedAsAlreadyExisting(bool existingIsXml)
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "Game");
        NewProjectResult first = await service.ExecuteAsync(
            new NewProjectRequest
            {
                Path = game, Platform = "raylib", Template = "empty", Restore = false, UseXmlFormat = existingIsXml
            },
            null, CancellationToken.None);
        first.Files.Success.ShouldBeTrue(first.Files.ErrorMessage);
        File.Delete(Path.Combine(game, "Game.csproj"));

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = game, Platform = "raylib", Template = "empty", UseXmlFormat = !existingIsXml
        });

        validation.Error.ShouldNotBeNull();
        validation.Error.Kind.ShouldBe(NewProjectErrorKind.ProjectAlreadyExists);
    }

    #endregion

    #region Source-linked

    [Theory]
    [InlineData("monogame")]
    [InlineData("kni")]
    [InlineData("kniskia")]
    [InlineData("raylib")]
    [InlineData("stride")]
    [InlineData("silknet")]
    public async Task Execute_SourceLinked_ReferencesARuntimeProjectThatExistsInTheCheckout(string platform)
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "Linked");

        NewProjectResult result = await service.ExecuteAsync(
            new NewProjectRequest { Path = game, Platform = platform, Template = "empty", SourceLinked = true, Restore = false },
            null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        ReferencedProject(result.Files.CsprojPath!).ShouldNotBeNull();
        File.Exists(ReferencedProject(result.Files.CsprojPath!)!).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_SourceLinkedToACheckoutMissingTheRuntimeProject_FailsAndLeavesNothing()
    {
        string partial = Path.Combine(_folder, "Partial");
        Directory.CreateDirectory(Path.Combine(partial, "GumCommon"));
        File.WriteAllText(Path.Combine(partial, "GumCommon", "GumCommon.csproj"), "<Project />");
        NewProjectService service = CreateService(partial);
        string game = Path.Combine(_folder, "Out", "Game");

        NewProjectResult result = await service.ExecuteAsync(
            new NewProjectRequest { Path = game, Platform = "monogame", Template = "empty", SourceLinked = true, Restore = false },
            null, CancellationToken.None);

        result.Files.Success.ShouldBeFalse();
        result.Files.ErrorMessage.ShouldContain("Gum source not found");
        Directory.Exists(Path.Combine(_folder, "Out")).ShouldBeFalse();
    }

    private static string? ReferencedProject(string csproj)
    {
        string? include = XDocument.Load(csproj).Descendants("ProjectReference").SingleOrDefault()?.Attribute("Include")?.Value;
        return include == null
            ? null
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(csproj)!, include.Replace('\\', Path.DirectorySeparatorChar)));
    }

    #endregion

    #region Themes and locators

    [Fact]
    public void Validate_ThemeWithOddCasing_ResolvesToTheThemeFolder()
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = Path.Combine(_folder, "Themed"), Template = "forms", Theme = "mEaDoW"
        });

        validation.IsValid.ShouldBeTrue(validation.Error?.Message);
        Path.GetFileName(validation.Resolved!.ThemeDirectory).ShouldBe("Meadow");
    }

    [Fact]
    public void FormsThemeLocator_SameThemeInTwoRoots_TheEarlierRootWinsAndTheNameIsListedOnce()
    {
        string first = MakeThemeRoot("First", "meadow");
        string second = MakeThemeRoot("Second", "Meadow", "Other");
        FormsThemeLocator locator = new FormsThemeLocator(new[] { first, second });

        locator.FindThemeDirectory("MEADOW").ShouldBe(Path.Combine(first, "meadow"));
        locator.GetAvailableThemes().Count(name => string.Equals(name, "meadow", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
        locator.GetAvailableThemes().ShouldContain("Other");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" Meadow")]
    [InlineData("../Meadow")]
    [InlineData("Meadow/")]
    [InlineData("Meadow/..")]
    public void FormsThemeLocator_NameThatIsNotExactlyAThemeFolder_FindsNothing(string themeName)
    {
        string root = MakeThemeRoot("Roots", "Meadow");
        FormsThemeLocator locator = new FormsThemeLocator(new[] { root, Path.Combine(_folder, "MissingRoot") });

        locator.FindThemeDirectory(themeName).ShouldBeNull();
    }

    [Fact]
    public void FormsThemeLocator_FolderWithoutAThemeProject_IsNeitherListedNorFound()
    {
        string root = MakeThemeRoot("Roots", "Meadow");
        Directory.CreateDirectory(Path.Combine(root, "NotATheme"));
        FormsThemeLocator locator = new FormsThemeLocator(new[] { root });

        locator.GetAvailableThemes().ShouldBe(new[] { "Meadow" });
        locator.FindThemeDirectory("NotATheme").ShouldBeNull();
    }

    private string MakeThemeRoot(string rootName, params string[] themeNames)
    {
        string root = Path.Combine(_folder, rootName);
        foreach (string themeName in themeNames)
        {
            Directory.CreateDirectory(Path.Combine(root, themeName));
            File.WriteAllText(Path.Combine(root, themeName, "GumProject.gumx"), "<GumProjectSave />");
        }

        return root;
    }

    [Fact]
    public void GumSourceLocator_StartInsideACheckoutFolderThatDoesNotExist_FindsTheCheckout()
    {
        string checkout = MakeCheckout("Checkout");

        GumSourceLocator.Find(Path.Combine(checkout, "No", "Such", "Folder")).ShouldBe(checkout);
    }

    [Fact]
    public void GumSourceLocator_StartIsAFileInsideACheckout_FindsTheCheckout()
    {
        string checkout = MakeCheckout("Checkout");
        string file = Path.Combine(checkout, "GumCommon", "GumCommon.csproj");

        GumSourceLocator.Find(file).ShouldBe(checkout);
    }

    [Fact]
    public void GumSourceLocator_NestedCheckouts_TheNearestWins()
    {
        string outer = MakeCheckout("Outer");
        string inner = MakeCheckout(Path.Combine("Outer", "Vendor", "Inner"));

        GumSourceLocator.Find(Path.Combine(inner, "Tools")).ShouldBe(inner);
        GumSourceLocator.Find(Path.Combine(outer, "Vendor")).ShouldBe(outer);
    }

    [Fact]
    public void GumSourceLocator_FolderNamedLikeTheProjectFile_IsNotACheckout()
    {
        string notCheckout = Path.Combine(_folder, "Fake");
        Directory.CreateDirectory(Path.Combine(notCheckout, "GumCommon", "GumCommon.csproj"));

        GumSourceLocator.Find(notCheckout).ShouldBeNull();
    }

    private string MakeCheckout(string relative)
    {
        string checkout = Path.Combine(_folder, relative);
        Directory.CreateDirectory(Path.Combine(checkout, "GumCommon"));
        File.WriteAllText(Path.Combine(checkout, "GumCommon", "GumCommon.csproj"), "<Project />");
        return checkout;
    }

    #endregion

    private class NoRestoreRunner : IRestoreRunner
    {
        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
            Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded });
    }
}
