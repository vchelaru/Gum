using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.Editing;
using Gum.StateAnimation.SaveClasses;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class PendingAnimationEditsTests : BaseTestClass
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "PendingAnimationEditsTests_" + Guid.NewGuid().ToString("N"));

    public PendingAnimationEditsTests()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "Components"));
        Project.FullFileName = Path.Combine(_directory, "Project.gumx").Replace('\\', '/');
    }

    public override void Dispose()
    {
        base.Dispose();
        Directory.Delete(_directory, recursive: true);
    }

    private string SidecarPath(string extension = "ganx") => Path.Combine(_directory, "Components", "PopupAnimations." + extension);

    [Fact]
    public void Save_WritesWhatTheToolsSaveWritesAndDeletesAnEmptySidecar()
    {
        ComponentSave popup = EditingTestProject.AddComponent(Project, "Popup");
        PendingAnimationEdits pending = new PendingAnimationEdits(new FileElementAnimationsProvider());
        ElementAnimationsSave animations = pending.GetForEdit(popup, Project);
        animations.Animations.Add(new AnimationSave { Name = "FadeIn" });

        pending.Save(Project);

        File.Exists(SidecarPath()).ShouldBeTrue();
        File.ReadAllText(SidecarPath()).ShouldNotContain("ElementName");

        animations.Animations.Clear();
        pending.Save(Project);

        File.Exists(SidecarPath()).ShouldBeFalse();
    }

    [Fact]
    public void GetForEdit_StartsFromTheSidecarOnDisk()
    {
        ComponentSave popup = EditingTestProject.AddComponent(Project, "Popup");
        new ElementAnimationsSave { ElementName = "Popup", Animations = { new AnimationSave { Name = "Existing" } } }.Save(SidecarPath());
        PendingAnimationEdits pending = new PendingAnimationEdits(new FileElementAnimationsProvider());

        pending.GetForEdit(popup, Project).Animations.Select(item => item.Name).ShouldBe(new[] { "Existing" });
    }

    [Fact]
    public void GetAnimationsFor_ReturnsTheEditedAnimationsBeforeTheyAreSaved()
    {
        ComponentSave popup = EditingTestProject.AddComponent(Project, "Popup");
        PendingAnimationEdits pending = new PendingAnimationEdits(new FileElementAnimationsProvider());
        pending.GetForEdit(popup, Project).Animations.Add(new AnimationSave { Name = "FadeIn" });

        pending.GetAnimationsFor(popup, Project)!.Animations.Single().Name.ShouldBe("FadeIn");
        File.Exists(SidecarPath()).ShouldBeFalse();
    }

    [Fact]
    public void FillTopLevelNames_IncludesEditedAndOnDiskAnimations()
    {
        ComponentSave popup = EditingTestProject.AddComponent(Project, "Popup");
        ComponentSave other = EditingTestProject.AddComponent(Project, "Other");
        new ElementAnimationsSave { ElementName = "Other", Animations = { new AnimationSave { Name = "OnDisk" } } }
            .Save(Path.Combine(_directory, "Components", "OtherAnimations.ganx"));
        PendingAnimationEdits pending = new PendingAnimationEdits(new FileElementAnimationsProvider());
        pending.GetForEdit(popup, Project).Animations.Add(new AnimationSave { Name = "Edited" });
        List<TopLevelName> popupNames = new List<TopLevelName>();
        List<TopLevelName> otherNames = new List<TopLevelName>();

        pending.FillTopLevelNames(popup, popupNames);
        pending.FillTopLevelNames(other, otherNames);

        popupNames.Select(item => item.Name).ShouldBe(new[] { "Edited" });
        otherNames.Select(item => item.Name).ShouldBe(new[] { "OnDisk" });
    }

    [Fact]
    public void GetForEdit_WithAnUnreadableSidecar_FailsNamingTheFile()
    {
        ComponentSave popup = EditingTestProject.AddComponent(Project, "Popup");
        File.WriteAllText(SidecarPath(), "not xml");
        PendingAnimationEdits pending = new PendingAnimationEdits(new FileElementAnimationsProvider());

        Exception exception = Should.Throw<Exception>(() => pending.GetForEdit(popup, Project));

        exception.Message.ShouldContain("PopupAnimations.ganx");
    }

    [Fact]
    public void Save_WhenALaterSidecarCannotBeWritten_LeavesEarlierSidecarsAsTheyWere()
    {
        ComponentSave popup = EditingTestProject.AddComponent(Project, "Popup");
        ComponentSave other = EditingTestProject.AddComponent(Project, "Other");
        new ElementAnimationsSave { Animations = { new AnimationSave { Name = "Old" } } }.Save(SidecarPath());
        string before = File.ReadAllText(SidecarPath());
        PendingAnimationEdits pending = new PendingAnimationEdits(new FileElementAnimationsProvider());
        pending.GetForEdit(popup, Project).Animations.Add(new AnimationSave { Name = "New" });
        pending.GetForEdit(other, Project).Animations.Add(new AnimationSave { Name = "Blocked" });
        // A folder where the sidecar belongs makes writing it fail.
        Directory.CreateDirectory(Path.Combine(_directory, "Components", "OtherAnimations.ganx"));

        Should.Throw<Exception>(() => pending.Save(Project));

        File.ReadAllText(SidecarPath()).ShouldBe(before);
    }

    [Fact]
    public void Rollback_AfterASuccessfulSave_RestoresTheSidecarsAsTheyWere()
    {
        ComponentSave popup = EditingTestProject.AddComponent(Project, "Popup");
        new ElementAnimationsSave { Animations = { new AnimationSave { Name = "Old" } } }.Save(SidecarPath());
        string before = File.ReadAllText(SidecarPath());
        PendingAnimationEdits pending = new PendingAnimationEdits(new FileElementAnimationsProvider());
        pending.GetForEdit(popup, Project).Animations.Add(new AnimationSave { Name = "New" });
        pending.Save(Project);

        pending.Rollback();

        File.ReadAllText(SidecarPath()).ShouldBe(before);
    }
}
