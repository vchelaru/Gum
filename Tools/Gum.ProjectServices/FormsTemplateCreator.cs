using System;
using System.IO;
using System.Reflection;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class FormsTemplateCreator : IFormsTemplateCreator
{
    private const string ResourcePrefix = "Gum.ProjectServices.Templates.FormsTemplate.";
    private const string ManifestResourceName = ResourcePrefix + "manifest.txt";
    private const string ProjectTemplateRelativePath = "GumProject.gumx";

    /// <inheritdoc/>
    public void Create(string filePath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var projectFileName = Path.GetFileName(filePath);

        var manifest = ReadManifest(assembly);

        foreach (var relativePath in manifest)
        {
            var resourceName = ResourcePrefix + relativePath.Replace('/', '.');
            var destinationPath = BuildDestinationPath(directory, relativePath, projectFileName);

            ExtractResource(assembly, resourceName, destinationPath);
        }
    }

    private static string[] ReadManifest(Assembly assembly)
    {
        using var stream = assembly.GetManifestResourceStream(ManifestResourceName);
        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Forms template manifest not found as embedded resource '{ManifestResourceName}'.");
        }

        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return lines;
    }

    private static string BuildDestinationPath(string directory, string relativePath, string projectFileName)
    {
        if (relativePath == ProjectTemplateRelativePath)
        {
            return Path.Combine(directory, projectFileName);
        }

        return Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void ExtractResource(Assembly assembly, string resourceName, string destinationPath)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Forms template resource not found: '{resourceName}'. The manifest may be out of sync with the embedded resources.");
        }

        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        using var fileStream = File.Create(destinationPath);
        stream.CopyTo(fileStream);
    }
}
