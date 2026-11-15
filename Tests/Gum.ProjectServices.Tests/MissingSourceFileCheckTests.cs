using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// GUM0006 reports a referenced file that is not on disk, whatever the file's format.
/// </summary>
public class MissingSourceFileCheckTests : BaseTestClass
{
    private readonly HeadlessErrorChecker _sut = new HeadlessErrorChecker(new Mock<ITypeResolver>().Object);

    private IReadOnlyList<ErrorResult> Check(string sourceFile, bool createFile)
    {
        string directory = Path.Combine(Path.GetTempPath(), "GumMissingSource_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            Project.FullFileName = Path.Combine(directory, "Project.gumx");
            if (createFile)
            {
                File.WriteAllText(Path.Combine(directory, sourceFile), string.Empty);
            }
            ComponentSave component = new ComponentSave { Name = "Holder" };
            component.Instances.Add(new InstanceSave { Name = "MySprite", BaseType = "Sprite" });
            StateSave state = new StateSave { Name = "Default", ParentContainer = component };
            state.Variables.Add(new VariableSave { Name = "MySprite.SourceFile", Value = sourceFile, Type = "string", IsFile = true });
            component.States.Add(state);
            Project.Components.Add(component);

            return _sut.GetErrorsFor(component, Project);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData("pic.fake")]
    [InlineData("pic.psd")]
    [InlineData("pic.PNG")]
    public void GetErrorsFor_ShouldNotReportGum0006_WhenReferencedFileExists(string sourceFile)
    {
        Check(sourceFile, createFile: true).ShouldNotContain(e => e.Code == "GUM0006");
    }

    [Theory]
    [InlineData("pic.fake")]
    [InlineData("pic.psd")]
    [InlineData("pic.png")]
    public void GetErrorsFor_ShouldReportGum0006_WhenReferencedFileIsMissing(string sourceFile)
    {
        Check(sourceFile, createFile: false).ShouldContain(e => e.Code == "GUM0006");
    }
}
