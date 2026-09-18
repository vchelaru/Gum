using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using System;
using System.Drawing;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Covers <see cref="EditorRenderableFactory"/> (issue #4849): the editor's Container outline is the
/// runtime's fallback <see cref="LineRectangle"/> made dotted and painted the project's outline
/// color. The dotting used to live under <c>#if GUM</c> in the runtime-shared factory and silently
/// stopped compiling into the tool when the editor tab stopped linking its own copy of that file.
/// </summary>
public class EditorRenderableFactoryTests : IDisposable
{
    public EditorRenderableFactoryTests()
    {
        GraphicalUiElement.ShowLineRectangles = true;
    }

    public void Dispose()
    {
        GraphicalUiElement.ShowLineRectangles = false;
    }

    [Fact]
    public void CreateRenderableForType_Container_ReturnsDottedLineRectangleInProjectOutlineColor()
    {
        EditorRenderableFactory factory = new EditorRenderableFactory(ProjectStateWithOutlineColor(r: 10, g: 20, b: 30));

        IRenderableIpso? result = factory.CreateRenderableForType("Container", null);

        LineRectangle outline = result.ShouldBeOfType<LineRectangle>();
        outline.IsDotted.ShouldBeTrue();
        outline.Color.ToArgb().ShouldBe(Color.FromArgb(255, 10, 20, 30).ToArgb());
    }

    [Fact]
    public void CreateRenderableForType_Container_WhenOutlinesAreHidden_ReturnsInvisibleRenderable()
    {
        GraphicalUiElement.ShowLineRectangles = false;
        EditorRenderableFactory factory = new EditorRenderableFactory(ProjectStateWithOutlineColor(r: 10, g: 20, b: 30));

        IRenderableIpso? result = factory.CreateRenderableForType("Container", null);

        result.ShouldBeOfType<InvisibleRenderable>();
    }

    private static IProjectState ProjectStateWithOutlineColor(byte r, byte g, byte b)
    {
        Mock<IProjectState> projectState = new Mock<IProjectState>();
        projectState.SetupGet(x => x.OutlineColorR).Returns(r);
        projectState.SetupGet(x => x.OutlineColorG).Returns(g);
        projectState.SetupGet(x => x.OutlineColorB).Returns(b);
        return projectState.Object;
    }
}
