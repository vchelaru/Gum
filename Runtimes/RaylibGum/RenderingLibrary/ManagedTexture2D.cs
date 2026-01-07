using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary;

/// <summary>
/// Wrapper of raylib Texture2D that implements IDisposable for proper resource management.
/// </summary>
public class ManagedTexture : IDisposable
{
    /// <summary>
    /// Gets the texture associated with this object.
    /// </summary>
    public Texture2D Texture { get; private set; }
    private bool disposed = false;

    /// <summary>
    /// Initializes a new instance of the ManagedTexture class using the specified Texture2D object.
    /// </summary>
    /// <param name="texture">The Texture2D instance to be managed. Cannot be null.</param>
    public ManagedTexture(Texture2D texture)
    {
        Texture = texture;
    }

    /// <summary>
    /// Releases all resources used by the current instance.
    /// </summary>
    /// <remarks>Call this method when the instance is no longer needed to free associated resources
    /// immediately. After calling <see cref="Dispose"/>, the instance should not be used.</remarks>
    public void Dispose()
    {
        if (!disposed)
        {
            Raylib.UnloadTexture(Texture);
            disposed = true;
        }
    }
}