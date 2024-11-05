using Gum.DataTypes;
using Gum.Managers;
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
    static int Main(string[] args)
    {
        try
        {
            //System.Threading.Thread.Sleep(25_000);
            var projectName = args[0];

            if(!System.IO.File.Exists(projectName))
            {
                return RunResponseCodes.GumProjectFileNotFound;
            }

            StandardElementsManager.Self.Initialize();

            //var relativeDirectory = FileManager.RelativeDirectory;
            var gumProject = GumProjectSave.Load(projectName);

            gumProject.Initialize();
            ObjectFinder.Self.GumProjectSave = gumProject;
            FileManager.RelativeDirectory = FileManager.GetDirectory(projectName);

            FontManager.Self.CreateAllMissingFontFiles(gumProject);
        }
        catch(Exception e)
        {
            System.Console.Error.WriteLine($"Unexpected error: {e}");
            return RunResponseCodes.UnknownFailure;
        }

        return 0;
    }
}
