using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Gum.ProjectServices.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Shouldly;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Gum.Cli.Tests;

/// <summary>
/// Compiles the generated output of the Forms template (plus components derived from template
/// controls) and constructs the generated controls, so generated code that touches its children
/// before they exist, builds them in the wrong order, or creates a second Forms control on one
/// visual fails here rather than in a game. Gum Forms FullyInCode and legacy MonoGame FindByName.
/// </summary>
public class GeneratedFormsConstructionTests : BaseTestClass
{
    [Fact]
    public void Constructor_EveryGeneratedFormsControl_DoesNotThrow()
    {
        Assembly generated = GenerateAndCompile();
        Type frameworkElementType = GumCommon.GetType("Gum.Forms.Controls.FrameworkElement", throwOnError: true)!;

        List<string> failures = new List<string>();
        int constructed = 0;
        foreach (Type type in generated.GetTypes().Where(item =>
            frameworkElementType.IsAssignableFrom(item) && !item.IsAbstract && item.GetConstructor(Type.EmptyTypes) != null))
        {
            try
            {
                Activator.CreateInstance(type);
                constructed++;
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                string frames = string.Join(" <- ", (inner.StackTrace ?? "").Split('\n').Take(6).Select(line => line.Trim()));
                failures.Add($"{type.Name}: {inner.GetType().Name} {inner.Message} {frames}");
            }
        }

        failures.ShouldBeEmpty();
        constructed.ShouldBeGreaterThan(10);
    }

    // #6594: generated code uses no obsolete runtime member, so a game building it sees no CS0618.
    [Fact]
    public void Compile_GumForms_UsesNoObsoleteMember()
    {
        GenerateAndCompile(obsoleteUseIsError: true, editProject: AddLegacyColoredOutlineShapes);
    }

    // A project saved before the v3 standard surface keeps Red/Green/Blue/Alpha on the plain Circle
    // and Rectangle, and instances set them.
    private static void AddLegacyColoredOutlineShapes(GumProjectSave project)
    {
        ComponentSave component = AddComponent(project, "Elements/LegacyOutlines", "Container", ("OutlineCircle", "Circle"));
        component.Instances.Add(new InstanceSave { Name = "OutlineRectangle", BaseType = "Rectangle", ParentContainer = component });
        foreach (string standardName in new[] { "Circle", "Rectangle" })
        {
            StandardElementSave standard = project.StandardElements.Single(item => item.Name == standardName);
            foreach (string channel in new[] { "Red", "Green", "Blue", "Alpha" })
            {
                standard.DefaultState!.Variables.Add(new VariableSave { Name = channel, Type = "int", Value = 255, SetsValue = true });
                component.DefaultState!.Variables.Add(new VariableSave
                {
                    Name = $"Outline{standardName}.{channel}", Type = "int", Value = 100, SetsValue = true,
                });
            }
        }
    }

    [Fact]
    public void Constructor_GeneratedButton_AppliesStateWithChildrenParented()
    {
        object button = Construct(GenerateAndCompile(), "ButtonClose");

        Get(button, "ButtonCategoryState")!.ToString().ShouldBe("Enabled");
        Get(button, "Background.Parent").ShouldBeSameAs(Get(button, "Visual"));
    }

    [Theory]
    [InlineData("ButtonCloseDerived", "Background")]
    [InlineData("TextBoxDerived", "TextInstance")]
    [InlineData("WindowDerived", "InnerPanelInstance")]
    public void Constructor_DerivedComponent_BuildsEveryLevelUnderItsVisual(string typeName, string baseChildName)
    {
        object control = Construct(GenerateAndCompile(), typeName);
        object visual = Get(control, "Visual")!;

        Call(visual, "GetGraphicalUiElementByName", baseChildName).ShouldNotBeNull();
        Get(control, "Extra.Parent").ShouldBeSameAs(visual);
        // Defaults from both levels: the derived width wins over the base's, the derived child keeps its own.
        Get(visual, "Width").ShouldBe(150f);
        Get(control, "Extra.Width").ShouldBe(17f);
    }

    [Fact]
    public void Constructor_DerivedButton_AppliesStateAfterDerivedDefaults()
    {
        object button = Construct(GenerateAndCompile(), "ButtonCloseDerived");
        File.ReadAllText(Directory.GetFiles(TempDirectory, "ButtonCloseDerived.Generated.cs", SearchOption.AllDirectories).Single())
            .ShouldContain("this.FocusedIndicator.Visible = true;");

        // The derived default shows FocusedIndicator; the Enabled state, applied after the defaults, hides it.
        Get(button, "FocusedIndicator.Visible").ShouldBe(false);
        Get(button, "FocusedIndicator.Parent").ShouldBeSameAs(Get(button, "Visual"));
    }

    [Theory]
    [InlineData("ButtonCloseDerived", "Background,Icon,FocusedIndicator,Extra")]
    [InlineData("ButtonCloseThirdLevel", "Background,Icon,FocusedIndicator,Extra,ThirdExtra")]
    [InlineData("ChainC", "AChild,BChild,CChild")]
    public void Constructor_DerivedChain_ParentsEachChildOnceInLevelOrder(string typeName, string expectedChildren)
    {
        object control = Construct(GenerateAndCompile(), typeName);

        ChildNames(Get(control, "Visual")!).ShouldBe(expectedChildren);
    }

    [Fact]
    public void Constructor_DerivedLevelReparentingABaseChild_MovesIt()
    {
        object control = Construct(GenerateAndCompile(), "ChainReparent");
        object visual = Get(control, "Visual")!;

        ChildNames(visual).ShouldBe("AChild,Holder");
        ChildNames(Get(control, "Holder")!).ShouldBe("BChild");
    }

    [Fact]
    public void Constructor_ThirdLevel_AppliesEveryLevelsDefaultsInOrder()
    {
        object button = Construct(GenerateAndCompile(), "ButtonCloseThirdLevel");

        Get(button, "Visual.Width").ShouldBe(200f);
        Get(button, "Extra.Width").ShouldBe(33f);
    }

    private static string ChildNames(object visual) =>
        string.Join(",", ((IEnumerable)Get(visual, "Children")!).Cast<object>().Select(child => Get(child, "Name")));

    [Fact]
    public void Constructor_ExistingVisual_BindsToItsChildrenWithoutDuplicates()
    {
        Type type = GenerateAndCompile().GetTypes().Single(item => item.Name == "ButtonClose");
        object first = Activator.CreateInstance(type)!;
        object visual = Get(first, "Visual")!;
        type.GetProperty("Visual")!.SetValue(first, null);

        object second = Activator.CreateInstance(type, visual)!;

        Get(second, "Background").ShouldBeSameAs(Call(visual, "GetGraphicalUiElementByName", "Background"));
        Get(second, "Icon.Visual").ShouldBeSameAs(Call(visual, "GetGraphicalUiElementByName", "Icon"));
        ((ICollection)Get(visual, "Children")!).Count.ShouldBe(3);
    }

    private static object Construct(Assembly generated, string typeName) =>
        Activator.CreateInstance(generated.GetTypes().Single(item => item.Name == typeName))!;

    // Generated classes are internal to the compiled assembly, so members are read by reflection.
    private static object? Get(object? target, string path)
    {
        foreach (string name in path.Split('.'))
        {
            target = target!.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.GetValue(target);
        }
        return target;
    }

    private static object? Call(object target, string methodName, string argument) =>
        target.GetType().GetMethod(methodName, new[] { typeof(string) })!.Invoke(target, new object[] { argument });

    [Fact]
    public void Constructor_LegacyMonoGameFindByName_EveryFormsComponentConstructs()
    {
        Assembly generated = GenerateAndCompile(OutputLibrary.MonoGame, ObjectInstantiationType.FindByName);
        LoadProjectForFindByName();
        Type graphicalUiElementType = typeof(global::Gum.Wireframe.GraphicalUiElement);

        List<string> failures = new List<string>();
        int constructed = 0;
        foreach (Type type in generated.GetTypes().Where(item =>
            graphicalUiElementType.IsAssignableFrom(item) && !item.IsAbstract))
        {
            try
            {
                object visual = Activator.CreateInstance(type, true, true)!;
                // Components with a Forms behavior declare FormsControl and create it in AfterFullCreation.
                if (type.GetProperty("FormsControl", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) != null
                    && Get(visual, "FormsControl") == null)
                {
                    failures.Add($"{type.Name}: no Forms control");
                }
                constructed++;
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                string frames = string.Join(" <- ", (inner.StackTrace ?? "").Split('\n').Take(6).Select(line => line.Trim()));
                failures.Add($"{type.Name}: {inner.GetType().Name} {inner.Message} {frames}");
            }
        }

        failures.ShouldBeEmpty(string.Join(" | ", failures));
        constructed.ShouldBeGreaterThan(10);
    }

    [Theory]
    [InlineData("ButtonCloseDerivedRuntime", "Background,Icon,FocusedIndicator,Extra")]
    [InlineData("ButtonCloseThirdLevelRuntime", "Background,Icon,FocusedIndicator,Extra,ThirdExtra")]
    [InlineData("ChainCRuntime", "AChild,BChild,CChild")]
    public void Constructor_LegacyMonoGameFindByNameDerived_BuildsOnceWithOneFormsControl(string typeName, string expectedChildren)
    {
        Assembly generated = GenerateAndCompile(OutputLibrary.MonoGame, ObjectInstantiationType.FindByName);
        LoadProjectForFindByName();
        Type type = generated.GetTypes().Single(item => item.Name == typeName);

        object visual = Activator.CreateInstance(type, true, true)!;

        ChildNames(visual).ShouldBe(expectedChildren);
        object? formsControl = Get(visual, "FormsControlAsObject");
        if (typeName.StartsWith("ButtonClose"))
        {
            formsControl.ShouldNotBeNull();
            Get(formsControl, "Visual").ShouldBeSameAs(visual);
        }
        else
        {
            formsControl.ShouldBeNull();
        }
    }

    [Theory]
    [InlineData("ButtonCloseDerivedRuntime")]
    [InlineData("ButtonCloseThirdLevelRuntime")]
    public void Constructor_LegacyMonoGameFindByNameDerived_NoFormsControlWhenNotRequested(string typeName)
    {
        Assembly generated = GenerateAndCompile(OutputLibrary.MonoGame, ObjectInstantiationType.FindByName);
        LoadProjectForFindByName();
        Type type = generated.GetTypes().Single(item => item.Name == typeName);

        object visual = Activator.CreateInstance(type, true, false)!;

        Get(visual, "FormsControlAsObject").ShouldBeNull();
        ChildNames(visual).ShouldNotBeEmpty();
    }

    // #6597: a derived component saved without one of its base's instances as DefinedByBase.
    private static void AddDerivedMissingBaseInstance(GumProjectSave project)
    {
        ComponentSave baseComponent = AddComponent(project, "Elements/MissBase", "Container", ("A", "Container"));
        baseComponent.Instances.Add(new InstanceSave { Name = "B", BaseType = "Container", ParentContainer = baseComponent });
        ComponentSave derived = AddComponent(project, "Elements/MissDerived", "Elements/MissBase", ("C", "Container"));
        derived.Instances.RemoveAll(item => item.Name == "B");
    }

    // Three levels where every derived level is saved without the instances it inherits.
    private static void AddChainMissingBaseInstances(GumProjectSave project)
    {
        AddComponent(project, "Elements/MissChainG", "Container", ("G", "Container"));
        ComponentSave parent = AddComponent(project, "Elements/MissChainP", "Elements/MissChainG", ("P", "Container"));
        parent.Instances.RemoveAll(item => item.DefinedByBase);
        ComponentSave child = AddComponent(project, "Elements/MissChainK", "Elements/MissChainP", ("K", "Container"));
        child.Instances.RemoveAll(item => item.DefinedByBase);
    }

    // #6597. A derived element saved without one of its base's instances as DefinedByBase is repaired on
    // load (GumProjectSave.Initialize), so the generated FindByName ctor (base built with fullInstantiation:
    // false, #6577) and the plain runtime ToGraphicalUiElement both build the base's instance.
    [Fact]
    public void Constructor_LegacyMonoGameFindByNameDerived_BaseInstanceMissingFromDerivedList_IsBuilt()
    {
        Assembly generated = GenerateAndCompile(OutputLibrary.MonoGame, ObjectInstantiationType.FindByName, editProject: AddDerivedMissingBaseInstance);
        LoadProjectForFindByName();
        Type type = generated.GetTypes().Single(item => item.Name == "MissDerivedRuntime");

        object visual = Activator.CreateInstance(type, true, true)!;

        ChildNames(visual).ShouldBe("A,B,C");
        Get(visual, "A").ShouldNotBeNull();
        Get(visual, "B").ShouldBeSameAs(Call(visual, "GetGraphicalUiElementByName", "B"));
        Get(visual, "B").ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_LegacyMonoGameFindByNameDerived_EveryLevelMissingItsBaseInstances_BuildsAllOfThem()
    {
        Assembly generated = GenerateAndCompile(OutputLibrary.MonoGame, ObjectInstantiationType.FindByName, editProject: AddChainMissingBaseInstances);
        LoadProjectForFindByName();
        Type type = generated.GetTypes().Single(item => item.Name == "MissChainKRuntime");

        object visual = Activator.CreateInstance(type, true, true)!;

        ChildNames(visual).ShouldBe("G,P,K");
        Get(visual, "G").ShouldNotBeNull();
        Get(visual, "P").ShouldNotBeNull();
        Get(visual, "K").ShouldNotBeNull();
    }

    [Fact]
    public void ToGraphicalUiElement_DerivedComponent_BaseInstanceMissingFromDerivedList_IsBuilt()
    {
        GenerateAndCompile(OutputLibrary.MonoGame, ObjectInstantiationType.FindByName, editProject: AddDerivedMissingBaseInstance);
        LoadProjectForFindByName();

        RuntimeChildNames("Elements/MissDerived").ShouldBe("A,B,C");
    }

    // The base screen is hand written so the generated derived screen compiles (#6656).
    [Fact]
    public void ToGraphicalUiElement_DerivedScreen_BaseInstanceMissingFromDerivedList_IsBuilt()
    {
        GenerateAndCompile(
            OutputLibrary.MonoGame,
            ObjectInstantiationType.FindByName,
            editProject: project =>
            {
                ScreenSave baseScreen = AddScreen(project, "MissScreenBase", baseType: null, ("A", "Container"));
                baseScreen.Instances.Add(new InstanceSave { Name = "B", BaseType = "Container", ParentContainer = baseScreen });
                ScreenSave derived = AddScreen(project, "MissScreenDerived", baseScreen.Name, ("C", "Container"));
                derived.Instances.RemoveAll(item => item.DefinedByBase);
            },
            editSettings: settings => settings.BaseTypesNotCodeGenerated = "MissScreenBase",
            afterCodegen: () =>
            {
                foreach (string path in Directory.GetFiles(TempDirectory, "MissScreenBaseRuntime*.cs", SearchOption.AllDirectories))
                {
                    File.Delete(path);
                }
                File.WriteAllText(Path.Combine(TempDirectory, "MissScreenBase.cs"),
                    "namespace MyGame.Screens; public class MissScreenBase : global::Gum.Wireframe.GraphicalUiElement { }");
            });
        LoadProjectForFindByName();

        RuntimeChildNames("MissScreenDerived").ShouldBe("A,B,C");
    }

    // The base nests B under A with a base-defined Parent; the derived element lacks both.
    private static void AddDerivedMissingNestedBaseInstances(GumProjectSave project)
    {
        ComponentSave baseComponent = AddComponent(project, "Elements/NestBase", "Container", ("A", "Container"));
        baseComponent.Instances.Add(new InstanceSave { Name = "B", BaseType = "Container", ParentContainer = baseComponent });
        baseComponent.DefaultState!.Variables.Add(new VariableSave { Name = "B.Parent", Type = "string", Value = "A", SetsValue = true });
        ComponentSave derived = AddComponent(project, "Elements/NestDerived", "Elements/NestBase", ("C", "Container"));
        derived.Instances.RemoveAll(item => item.DefinedByBase);
    }

    [Fact]
    public void Constructor_LegacyMonoGameFindByNameDerived_RepairedInstanceWithBaseParent_StaysUnderItsParent()
    {
        Assembly generated = GenerateAndCompile(OutputLibrary.MonoGame, ObjectInstantiationType.FindByName, editProject: AddDerivedMissingNestedBaseInstances);
        LoadProjectForFindByName();
        Type type = generated.GetTypes().Single(item => item.Name == "NestDerivedRuntime");

        object visual = Activator.CreateInstance(type, true, true)!;

        ChildNames(visual).ShouldBe("A,C");
        ChildNames(Get(visual, "A")!).ShouldBe("B");
        Get(visual, "B.Parent").ShouldBeSameAs(Get(visual, "A"));
    }

    [Fact]
    public void ToGraphicalUiElement_DerivedComponent_RepairedInstanceWithBaseParent_StaysUnderItsParent()
    {
        GenerateAndCompile(OutputLibrary.MonoGame, ObjectInstantiationType.FindByName, editProject: AddDerivedMissingNestedBaseInstances);
        LoadProjectForFindByName();

        RuntimeChildNames("Elements/NestDerived").ShouldBe("A,C");
        RuntimeChildNames("Elements/NestDerived", "A").ShouldBe("B");
    }

    private static string RuntimeChildNames(string elementName, string? childPath = null)
    {
        ElementSave element = Gum.Managers.ObjectFinder.Self.GetElementSave(elementName)!;
        object systemManagers = MonoGameGum.GetType("RenderingLibrary.SystemManagers", throwOnError: true)!
            .GetProperty("Default")!.GetValue(null)!;

        global::Gum.Wireframe.GraphicalUiElement visual = global::GumRuntime.ElementSaveExtensions.ToGraphicalUiElement(
            element, (global::RenderingLibrary.ISystemManagers)systemManagers, addToManagers: false);
        if (childPath != null)
        {
            visual = visual.GetGraphicalUiElementByName(childPath)!;
        }

        return string.Join(",", visual.Children.Cast<global::Gum.Wireframe.GraphicalUiElement>().Select(child => child.Name));
    }

    // The base is hand written (listed in BaseTypesNotCodeGenerated), so it never builds its element:
    // the derived ctor makes no base(fullInstantiation: false) call and builds every level itself.
    [Fact]
    public void Constructor_LegacyMonoGameFindByNameDerived_BaseTypeNotCodeGenerated_BuildsEachChildOnce()
    {
        Assembly generated = GenerateAndCompile(
            OutputLibrary.MonoGame,
            ObjectInstantiationType.FindByName,
            editProject: project =>
            {
                ComponentSave baseComponent = AddComponent(project, "Elements/HandBase", "Container", ("A", "Container"));
                AddComponent(project, "Elements/HandDerived", baseComponent.Name, ("C", "Container"));
            },
            editSettings: settings => settings.BaseTypesNotCodeGenerated = "Elements/HandBase",
            afterCodegen: () =>
            {
                foreach (string path in Directory.GetFiles(TempDirectory, "HandBaseRuntime*.cs", SearchOption.AllDirectories))
                {
                    File.Delete(path);
                }
                File.WriteAllText(Path.Combine(TempDirectory, "HandBaseRuntime.cs"),
                    "namespace MyGame.Components.Elements; public class HandBaseRuntime : global::Gum.GueDeriving.ContainerRuntime { }");
            });
        LoadProjectForFindByName();
        Type type = generated.GetTypes().Single(item => item.Name == "HandDerivedRuntime");

        object visual = Activator.CreateInstance(type, true, true)!;

        ChildNames(visual).ShouldBe("A,C");
        File.ReadAllText(Directory.GetFiles(TempDirectory, "HandDerivedRuntime.Generated.cs", SearchOption.AllDirectories).Single())
            .ShouldNotContain("base(fullInstantiation");
    }

    // A screen derives from a screen through its BaseType. The derived class inherits the BaseType name
    // verbatim, so the base is a hand-written class listed in BaseTypesNotCodeGenerated (#6656 covers a
    // generated base screen).
    [Fact]
    public void Constructor_LegacyMonoGameFindByNameDerived_ScreenWithHandWrittenBase_BuildsEachChildOnce()
    {
        Assembly generated = GenerateAndCompile(
            OutputLibrary.MonoGame,
            ObjectInstantiationType.FindByName,
            editProject: project =>
            {
                ScreenSave baseScreen = AddScreen(project, "ScreenBase", baseType: null, ("A", "Container"));
                AddScreen(project, "ScreenDerived", baseScreen.Name, ("C", "Container"));
            },
            editSettings: settings => settings.BaseTypesNotCodeGenerated = "ScreenBase",
            afterCodegen: () =>
            {
                foreach (string path in Directory.GetFiles(TempDirectory, "ScreenBaseRuntime*.cs", SearchOption.AllDirectories))
                {
                    File.Delete(path);
                }
                File.WriteAllText(Path.Combine(TempDirectory, "ScreenBase.cs"),
                    "namespace MyGame.Screens; public class ScreenBase : global::Gum.Wireframe.GraphicalUiElement { }");
            });
        LoadProjectForFindByName();
        Type type = generated.GetTypes().Single(item => item.Name == "ScreenDerivedRuntime");

        object visual = Activator.CreateInstance(type, true, true)!;

        ChildNames(visual).ShouldBe("A,C");
    }

    // Raylib and Silk emit the same derived ctor as MonoGame, so a derived runtime builds its tree once.
    [Theory]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.Raylib)]
    [InlineData(OutputLibrary.Silk)]
    public void Generate_LegacyFindByNameDerived_PassesFullInstantiationFalseToBase(OutputLibrary outputLibrary)
    {
        Generate(outputLibrary, ObjectInstantiationType.FindByName, editProject: null);

        string derived = File.ReadAllText(Directory.GetFiles(TempDirectory, "ButtonCloseDerivedRuntime.Generated.cs", SearchOption.AllDirectories).Single());
        derived.ShouldContain("public ButtonCloseDerivedRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(fullInstantiation: false, tryCreateFormsObject: tryCreateFormsObject)");
        string root = File.ReadAllText(Directory.GetFiles(TempDirectory, "ButtonCloseRuntime.Generated.cs", SearchOption.AllDirectories).Single());
        root.ShouldNotContain("base(fullInstantiation");
    }

    private static ScreenSave AddScreen(GumProjectSave project, string name, string? baseType, (string Name, string BaseType) instance)
    {
        ScreenSave screen = new ScreenSave { Name = name, BaseType = baseType };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        ScreenSave? baseScreen = project.Screens.SingleOrDefault(item => item.Name == baseType);
        foreach (InstanceSave baseInstance in baseScreen?.Instances ?? new List<InstanceSave>())
        {
            screen.Instances.Add(new InstanceSave { Name = baseInstance.Name, BaseType = baseInstance.BaseType, DefinedByBase = true, ParentContainer = screen });
        }
        screen.Instances.Add(new InstanceSave { Name = instance.Name, BaseType = instance.BaseType, ParentContainer = screen });
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Screen });
        return screen;
    }

    // A generated runtime registers itself as the default visual of its Forms type, so `new Button()`
    // builds that runtime with tryCreateFormsObject: false and then becomes the Forms control of it.
    [Fact]
    public void FormsControlConstructor_LegacyMonoGameFindByName_UsesGeneratedVisualWithoutSecondControl()
    {
        Assembly generated = GenerateAndCompile(OutputLibrary.MonoGame, ObjectInstantiationType.FindByName);
        LoadProjectForFindByName();
        Type frameworkElementType = GumCommon.GetType("Gum.Forms.Controls.FrameworkElement", throwOnError: true)!;
        IDictionary templates = (IDictionary)frameworkElementType.GetProperty("DefaultFormsTemplates")!.GetValue(null)!;
        Dictionary<object, object?> previousTemplates = templates.Keys.Cast<object>().ToDictionary(key => key, key => templates[key]);
        try
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(generated.ManifestModule.ModuleHandle);
            Type buttonType = GumCommon.GetType("Gum.Forms.Controls.Button", throwOnError: true)!;
            Type listBoxItemType = GumCommon.GetType("Gum.Forms.Controls.ListBoxItem", throwOnError: true)!;

            List<string> failures = new List<string>();
            foreach (Type formsType in new[] { buttonType, listBoxItemType })
            {
                try
                {
                    object control = Activator.CreateInstance(formsType)!;
                    object visual = Get(control, "Visual")!;
                    visual.GetType().Assembly.ShouldBeSameAs(generated, formsType.Name);
                    Get(visual, "FormsControlAsObject").ShouldBeSameAs(control, formsType.Name);
                }
                catch (TargetInvocationException exception)
                {
                    failures.Add($"{formsType.Name}: {exception.InnerException?.Message}");
                }
            }
            failures.ShouldBeEmpty(string.Join(" | ", failures));
        }
        finally
        {
            templates.Clear();
            foreach (KeyValuePair<object, object?> pair in previousTemplates)
            {
                templates[pair.Key] = pair.Value;
            }
        }
    }

    // What GumService.Initialize does with a gumx before FindByName code builds from it.
    private void LoadProjectForFindByName()
    {
        Type systemManagersType = MonoGameGum.GetType("RenderingLibrary.SystemManagers", throwOnError: true)!;
        systemManagersType.GetMethod("RegisterComponentRuntimeInstantiations", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(systemManagersType.GetProperty("Default")!.GetValue(null), null);
        GumProjectSave project = GumProjectSave.Load(Path.Combine(TempDirectory, "MyProject.gumx"), out _)!;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = project;
        Gum.Managers.StandardElementsManager.Self.RegisterExtendedDefaultStates();
        project.Initialize();
        MonoGameGum.GetType("Gum.Forms.FormsUtilities", throwOnError: true)!
            .GetMethod("RegisterFromFileFormRuntimeDefaults", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, null);
    }

    private Assembly GenerateAndCompile(
        OutputLibrary outputLibrary = OutputLibrary.GumForms,
        ObjectInstantiationType instantiationType = ObjectInstantiationType.FullyInCode,
        bool obsoleteUseIsError = false,
        Action<GumProjectSave>? editProject = null,
        Action<CodeOutputProjectSettings>? editSettings = null,
        Action? afterCodegen = null)
    {
        Generate(outputLibrary, instantiationType, editProject, editSettings);
        afterCodegen?.Invoke();

        Assembly generated = Compile(Directory.GetFiles(TempDirectory, "*.cs", SearchOption.AllDirectories), obsoleteUseIsError);
        InstallHeadlessRuntime();
        return generated;
    }

    private void Generate(
        OutputLibrary outputLibrary,
        ObjectInstantiationType instantiationType,
        Action<GumProjectSave>? editProject,
        Action<CodeOutputProjectSettings>? editSettings = null)
    {
        string gumxPath = Path.Combine(TempDirectory, "MyProject.gumx");
        new FormsTemplateCreator().Create(gumxPath);
        AddDerivedComponents(gumxPath, editProject);
        File.WriteAllText(Path.Combine(TempDirectory, "MyGame.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup>" +
            "<PackageReference Include=\"MonoGame.Framework.DesktopGL\" Version=\"3.8.4\" />" +
            "</ItemGroup></Project>");

        // codegen replaces settings without a CodeProjectRoot with auto-detected FindByName ones.
        CliTestHelper.Run("codegen-init", gumxPath).ExitCode.ShouldBe(0);
        CodeOutputProjectSettingsManager settingsManager = new CodeOutputProjectSettingsManager(
            new SilentLogger(), new FixedProjectDirectoryProvider(TempDirectory + Path.DirectorySeparatorChar));
        CodeOutputProjectSettings settings = settingsManager.CreateOrLoadSettingsForProject();
        settings.OutputLibrary = outputLibrary;
        settings.ObjectInstantiationType = instantiationType;
        editSettings?.Invoke(settings);
        settingsManager.WriteSettingsForProject(settings);

        CliTestHelper codegen = CliTestHelper.Run("codegen", gumxPath);
        codegen.ExitCode.ShouldBe(0, codegen.StandardError + codegen.StandardOutput);
    }

    // Each derived component adds a child and overrides a default at both levels. Every base child is
    // also stored on the derived level as a DefinedByBase instance, as the tool saves it.
    private static void AddDerivedComponents(string gumxPath, Action<GumProjectSave>? editProject)
    {
        GumProjectSave project = GumProjectSave.Load(gumxPath, out _)!;
        foreach (string baseName in new[] { "Controls/ButtonClose", "Controls/TextBox", "Controls/Window" })
        {
            ComponentSave component = AddComponent(project, baseName + "Derived", baseName, ("Extra", "Container"));
            Set(component, "Width", 150f);
            Set(component, "Extra.Width", 17f);
        }

        ComponentSave derivedButton = project.Components.Single(item => item.Name == "Controls/ButtonCloseDerived");
        Set(derivedButton, "FocusedIndicator.Visible", true);

        // Third level over a Forms control.
        ComponentSave thirdLevel = AddComponent(project, "Controls/ButtonCloseThirdLevel", "Controls/ButtonCloseDerived", ("ThirdExtra", "Container"));
        Set(thirdLevel, "Width", 200f);
        Set(thirdLevel, "Extra.Width", 33f);

        // A chain with no Forms behavior.
        AddComponent(project, "Elements/ChainA", "Container", ("AChild", "Container"));
        ComponentSave chainB = AddComponent(project, "Elements/ChainB", "Elements/ChainA", ("BChild", "Container"));
        Set(chainB, "AChild.Width", 5f);
        ComponentSave chainC = AddComponent(project, "Elements/ChainC", "Elements/ChainB", ("CChild", "Container"));
        Set(chainC, "BChild.Width", 6f);

        // A derived level that moves a base child under its own child.
        ComponentSave reparent = AddComponent(project, "Elements/ChainReparent", "Elements/ChainB", ("Holder", "Container"));
        reparent.DefaultState!.Variables.Add(new VariableSave { Name = "BChild.Parent", Type = "string", Value = "Holder", SetsValue = true });

        editProject?.Invoke(project);

        project.Save(gumxPath, saveElements: true);
    }

    private static ComponentSave AddComponent(GumProjectSave project, string name, string baseType, (string Name, string BaseType) instance)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        ComponentSave? baseComponent = project.Components.SingleOrDefault(item => item.Name == baseType);
        foreach (InstanceSave baseInstance in baseComponent?.Instances ?? new List<InstanceSave>())
        {
            component.Instances.Add(new InstanceSave { Name = baseInstance.Name, BaseType = baseInstance.BaseType, DefinedByBase = true, ParentContainer = component });
        }
        component.Instances.Add(new InstanceSave { Name = instance.Name, BaseType = instance.BaseType, ParentContainer = component });
        project.Components.Add(component);
        project.ComponentReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Component });
        return component;
    }

    private static void Set(ComponentSave component, string name, object value) =>
        component.DefaultState!.Variables.Add(new VariableSave
        {
            Name = name,
            Type = value is bool ? "bool" : "float",
            Value = value,
            SetsValue = true,
        });

    private static void InstallHeadlessRuntime()
    {
        // Gum.Cli.Tests also loads RaylibGum and the Forms placeholders, so the MonoGame runtime is reached by name.
        Type systemManagersType = MonoGameGum.GetType("RenderingLibrary.SystemManagers", throwOnError: true)!;
        PropertyInfo defaultProperty = systemManagersType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static)!;
        if (defaultProperty.GetValue(null) == null)
        {
            object systemManagers = Activator.CreateInstance(systemManagersType)!;
            systemManagersType.GetProperty("Renderer")!.SetValue(systemManagers,
                Activator.CreateInstance(MonoGameGum.GetType("RenderingLibrary.Graphics.Renderer", throwOnError: true)!));
            defaultProperty.SetValue(null, systemManagers);
        }

        // The hooks SystemManagers.Initialize installs, minus the graphics device. Other tests in this
        // process may have left another runtime's hooks installed.
        Type customSetProperty = MonoGameGum.GetTypes().Single(item => item.Name == "CustomSetPropertyOnRenderable");
        Type renderableCreator = MonoGameGum.GetType("Gum.Renderables.RenderableCreator", throwOnError: true)!;
        Install(typeof(global::Gum.Wireframe.GraphicalUiElement), "SetPropertyOnRenderable", customSetProperty, "SetPropertyOnRenderable");
        Install(typeof(global::Gum.Wireframe.GraphicalUiElement), "UpdateFontFromProperties", customSetProperty, "UpdateToFontValues");
        Install(typeof(global::GumRuntime.ElementSaveExtensions), "CustomCreateGraphicalComponentFunc", renderableCreator, "HandleCreateGraphicalComponent");

        // The template's sprites point at a texture the headless run cannot load. An in-process gumcli
        // command that ran SystemManagers.Initialize earlier leaves this at ThrowException; BaseTestClass restores it.
        global::Gum.Wireframe.GraphicalUiElement.MissingFileBehavior = global::Gum.Wireframe.MissingFileBehavior.ConsumeSilently;

        // No project loaded: generated code then skips the standard-element lookups, which need a renderer.
        Gum.Managers.ObjectFinder.Self.GumProjectSave = null;
    }

    private static Assembly GumCommon => Assembly.Load("GumCommon");

    private static Assembly MonoGameGum => Assembly.Load("MonoGameGum");

    private static Assembly Compile(IEnumerable<string> paths, bool obsoleteUseIsError)
    {
        CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        List<SyntaxTree> trees = paths
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parseOptions, path))
            .ToList();

        // Only the framework and the MonoGame runtime: RaylibGum and the Forms placeholders declare the same type names.
        string frameworkDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        HashSet<string> referencePaths = new HashSet<string>(
            Directory.GetFiles(frameworkDirectory, "*.dll").Where(IsManagedAssembly),
            StringComparer.OrdinalIgnoreCase)
        {
            GumCommon.Location,
            MonoGameGum.Location,
            Assembly.Load("MonoGame.Framework").Location,
        };

        CSharpCompilation compilation = CSharpCompilation.Create(
            "FullyInCodeGenerated_" + Guid.NewGuid().ToString("N"),
            trees,
            referencePaths.Where(File.Exists).Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Annotations,
                specificDiagnosticOptions: obsoleteUseIsError
                    ? new Dictionary<string, ReportDiagnostic> { ["CS0618"] = ReportDiagnostic.Error }
                    : null));

        using MemoryStream stream = new MemoryStream();
        EmitResult result = compilation.Emit(stream);
        result.Diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .Take(10)
            .ShouldBeEmpty();

        return Assembly.Load(stream.ToArray());
    }

    private static void Install(Type owner, string memberName, Type source, string methodName)
    {
        MethodInfo method = source.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!;
        FieldInfo? field = owner.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
        if (field != null)
        {
            field.SetValue(null, Delegate.CreateDelegate(field.FieldType, method));
            return;
        }
        PropertyInfo property = owner.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static)!;
        property.SetValue(null, Delegate.CreateDelegate(property.PropertyType, method));
    }

    private static bool IsManagedAssembly(string path)
    {
        try
        {
            AssemblyName.GetAssemblyName(path);
            return true;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    private class SilentLogger : ICodeGenLogger
    {
        public void PrintOutput(string message) { }

        public void PrintError(string message) { }
    }
}
