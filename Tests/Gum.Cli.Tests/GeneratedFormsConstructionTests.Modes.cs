using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Gum.Cli.Tests;

// The same project built three ways: by reference (the game loads the .gumx), FullyInCode (the
// generated class builds everything) and FindByName (the generated class wraps what the .gumx built).
public partial class GeneratedFormsConstructionTests
{
    private const string HomeScreenName = "Screens/HomeScreen";

    private static void SetValue(ElementSave element, string name, string type, object value) =>
        element.DefaultState!.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });

    // A screen a beginner builds: a heading, a card component and a button, with sizes, text and color set.
    private static void AddHomeScreen(GumProjectSave project)
    {
        ComponentSave card = AddComponent(project, "Elements/Card", "Container", ("Title", "Text"));
        SetValue(card, "Width", "float", 120f);
        SetValue(card, "Title.Text", "string", "Card title");
        SetValue(card, "Title.Red", "int", 10);

        ScreenSave screen = AddScreen(project, HomeScreenName, null, ("Heading", "Text"));
        screen.Instances.Add(new InstanceSave { Name = "Card1", BaseType = "Elements/Card", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "StartButton", BaseType = "Controls/ButtonStandard", ParentContainer = screen });
        SetValue(screen, "Heading.Text", "string", "Welcome");
        SetValue(screen, "Heading.Red", "int", 200);
        SetValue(screen, "Heading.Width", "float", 140f);
        SetValue(screen, "Card1.X", "float", 12f);
        SetValue(screen, "Card1.Width", "float", 130f);
        SetValue(screen, "StartButton.Width", "float", 100f);
        SetValue(screen, "StartButton.Y", "float", 50f);
    }

    // Name, layout, text and color of every instance, down the hierarchy.
    private static string DescribeTree(global::Gum.Wireframe.GraphicalUiElement visual, int depth = 0)
    {
        StringBuilder text = new StringBuilder();
        // The root of a loaded element carries the element name; a generated root has none.
        text.Append(' ', depth * 2).Append(depth == 0 ? "(root)" : visual.Name).Append(": ")
            .Append(visual.X).Append(',').Append(visual.Y).Append(' ').Append(visual.Width).Append('x').Append(visual.Height);
        foreach (string property in new[] { "Text", "Red", "Green", "Blue" })
        {
            PropertyInfo? info = visual.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
            if (info != null && info.DeclaringType != typeof(global::Gum.Wireframe.GraphicalUiElement))
            {
                text.Append(' ').Append(property).Append('=').Append(info.GetValue(visual));
            }
        }
        text.AppendLine();
        foreach (global::Gum.Wireframe.GraphicalUiElement child in visual.Children.Cast<global::Gum.Wireframe.GraphicalUiElement>())
        {
            text.Append(DescribeTree(child, depth + 1));
        }
        return text.ToString();
    }

    private static global::Gum.Wireframe.GraphicalUiElement BuildByReference(string elementName)
    {
        ElementSave element = Gum.Managers.ObjectFinder.Self.GetElementSave(elementName)!;
        object systemManagers = MonoGameGum.GetType("RenderingLibrary.SystemManagers", throwOnError: true)!
            .GetProperty("Default")!.GetValue(null)!;
        return global::GumRuntime.ElementSaveExtensions.ToGraphicalUiElement(
            element, (global::RenderingLibrary.ISystemManagers)systemManagers, addToManagers: false);
    }

    // The hand-written half a beginner adds: a click handler and a binding on generated instances.
    private void WriteHandWrittenHalf()
    {
        string custom = Directory.GetFiles(TempDirectory, "HomeScreen.cs", SearchOption.AllDirectories).Single();
        File.WriteAllText(custom,
            "namespace MyGame.Screens.Screens;\n" +
            "public class HomeVm { public string Label { get; set; } = \"\"; }\n" +
            "partial class HomeScreen\n{\n" +
            "    public int Clicks { get; set; }\n" +
            "    partial void CustomInitialize()\n    {\n" +
            "        StartButton.Click += (sender, args) => Clicks++;\n" +
            "        StartButton.SetBinding(nameof(StartButton.Text), nameof(HomeVm.Label));\n" +
            "    }\n}\n");
    }

    private static Type TypeNamed(Assembly generated, string name) => generated.GetTypes().Single(item => item.Name == name);

    private static void ShouldHandleClicksAndBindingContext(object screen, Assembly generated)
    {
        object button = Get(screen, "StartButton")!;
        button.GetType().GetMethod("PerformClick")!.Invoke(button, new object?[] { null });
        Get(screen, "Clicks").ShouldBe(1, "the hand-written Click handler on the generated button");

        object viewModel = Activator.CreateInstance(TypeNamed(generated, "HomeVm"))!;
        viewModel.GetType().GetProperty("Label")!.SetValue(viewModel, "Start game");
        screen.GetType().GetProperty("BindingContext")!.SetValue(screen, viewModel);
        Get(button, "Text").ShouldBe("Start game", "the binding the hand-written half set on the generated button");
    }

    [Fact]
    public void FullyInCode_BuildsTheTreeTheProjectDescribes_AndTheHandWrittenHalfWorks()
    {
        Assembly generated = GenerateAndCompile(editProject: AddHomeScreen, afterCodegen: WriteHandWrittenHalf);
        object screen = Construct(generated, "HomeScreen");
        string fullyInCode = DescribeTree((global::Gum.Wireframe.GraphicalUiElement)Get(screen, "Visual")!);
        LoadProjectForFindByName();
        string byReference = DescribeTree(BuildByReference(HomeScreenName));

        fullyInCode.ShouldBe(byReference, "FullyInCode and loading the .gumx should show the same tree");
        byReference.ShouldContain("Heading: 0,0 140x");
        byReference.ShouldContain("Text=Welcome Red=200");
        byReference.ShouldContain("Card1: 12,0 130x");
        byReference.ShouldContain("Text=Card title Red=10");
        byReference.ShouldContain("StartButton: 0,50 100x");
        ShouldHandleClicksAndBindingContext(screen, generated);
    }

    [Fact]
    public void FindByName_WrapsTheTreeTheProjectBuilt_AndTheHandWrittenHalfWorks()
    {
        Assembly generated = GenerateAndCompile(
            OutputLibrary.GumForms, ObjectInstantiationType.FindByName, editProject: AddHomeScreen, afterCodegen: WriteHandWrittenHalf);
        // The generated type registers itself from its module initializer, which runs on first use.
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(generated.ManifestModule.ModuleHandle);
        LoadProjectForFindByName();

        global::Gum.Wireframe.GraphicalUiElement visual = BuildByReference(HomeScreenName);
        string before = DescribeTree(visual);
        // The generated screen registered itself, so the loaded visual already has it as its Forms control.
        object screen = Get(visual, "FormsControlAsObject")!;
        screen.GetType().ShouldBe(TypeNamed(generated, "HomeScreen"));

        Get(screen, "Heading").ShouldBeSameAs(visual.GetGraphicalUiElementByName("Heading"));
        Get(screen, "Card1.Visual").ShouldBeSameAs(visual.GetGraphicalUiElementByName("Card1"));
        Get(screen, "StartButton.Visual").ShouldBeSameAs(visual.GetGraphicalUiElementByName("StartButton"));
        DescribeTree(visual).ShouldBe(before, "wrapping the tree should not change it");
        ShouldHandleClicksAndBindingContext(screen, generated);
    }
}
