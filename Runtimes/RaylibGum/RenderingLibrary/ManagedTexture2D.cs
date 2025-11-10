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
    public Texture2D Texture { get; private set; }
    private bool disposed = false;

    public ManagedTexture(Texture2D texture)
    {
        Texture = texture;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            Raylib.UnloadTexture(Texture);
            disposed = true;
        }
    }
}