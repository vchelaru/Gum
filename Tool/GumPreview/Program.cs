using System;
using GumPreview;

string? gumxPath = null;
string? elementName = null;
string? selectionFilePath = null;
string? contentRootDirectory = null;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--project" && i + 1 < args.Length)
    {
        gumxPath = args[++i];
    }
    else if (args[i] == "--element" && i + 1 < args.Length)
    {
        elementName = args[++i];
    }
    else if (args[i] == "--selection-file" && i + 1 < args.Length)
    {
        selectionFilePath = args[++i];
    }
    else if (args[i] == "--content-root" && i + 1 < args.Length)
    {
        contentRootDirectory = args[++i];
    }
}

if (string.IsNullOrEmpty(gumxPath) || string.IsNullOrEmpty(elementName))
{
    Console.Error.WriteLine("Usage: GumPreview --project <path to .gumx/.gumj> --element <ScreenOrComponentName> [--selection-file <path>] [--content-root <path>]");
    return 1;
}

using Game1 game = new Game1(gumxPath, elementName, selectionFilePath, contentRootDirectory);
game.Run();
return 0;
