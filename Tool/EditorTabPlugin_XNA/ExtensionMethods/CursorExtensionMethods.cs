﻿using InputLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary;

namespace Gum.Input
{
    public static class CursorExtensionMethods
    {
        public static float GetWorldX(this Cursor cursor, SystemManagers managers = null)
        {
            Renderer renderer = null;
            if (managers == null)
            {
                renderer = Renderer.Self;
            }
            else
            {
                renderer = managers.Renderer;
            }

            float worldX, worldY;
            renderer.Camera.ScreenToWorld(cursor.X, cursor.Y, out worldX, out worldY);

            return worldX;
        }

        public static float GetWorldY(this Cursor cursor, SystemManagers managers = null)
        {
            Renderer renderer = null;
            if (managers == null)
            {
                renderer = Renderer.Self;
            }
            else
            {
                renderer = managers.Renderer;
            }

            float worldX, worldY;
            renderer.Camera.ScreenToWorld(cursor.X, cursor.Y, out worldX, out worldY);

            return worldY;
        }
    }
}
