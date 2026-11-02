using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A focused CheckBox or RadioButton draws its focus ring inside the control's own bounds and centered
/// on the glyph (the box or circle) within 1 px, in every packaged theme (#7459). A ring that sticks out
/// of the control is cut off by any clipping parent, and one that is off-center looks lopsided.
/// </summary>
public class ThemeFocusIndicatorTests : BeginnerDriverTestBase
{
    private static readonly string[] GlyphNames =
    {
        "BoxBorder", "RadioBackground", "CheckBoxBackground", "OuterCircle",
    };

    public ThemeFocusIndicatorTests()
    {
        // BaseTestClass.Dispose resets the registry, so a later test would find the shape defaults gone.
        Gum.Renderables.DefaultStrokedCircleRenderable.RegisterRuntimeTypes();
        Gum.Renderables.DefaultFilledRectangleRenderable.RegisterRuntimeTypes();
        Gum.Renderables.DefaultStrokedRectangleRenderable.RegisterRuntimeTypes();
    }

    public static IEnumerable<object[]> Themes() =>
        new[] { "", "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95" }
            .Select(theme => new object[] { theme });

    [Theory]
    [MemberData(nameof(Themes))]
    public void FocusedIndicator_CheckBoxAndRadioButton_InsideControlAndCenteredOnGlyph(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        List<string> failures = new List<string>();
        StringBuilder log = new StringBuilder();

        foreach (string suffix in new[] { "Controls/CheckBox", "Controls/RadioButton" })
        {
            string componentName = project.Components.Single(item => item.Name.EndsWith(suffix)).Name;
            GraphicalUiElement control = PlaceAlone(project, componentName, out ScreenSave screen, out GraphicalUiElement screenVisual);
            ((FrameworkElement)((InteractiveGue)control).FormsControlAsObject!).IsFocused = true;
            Driver.Step(2);

            GraphicalUiElement? indicator = control.GetGraphicalUiElementByName("FocusedIndicator") ?? control.GetGraphicalUiElementByName("FocusRing") ?? control.GetGraphicalUiElementByName("FocusRectInstance");
            GraphicalUiElement? glyph = GlyphNames
                .Select(name => control.GetGraphicalUiElementByName(name))
                .FirstOrDefault(item => item != null);
            string scene = $"{theme} {suffix}";

            if (indicator == null || (glyph == null && indicator.Name != "FocusRectInstance"))
            {
                failures.Add($"{scene}: indicator {(indicator == null ? "missing" : "found")}, glyph {(glyph == null ? "missing" : "found")}");
            }
            else
            {
                glyph ??= control; // Retro95 CheckBox has no glyph shape to center on, only the label rectangle
                float indLeft = indicator.AbsoluteLeft, indTop = indicator.AbsoluteTop;
                float indRight = indLeft + indicator.AbsoluteWidth, indBottom = indTop + indicator.AbsoluteHeight;
                float ctlLeft = control.AbsoluteLeft, ctlTop = control.AbsoluteTop;
                float ctlRight = ctlLeft + control.AbsoluteWidth, ctlBottom = ctlTop + control.AbsoluteHeight;
                float indCenterX = (indLeft + indRight) / 2, indCenterY = (indTop + indBottom) / 2;
                float glyphCenterX = glyph.AbsoluteLeft + glyph.AbsoluteWidth / 2;
                float glyphCenterY = glyph.AbsoluteTop + glyph.AbsoluteHeight / 2;

                string numbers = $"{scene}: control x {ctlLeft}..{ctlRight} y {ctlTop}..{ctlBottom}; " +
                    $"indicator ({indicator.Name}) x {indLeft}..{indRight} y {indTop}..{indBottom}; " +
                    $"glyph ({glyph.Name}) x {glyph.AbsoluteLeft}..{glyph.AbsoluteLeft + glyph.AbsoluteWidth} " +
                    $"y {glyph.AbsoluteTop}..{glyph.AbsoluteTop + glyph.AbsoluteHeight}";
                log.AppendLine(numbers);

                bool isFocusRect = indicator.Name == "FocusRectInstance"; // Retro95: a dotted rectangle around the label
                bool isRing = indicator.AbsoluteHeight > 4 && !isFocusRect; // Editor's focus mark is a 2 px underline
                bool inside = indLeft >= ctlLeft && indTop >= ctlTop && indRight <= ctlRight && indBottom <= ctlBottom;
                bool centered = Math.Abs(indCenterX - glyphCenterX) <= 0.5f && Math.Abs(indCenterY - glyphCenterY) <= 0.5f;
                bool mustBeInside = (isRing && theme == "DarkPro") || isFocusRect; // other themes draw a halo that hugs the glyph and reaches past the control by design
                if ((mustBeInside && !inside) || (isRing && !centered))
                {
                    failures.Add(numbers + $" => inside={inside} centered={centered}");
                }
            }

            screenVisual.RemoveFromRoot();
            project.Screens.Remove(screen);
        }

        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures) + Environment.NewLine + "ALL:" + Environment.NewLine + log);
    }

    private GraphicalUiElement PlaceAlone(GumProjectSave project, string componentName, out ScreenSave screen, out GraphicalUiElement screenVisual)
    {
        screen = new ScreenSave { Name = "AloneScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = componentName, ParentContainer = screen });
        project.Screens.Add(screen);
        project.Initialize();
        screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);
        return screenVisual.GetGraphicalUiElementByName("Box")!;
    }

    private static GumProjectSave LoadTheme(string theme)
    {
        string themeFolder = theme.Length == 0 ? "FormsTemplate" : Path.Combine("FormsThemes", theme);
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", themeFolder, "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        // A running game gets this registration from GumService.Initialize.
        ElementSaveExtensions.RegisterGueInstantiationType("Container", typeof(Gum.GueDeriving.ContainerRuntime));
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        return project;
    }

    private static string LocateRepoRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory != null && !File.Exists(Path.Combine(directory, "GumFull.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }
        return directory ?? throw new InvalidOperationException("Repo root not found.");
    }
}
