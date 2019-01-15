using FlatRedBall;
using Gum.DataTypes;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
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
                Game.Services.GetService<IGraphicsDeviceManager>() as GraphicsDeviceManager;

            FlatRedBallServices.InitializeFlatRedBall(Game, graphicsDeviceManager);

            global::RenderingLibrary.SystemManagers.Default =
                new global::RenderingLibrary.SystemManagers();
            global::RenderingLibrary.SystemManagers.Default.Initialize(graphicsDeviceManager.GraphicsDevice);

            global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.CameraCenterOnScreen =
                global::RenderingLibrary.CameraCenterOnScreen.TopLeft;

        }


        public static GumProjectSave LoadGumProject(string fileName)
        {
            if(!hasInitializeBeenCalled)
            {
                throw new InvalidOperationException("Need to call Initialize first. If this has been added as a game component, make this call after your game's base.Initialize() call.");
            }
            var gumProject = GumProjectSave.Load(
                fileName, out GumLoadResult loadResult);
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
