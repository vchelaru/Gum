using System.Reflection;
using System.Text.RegularExpressions;
using Shouldly;

namespace SkiaGum.Tests.Architecture;

/// <summary>
/// <c>LoaderManager.ContentLoader</c>, <c>LoaderManager.CacheTextures</c> and
/// <c>FileManager.RelativeDirectory</c> are process-wide, and SkiaGum.Tests runs serially in whatever
/// order xUnit picks. A test class that assigns one without deriving from <see cref="BaseTestClass"/>
/// (which restores all three after each test) can leave a mock loader or a deleted folder behind and fail
/// an unrelated class, such as an SVG load, only in some orders (#7926).
/// </summary>
public class ProcessStaticsBaseClassGuardTests
{
    private static readonly Regex Assignment = new(@"\b(ContentLoader|CacheTextures|RelativeDirectory)\s*(?:\?\?|[-+*/%&|^])?=(?!=)");

    [Theory]
    [InlineData("LoaderManager.Self.ContentLoader = loader;")]
    [InlineData("LoaderManager.Self.ContentLoader ??= loader;")]
    [InlineData("LoaderManager.Self.CacheTextures = false;")]
    [InlineData("LoaderManager.Self.CacheTextures ??= false;")]
    [InlineData("FileManager.RelativeDirectory = path;")]
    [InlineData("FileManager.RelativeDirectory ??= path;")]
    [InlineData("FileManager.RelativeDirectory += \"sub/\";")]
    public void Assignment_Matches_AssignmentsToTheGuardedStatics(string line)
    {
        Assignment.IsMatch(line).ShouldBeTrue();
    }

    [Theory]
    [InlineData("if (LoaderManager.Self.CacheTextures == true)")]
    [InlineData("bool cached = LoaderManager.Self.CacheTextures;")]
    [InlineData("FileManager.RelativeDirectory.ShouldBe(path);")]
    public void Assignment_DoesNotMatch_ReadsAndComparisons(string line)
    {
        Assignment.IsMatch(line).ShouldBeFalse();
    }

    [Fact]
    public void TestClassesAssigningLoaderOrRelativeDirectory_DeriveFromBaseTestClass()
    {
        // Classes that assign these statics but manage them themselves; each entry needs a reason.
        string[] exempt =
        {
            // Exercises BaseTestClass.Dispose, so it restores the statics through the class under test.
            nameof(BaseTestClassTests),
        };

        string sourceDirectory = FindSourceDirectory();
        Regex assignment = Assignment;
        Regex classDeclaration = new(@"\bclass\s+(\w+)");
        Assembly testAssembly = typeof(ProcessStaticsBaseClassGuardTests).Assembly;
        List<string> violations = new List<string>();

        foreach (string file in Directory.EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string separator = Path.DirectorySeparatorChar.ToString();
            if (file.Contains(separator + "obj" + separator) || file.Contains(separator + "bin" + separator))
            {
                continue;
            }

            string[] codeLines = File.ReadAllLines(file).Where(line => !IsCommentLine(line)).ToArray();
            if (!codeLines.Any(assignment.IsMatch))
            {
                continue;
            }

            IEnumerable<string> classNames = codeLines
                .SelectMany(line => classDeclaration.Matches(line).Select(match => match.Groups[1].Value));
            foreach (string className in classNames.Distinct())
            {
                IEnumerable<Type> offenders = testAssembly.GetTypes()
                    .Where(type => type.Name == className
                        && type != typeof(ProcessStaticsBaseClassGuardTests)
                        && IsTestClass(type)
                        && !typeof(BaseTestClass).IsAssignableFrom(type)
                        && !exempt.Contains(type.Name));
                violations.AddRange(offenders.Select(type =>
                    $"{type.FullName} ({Path.GetRelativePath(sourceDirectory, file)})"));
            }
        }

        violations.ShouldBeEmpty(
            "These test classes assign ContentLoader, CacheTextures or RelativeDirectory but do not derive " +
            "from BaseTestClass, so they can leak them into later tests:" +
            Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    private static bool IsCommentLine(string line)
    {
        string trimmed = line.TrimStart();
        return trimmed.StartsWith("//") || trimmed.StartsWith("*") || trimmed.StartsWith("/*");
    }

    private static bool IsTestClass(Type type)
    {
        return !type.IsAbstract && type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Any(method => method.GetCustomAttributes<FactAttribute>(inherit: true).Any());
    }

    private static string FindSourceDirectory()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(current, "Tests", "SkiaGum.Tests");
            if (File.Exists(Path.Combine(candidate, "SkiaGum.Tests.csproj")))
            {
                return candidate;
            }
            current = Path.GetFullPath(Path.Combine(current, ".."));
        }
        throw new DirectoryNotFoundException("Could not locate Tests/SkiaGum.Tests from " + AppContext.BaseDirectory);
    }
}
