using FlatRedBall;
using Gum.DataTypes;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Gum.MonoGameIntegration
{
    public class RuntimeLogic : DrawableGameComponent
    {
        static bool hasInitializeBeenCalled = false;

        public RuntimeLogic(Game game) : base(game)
        {
        }

        public override void Initialize()
        {
            hasInitializeBeenCalled = true;

            var graphicsDeviceManager =
                // This seems to only be supported on MonoGame, not XNA
                //Game.Services.GetService<IGraphicsDeviceManager>() as GraphicsDeviceManager;
                Game.Services.GetService(typeof(IGraphicsDeviceManager)) as GraphicsDeviceManager; 

            FlatRedBallServices.InitializeFlatRedBall(Game, graphicsDeviceManager);

            global::RenderingLibrary.SystemManagers.Default =
                new global::RenderingLibrary.SystemManagers();
            global::RenderingLibrary.SystemManagers.Default.Initialize(graphicsDeviceManager.GraphicsDevice);

            global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.CameraCenterOnScreen =
                global::RenderingLibrary.CameraCenterOnScreen.TopLeft;

        }

        /// <summary>
        /// Loads a Gum project and fully initializes it for runtime use.
        /// </summary>
        /// <param name="fileName">The .gumx file name (typically relative to the executable) to load.</param>
        /// <returns>The fully-initialized GumProjectSave</returns>
        public static GumProjectSave LoadGumProject(string fileName)
        {
            if(!hasInitializeBeenCalled)
            {
                throw new InvalidOperationException("Need to call Initialize first. If this has been added as a game component, make this call after your game's base.Initialize() call.");
            }
            GumLoadResult loadResult;
            var gumProject = GumProjectSave.Load(
                fileName, out loadResult);



            if(loadResult.MissingFiles.Count > 0)
            {
                string message = null;
                if(!string.IsNullOrEmpty(loadResult.ErrorMessage))
                {
                    message = loadResult.ErrorMessage + "\n\n";
                }
                foreach(var missingFile in loadResult.MissingFiles)
                {
                    message += $"Missing:{missingFile}";
                }
                throw new FileNotFoundException(message);
            }


            Gum.Managers.ObjectFinder.Self.GumProjectSave = gumProject;
            foreach (var item in gumProject.Screens)
            {
                // Only initialize using the default state
                if (item.DefaultState != null)
                {
                    item.Initialize(item.DefaultState);
                }
            }
            foreach (var item in gumProject.Components)
            {
                // Only initialize using the default state
                if (item.DefaultState != null)
                {
                    item.Initialize(item.DefaultState);
                }
            }
            foreach (var item in gumProject.StandardElements)
            {
                // Only initialize using the default state
                if (item.DefaultState != null)
                {
                    item.Initialize(item.DefaultState);
                }
            }

            Gum.Managers.StandardElementsManager.Self.Initialize();

            return gumProject;
        }

        public override void Update(GameTime gameTime)
        {
            global::RenderingLibrary.SystemManagers.Default.Activity(gameTime.ElapsedGameTime.TotalSeconds);
            FlatRedBallServices.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            global::RenderingLibrary.SystemManagers.Default.Draw();

            // do we also draw FRB? Not sure if we should...
        }
    }
}
