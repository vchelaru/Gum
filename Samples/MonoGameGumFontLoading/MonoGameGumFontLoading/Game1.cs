using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.IO;

namespace MonoGameGumFontLoading
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private readonly List<TextRuntime> _texts = new List<TextRuntime>();
        private bool _disposed;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            SystemManagers.Default = new SystemManagers();
            SystemManagers.Default.Initialize(_graphics.GraphicsDevice, fullInstantiation: true);

            AddText("Fonts/Font16Jing_Jing.fnt", "I use Jing_Jing", 0);
            AddText("Fonts/Font18Arial.fnt", "I use Arial 18", 30);
            AddText("Fonts/Font18Bahnschrift_Light.fnt", "I use Font18Bahnschrift Light", 60);

            TryAddInvalidText("Fonts/InvalidFont.fnt");

            base.Initialize();
        }

        private void AddText(string fontFile, string text, float yPosition)
        {
            var textRuntime = new TextRuntime
            {
                UseCustomFont = true,
                CustomFontFile = fontFile,
                Text = text,
                Y = yPosition
            };
            textRuntime.AddToManagers(SystemManagers.Default, null);
            GraphicalUiElement.ThrowExceptionsForMissingFiles(textRuntime);
            _texts.Add(textRuntime);
        }

        private void TryAddInvalidText(string fontFile)
        {
            try
            {
                var textRuntime = new TextRuntime
                {
                    UseCustomFont = true,
                    CustomFontFile = fontFile
                };
                GraphicalUiElement.ThrowExceptionsForMissingFiles(textRuntime);
            }
            catch (FileNotFoundException)
            {
                System.Diagnostics.Debug.WriteLine("Expected exception for invalid font.");
            }
        }

        protected override void Update(GameTime gameTime)
        {
            SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            SystemManagers.Default.Draw();

            var renderer = SystemManagers.Default.Renderer;
            renderer.Begin();

            var immediateText = new TextRuntime
            {
                UseCustomFont = true,
                CustomFontFile = "Fonts/Font16Jing_Jing.fnt",
                Text = "I am an immediate mode TextRuntime",
                X = 110,
                Y = 120
            };
            renderer.Draw(immediateText);

            renderer.End();
            base.Draw(gameTime);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources
                foreach (var text in _texts)
                {
                    text.RemoveFromManagers();
                }
                _texts.Clear();

                _graphics?.Dispose();
            }

            _disposed = true;

            base.Dispose(disposing);
        }
    }
}