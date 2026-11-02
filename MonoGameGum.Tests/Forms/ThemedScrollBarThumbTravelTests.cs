using GumRuntime;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Managers;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// In every shipped Add Forms theme (and the FormsTemplate standard theme) the vertical scroll bar's
/// thumb fills the track proportionally to the viewport, touches the track's top at the minimum and
/// its bottom at the maximum, and a drag travels the whole track (#7433).
/// </summary>
public class ThemedScrollBarThumbTravelTests : BaseTestClass
{
    private const float Tolerance = 0.51f;

    public static IEnumerable<object[]> Themes()
    {
        foreach (string theme in new[] { "FormsTemplate", "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95" })
        {
            yield return new object[] { theme };
        }
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ListBoxScrollBar_AtMinimum_ThumbShouldTouchTrackTop(string theme)
    {
        Bar bar = CreateListBoxBar(theme);

        bar.ScrollBar.Value = bar.ScrollBar.Minimum;
        bar.Visual.UpdateLayout();

        bar.Thumb.AbsoluteTop.ShouldBe(bar.Track.AbsoluteTop, Tolerance);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ListBoxScrollBar_AtMaximum_ThumbShouldTouchTrackBottom(string theme)
    {
        Bar bar = CreateListBoxBar(theme);

        bar.ScrollBar.Value = bar.ScrollBar.Maximum;
        bar.Visual.UpdateLayout();

        (bar.Thumb.AbsoluteTop + bar.Thumb.AbsoluteHeight).ShouldBe(bar.Track.AbsoluteTop + bar.Track.AbsoluteHeight, Tolerance);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ListBoxScrollBar_ThumbLength_ShouldBeProportionalToViewport(string theme)
    {
        Bar bar = CreateListBoxBar(theme);

        float expected = bar.Track.AbsoluteHeight * (float)(bar.ScrollBar.ViewportSize /
            (bar.ScrollBar.ViewportSize + bar.ScrollBar.Maximum - bar.ScrollBar.Minimum));
        bar.Thumb.AbsoluteHeight.ShouldBe(Math.Max(expected, bar.ScrollBar.MinimumThumbSize), Tolerance);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ListBoxScrollBar_DragThumbPastTrackEnds_ShouldTravelWholeTrack(string theme)
    {
        Bar bar = CreateListBoxBar(theme);
        float grabY = bar.Thumb.AbsoluteTop + bar.Thumb.AbsoluteHeight / 2;

        Drag(bar, grabY, bar.Track.AbsoluteTop + bar.Track.AbsoluteHeight + 500);
        bar.ScrollBar.Value.ShouldBe(bar.ScrollBar.Maximum, 0.5);
        (bar.Thumb.AbsoluteTop + bar.Thumb.AbsoluteHeight).ShouldBe(bar.Track.AbsoluteTop + bar.Track.AbsoluteHeight, Tolerance);

        grabY = bar.Thumb.AbsoluteTop + bar.Thumb.AbsoluteHeight / 2;
        Drag(bar, grabY, bar.Track.AbsoluteTop - 500);
        bar.ScrollBar.Value.ShouldBe(bar.ScrollBar.Minimum, 0.5);
        bar.Thumb.AbsoluteTop.ShouldBe(bar.Track.AbsoluteTop, Tolerance);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ListBoxScrollBar_ShouldSpanTheListBoxHeight(string theme)
    {
        Bar bar = CreateListBoxBar(theme);

        // The thumb can only reach the list's top and bottom if the bar runs the list's full height
        // (less a few pixels of inset), not a fixed-height bar floating in the middle (#7433).
        const float maxInset = 8;
        (bar.Track.AbsoluteTop - bar.ListBox.AbsoluteTop).ShouldBeLessThanOrEqualTo(maxInset + 24 /* up button */);
        bar.Track.AbsoluteHeight.ShouldBeGreaterThanOrEqualTo(bar.ListBox.AbsoluteHeight - 2 * maxInset - 48 /* both buttons */);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ListBoxScrollBar_ShouldNotBeVerticallyCenteredInAShortSpan(string theme)
    {
        Bar bar = CreateListBoxBar(theme);

        bar.Visual.AbsoluteHeight.ShouldBeGreaterThan(bar.ListBox.AbsoluteHeight * 0.8f);
    }

    private sealed record Bar(GraphicalUiElement ListBox, ScrollBar ScrollBar, GraphicalUiElement Visual, GraphicalUiElement Track, GraphicalUiElement Thumb);

    private static Bar CreateListBoxBar(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        ComponentSave listBoxSave = project.Components.Single(c => c.Name.EndsWith("Controls/ListBox"));
        GraphicalUiElement listBoxVisual = listBoxSave.ToGraphicalUiElement();
        ListBox listBox = (ListBox)((InteractiveGue)listBoxVisual).FormsControlAsObject;
        for (int i = 0; i < 100; i++)
        {
            listBox.Items.Add("Item " + i);
        }
        listBoxVisual.UpdateLayout();
        GraphicalUiElement barVisual = listBoxVisual.GetGraphicalUiElementByName("VerticalScrollBarInstance")!;
        ScrollBar scrollBar = (ScrollBar)((InteractiveGue)barVisual).FormsControlAsObject;
        barVisual.UpdateLayout();
        return new Bar(listBoxVisual, scrollBar, barVisual,
            barVisual.GetGraphicalUiElementByName("TrackInstance")!,
            barVisual.GetGraphicalUiElementByName("ThumbInstance")!);
    }

    private static void Drag(Bar bar, float grabY, float releaseY)
    {
        Mock<ICursor> cursor = new();
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(grabY);
        Gum.Forms.FormsUtilities.SetCursor(cursor.Object);
        Type type = typeof(ScrollBar);
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        type.GetMethod("HandleThumbPush", flags)!.Invoke(bar.ScrollBar, new object?[] { null, EventArgs.Empty });

        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(releaseY);
        type.GetMethod("UpdateThumbPositionToCursorDrag", flags, new[] { typeof(ICursor) })!
            .Invoke(bar.ScrollBar, new object?[] { cursor.Object });
        bar.Visual.UpdateLayout();
    }

    private static GumProjectSave LoadTheme(string theme)
    {
        string templates = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates");
        string gumx = theme == "FormsTemplate"
            ? Path.Combine(templates, "FormsTemplate", "GumProject.gumx")
            : Path.Combine(templates, "FormsThemes", theme, "GumProject.gumx");
        // SystemManagers registers these in a running game; without them "Container" loads as a plain
        // GraphicalUiElement and ScrollBar.Track (an InteractiveGue) would be null.
        StandardRuntimeRegistrations.Register();
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        return project;
    }

    private static string LocateRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GumFull.sln")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not locate GumFull.sln by walking up from {AppContext.BaseDirectory}.");
    }
}
