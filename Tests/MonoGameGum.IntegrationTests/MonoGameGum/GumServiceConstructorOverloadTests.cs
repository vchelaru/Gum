using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Forms controls constructed with a service (<c>new Button(service)</c>) build their visual in that
/// service's context without the caller activating it, and leave the previously current service current.
/// </summary>
public class GumServiceConstructorOverloadTests : BaseTestClass
{
    private const string MarkerName = "BuiltForServiceB";

    private readonly string _root;

    public GumServiceConstructorOverloadTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumCtorOverloads_" + Guid.NewGuid().ToString("N"));
    }

    public override void Dispose()
    {
        base.Dispose();
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private string WriteProject(string name)
    {
        GumProjectFolder folder = new GumProjectFolder(Path.Combine(_root, name));
        folder.Add(new GumElement("MainScreen", isScreen: true));
        return folder.Write();
    }

    /// <summary>
    /// Initializes A and B, then wraps B's template for every Forms type so the visual it makes is named
    /// <see cref="MarkerName"/> and the context that was current while it was built is recorded.
    /// </summary>
    private static CollisionGame CreateTwoServices(string pathA, string pathB, List<Gum.GumContext?> builtUnder)
    {
        CollisionGame game = new CollisionGame();
        game.RunOneFrame();
        game.InitializeA(pathA);
        game.InitializeB(pathB);

        game.ServiceB.Activate();
        foreach (Type type in new List<Type>(FrameworkElement.DefaultFormsTemplates.Keys))
        {
            VisualTemplate original = FrameworkElement.DefaultFormsTemplates[type];
            FrameworkElement.DefaultFormsTemplates[type] = new VisualTemplate((viewModel, createForms) =>
            {
                builtUnder.Add(Gum.GumContext.Current);
                GraphicalUiElement visual = original.CreateContent(viewModel, createForms);
                visual.Name = MarkerName;
                return visual;
            });
        }
        return game;
    }

    public static IEnumerable<object[]> Controls()
    {
        yield return new object[] { nameof(Button) };
        yield return new object[] { nameof(CheckBox) };
        yield return new object[] { nameof(TextBox) };
        yield return new object[] { nameof(ListBox) };
        yield return new object[] { nameof(ComboBox) };
        yield return new object[] { nameof(ScrollViewer) };
        yield return new object[] { nameof(Window) };
        yield return new object[] { nameof(Label) };
    }

    private static FrameworkElement Construct(string control, RenderingLibrary.IGumService? service) => control switch
    {
        nameof(Button) => service == null ? new Button() : new Button(service),
        nameof(CheckBox) => service == null ? new CheckBox() : new CheckBox(service),
        nameof(TextBox) => service == null ? new TextBox() : new TextBox(service),
        nameof(ListBox) => service == null ? new ListBox() : new ListBox(service),
        nameof(ComboBox) => service == null ? new ComboBox() : new ComboBox(service),
        nameof(ScrollViewer) => service == null ? new ScrollViewer() : new ScrollViewer(service),
        nameof(Window) => service == null ? new Window() : new Window(service),
        nameof(Label) => service == null ? new Label() : new Label(service),
        _ => throw new ArgumentException(control),
    };

    [Theory]
    [MemberData(nameof(Controls))]
    public void ConstructWithService_ShouldBuildTheVisualFromThatServicesTemplate_WithoutActivatingIt(string control)
    {
        List<Gum.GumContext?> builtUnder = new();
        using CollisionGame game = CreateTwoServices(WriteProject("A"), WriteProject("B"), builtUnder);
        game.ServiceA.Activate();
        builtUnder.Clear();

        FrameworkElement element = Construct(control, game.ServiceB);

        element.Visual.ShouldNotBeNull();
        element.Visual.Name.ShouldBe(MarkerName, "the visual should come from B's template");
        builtUnder.ShouldNotBeEmpty();
        builtUnder.ShouldAllBe(context => ReferenceEquals(context, game.ServiceB.Context),
            "every visual built for the control should be built with B current");
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void ConstructWithService_ShouldRestoreThePreviousCurrentService(string control)
    {
        List<Gum.GumContext?> builtUnder = new();
        using CollisionGame game = CreateTwoServices(WriteProject("A"), WriteProject("B"), builtUnder);
        game.ServiceA.Activate();

        Construct(control, game.ServiceB);

        Gum.GumContext.Current.ShouldBeSameAs(game.ServiceA.Context);
    }

    [Fact]
    public void ConstructWithoutService_ShouldResolveViaTheCurrentService()
    {
        List<Gum.GumContext?> builtUnder = new();
        using CollisionGame game = CreateTwoServices(WriteProject("A"), WriteProject("B"), builtUnder);

        game.ServiceB.Activate();
        Button underB = new Button();
        game.ServiceA.Activate();
        Button underA = new Button();

        underB.Visual.Name.ShouldBe(MarkerName);
        underA.Visual.Name.ShouldNotBe(MarkerName);
    }
}
