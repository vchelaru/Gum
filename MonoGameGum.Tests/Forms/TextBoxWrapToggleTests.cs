using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Managers;
using Gum.Wireframe;
using Text = RenderingLibrary.Graphics.Text;
using VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Toggling TextWrapping and AcceptsReturn at runtime, in any order and repeatedly, leaves a
/// TextBox laid out for its current configuration in every packaged theme loaded from a file:
/// no wrap means one visual line per paragraph that scrolls with the caret, wrap means the text
/// fits the box, and the selection and caret follow the lines (#7434).
/// </summary>
public class TextBoxWrapToggleTests : BaseTestClass
{
    #region Helpers

    private const string LongLine =
        "This little shack is where the Smith stays and I shouldn't go in if I'm not invited by him";
    private const string Paragraphs = "first line of text here\nsecond line of text here\nthird line of text here";

    private static readonly (TextWrapping Wrap, bool Accepts)[] Configurations =
    {
        (TextWrapping.Wrap, false),
        (TextWrapping.Wrap, true),
        (TextWrapping.NoWrap, false),
        (TextWrapping.NoWrap, true),
    };

    public static IEnumerable<object[]> Themes() =>
        new[] { "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95", "FormsTemplate" }
            .Select(item => new object[] { item });

    private static string LocateTemplates()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GumFull.sln")))
            {
                return Path.Combine(directory.FullName, "Tools", "Gum.ProjectServices", "Templates");
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not locate GumFull.sln by walking up from {AppContext.BaseDirectory}.");
    }

    private static (GraphicalUiElement Screen, TextBox Box) Load(string project)
    {
        string root = LocateTemplates();
        bool isTheme = project != "FormsTemplate";
        string gumx = isTheme
            ? Path.Combine(root, "FormsThemes", project, "GumProject.gumx")
            : Path.Combine(root, project, "GumProject.gumx");
        GumProjectSave gumProject = GumProjectSave.Load(gumx, out _) ?? throw new InvalidOperationException(gumx);
        gumProject.Initialize();
        ObjectFinder.Self.GumProjectSave = gumProject;
        string baseType = isTheme ? $"{project}/Controls/TextBox" : "Controls/TextBox";

        FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        ScreenSave screen = new ScreenSave { Name = "WrapToggleScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = baseType, ParentContainer = screen });
        gumProject.Screens.Add(screen);
        gumProject.Initialize();
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement()!;
        screenVisual.UpdateLayout();
        GraphicalUiElement boxVisual = screenVisual.GetGraphicalUiElementByName("Box")!;
        TextBox box = (TextBox)((InteractiveGue)boxVisual).FormsControlAsObject!;
        // Narrow and tall enough that the long line wraps and several lines fit.
        box.Width = 160;
        box.Height = 120;
        box.IsFocused = true;
        screenVisual.UpdateLayout();
        return (screenVisual, box);
    }

    private static List<GraphicalUiElement> SelectionInstances(TextBox box) =>
        (List<GraphicalUiElement>)typeof(TextBoxBase)
            .GetField("_selectionInstances", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(box)!;

    private static void Apply(TextBox box, (TextWrapping Wrap, bool Accepts) configuration)
    {
        box.TextWrapping = configuration.Wrap;
        box.AcceptsReturn = configuration.Accepts;
    }

    /// <summary>Everything that must hold for the box's current configuration; returns the first violation.</summary>
    private static string? Violation(GraphicalUiElement screen, TextBox box, bool checkAlignment = false)
    {
        (TextWrapping wrap, bool accepts) = (box.TextWrapping, box.AcceptsReturn);
        GraphicalUiElement visual = box.Visual;
        GraphicalUiElement textInstance = visual.GetGraphicalUiElementByName("TextInstance")!;
        Text core = (Text)textInstance.RenderableComponent!;
        bool singleLineMode = wrap == TextWrapping.NoWrap && !accepts;

        foreach (string text in accepts ? new[] { LongLine, Paragraphs } : new[] { LongLine })
        {
            box.Text = text;
            box.IsFocused = true;
            screen.UpdateLayout();
            box.CaretIndex = text.Length;
            screen.UpdateLayout();

            int paragraphs = text.Split('\n').Length;
            int lines = core.WrappedText.Count;

            DimensionUnitType expectedUnits = wrap == TextWrapping.Wrap
                ? DimensionUnitType.RelativeToParent
                : DimensionUnitType.RelativeToChildren;
            if (textInstance.WidthUnits != expectedUnits)
            {
                return $"[{wrap},{accepts}] text WidthUnits {textInstance.WidthUnits}, expected {expectedUnits}";
            }

            VerticalAlignment expectedAlignment = singleLineMode ? VerticalAlignment.Center : VerticalAlignment.Top;
            if (checkAlignment && core.VerticalAlignment != expectedAlignment)
            {
                return $"[{wrap},{accepts}] text VerticalAlignment {core.VerticalAlignment}, expected {expectedAlignment}";
            }

            if (wrap == TextWrapping.Wrap && text == LongLine && lines < 2)
            {
                return $"[{wrap},{accepts}] long line did not wrap ({lines} lines)";
            }
            if (wrap == TextWrapping.NoWrap)
            {
                if (lines != paragraphs)
                {
                    return $"[{wrap},{accepts}] expected {paragraphs} lines, got {lines} for \"{text.Substring(0, 10)}...\"";
                }
                if (text == LongLine && textInstance.GetAbsoluteWidth() <= visual.GetAbsoluteWidth())
                {
                    return $"[{wrap},{accepts}] long no-wrap text ({textInstance.GetAbsoluteWidth()}) not wider than the box ({visual.GetAbsoluteWidth()})";
                }
            }

            // The caret (at the end of the text) stays inside the box, scrolling the text if needed.
            GraphicalUiElement caret = visual.GetGraphicalUiElementByName("CaretInstance")!;
            float boxLeft = visual.AbsoluteLeft;
            float boxRight = boxLeft + visual.GetAbsoluteWidth();
            float caretX = caret.AbsoluteLeft;
            if (caretX < boxLeft - 1 || caretX > boxRight + 1)
            {
                return $"[{wrap},{accepts}] caret x {caretX} outside the box {boxLeft}..{boxRight}";
            }
            float boxTop = visual.AbsoluteTop;
            float boxBottom = boxTop + visual.GetAbsoluteHeight();
            float caretY = caret.AbsoluteTop;
            if (caretY < boxTop - 6 || caretY > boxBottom)
            {
                return $"[{wrap},{accepts}] caret y {caretY} outside the box {boxTop}..{boxBottom}";
            }

            // Select-all highlights every line, each rectangle on its own line.
            box.SelectAll();
            screen.UpdateLayout();
            List<GraphicalUiElement> shown = SelectionInstances(box).Where(item => item.Visible).ToList();
            if (shown.Count != lines)
            {
                return $"[{wrap},{accepts}] {shown.Count} selection rectangles for {lines} lines";
            }
            if (shown.Any(item => item.GetAbsoluteHeight() <= 0))
            {
                return $"[{wrap},{accepts}] a selection rectangle has no height";
            }
            if (shown.Select(item => (int)item.AbsoluteTop).Distinct().Count() != shown.Count)
            {
                return $"[{wrap},{accepts}] selection rectangles share a top: {string.Join(",", shown.Select(item => item.AbsoluteTop))}";
            }
            float lineHeight = core.BitmapFont?.LineHeightInPixels ?? 0;
            if (lines > 1 && lineHeight > 0)
            {
                float step = shown[1].AbsoluteTop - shown[0].AbsoluteTop;
                if (Math.Abs(step - lineHeight) > 2)
                {
                    return $"[{wrap},{accepts}] selection rectangles step {step} but the line height is {lineHeight}";
                }
            }
            box.SelectionLength = 0;
        }
        return null;
    }

    #endregion

    [Theory]
    [MemberData(nameof(Themes))]
    public void ThemedTextBox_ToggledAtRuntime_EveryConfigurationIsLaidOutForItself(string project)
    {
        (GraphicalUiElement screen, TextBox box) = Load(project);
        List<string> problems = new();

        foreach ((TextWrapping Wrap, bool Accepts) first in Configurations)
        {
            foreach ((TextWrapping Wrap, bool Accepts) second in Configurations)
            {
                // A -> B -> A, which also covers every order of changing the two properties.
                foreach (var step in new[] { first, second, first })
                {
                    Apply(box, step);
                    string? violation = Violation(screen, box);
                    if (violation != null)
                    {
                        problems.Add($"{first} -> {second}: {violation}");
                    }
                }
            }
        }

        problems.Distinct().ShouldBeEmpty(string.Join("\n", problems.Distinct().Take(12)));
    }

    [Fact]
    public void CodeOnlyTextBox_ToggledAtRuntime_EveryConfigurationIsLaidOutForItself()
    {
        TextBox box = new();
        box.Width = 160;
        box.Height = 120;
        box.AddToRoot();
        GraphicalUiElement screen = box.Visual;
        List<string> problems = new();

        foreach ((TextWrapping Wrap, bool Accepts) first in Configurations)
        {
            foreach ((TextWrapping Wrap, bool Accepts) second in Configurations)
            {
                foreach (var step in new[] { first, second, first })
                {
                    Apply(box, step);
                    string? violation = Violation(screen, box, checkAlignment: true);
                    if (violation != null)
                    {
                        problems.Add($"{first} -> {second}: {violation}");
                    }
                }
            }
        }

        problems.Distinct().ShouldBeEmpty(string.Join("\n", problems.Distinct().Take(12)));
    }
}
