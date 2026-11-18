using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.Editing;
using Gum.Services;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class InstanceAddRulesTests : BaseTestClass
{
    [Fact]
    public void GetWhyCannotAdd_RejectsScreensStandardTargetsAndCycles()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        ComponentSave counter = EditingTestProject.AddComponent(Project, "Counter");
        counter.Instances.Add(new InstanceSave { Name = "Inner", BaseType = "Hud", ParentContainer = counter });
        ScreenSave game = new ScreenSave { Name = "Game" };
        Project.Screens.Add(game);
        InstanceAddRules rules = new InstanceAddRules(new CircularReferenceManager(ObjectFinder.Self));

        rules.GetWhyCannotAdd(game, hud).ShouldNotBeNull();
        rules.GetWhyCannotAdd(hud, Project.StandardElements[0]).ShouldNotBeNull();
        rules.GetWhyCannotAdd(counter, hud)!.ShouldContain("circular");
        rules.GetWhyCannotAdd(hud, game).ShouldBeNull();
    }
}
