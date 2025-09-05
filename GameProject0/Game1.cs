using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace GameProject0
{
    public enum GameState
    {
        TitleScreen,
        Playing
    }


    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _spriteFont;

        private Texture2D _backgroundTexture;

        private PlayerSprite _playerSprite;
        private InputManager _inputManager;

        private GameState _gameState;
        private List<string> _menuItems;

        private float _menuItemSpacing = 200f;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _playerSprite = new PlayerSprite();
            _inputManager = new InputManager();

            _gameState = GameState.TitleScreen;

            _menuItems = new List<string>
            {
                "START GAME",
                "OPTIONS",
                "EXIT"
            };

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            
            _playerSprite.LoadContent(Content);
            _playerSprite.Position = new Vector2(300, 300);

            _spriteFont = Content.Load<SpriteFont>("vcr");

            _backgroundTexture = Content.Load<Texture2D>("orig");

        }

        protected override void Update(GameTime gameTime)
        {

            _inputManager.Update(gameTime);

            if (_inputManager.Exit)
            {
                Exit();
            }
            switch (_gameState)
            {
                case GameState.TitleScreen:
                    HandleTitleScreenInput(gameTime);
                    break;
                case GameState.Playing:
                    break;
            }

            _playerSprite.Update(gameTime);

            base.Update(gameTime);

            _playerSprite.Update(gameTime);

            base.Update(gameTime);
        }

        private void HandleTitleScreenInput(GameTime gameTime)
        {
            if (_inputManager.Direction.X != 0)
            {
                _playerSprite.SetState(CurrentState.Running);
                _playerSprite.SetDirection(_inputManager.Direction.X > 0 ? Direction.Right : Direction.Left);
            }
            else
            {
                _playerSprite.SetState(CurrentState.Idle);
            }

            // Move the sprite left and right based on input
            _playerSprite.Position += _inputManager.Direction * 150f * (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Constrain the sprite's position to the screen
            _playerSprite.Position = Vector2.Clamp(_playerSprite.Position, Vector2.Zero, new Vector2(_graphics.PreferredBackBufferWidth - 32, _graphics.PreferredBackBufferHeight - 32));

            if (_inputManager.Select)
            {
                // Find the selected menu item based on the sprite's position
                int selectedIndex = GetSelectedMenuIndex();
                if (selectedIndex != -1)
                {
                    System.Console.WriteLine($"Selected: {_menuItems[selectedIndex]}");
                }
            }
        }

        private int GetSelectedMenuIndex()
        {
            Vector2 menuStartPosition = new Vector2(
                _graphics.PreferredBackBufferWidth / 2f - (_menuItemSpacing * (_menuItems.Count - 1) / 2f),
                _graphics.PreferredBackBufferHeight / 2f
            );

            for (int i = 0; i < _menuItems.Count; i++)
            {
                Vector2 textSize = _spriteFont.MeasureString(_menuItems[i]);
                Vector2 textPosition = new Vector2(
                    menuStartPosition.X + i * _menuItemSpacing,
                    menuStartPosition.Y
                );

                Rectangle textRect = new Rectangle(
                    (int)(textPosition.X - textSize.X / 2),
                    (int)textPosition.Y,
                    (int)textSize.X,
                    (int)textSize.Y
                );

                Rectangle spriteRect = new Rectangle(
                    (int)_playerSprite.Position.X,
                    (int)_playerSprite.Position.Y,
                    32,
                    32
                );

                if (textRect.Intersects(spriteRect))
                {
                    return i;
                }
            }
            return -1;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Gray);

            // TODO: Add your drawing code here
            _spriteBatch.Begin();

            switch(_gameState)
            {
                case GameState.TitleScreen:
                    DrawTitleScreen();
                    break;
                case GameState.Playing:
                    break;
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawTitleScreen()
        {

            _spriteBatch.Draw(_backgroundTexture, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Color.White);

            int selectedIndex = GetSelectedMenuIndex();

            Vector2 menuStartPos = new Vector2(
                _graphics.PreferredBackBufferWidth / 2f - (_menuItemSpacing * (_menuItems.Count - 1) / 2f),
                _graphics.PreferredBackBufferHeight / 2f
            );

            for (int i = 0; i < _menuItems.Count; i++)
            {
                Vector2 textSize = _spriteFont.MeasureString(_menuItems[i]);
                Vector2 textPosition = new Vector2(
                    menuStartPos.X + i * _menuItemSpacing - textSize.X / 2f,
                    menuStartPos.Y
                );

                // Set the color to white if the sprite is intersecting the text, otherwise set it to black.
                Color color = (i == selectedIndex) ? Color.White : Color.Black;

                // Draw the text
                _spriteBatch.DrawString(_spriteFont, _menuItems[i], textPosition, color);
            }

            _playerSprite.Draw(_spriteBatch);
        }

    }
}
