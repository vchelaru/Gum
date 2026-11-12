using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using RenderingLibrary;
using Shouldly;
using System.Text;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

// A game process can run the project checker or headless codegen on another project while a GumService has
// its own project loaded. Neither may show that service the other project, during the run or after it.
// The services here are stand-ins that own a real GumContext, as in MonoGameGum.Tests' GumContextTests:
// ObjectFinder.Self and FileManager.RelativeDirectory resolve through the context, so no graphics device is needed.
public class LiveServiceIsolationTests : BaseTestClass
{
    private readonly List<GumContext> _contexts = new List<GumContext>();
    private readonly IGumService? _originalDefault = IGumService.Default;
    private readonly string _tempDirectory;

    public LiveServiceIsolationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumLiveServiceIsolation_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public override void Dispose()
    {
        IGumService.Default = _originalDefault;
        foreach (GumContext context in _contexts)
        {
            context.Release();
        }
        base.Dispose();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetAllErrors_ShouldLeaveTheLiveServicesProjectInPlace_DuringAndAfterTheCheck(bool isSecondService)
    {
        GumProjectSave projectA = CreateProjectA("AButton");
        string relativeDirectoryA = Path.Combine(_tempDirectory, "A") + Path.DirectorySeparatorChar;
        MakeServiceWithProject(isSecondService, projectA, relativeDirectoryA);
        string gumxB = CreateProjectBOnDisk("BPanel", instanceBaseType: "NoSuchType");

        ProjectLoadResult loadResult = new ProjectLoader().Load(gumxB);
        loadResult.Success.ShouldBeTrue(loadResult.ErrorMessage);
        ServiceViewProbe probe = new ServiceViewProbe();
        HeadlessErrorChecker checker = new HeadlessErrorChecker(new Mock<ITypeResolver>().Object, new[] { probe });

        IReadOnlyList<ErrorResult> errors = checker.GetAllErrors(loadResult.Project!);

        errors.ShouldContain(error => error.ElementName == "BPanel");
        errors.ShouldNotContain(error => error.ElementName == "AButton");
        probe.ProjectSeenByService.ShouldBeSameAs(projectA, "the service saw project B while the check ran");
        ObjectFinder.Self.GumProjectSave.ShouldBeSameAs(projectA);
        ObjectFinder.Self.GetElementSave("AButton").ShouldNotBeNull();
        ObjectFinder.Self.GetElementSave("BPanel").ShouldBeNull();
        FileManager.RelativeDirectory.ShouldBe(relativeDirectoryA);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Run_ShouldLeaveTheLiveServicesProjectInPlace_DuringAndAfterCodegen(bool isSecondService)
    {
        GumProjectSave projectA = CreateProjectA("AButton");
        string relativeDirectoryA = Path.Combine(_tempDirectory, "A") + Path.DirectorySeparatorChar;
        MakeServiceWithProject(isSecondService, projectA, relativeDirectoryA);
        string gumxB = CreateProjectBOnDisk("BPanel", instanceBaseType: null);
        File.WriteAllText(Path.Combine(_tempDirectory, "B", "GameB.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
        ProbingWriter output = new ProbingWriter();
        StringWriter error = new StringWriter();

        int exitCode = new HeadlessCodegenRunner().Run(gumxB, elementNames: null, prune: false, output, error);

        exitCode.ShouldBe(0, error.ToString());
        Directory.GetFiles(Path.Combine(_tempDirectory, "B"), "BPanel.Generated.cs", SearchOption.AllDirectories).ShouldNotBeEmpty();
        Directory.GetFiles(Path.Combine(_tempDirectory, "B"), "AButton.Generated.cs", SearchOption.AllDirectories).ShouldBeEmpty();
        output.ProbedDuringRun.ShouldBeTrue(output.ToString());
        output.ProjectSeenByService.ShouldBeSameAs(projectA, "the service saw project B while codegen ran");
        ObjectFinder.Self.GumProjectSave.ShouldBeSameAs(projectA);
        ObjectFinder.Self.GetElementSave("AButton").ShouldNotBeNull();
        FileManager.RelativeDirectory.ShouldBe(relativeDirectoryA);
    }

    // The live service is the default service (first live, uses the process-wide statics) or a second one
    // (its own ObjectFinder and relative directory), made current the way GumService.Update makes it.
    private void MakeServiceWithProject(bool isSecondService, GumProjectSave project, string relativeDirectory)
    {
        GumContext first = CreateLiveContext();
        GumContext service = isSecondService ? CreateLiveContext() : first;
        IGumService.Default = service.Service;
        service.AdoptsProcessStatics.ShouldBe(!isSecondService);

        ObjectFinder.Self.GumProjectSave = project;
        FileManager.RelativeDirectory = relativeDirectory;
    }

    private GumContext CreateLiveContext()
    {
        GumContext context = new GumContext();
        context.ServiceTypeName = "TestService";
        Mock<IGumService> service = new Mock<IGumService>();
        service.SetupGet(item => item.Context).Returns(context);
        context.Service = service.Object;
        context.Register();
        _contexts.Add(context);
        return context;
    }

    private GumProjectSave CreateProjectA(string componentName)
    {
        ComponentSave component = new ComponentSave { Name = componentName, BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);
        Project.Components.Add(component);
        return Project;
    }

    private string CreateProjectBOnDisk(string componentName, string? instanceBaseType)
    {
        string directory = Path.Combine(_tempDirectory, "B", "Content", "GumProject");
        Directory.CreateDirectory(directory);
        string gumxPath = Path.Combine(directory, "ProjectB.gumx");
        new ProjectCreator().Create(gumxPath);

        GumProjectSave project = GumProjectSave.Load(gumxPath)!;
        ComponentSave component = new ComponentSave { Name = componentName, BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);
        if (instanceBaseType != null)
        {
            component.Instances.Add(new InstanceSave { Name = "Child", BaseType = instanceBaseType, ParentContainer = component });
        }
        project.Components.Add(component);
        project.ComponentReferences.Add(new ElementReference { Name = componentName, ElementType = ElementType.Component });
        project.Save(gumxPath, saveElements: true);
        return gumxPath;
    }

    // What the live service sees, read from another thread so a lookup override on the checking thread
    // does not hide what every other thread (the game loop) reads.
    private static GumProjectSave? ReadServiceProjectFromAnotherThread() =>
        Task.Run(() => ObjectFinder.Self.GumProjectSave).GetAwaiter().GetResult();

    private sealed class ServiceViewProbe : IAdditionalErrorSource
    {
        public GumProjectSave? ProjectSeenByService { get; private set; }

        public IEnumerable<ErrorResult> GetErrors(ElementSave element, GumProjectSave project)
        {
            if (element.Name == "BPanel")
            {
                ProjectSeenByService = ReadServiceProjectFromAnotherThread();
            }
            return Array.Empty<ErrorResult>();
        }
    }

    // Codegen prints "Generating code into ..." after it has loaded project B and before it generates.
    private sealed class ProbingWriter : StringWriter
    {
        public bool ProbedDuringRun { get; private set; }
        public GumProjectSave? ProjectSeenByService { get; private set; }

        public override void WriteLine(string? value)
        {
            if (!ProbedDuringRun && value != null && value.StartsWith("Generating code into", StringComparison.Ordinal))
            {
                ProbedDuringRun = true;
                ProjectSeenByService = ReadServiceProjectFromAnotherThread();
            }
            base.WriteLine(value);
        }
    }
}
