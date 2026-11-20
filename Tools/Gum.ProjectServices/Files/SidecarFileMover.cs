using System;
using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// Moves a file that follows an element on rename or folder move, such as the element's animation
/// or code settings file. Shared by the plugins that own those files so each handles a rename that
/// only changes casing, and a destination folder that does not exist yet, the same way.
/// </summary>
public static class SidecarFileMover
{
    /// <summary>
    /// True when the two paths differ only by casing, which means they are the same physical file on
    /// a case-insensitive filesystem (Windows, macOS).
    /// </summary>
    public static bool IsSameFileWithDifferentCase(FilePath first, FilePath second) =>
        first.FullPath != second.FullPath &&
        string.Equals(first.FullPath, second.FullPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Moves a file, creating the destination directory if needed and translating file-access
    /// failures into a <see cref="FileOperationException"/> with a message the user can act on.
    /// </summary>
    public static void Move(FilePath source, FilePath destination)
    {
        try
        {
            // Moving into a folder for the first time means the destination directory may not exist
            // yet - without this the move throws and the file orphans at its old path.
            var destinationDirectory = destination.GetDirectoryContainingThis();
            if (destinationDirectory != null && !System.IO.Directory.Exists(destinationDirectory.FullPath))
            {
                System.IO.Directory.CreateDirectory(destinationDirectory.FullPath);
            }

            if (IsSameFileWithDifferentCase(source, destination))
            {
                // Source and destination are the same physical file, so move through a temporary
                // name. Windows corrects the casing with a direct move, but File.Move pre-checks the
                // destination on Unix and throws "already exists" on a case-insensitive macOS
                // volume. The casing on disk does have to change: git is case-sensitive even where
                // the filesystem is not.
                var temporaryPath = destination.FullPath + ".gumrename";
                System.IO.File.Move(source.FullPath, temporaryPath);
                System.IO.File.Move(temporaryPath, destination.FullPath);
            }
            else
            {
                System.IO.File.Move(source.FullPath, destination.FullPath);
            }
        }
        catch (Exception e) when (FileOperationFailure.IsAccessFailure(e))
        {
            throw new FileOperationException(
                FileOperationFailure.BuildMessage(
                    $"Could not move this file:\n{source.FullPath}\nto:\n{destination.FullPath}", e),
                e);
        }
    }
}
