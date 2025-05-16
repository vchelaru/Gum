using Gum.DataTypes;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Input;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;

namespace MonoGameGum.Interfaces
{
    public interface IGumService
    {
        float CanvasHeight { get; set; }
        float CanvasWidth { get; set; }
        ContentLoader? ContentLoader { get; }
        Cursor Cursor { get; }
        GamePad[] Gamepads { get; }
        GameTime GameTime { get; }
        Keyboard Keyboard { get; }
        Renderer Renderer { get; }
        InteractiveGue Root { get; }
        SystemManagers SystemManagers { get; }

        void Draw();
        GumProjectSave? Initialize(Game game, string? gumProjectFile = null);
        void Initialize(Game game, SystemManagers systemManagers);
        GumProjectSave? Initialize(GraphicsDevice graphicsDevice, string? gumProjectFile = null);
        void LoadAnimations();
        void Update(Game game, GameTime gameTime);
        void Update(Game game, GameTime gameTime, FrameworkElement root);
        void Update(Game game, GameTime gameTime, GraphicalUiElement root);
        void Update(Game game, GameTime gameTime, IEnumerable<GraphicalUiElement> roots);
        void Update(GameTime gameTime);
    }
}