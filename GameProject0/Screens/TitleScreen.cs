using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace GameProject0
{
    public class TitleScreen : IGameScreen
    {
        private ScreenManager _screenManager;
        private SpriteFont _spriteFont;
        private Texture2D _backgroundTexture, _titleTexture;
        private PlayerSprite _playerSprite;
        private List<string> _menuItems;
        private float _menuItemSpacing = 200f;
        private ContentManager _content;
        private GraphicsDeviceManager _graphicsDeviceManager;

        public void Initialize(ScreenManager screenManager, ContentManager content, GraphicsDeviceManager graphicsDeviceManager)
        {
            _screenManager = screenManager;
            _content = content;
            _graphicsDeviceManager = graphicsDeviceManager;
            _playerSprite = new PlayerSprite();
            _menuItems = new List<string>
            {
                "START GAME",
                "OPTIONS",
                "EXIT"
            };
        }

        public void LoadContent()
        {
            _playerSprite.LoadContent(_content);
            _playerSprite.Position = new Vector2(300, 140);
            _spriteFont = _content.Load<SpriteFont>("vcr");
            _backgroundTexture = _content.Load<Texture2D>("platform-background");
            _titleTexture = _content.Load<Texture2D>("runner");
        }

        public void Update(GameTime gameTime, InputManager inputManager)
        {
            if (inputManager.Exit)
            {
                _screenManager.ExitGame();
                return;
            }

            HandleTitleScreenInput(gameTime, inputManager);
            _playerSprite.Update(gameTime);
        }

        private void HandleTitleScreenInput(GameTime gameTime, InputManager inputManager)
        {
            if (inputManager.Direction.X != 0)
            {
                _playerSprite.SetState(CurrentState.Running);
                _playerSprite.SetDirection(inputManager.Direction.X > 0 ? Direction.Right : Direction.Left);
            }
            else
            {
                _playerSprite.SetState(CurrentState.Idle);
            }

            _playerSprite.Position += inputManager.Direction * 150f * (float)gameTime.ElapsedGameTime.TotalSeconds;

            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            _playerSprite.Position = new Vector2(
                MathHelper.Clamp(_playerSprite.Position.X, 0, viewport.Width - _playerSprite.Width),
                _playerSprite.Position.Y
            );

            if (inputManager.Select)
            {
                int selectedIndex = GetSelectedMenuIndex();
                if (selectedIndex != -1)
                {
                    switch (selectedIndex)
                    {
                        case 0:
                            _screenManager.LoadScreen(new MainGameScreen());
                            break;
                        case 1:
                            System.Console.WriteLine("Selected: OPTIONS");
                            break;
                        case 2:
                            _screenManager.ExitGame();
                            break;
                    }
                }
            }
        }

        private int GetSelectedMenuIndex()
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            Vector2 menuStartPosition = new Vector2(
                viewport.Width / 2f - (_menuItemSpacing * (_menuItems.Count - 1) / 2f),
                viewport.Height / 2f
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
                    (int)(_playerSprite.Position.X + _playerSprite.Width / 2),
                    (int)_playerSprite.Position.Y,
                    1,
                    (int)_playerSprite.Height
                );

                if (textRect.Intersects(spriteRect))
                {
                    return i;
                }
            }
            return -1;
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            spriteBatch.Draw(_backgroundTexture, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);

            Vector2 titlePosition = new Vector2((viewport.Width / 2f) - ((_titleTexture.Width * 3f) / 2f), 10f);
            spriteBatch.Draw(_titleTexture, titlePosition, null, Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0f);

            int selectedIndex = GetSelectedMenuIndex();

            Vector2 menuStartPos = new Vector2(
                viewport.Width / 2f - (_menuItemSpacing * (_menuItems.Count - 1) / 2f),
                viewport.Height / 2f
            );

            for (int i = 0; i < _menuItems.Count; i++)
            {
                Vector2 textSize = _spriteFont.MeasureString(_menuItems[i]);
                Vector2 textPosition = new Vector2(
                    menuStartPos.X + i * _menuItemSpacing - textSize.X / 2f,
                    menuStartPos.Y - 50
                );

                Color color = (i == selectedIndex) ? Color.White : Color.Black;

                spriteBatch.DrawString(_spriteFont, _menuItems[i], textPosition, color);
            }

            string instructions = "<- -> or A D TO MOVE     ENTER TO SELECT";
            Vector2 instructionsSize = _spriteFont.MeasureString(instructions);
            Vector2 instructionsPosition = new Vector2(
                viewport.Width - instructionsSize.X - 10,
                viewport.Height - instructionsSize.Y - 10
            );
            spriteBatch.DrawString(_spriteFont, instructions, instructionsPosition, Color.White);

            _playerSprite.Draw(spriteBatch);
        }
    }
}