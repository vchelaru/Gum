using System;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Wireframe;

public class RuntimeSnapshotSerializerProjectTests : BaseTestClass
{
    [Fact]
    public void Snapshot_ShouldWriteProjectThatLoadsWithoutErrors()
    {
        StandardElementsManager.Self.Initialize();

        // Build a small live tree: a panel containing a label.
        ContainerRuntime root = new();
        ContainerRuntime panel = new() { Name = "Panel" };
        TextRuntime label = new() { Name = "Label" };
        label.Text = "Hi";
        root.AddChild(panel);
        panel.AddChild(label);

        // Serialize the tree into a screen, then assemble a full project (standards populated so the
        // instances' BaseTypes resolve). Standards population is a composition-root concern, kept out
        // of the catalog-injected serializer.
        RuntimeSnapshotSerializer serializer = new(StandardElementsManager.Self.DefaultStates);
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot");

        GumProjectSave project = new();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = "Snapshot", ElementType = ElementType.Screen });

        string tempDirectory = Path.Combine(Path.GetTempPath(), "GumSnapshotTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(tempDirectory, ElementReference.ScreenSubfolder));
            Directory.CreateDirectory(Path.Combine(tempDirectory, ElementReference.StandardSubfolder));

            string gumxPath = Path.Combine(tempDirectory, "Snapshot." + GumProjectSave.ProjectExtension);
            project.Save(gumxPath, saveElements: true);

            // Reload via the same deserialization core (catches malformed XML + missing files).
            // Full Gum.ProjectServices/gumcli validation runs out-of-process: ProjectServices and
            // MonoGameGum both compile the Forms control types, so they can't be referenced together.
            GumProjectSave loaded = GumProjectSave.Load(gumxPath, out GumLoadResult loadResult);

            loaded.ShouldNotBeNull();
            loadResult.ErrorMessage.ShouldBeNullOrEmpty();
            loadResult.MissingFiles.ShouldBeEmpty();

            ScreenSave loadedScreen = loaded.Screens.First(s => s.Name == "Snapshot");
            loadedScreen.Instances.Select(i => i.Name).ShouldBe(new[] { "Panel", "Label" }, ignoreOrder: true);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}
