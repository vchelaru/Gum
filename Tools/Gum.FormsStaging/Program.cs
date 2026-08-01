using Gum.ProjectServices;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: Gum.FormsStaging <project.gumx> <destination>");
    return 2;
}

return FormsThemeBehaviorStagingRunner.Run(args[0], args[1]);
