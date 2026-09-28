using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.Migration;

/// <summary>
/// A project saved by an older tool opens in this head through the real load path (repair passes,
/// plugin ProjectLoad, the re-save on load) and keeps its content and its version.
/// </summary>
public class OldProjectLoadTests : IDisposable
{
    private readonly string _folder;
    private readonly string? _originalOverride;

    public OldProjectLoadTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "GumAvaloniaTests", "OldProject", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_folder, "Components"));
        // Loading adds the project to the recent list in GeneralSettings.xml.
        _originalOverride = FileManager.UserApplicationDataFolderOverride;
        FileManager.UserApplicationDataFolderOverride = Path.Combine(_folder, "UserData");
    }

    public void Dispose()
    {
        FileManager.UserApplicationDataFolderOverride = _originalOverride;
        TestAppBuilder.Services.GetRequiredService<IProjectManager>().CreateNewProject();
    }

    [AvaloniaFact]
    public void Version1Project_LoadsAndResavesWithoutLosingValues()
    {
        // The verbose (pre-attribute) format, with an instance of the deprecated ColoredRectangle.
        string gumx = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Version>1</Version>
              <DefaultCanvasWidth>1024</DefaultCanvasWidth>
              <DefaultCanvasHeight>768</DefaultCanvasHeight>
              <ComponentReference>
                <Name>Panel</Name>
                <ElementType>Component</ElementType>
                <LinkType>ReferenceOriginal</LinkType>
              </ComponentReference>
            </GumProjectSave>
            """;
        string panel = """
            <?xml version="1.0" encoding="utf-8"?>
            <ComponentSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Name>Panel</Name>
              <BaseType>Container</BaseType>
              <State>
                <Name>Default</Name>
                <Variable>
                  <Type>float</Type>
                  <Name>Width</Name>
                  <Value xsi:type="xsd:float">150</Value>
                  <SetsValue>true</SetsValue>
                </Variable>
                <Variable>
                  <Type>int</Type>
                  <Name>Background.Red</Name>
                  <Value xsi:type="xsd:int">12</Value>
                  <SetsValue>true</SetsValue>
                </Variable>
              </State>
              <Instance>
                <Name>Background</Name>
                <BaseType>ColoredRectangle</BaseType>
              </Instance>
            </ComponentSave>
            """;
        string gumxPath = Path.Combine(_folder, "Old.gumx");
        File.WriteAllText(gumxPath, gumx);
        File.WriteAllText(Path.Combine(_folder, "Components", "Panel.gucx"), panel);
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();

        Task load = projectManager.LoadProjectAsync(new FilePath(gumxPath));
        while (!load.IsCompleted)
        {
            Thread.Sleep(10);
            Dispatcher.UIThread.RunJobs();
        }
        load.GetAwaiter().GetResult();

        projectManager.HaveErrorsOccurredLoadingProject.ShouldBeFalse();
        GumProjectSave loaded = projectManager.GumProjectSave!;
        loaded.Version.ShouldBe(1);
        AssertPanelValues(loaded);

        // What is on disk now, after any re-save on load.
        GumProjectSave reread = GumProjectSave.Load(gumxPath, out GumLoadResult result)!;
        result.ErrorMessage.ShouldBeNullOrEmpty();
        reread.Version.ShouldBe(1);
        AssertPanelValues(reread);
    }

    private static void AssertPanelValues(GumProjectSave project)
    {
        ComponentSave panel = project.Components.Single(component => component.Name == "Panel");
        StateSave state = panel.DefaultState!;
        state.GetValue("Width").ShouldBe(150f);
        state.GetValue("Background.Red").ShouldBe(12);
        panel.Instances.Single().BaseType.ShouldBe("ColoredRectangle");
    }
}
