using System.IO;
using System.Reflection;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class DefaultFontBundler : IDefaultFontBundler
{
    private static readonly string[] FileNames =
    {
        "LiberationSans-Regular.ttf",
        "LiberationSans-LICENSE.txt"
    };

    /// <inheritdoc/>
    public void CopyTo(string projectDirectory)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string? fontsDir = null;

        foreach (var fileName in FileNames)
        {
            var resourceName = $"Gum.ProjectServices.Templates.Default.Fonts.{fileName}";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                continue;
            }

            fontsDir ??= Path.Combine(projectDirectory, "Fonts");
            Directory.CreateDirectory(fontsDir);

            var outputPath = Path.Combine(fontsDir, fileName);
            using var fileStream = File.Create(outputPath);
            stream.CopyTo(fileStream);
        }
    }
}
