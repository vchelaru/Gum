using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.FontGeneration;
using ToolsUtilities;

namespace GumProjectFontGenerator;

static class RunResponseCodes
{
    public const int Success = 0;
    public const int UnknownFailure = 1;
    public const int NotWindows = 2;
    public const int GumProjectFileNotFound = 3;
}

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            string projectName = args[0];

            if (!File.Exists(projectName))
            {
                return RunResponseCodes.GumProjectFileNotFound;
            }

            StandardElementsManager.Self.Initialize();

            GumProjectSave gumProject = GumProjectSave.Load(projectName);

            gumProject.Initialize(
                // This could be using plugins that add things like Arc and other Skia elements
                tolerateMissingDefaultStates: true);
            ObjectFinder.Self.GumProjectSave = gumProject;

            string projectDirectory = FileManager.GetDirectory(projectName);
            FileManager.RelativeDirectory = projectDirectory;

            IFontFileGenerator fontFileGenerator = gumProject.FontGenerator == FontGeneratorType.KernSmith
                ? new KernSmithFileGenerator()
                : new BmFontExeFileGenerator();
            IHeadlessFontGenerationService fontGenerationService = new HeadlessFontGenerationService(fontFileGenerator);
            await fontGenerationService.CreateAllMissingFontFiles(gumProject, projectDirectory);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Unexpected error: {e}");
            return RunResponseCodes.UnknownFailure;
        }

        return RunResponseCodes.Success;
    }
}
