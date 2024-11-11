using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.IO;

namespace MonoGameGumFontLoading
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;

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

            // fonts can be explicitly loaded too:
            var bitmapFont = new BitmapFont("Fonts/Font18Arial.fnt", SystemManagers.Default);

            var text = new TextRuntime();
            text.UseCustomFont = true;
            text.CustomFontFile = "Fonts/Font16Jing_Jing.fnt";
            text.Text = "I use Jing_Jing";
            text.AddToManagers(SystemManagers.Default, null);
            // No errors here:
            GraphicalUiElement.ThrowExceptionsForMissingFiles(text);

            var text2 = new TextRuntime();
            text2.UseCustomFont = true;
            text2.CustomFontFile = "Fonts/Font18Arial.fnt";
            text2.Text = "I use Arial 18";
            text2.AddToManagers(SystemManagers.Default, null);
            text2.Y = 30;
            // No errors here:
            GraphicalUiElement.ThrowExceptionsForMissingFiles(text2);

            var text3 = new TextRuntime();
            text3.UseCustomFont = true;
            text3.CustomFontFile = "Fonts/Font18Bahnschrift_Light.fnt";
            text3.Text = "I use Font18Bahnschrift Light";
            text3.AddToManagers(SystemManagers.Default, null);
            text3.Y = 60;
            // No errors here:
            GraphicalUiElement.ThrowExceptionsForMissingFiles(text3);


            try
            {
                var textThatHasError = new TextRuntime();
                textThatHasError.UseCustomFont = true;
                textThatHasError.CustomFontFile = "Fonts/InvalidFont.fnt";
                GraphicalUiElement.ThrowExceptionsForMissingFiles(textThatHasError);
            }
            catch (FileNotFoundException e)
            {
                System.Diagnostics.Debug.WriteLine("Yay we got an exception! That's expected");     
            }

            base.Initialize();
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

            var text = new TextRuntime();
            text.UseCustomFont = true;
            text.CustomFontFile = "Fonts/Font16Jing_Jing.fnt";
            text.Text = "I am an immediate mode TextRuntime";
            text.X = 110;
            text.Y = 120;

            renderer.Draw(text);

            renderer.End();


            base.Draw(gameTime);
        }
    }
}
