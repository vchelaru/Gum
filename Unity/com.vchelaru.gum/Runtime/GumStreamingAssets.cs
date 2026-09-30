#nullable enable
using System.IO;
using ToolsUtilities;

namespace Gum.Unity
{
    /// <summary>
    /// Serves Gum's file reads (the project, its elements, textures, fonts) through
    /// <see cref="FileManager.CustomGetStreamFromFile"/>. Keeping every read on this one hook is what lets
    /// a platform whose StreamingAssets aren't a plain folder (Android, WebGL) be supported by changing
    /// only <see cref="Open"/>. <see cref="GumRenderer"/> installs it when it loads a project.
    /// </summary>
    public sealed class GumStreamingAssets
    {
        /// <summary>
        /// Installs the hook, unless the app already installed its own.
        /// </summary>
        public void Install()
        {
            if (FileManager.CustomGetStreamFromFile == null)
            {
                // The delegate is typed non-null, but Gum treats a null stream as a missing file.
                FileManager.CustomGetStreamFromFile = path => Open(path)!;
            }
        }

        /// <summary>
        /// Opens <paramref name="path"/>, or returns null when it doesn't exist so Gum treats it as missing.
        /// </summary>
        public Stream? Open(string path)
        {
            // Gum marks already-resolved relative paths with a leading "./".
            if (path.StartsWith("./") || path.StartsWith(".\\"))
            {
                path = path.Substring(2);
            }
            return File.Exists(path) ? File.OpenRead(path) : null;
        }
    }
}
