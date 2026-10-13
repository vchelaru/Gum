using System;
using System.Reflection;
using System.Text;

namespace Gum.Plugins;

/// <summary>
/// Builds the text shown when plugin loading throws: where Gum is running from and the whole
/// exception chain, so a mismatched build can be told apart from a code bug.
/// </summary>
internal static class PluginLoadErrorMessage
{
    public static string Build(
        Exception exception,
        string executablePath,
        string pluginFolder,
        string presentationAssemblyPath)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Error loading plugins");
        builder.AppendLine();
        builder.AppendLine("Running: " + executablePath);
        builder.AppendLine("Plugin folder: " + pluginFolder);
        builder.AppendLine("Gum.Presentation: " + presentationAssemblyPath);

        Exception? current = exception;
        while (current != null)
        {
            builder.AppendLine();
            builder.AppendLine(current.GetType().Name + ": " + current.Message);

            if (current is ReflectionTypeLoadException typeLoadException)
            {
                foreach (Exception? loaderException in typeLoadException.LoaderExceptions)
                {
                    if (loaderException != null)
                    {
                        builder.AppendLine("  Loader exception: " + loaderException);
                    }
                }
            }

            current = current.InnerException;
        }

        return builder.ToString();
    }
}
