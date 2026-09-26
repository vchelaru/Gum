using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.ScrollBarPlugin;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.EditorTab;

public class ScrollbarServiceTests
{
    [Fact]
    public void HandleElementSelected_NoProjectLoaded_DoesNotThrow()
    {
        Mock<IProjectManager> projectManager = new Mock<IProjectManager>();
        projectManager.SetupGet(item => item.GumProjectSave).Returns((GumProjectSave?)null);
        ScrollbarService scrollbarService = new ScrollbarService(
            Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>(), projectManager.Object);

        Should.NotThrow(() => scrollbarService.HandleElementSelected(null));
    }
}
