using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameProject0
{
    public class ScreenManager
    {
        private IGameScreen _currentScreen;
        private ContentManager _content;
        private GraphicsDeviceManager _graphicsDeviceManager;

        public bool ShouldExit { get; private set; } = false;

        public void ExitGame()
        {
            ShouldExit = true;
        }

        public void Initialize(ContentManager content, GraphicsDeviceManager graphicsDeviceManager)
        {
            _content = content;
            _graphicsDeviceManager = graphicsDeviceManager;
        }

        public void LoadScreen(IGameScreen screen)
        {
            ShouldExit = false;
            _currentScreen = screen;
            _currentScreen.Initialize(this, _content, _graphicsDeviceManager);
            _currentScreen.LoadContent();
        }

        public void Update(GameTime gameTime, InputManager inputManager)
        {
            _currentScreen?.Update(gameTime, inputManager);
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            _currentScreen?.Draw(gameTime, spriteBatch);
        }
    }
}