#nullable enable
using Gum.Wireframe;
using RenderingLibrary.Content;
using System;

namespace SkiaGum.Content;

internal static class SourceFileLoading
{
    /// <summary>
    /// Loads the image, SVG or Lottie document at <paramref name="path"/> through the <see cref="LoaderManager"/>
    /// for a SourceFile assignment, or returns <c>null</c> for a null or empty path (the "no file" value). A file
    /// that is missing or exists but cannot be read is one failure: <see cref="MissingFileBehavior.ThrowException"/>
    /// throws a <see cref="System.IO.FileNotFoundException"/> naming the file, otherwise the result is null and,
    /// when <paramref name="raiseError"/> is true, <see cref="CustomSetPropertyOnRenderable.PropertyAssignmentError"/>
    /// is raised. Mirrors the MonoGame/raylib dispatcher (#6889, #6903).
    /// </summary>
    internal static T? Load<T>(string? path, GraphicalUiElement owner, string elementTypeName, bool raiseError = true) where T : class
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        Exception? loadException = null;
        T? loaded = null;
        try
        {
            loaded = LoaderManager.Self.LoadContent<T>(path);
        }
        catch (Exception ex)
        {
            // Whatever the loader throws is treated as a failed load, as MonoGame does.
            loadException = ex;
        }

        if (loaded != null)
        {
            return loaded;
        }

        string message = $"Error setting SourceFile on {elementTypeName}";
        if (owner.Tag != null)
        {
            message += $" in {owner.Tag}";
        }
        message += $"\n{path}";
        message += "\nCheck if the file exists. If necessary, set FileManager.RelativeDirectory";
        message += "\nThe current relative directory is:\n" + ToolsUtilities.FileManager.RelativeDirectory;
        if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
        {
            throw new System.IO.FileNotFoundException(message, loadException);
        }
        if (raiseError)
        {
            CustomSetPropertyOnRenderable.RaisePropertyAssignmentError(loadException != null ? message + "\n" + loadException : message);
        }
        return null;
    }
}
