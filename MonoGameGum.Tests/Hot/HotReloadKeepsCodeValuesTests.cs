using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// A hot reload leaves a variable alone unless the project changed it (#6980). A value set in code
/// survives a reload of unrelated variables on every kind of instance, a component instance as much
/// as a plain standard-element instance; a value the file changed is applied. Each test builds the
/// project twice because a real reload reads a second project object and the first is gone.
/// </summary>
public class HotReloadKeepsCodeValuesTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    /// <summary>One version of the saved project: only the values the tests edit.</summary>
    private sealed record Spec(
        float ButtonWidth = 150,
        float ButtonHeight = 255,
        float? BtnWidthOverride = null,
        float? BtnHeightOverride = null,
        float? RectWidth = null,
        float? RectHeight = null,
        bool NestedPanel = false,
        float ContainerWidth = 100,
        bool RectInHolder = false,
        float PanelButtonWidth = 150);

    private static void Set(StateSave state, string name, object value, string type)
    {
        state.Variables.Add(new VariableSave { Name = name, Value = value, Type = type, SetsValue = true });
    }

    private static StandardElementSave AddStandard(GumProjectSave project, string name)
    {
        StandardElementSave standard = new StandardElementSave { Name = name };
        StateSave state = new StateSave { Name = "Default", ParentContainer = standard };
        Set(state, "Width", 100f, "float");
        Set(state, "Height", 255f, "float");
        standard.States.Add(state);
        project.StandardElements.Add(standard);
        return standard;
    }

    private static GumProjectSave Build(Spec spec)
    {
        GumProjectSave project = new GumProjectSave();
        AddStandard(project, "Container").DefaultState!.Variables.Single(v => v.Name == "Width").Value = spec.ContainerWidth;
        AddStandard(project, "ColoredRectangle");

        ComponentSave button = new ComponentSave { Name = "Button", BaseType = "Container" };
        StateSave buttonDefault = new StateSave { Name = "Default", ParentContainer = button };
        Set(buttonDefault, "Width", spec.ButtonWidth, "float");
        Set(buttonDefault, "Height", spec.ButtonHeight, "float");
        button.States.Add(buttonDefault);
        project.Components.Add(button);

        ComponentSave panel = new ComponentSave { Name = "Panel", BaseType = "Container" };
        StateSave panelDefault = new StateSave { Name = "Default", ParentContainer = panel };
        panel.States.Add(panelDefault);
        panel.Instances.Add(new InstanceSave { Name = "InnerButton", BaseType = "Button", ParentContainer = panel });
        Set(panelDefault, "InnerButton.Width", spec.PanelButtonWidth, "float");
        project.Components.Add(panel);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        screen.Instances.Add(new InstanceSave { Name = "Btn", BaseType = "Button", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Rect", BaseType = "ColoredRectangle", ParentContainer = screen });
        if (spec.NestedPanel)
        {
            screen.Instances.Add(new InstanceSave { Name = "Panel1", BaseType = "Panel", ParentContainer = screen });
        }
        if (spec.RectInHolder)
        {
            screen.Instances.Add(new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = screen });
            Set(screenDefault, "Rect.Parent", "Holder", "string");
        }
        if (spec.BtnWidthOverride != null) Set(screenDefault, "Btn.Width", spec.BtnWidthOverride.Value, "float");
        if (spec.BtnHeightOverride != null) Set(screenDefault, "Btn.Height", spec.BtnHeightOverride.Value, "float");
        if (spec.RectWidth != null) Set(screenDefault, "Rect.Width", spec.RectWidth.Value, "float");
        if (spec.RectHeight != null) Set(screenDefault, "Rect.Height", spec.RectHeight.Value, "float");
        project.Screens.Add(screen);
        return project;
    }

    private sealed class Session
    {
        public GumProjectSave Project = null!;
        public GraphicalUiElement Screen = null!;

        public GraphicalUiElement Btn => Find(Screen, "Btn");
        public GraphicalUiElement Rect => Find(Screen, "Rect");
    }

    private static GraphicalUiElement Find(GraphicalUiElement parent, string name) =>
        parent.ContainedElements.Concat(parent.Children).First(c => c.Name == name);

    private static Session Load(Spec spec)
    {
        GumProjectSave project = Build(spec);
        ObjectFinder.Self.GumProjectSave = project;
        return new Session { Project = project, Screen = project.Screens.Single().ToGraphicalUiElement() };
    }

    private static void Reload(Session session, Spec spec)
    {
        GumProjectSave newProject = Build(spec);
        ObjectFinder.Self.GumProjectSave = newProject;
        GumHotReloadManager.ApplyDiff(new[] { session.Screen }, newProject, SystemManagers.Default, session.Project);
        session.Project = newProject;
    }

    [Fact]
    public void Reload_UnrelatedEdit_KeepsCodeSetWidthOnComponentInstance()
    {
        Spec before = new Spec(BtnHeightOverride: 255f);
        Session session = Load(before);
        session.Btn.Width = 321;

        Reload(session, before with { BtnHeightOverride = 128f });

        session.Btn.Height.ShouldBe(128f, "sanity: the edited variable is applied");
        session.Btn.Width.ShouldBe(321);
    }

    [Fact]
    public void Reload_UnrelatedEdit_KeepsCodeSetWidthOnStandardElementInstance()
    {
        Spec before = new Spec(RectHeight: 255f);
        Session session = Load(before);
        session.Rect.Width = 321;

        Reload(session, before with { RectHeight = 128f });

        session.Rect.Height.ShouldBe(128f, "sanity: the edited variable is applied");
        session.Rect.Width.ShouldBe(321);
    }

    [Fact]
    public void Reload_ComponentDefaultWidthChanged_OverwritesCodeSetWidth()
    {
        Spec before = new Spec();
        Session session = Load(before);
        session.Btn.Width = 321;

        Reload(session, before with { ButtonWidth = 200 });

        session.Btn.Width.ShouldBe(200);
    }

    [Fact]
    public void Reload_InstanceOverrideChanged_AppliesItOverCodeSetWidth()
    {
        Spec before = new Spec(BtnWidthOverride: 300);
        Session session = Load(before);
        session.Btn.Width = 321;

        Reload(session, before with { BtnWidthOverride = 310 });

        session.Btn.Width.ShouldBe(310);
    }

    [Fact]
    public void Reload_ComponentDefaultChangedUnderUnchangedInstanceOverride_KeepsTheOverride()
    {
        // The default moved, which resets the instance to it, so the unchanged override has to be
        // applied again or the file's value (300) is lost.
        Spec before = new Spec(BtnWidthOverride: 300);
        Session session = Load(before);

        Reload(session, before with { ButtonWidth = 200 });

        session.Btn.Width.ShouldBe(300);
    }

    [Fact]
    public void Reload_StandardInstanceOverrideChanged_AppliesItOverCodeSetWidth()
    {
        Spec before = new Spec(RectWidth: 150);
        Session session = Load(before);
        session.Rect.Width = 321;

        Reload(session, before with { RectWidth = 175 });

        session.Rect.Width.ShouldBe(175);
    }

    [Fact]
    public void Reload_BaseLayerChangedUnderUnchangedDerivedValue_KeepsTheDerivedValue()
    {
        // Container's width changed, which resets the Button instance to it, so Button's own unchanged
        // width has to be applied again.
        Spec before = new Spec();
        Session session = Load(before);
        session.Btn.Width = 321;

        Reload(session, before with { ContainerWidth = 130 });

        session.Btn.Width.ShouldBe(150);
    }

    [Fact]
    public void Reload_UnrelatedEdit_PutsAnInstanceTheFileParentsBackUnderItsParent()
    {
        Spec before = new Spec(RectInHolder: true, RectHeight: 255f);
        Session session = Load(before);
        GraphicalUiElement holder = Find(session.Screen, "Holder");
        session.Rect.Parent.ShouldBe(holder, "sanity");
        session.Rect.Parent = session.Screen;

        Reload(session, before with { RectHeight = 128f });

        session.Rect.Parent.ShouldBe(holder);
    }

    [Fact]
    public void Reload_NestedComponentInstance_KeepsCodeSetWidthUntilTheFileChangesIt()
    {
        Spec before = new Spec(NestedPanel: true);
        Session session = Load(before);
        GraphicalUiElement inner = Find(Find(session.Screen, "Panel1"), "InnerButton");
        inner.Width = 321;

        Reload(session, before with { BtnHeightOverride = 128f });
        inner.Width.ShouldBe(321, "an unrelated edit");

        Reload(session, before with { BtnHeightOverride = 128f, PanelButtonWidth = 180 });
        inner.Width.ShouldBe(180, "the component's own override of the nested button changed");
    }

    [Fact]
    public void Reload_CodeSetValuesSurviveRepeatedUnrelatedReloads()
    {
        Spec spec = new Spec(BtnHeightOverride: 255f);
        Session session = Load(spec);
        session.Btn.Width = 321;
        session.Rect.Width = 222;

        for (float height = 100; height < 104; height++)
        {
            spec = spec with { BtnHeightOverride = height };
            Reload(session, spec);
        }

        session.Btn.Width.ShouldBe(321);
        session.Rect.Width.ShouldBe(222);
    }

    [Fact]
    public void Reload_ControlInDisabledState_KeepsCodeSetWidthAndAppliesTheStatesNewValues()
    {
        GumProjectSave first = HotReloadFormsProjects.Build(HotReloadFormsProjects.First);
        GraphicalUiElement screen = HotReloadFormsProjects.Load(first);
        GraphicalUiElement buttonVisual = HotReloadFormsProjects.Child(screen, "Button1");
        HotReloadFormsProjects.Control<Button>(buttonVisual).IsEnabled = false;
        buttonVisual.Width = 321;

        GumProjectSave second = HotReloadFormsProjects.Build(HotReloadFormsProjects.Second);
        // The builder hands both projects the same default-state object for a standard element; a
        // file read from disk gives each project its own, which is what a reload compares.
        foreach (StandardElementSave standard in second.StandardElements)
        {
            StateSave own = new StateSave { Name = "Default", ParentContainer = standard };
            own.Variables.AddRange(standard.DefaultState!.Variables);
            standard.States.Clear();
            standard.States.Add(own);
        }
        ObjectFinder.Self.GumProjectSave = second;
        GumHotReloadManager.ApplyDiff(new[] { screen }, second, SystemManagers.Default, first);

        HotReloadFormsProjects.Control<Button>(buttonVisual).IsEnabled.ShouldBeFalse();
        HotReloadFormsProjects.Red(HotReloadFormsProjects.Child(buttonVisual, "Background"))
            .ShouldBe(HotReloadFormsProjects.Second.Disabled);
        buttonVisual.Width.ShouldBe(321);
    }

    [Fact]
    public void Reload_RandomEdits_OnlyTheChangedVariablesTakeTheFilesValues()
    {
        // Edits touch a random subset of the variables; every variable outside it keeps the value
        // code gave it, every variable inside shows the file's value.
        for (int seed = 1; seed <= 40; seed++)
        {
            Random random = new Random(seed);
            Spec spec = RandomSpec(random);
            Session session = Load(spec);
            session.Btn.Width = 1001;
            session.Btn.Height = 1002;
            session.Rect.Width = 1003;
            session.Rect.Height = 1004;

            for (int step = 0; step < 3; step++)
            {
                float btnWidth = session.Btn.Width;
                float btnHeight = session.Btn.Height;
                float rectWidth = session.Rect.Width;
                float rectHeight = session.Rect.Height;

                Spec next = Mutate(spec, random);
                Reload(session, next);

                string context = $"seed {seed} step {step}";
                bool btnWidthChanged = next.ButtonWidth != spec.ButtonWidth || next.BtnWidthOverride != spec.BtnWidthOverride;
                bool btnHeightChanged = next.ButtonHeight != spec.ButtonHeight || next.BtnHeightOverride != spec.BtnHeightOverride;

                session.Btn.Width.ShouldBe(btnWidthChanged ? next.BtnWidthOverride ?? next.ButtonWidth : btnWidth, context + " Btn.Width");
                session.Btn.Height.ShouldBe(btnHeightChanged ? next.BtnHeightOverride ?? next.ButtonHeight : btnHeight, context + " Btn.Height");
                session.Rect.Width.ShouldBe(next.RectWidth != spec.RectWidth ? next.RectWidth!.Value : rectWidth, context + " Rect.Width");
                session.Rect.Height.ShouldBe(next.RectHeight != spec.RectHeight ? next.RectHeight!.Value : rectHeight, context + " Rect.Height");
                spec = next;
            }
        }
    }

    // Overrides are always present so the edits only change values, which keeps the expectation
    // above a function of what changed (removing a variable is HotReloadRemovedVariableTests).
    private static Spec RandomSpec(Random random) => new Spec(
        ButtonWidth: Pick(random, 150f, 200f),
        ButtonHeight: Pick(random, 255f, 200f),
        BtnWidthOverride: Pick(random, 300f, 310f),
        BtnHeightOverride: Pick(random, 128f, 64f),
        RectWidth: Pick(random, 120f, 130f),
        RectHeight: Pick(random, 100f, 50f));

    private static Spec Mutate(Spec spec, Random random)
    {
        Spec next = spec;
        int count = random.Next(0, 4);
        for (int i = 0; i < count; i++)
        {
            switch (random.Next(6))
            {
                case 0: next = next with { ButtonWidth = Pick(random, 150f, 200f, 250f) }; break;
                case 1: next = next with { ButtonHeight = Pick(random, 255f, 200f, 180f) }; break;
                case 2: next = next with { BtnWidthOverride = Pick(random, 300f, 310f, 320f) }; break;
                case 3: next = next with { BtnHeightOverride = Pick(random, 128f, 64f, 32f) }; break;
                case 4: next = next with { RectWidth = Pick(random, 120f, 130f, 140f) }; break;
                default: next = next with { RectHeight = Pick(random, 100f, 50f, 25f) }; break;
            }
        }
        return next;
    }

    private static T Pick<T>(Random random, params T[] values) => values[random.Next(values.Length)];
}
