using Gum.DataTypes;
using Gum.Managers;
using ToolsUtilities;

namespace GumProjectFontGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var projectName =
                @"C:\Users\Owner\Documents\GitHub\Gum\Samples\MonoGameGumFromFile\MonoGameGumFromFile\Content\GumProject.gumx";
            //var relativeDirectory = FileManager.RelativeDirectory;
            var gumProject = GumProjectSave.Load(projectName);

            gumProject.Initialize();

            FontManager.Self.CreateAllMissingFontFiles(gumProject);


        }
    }
}
