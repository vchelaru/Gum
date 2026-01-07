using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary;

/// <summary>
/// Wrapper of raylib Font that implements IDisposable for proper resource management.
/// </summary>
public class ManagedFont : IDisposable
{
    /// <summary>
    /// Gets the font used to render text within the control.
    /// </summary>
    public Font Font { get; private set; }
    private bool disposed = false;

    /// <summary>
    /// Initializes a new instance of the ManagedFont class using the specified font.
    /// </summary>
    /// <param name="font">The Font object to be managed. Cannot be null.</param>
    public ManagedFont(Font font)
    {
        Font = font;
    }

    /// <summary>
    /// Releases all resources used by the current instance.
    /// </summary>
    /// <remarks>Call this method when you are finished using the object to free unmanaged resources
    /// associated with it. After calling <see cref="Dispose"/>, the object should not be used.</remarks>
    public void Dispose()
    {
        if (!disposed)
        {
            Raylib_cs.Raylib.UnloadFont(Font);
            disposed = true;
        }
    }
}
