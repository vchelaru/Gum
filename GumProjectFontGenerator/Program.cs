using Gum.DataTypes;
using Gum.Managers;
using Gum.Services.Fonts;
using ToolsUtilities;

namespace GumProjectFontGenerator;

static class RunResponseCodes
{
    public const int Success = 0;
    public const int UnknownFailure = 1;
    // Although this can't happen, we'll keep it here so it's the same 
    // as the Gum tool
    public const int XnaNotInstalled = 2;
    public const int GumProjectFileNotFound = 3;
}

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var projectName = args[0];
            if(!System.IO.File.Exists(projectName))
            {
                return RunResponseCodes.GumProjectFileNotFound;
            }

            StandardElementsManager.Self.Initialize();

            //var relativeDirectory = FileManager.RelativeDirectory;
            var gumProject = GumProjectSave.Load(projectName);

            gumProject.Initialize(
                // This could be using plugins that add things like Arc and other Skia elements
                tolerateMissingDefaultStates:true);
            ObjectFinder.Self.GumProjectSave = gumProject;
            FileManager.RelativeDirectory = FileManager.GetDirectory(projectName);

            FontManager fontManager = new FontManager();

            await fontManager.CreateAllMissingFontFiles(gumProject);
        }
        catch(Exception e)
        {
            System.Console.Error.WriteLine($"Unexpected error: {e}");
            return RunResponseCodes.UnknownFailure;
        }

        return 0;
    }
}
