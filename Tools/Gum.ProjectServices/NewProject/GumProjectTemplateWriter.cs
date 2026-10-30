namespace Gum.ProjectServices;

/// <summary>
/// Writes the Gum project for a new project: from a theme, the plain Forms template, or the empty standards.
/// The one place that choice is made, shared by the Gum-only and the platform paths.
/// </summary>
internal static class GumProjectTemplateWriter
{
    /// <param name="gumProjectPath">Absolute path ending in .gumj or .gumx.</param>
    /// <param name="includeFormsTemplate">Plain Forms controls; ignored when <paramref name="themeDirectory"/> is set.</param>
    /// <param name="themeDirectory">A Forms theme folder, which takes precedence.</param>
    internal static void Write(string gumProjectPath, bool includeFormsTemplate, string? themeDirectory)
    {
        if (themeDirectory != null)
        {
            new FormsTemplateCreator().CreateFromTheme(gumProjectPath, themeDirectory);
        }
        else if (includeFormsTemplate)
        {
            new FormsTemplateCreator().Create(gumProjectPath);
        }
        else
        {
            new ProjectCreator().Create(gumProjectPath);
        }
    }
}
