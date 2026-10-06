using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Architecture;

/// <summary>
/// <c>ObjectFinder.Self</c> and <c>StandardElementsManager.Self</c> are process-wide singletons, and
/// xUnit runs test classes in a different order every run. A test class that touches them without
/// deriving from <see cref="BaseTestClass"/> can leave a project loaded (or rely on one) and break an
/// unrelated class only in some orders (#5810). This scans the project's source for files that name
/// either singleton and fails for any test class declared there that does not derive from
/// <see cref="BaseTestClass"/>.
/// </summary>
public class StaticStateBaseClassGuardTests
{
    [Fact]
    public void TestClassesTouchingSingletons_DeriveFromBaseTestClass()
    {
        // Classes that touch a singleton but manage it themselves; each entry needs a reason.
        string[] exempt =
        {
        };

        string sourceDirectory = Path.Combine(FindRepoRoot(), "MonoGameGum.Tests");
        Regex singletonReference = new(@"\b(ObjectFinder|StandardElementsManager)\b");
        Regex classDeclaration = new(@"\bclass\s+(\w+)");
        Assembly testAssembly = typeof(StaticStateBaseClassGuardTests).Assembly;
        List<string> violations = new();

        foreach (string file in Directory.EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories))
        {
            string separator = Path.DirectorySeparatorChar.ToString();
            if (file.Contains(separator + "obj" + separator) || file.Contains(separator + "bin" + separator))
            {
                continue;
            }

            string[] codeLines = File.ReadAllLines(file)
                .Where(line => !IsCommentLine(line))
                .ToArray();
            if (!codeLines.Any(singletonReference.IsMatch))
            {
                continue;
            }

            IEnumerable<string> classNames = codeLines
                .SelectMany(line => classDeclaration.Matches(line).Select(match => match.Groups[1].Value));
            foreach (string className in classNames.Distinct())
            {
                IEnumerable<Type> offenders = testAssembly.GetTypes()
                    .Where(type => type.Name == className
                        && type != typeof(StaticStateBaseClassGuardTests)
                        && IsTestClass(type)
                        && !typeof(BaseTestClass).IsAssignableFrom(type)
                        && !exempt.Contains(type.Name));
                violations.AddRange(offenders.Select(type =>
                    $"{type.FullName} ({Path.GetRelativePath(sourceDirectory, file)})"));
            }
        }

        violations.ShouldBeEmpty(
            "These test classes reference ObjectFinder or StandardElementsManager but do not derive " +
            "from BaseTestClass, so they can leak singleton state into later tests:" +
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

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Gum")) && File.Exists(Path.Combine(current, "GumFull.sln")))
            {
                return current;
            }
            current = Path.GetFullPath(Path.Combine(current, ".."));
        }
        throw new DirectoryNotFoundException("Could not locate the repo root from " + AppContext.BaseDirectory);
    }
}
