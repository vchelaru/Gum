using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using GumFormsPlugin.Services;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Presentation.Tests;

public class ModifiedStandardsFinderTests : BaseTestClass
{
    private readonly ModifiedStandardsFinder _sut = new ModifiedStandardsFinder(new StandardComparer());
    private readonly GumProjectSave _project = new GumProjectSave();
    private readonly GumProjectSave _theme = new GumProjectSave();

    public ModifiedStandardsFinderTests()
    {
        ObjectFinder.Self.GumProjectSave = _project;
    }

    // A Standard as the tool's defaults define it, so it reads as unmodified.
    private static StandardElementSave DefaultStandard(string name)
    {
        StandardElementSave standard = new StandardElementSave { Name = name };
        StateSave defaults = StandardElementsManager.Self.GetDefaultStateFor(name)!.Clone();
        defaults.ParentContainer = standard;
        standard.States.Add(defaults);
        return standard;
    }

    private static StandardElementSave ModifiedStandard(string name, float x)
    {
        StandardElementSave standard = DefaultStandard(name);
        standard.DefaultState.SetValue("X", x, "float");
        return standard;
    }

    private void ProjectHas(StandardElementSave standard, bool usedByAComponent)
    {
        _project.StandardElements.Add(standard);
        if (usedByAComponent)
        {
            ComponentSave component = new ComponentSave { Name = "Card" };
            component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
            component.Instances.Add(new InstanceSave { Name = "Title", BaseType = standard.Name, ParentContainer = component });
            _project.Components.Add(component);
        }
        ObjectFinder.Self.ForceResetCache();
    }

    [Fact]
    public void FindModifiedInUse_ReturnsAStandardTheProjectChangedAndSomethingUses()
    {
        ProjectHas(ModifiedStandard("Text", x: 5), usedByAComponent: true);
        _theme.StandardElements.Add(ModifiedStandard("Text", x: 9));

        _sut.FindModifiedInUse(_theme).ShouldBe(new[] { "Text" });
    }

    [Fact]
    public void FindModifiedInUse_IgnoresAStandardTheProjectNeverChanged()
    {
        ProjectHas(DefaultStandard("Text"), usedByAComponent: true);
        _theme.StandardElements.Add(ModifiedStandard("Text", x: 9));

        _sut.FindModifiedInUse(_theme).ShouldBeEmpty();
    }

    [Fact]
    public void FindModifiedInUse_IgnoresAChangedStandardNothingUses()
    {
        ProjectHas(ModifiedStandard("Text", x: 5), usedByAComponent: false);
        _theme.StandardElements.Add(ModifiedStandard("Text", x: 9));

        _sut.FindModifiedInUse(_theme).ShouldBeEmpty();
    }

    [Fact]
    public void FindModifiedInUse_IgnoresAStandardThatAlreadyMatchesTheThemes()
    {
        // Adding the same theme twice must not warn about the Standards it wrote the first time.
        ProjectHas(ModifiedStandard("Text", x: 9), usedByAComponent: true);
        _theme.StandardElements.Add(ModifiedStandard("Text", x: 9));

        _sut.FindModifiedInUse(_theme).ShouldBeEmpty();
    }

    [Fact]
    public void FindModifiedInUse_IgnoresAThemeStandardTheProjectLacks()
    {
        _theme.StandardElements.Add(ModifiedStandard("Text", x: 9));

        _sut.FindModifiedInUse(_theme).ShouldBeEmpty();
    }
}
