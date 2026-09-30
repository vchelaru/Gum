#nullable enable
using System.IO;
using ToolsUtilities;
using UnityEngine;
using UnityEngine.Networking;

namespace Gum.Unity
{
    /// <summary>
    /// Serves Gum's file reads (the project, its elements, textures, fonts) through
    /// <see cref="FileManager.CustomGetStreamFromFile"/>. Where StreamingAssets is a plain folder the
    /// read is <see cref="File.OpenRead(string)"/>; where it is a URL (Android, where it is inside the
    /// APK) Gum loads a path under <see cref="StreamingAssetsLocation.VirtualRoot"/> and the read goes
    /// through <see cref="UnityWebRequest"/>. <see cref="GumRenderer"/> installs it when it loads a project.
    /// </summary>
    public sealed class GumStreamingAssets
    {
        readonly StreamingAssetsLocation _location;

        /// <summary>Serves files from <see cref="Application.streamingAssetsPath"/>.</summary>
        public GumStreamingAssets() : this(Application.streamingAssetsPath)
        {
        }

        /// <summary>Serves files from <paramref name="streamingAssetsPath"/>, a folder or a URL.</summary>
        public GumStreamingAssets(string streamingAssetsPath)
        {
            _location = new StreamingAssetsLocation(streamingAssetsPath);
        }

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
        /// Returns the path to hand Gum for <paramref name="projectFile"/>, a path relative to StreamingAssets.
        /// </summary>
        public string GetProjectPath(string projectFile) => _location.GetGumPath(projectFile);

        /// <summary>
        /// Opens <paramref name="path"/>, or returns null when it doesn't exist so Gum treats it as missing.
        /// </summary>
        public Stream? Open(string path)
        {
            string? url = _location.GetUrl(path);
            if (url != null)
            {
                return OpenUrl(url);
            }

            // Gum marks already-resolved relative paths with a leading "./".
            if (path.StartsWith("./") || path.StartsWith(".\\"))
            {
                path = path.Substring(2);
            }
            return File.Exists(path) ? File.OpenRead(path) : null;
        }

        // Gum's file reads are synchronous, so this blocks until the request finishes. That works
        // for Android's local jar: URLs but would never finish on WebGL, where the request needs the
        // browser's event loop.
        static Stream? OpenUrl(string url)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                }
                if (request.result != UnityWebRequest.Result.Success)
                {
                    return null;
                }
                return new MemoryStream(request.downloadHandler.data, writable: false);
            }
        }
    }
}
