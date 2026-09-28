using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.StateAnimation.SaveClasses;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Presentation.Tests.Logic;

public class MovedInstanceReferenceFinderTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly GumProjectSave _project;
    private readonly MovedInstanceReferenceFinder _finder;

    public MovedInstanceReferenceFinderTests()
    {
        _mocker = new AutoMocker();
        _project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _project;
        _mocker.GetMock<IReferenceFinderProjectProvider>().Setup(x => x.GumProjectSave).Returns(_project);
        _mocker.Use<IReferenceFinder>(_mocker.CreateInstance<ReferenceFinder>());
        _finder = _mocker.CreateInstance<MovedInstanceReferenceFinder>();
    }

    [Fact]
    public void GetReferencesBrokenByMove_IgnoresWhatTheMoveCarries()
    {
        ComponentSave button = CreateButton(out InstanceSave box, out InstanceSave label, out _);
        StateSave defaultState = button.GetDefaultStateOrThrow();
        defaultState.SetValue("Label.Text", "Hi", "string");
        defaultState.VariableLists.Add(new VariableListSave<string> { Name = "Label.VariableReferences", Value = new List<string> { "Y = Label.X" } });
        defaultState.VariableLists.Add(new VariableListSave<string> { Name = "Box.VariableReferences", Value = new List<string> { "Width = Label.Width" } });

        List<MovedInstanceReference> result = _finder.GetReferencesBrokenByMove(button, box, new[] { label });

        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetReferencesBrokenByMove_ReportsEachKindOfOutsideReference()
    {
        ComponentSave button = CreateButton(out InstanceSave box, out InstanceSave label, out _);
        StateSave defaultState = button.GetDefaultStateOrThrow();
        defaultState.VariableLists.Add(new VariableListSave<string> { Name = "Caption.VariableReferences", Value = new List<string> { "X = Label.X" } });
        defaultState.Variables.Add(new VariableSave { Name = "Label.Text", Type = "string", Value = "Hi", SetsValue = true, ExposedAsName = "LabelText" });
        StateSaveCategory category = new StateSaveCategory { Name = "Look" };
        StateSave hover = new StateSave { Name = "Hover", ParentContainer = button };
        hover.SetValue("Label.Red", 10, "int");
        hover.SetValue("Caption.Parent", "Label", "string");
        category.States.Add(hover);
        button.Categories.Add(category);

        ScreenSave menu = new ScreenSave { Name = "Menu" };
        menu.States.Add(new StateSave { Name = "Default", ParentContainer = menu });
        menu.Instances.Add(new InstanceSave { Name = "OkButton", BaseType = "Button", ParentContainer = menu });
        menu.GetDefaultStateOrThrow().VariableLists.Add(new VariableListSave<string> { Name = "VariableReferences", Value = new List<string> { "X = Components/Button.Label.X" } });
        _project.Screens.Add(menu);

        ComponentSave bigButton = new ComponentSave { Name = "BigButton", BaseType = "Button" };
        bigButton.States.Add(new StateSave { Name = "Default", ParentContainer = bigButton });
        bigButton.GetDefaultStateOrThrow().SetValue("Label.FontSize", 30, "int");
        _project.Components.Add(bigButton);

        ElementAnimationsSave animations = new ElementAnimationsSave();
        AnimationSave show = new AnimationSave { Name = "Show" };
        show.Animations.Add(new AnimationReferenceSave { Name = "Label.FadeIn" });
        animations.Animations.Add(show);
        _mocker.GetMock<IElementAnimationsProvider>().Setup(x => x.GetAnimationsFor(button, _project)).Returns(animations);

        List<MovedInstanceReference> result = _finder.GetReferencesBrokenByMove(button, box, new[] { label });

        result.ShouldAllBe(item => item.MovedInstanceName == "Label");
        result.Select(item => item.Description).ShouldBe(new[]
        {
            "Button (Default): Caption.VariableReferences line \"X = Label.X\"",
            "Menu (Default): VariableReferences line \"X = Components/Button.Label.X\"",
            "Button: exposed variable LabelText (Label.Text)",
            "Button (Hover): Label.Red = 10",
            "Button (Hover): Caption.Parent = Label",
            "BigButton (Default): Label.FontSize = 30",
            "Button: animation Show plays Label.FadeIn",
        }, ignoreOrder: true);
    }

    private ComponentSave CreateButton(out InstanceSave box, out InstanceSave label, out InstanceSave caption)
    {
        ComponentSave button = new ComponentSave { Name = "Button", BaseType = "Container" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        box = new InstanceSave { Name = "Box", BaseType = "Container", ParentContainer = button };
        label = new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = button };
        caption = new InstanceSave { Name = "Caption", BaseType = "Text", ParentContainer = button };
        button.Instances.Add(box);
        button.Instances.Add(label);
        button.Instances.Add(caption);
        button.GetDefaultStateOrThrow().SetValue("Label.Parent", "Box", "string");
        _project.Components.Add(button);
        return button;
    }
}
