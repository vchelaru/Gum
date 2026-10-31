using System;
using System.IO;
using Shouldly;
using Xunit;

namespace Gum.ProjectServices.Tests;

/// <summary>#7281: the one rule that decides the project folder and the folder remembered for next time.</summary>
public class NewProjectFolderTests
{
    private static readonly string Parent = Path.Combine(Path.GetTempPath(), "GumProjects");
    private static readonly string Own = Path.Combine(Parent, "MyProject1");

    [Fact]
    public void LocationEndingInTheName_IsTheProjectFolder_AndItsParentIsRemembered()
    {
        NewProjectFolder folder = NewProjectFolder.Resolve(Own, "MyProject1");

        folder.LocationIsProjectFolder.ShouldBeTrue();
        folder.ProjectFolder.ShouldBe(Own);
        folder.ParentFolder.ShouldBe(Parent);
        folder.ParentFolder.ShouldBe(Parent);
    }

    [Fact]
    public void LocationNotEndingInTheName_IsTheParent()
    {
        NewProjectFolder folder = NewProjectFolder.Resolve(Parent, "MyProject1");

        folder.LocationIsProjectFolder.ShouldBeFalse();
        folder.ProjectFolder.ShouldBe(Own);
        folder.ParentFolder.ShouldBe(Parent);
        folder.ParentFolder.ShouldBe(Parent);
    }

    [Fact]
    public void ATrailingSeparator_IsIgnored()
    {
        NewProjectFolder folder = NewProjectFolder.Resolve(Own + Path.DirectorySeparatorChar, "MyProject1");

        folder.ProjectFolder.ShouldBe(Own);
        folder.ParentFolder.ShouldBe(Parent);
    }

    [Fact]
    public void AnotherSeparatorStyle_IsIgnored()
    {
        NewProjectFolder folder = NewProjectFolder.Resolve(Own + Path.AltDirectorySeparatorChar, "MyProject1");

        folder.LocationIsProjectFolder.ShouldBeTrue();
        folder.ParentFolder.ShouldBe(Parent);
    }

    [Fact]
    public void ADifferentCase_MatchesWhenTheComparisonIgnoresCase()
    {
        NewProjectFolder folder = NewProjectFolder.Resolve(Path.Combine(Parent, "MYPROJECT1"), "MyProject1", StringComparison.OrdinalIgnoreCase);

        folder.LocationIsProjectFolder.ShouldBeTrue();
        folder.ParentFolder.ShouldBe(Parent);
    }

    [Fact]
    public void ADifferentCase_IsNotAMatchWhenTheComparisonIsExact()
    {
        string location = Path.Combine(Parent, "MYPROJECT1");

        NewProjectFolder folder = NewProjectFolder.Resolve(location, "MyProject1", StringComparison.Ordinal);

        folder.LocationIsProjectFolder.ShouldBeFalse();
        folder.ProjectFolder.ShouldBe(Path.Combine(location, "MyProject1"));
        folder.ParentFolder.ShouldBe(location);
    }

    [Fact]
    public void TheOsComparison_IsExactOnLinuxAndCaseInsensitiveElsewhere()
    {
        NewProjectFolder folder = NewProjectFolder.Resolve(Path.Combine(Parent, "myproject1"), "MyProject1");

        folder.LocationIsProjectFolder.ShouldBe(!OperatingSystem.IsLinux());
    }

    [Fact]
    public void ANameThatOnlyEndsLikeTheLocation_IsNotAMatch()
    {
        NewProjectFolder folder = NewProjectFolder.Resolve(Path.Combine(Parent, "OldMyProject1"), "MyProject1");

        folder.LocationIsProjectFolder.ShouldBeFalse();
    }

    [Fact]
    public void AFileSystemRoot_IsNeverTheProjectFolder_AndKeepsItsSeparator()
    {
        string root = Path.GetPathRoot(Parent)!;

        NewProjectFolder folder = NewProjectFolder.Resolve(root, "MyProject1");

        folder.LocationIsProjectFolder.ShouldBeFalse();
        folder.ProjectFolder.ShouldBe(Path.Combine(root, "MyProject1"));
        folder.ParentFolder.ShouldBe(root);
    }

    [Fact]
    public void AUncShareRootNamedLikeTheProject_IsTheParent_NotTheProjectFolder()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string share = new string(Path.DirectorySeparatorChar, 2) + "server" + Path.DirectorySeparatorChar + "MyProject1";

        NewProjectFolder folder = NewProjectFolder.Resolve(share, "MyProject1");

        folder.LocationIsProjectFolder.ShouldBeFalse();
        folder.ProjectFolder.ShouldBe(Path.Combine(share, "MyProject1"));
        folder.ParentFolder.ShouldBe(share);
    }
}
