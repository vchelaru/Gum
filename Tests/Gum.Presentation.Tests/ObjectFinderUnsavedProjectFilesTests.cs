using Gum.DataTypes;
using Gum.Managers;
using Shouldly;
using System.Linq;

namespace Gum.Presentation.Tests;

/// <summary>A project with no file name (new and unsaved, or whatever a test left in the singleton) has no
/// directory to resolve referenced files against, so the file walk reports none instead of throwing.</summary>
public class ObjectFinderUnsavedProjectFilesTests : BaseTestClass
{
    [Fact]
    public void GetAllFilesInProject_ShouldBeEmpty_WhenProjectHasNoFileName()
    {
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();

        ObjectFinder.Self.GetAllFilesInProject().ToList().ShouldBeEmpty();
    }

    [Fact]
    public void GetFilesReferencedBy_ShouldBeEmpty_WhenProjectHasNoFileName()
    {
        ScreenSave screen = new ScreenSave { Name = "Screen" };
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Screens.Add(screen);

        ObjectFinder.Self.GetFilesReferencedBy(screen).ShouldBeEmpty();
    }
}
