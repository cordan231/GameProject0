using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameProject0
{
    public class ScreenManager
    {
        // Currently active screen (Title, Game, Over)
        private IGameScreen _currentScreen;
        private ContentManager _content;
        private GraphicsDeviceManager _graphicsDeviceManager;

        public IGameScreen CurrentScreen => _currentScreen;

        // Flag to tell Game1 to exit
        public bool ShouldExit { get; private set; } = false;

        public void ExitGame()
        {
            ShouldExit = true;
        }

        // Initialize the screen manager with content and graphics device
        public void Initialize(ContentManager content, GraphicsDeviceManager graphicsDeviceManager)
        {
            _content = content;
            _graphicsDeviceManager = graphicsDeviceManager;
        }

        // Switch to a new screen and initialize it
        public void LoadScreen(IGameScreen screen)
        {
            ShouldExit = false;
            _currentScreen = screen;
            _currentScreen.Initialize(this, _content, _graphicsDeviceManager);
            _currentScreen.LoadContent();
        }

        // Update current screen
        public void Update(GameTime gameTime, InputManager inputManager)
        {
            _currentScreen?.Update(gameTime, inputManager);
        }

        // Draw current screen
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            _currentScreen?.Draw(gameTime, spriteBatch);
        }
    }
}