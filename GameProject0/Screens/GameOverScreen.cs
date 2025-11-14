using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace GameProject0
{
    public class GameOverScreen : IGameScreen
    {
        private ScreenManager _screenManager;
        private ContentManager _content;
        private GraphicsDeviceManager _graphics;
        private SpriteFont _font;
        private int _finalScore;
        private Texture2D _pixel;

        private List<string> _menuItems = new List<string> { "PLAY AGAIN", "MAIN MENU" };
        private int _selectedIndex = 0;

        public GameOverScreen(int score)
        {
            _finalScore = score;
        }

        public void Initialize(ScreenManager screenManager, ContentManager content, GraphicsDeviceManager graphics)
        {
            _screenManager = screenManager;
            _content = content;
            _graphics = graphics;
        }

        public void LoadContent()
        {
            _font = _content.Load<SpriteFont>("vcr");
            _pixel = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        public void Update(GameTime gameTime, InputManager input)
        {
            // Navigation
            if (input.Direction.Y > 0) _selectedIndex = 1;
            if (input.Direction.Y < 0) _selectedIndex = 0;

            // Selection
            if (input.Attack || input.Select)
            {
                if (_selectedIndex == 0)
                    _screenManager.LoadScreen(new MainGameScreen()); // Play Again
                else
                    _screenManager.LoadScreen(new TitleScreen()); // Menu
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var viewport = _graphics.GraphicsDevice.Viewport;
            Vector2 center = new Vector2(viewport.Width / 2, viewport.Height / 2);

            // Draw dark background
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.8f);

            // Draw "GAME OVER"
            string title = "GAME OVER";
            Vector2 titleSize = _font.MeasureString(title);
            spriteBatch.DrawString(_font, title, center - titleSize / 2 - new Vector2(0, 100), Color.Red);

            // Draw Score
            string scoreText = $"Final Score: {_finalScore}";
            Vector2 scoreSize = _font.MeasureString(scoreText);
            spriteBatch.DrawString(_font, scoreText, center - scoreSize / 2 - new Vector2(0, 50), Color.White);

            // Draw Menu Items
            for (int i = 0; i < _menuItems.Count; i++)
            {
                Color color = (i == _selectedIndex) ? Color.Yellow : Color.Gray;
                string text = (i == _selectedIndex) ? $"> {_menuItems[i]} <" : _menuItems[i];
                Vector2 textSize = _font.MeasureString(text);
                spriteBatch.DrawString(_font, text, center - textSize / 2 + new Vector2(0, i * 40 + 20), color);
            }
        }

        public void Draw3D(GameTime gameTime, GraphicsDevice graphicsDevice) { }
    }
}