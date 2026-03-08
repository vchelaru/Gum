using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.FontGeneration;
using System.Runtime.InteropServices;
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
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.Error.WriteLine("Font generation requires Windows (bmfont.exe is a Windows-only application).");
            return RunResponseCodes.NotWindows;
        }

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

            IHeadlessFontGenerationService fontGenerationService = new HeadlessFontGenerationService();
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
