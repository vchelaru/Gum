using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ToolsUtilities;

namespace GumFromZipFile
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        GraphicalUiElement Root;

        Dictionary<string, byte[]> zipFileBytes = new Dictionary<string, byte[]>();

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            LoadZip();

            FileManager.CustomGetStreamFromFile = GetStreamForFile;

            GumService.Default.Initialize(this, "GumProject/FromZipFileGumProject.gumx");

            Root = ObjectFinder.Self.GumProjectSave.Screens.First().ToGraphicalUiElement(
                SystemManagers.Default, addToManagers: true);

            base.Initialize();
        }

        private void LoadZip()
        {
            using FileStream zipToOpen = new FileStream(@"Content/GumProject.zip", FileMode.Open);
            using ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update);

            foreach (var entry in archive.Entries)
            {
                if(entry.FullName.EndsWith("/"))
                {
                    continue;
                }
                using var stream = entry.Open();
                var byteLength = stream.Length;
                var bytes = new byte[byteLength];
                stream.Read(bytes, 0, (int)byteLength);

                var currentDirectory = Path.Combine( Directory.GetCurrentDirectory(), "Content");

                var fullPath = Path.Combine(currentDirectory, entry.FullName);
                zipFileBytes.Add(fullPath.Replace("\\", "/"), bytes);
            }
        }

        private Stream GetStreamForFile(string fullPath)
        {

            fullPath = fullPath.Replace("\\", "/");

            if(zipFileBytes.ContainsKey(fullPath))
            {
                return new MemoryStream(zipFileBytes[fullPath]);
            }

            return null;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            GumService.Default.Update(gameTime, Root);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            GumService.Default.Draw();

            base.Draw(gameTime);
        }
    }
}
