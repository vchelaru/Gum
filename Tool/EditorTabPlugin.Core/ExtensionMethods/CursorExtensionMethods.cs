using InputLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary;

namespace Gum.Input;

public static class CursorExtensionMethods
{
    public static float GetWorldX(this Cursor cursor, SystemManagers? managers = null)
    {
        Renderer? renderer = null;
        if (managers == null)
        {
            renderer = SystemManagers.Default?.Renderer;
        }
        else
        {
            renderer = managers.Renderer;
        }

        float worldX = cursor.X;
        float worldY = cursor.Y;
        if(renderer?.Camera != null)
        {
            renderer.Camera.ScreenToWorld(cursor.X, cursor.Y, out worldX, out worldY);
        }

        return worldX;
    }

    public static float GetWorldY(this Cursor cursor, SystemManagers? managers = null)
    {
        Renderer? renderer = null;
        if (managers == null)
        {
            renderer = SystemManagers.Default?.Renderer;
        }
        else
        {
            renderer = managers.Renderer;
        }

        if (renderer != null)
        {
            renderer.Camera.ScreenToWorld(cursor.X, cursor.Y, out float worldX, out float worldY);
            return worldY;
        }
        else
        {
            return cursor.Y;
        }

    }

}