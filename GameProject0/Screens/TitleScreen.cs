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
        private Texture2D _titleTexture;
        private PlayerSprite _playerSprite;
        private Tilemap _tilemap;
        private List<string> _menuItems;
        private float _menuItemSpacing = 200f;
        private ContentManager _content;
        private GraphicsDeviceManager _graphicsDeviceManager;
        private Texture2D _whitePixelTexture;

        private const float GROUND_Y = 3 * 64 * 2.0f;

        public void Initialize(ScreenManager screenManager, ContentManager content, GraphicsDeviceManager graphicsDeviceManager)
        {
            _screenManager = screenManager;
            _content = content;
            _graphicsDeviceManager = graphicsDeviceManager;
            _playerSprite = new PlayerSprite();
            _tilemap = new Tilemap("map.txt");
            _menuItems = new List<string>
            {
                "START GAME",
                "CONTROLS",
                "EXIT"
            };
        }

        public void LoadContent()
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            _tilemap.LoadContent(_content);

            _playerSprite.LoadContent(_content);
            _spriteFont = _content.Load<SpriteFont>("vcr");
            _titleTexture = _content.Load<Texture2D>("fight-logo");

            _whitePixelTexture = new Texture2D(_graphicsDeviceManager.GraphicsDevice, 1, 1);
            _whitePixelTexture.SetData(new[] { Color.White });

            _playerSprite.Position = new Vector2(
                300,
                GROUND_Y - _playerSprite.Height
            );
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
                GROUND_Y - _playerSprite.Height
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
                            _screenManager.LoadScreen(new ControlScreen());
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

            _tilemap.Draw(gameTime, spriteBatch);

            Vector2 titlePosition = new Vector2((viewport.Width / 2f) - ((_titleTexture.Width * 3f) / 2f), 10f);
            spriteBatch.Draw(_titleTexture, titlePosition, null, Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0f);

            int selectedIndex = GetSelectedMenuIndex();

            Vector2 menuStartPos = new Vector2(
                viewport.Width / 2f - (_menuItemSpacing * (_menuItems.Count - 1) / 2f),
                viewport.Height / 2f
            );

            for (int i = 0; i < _menuItems.Count; i++)
            {
                string text = _menuItems[i];
                Vector2 textSize = _spriteFont.MeasureString(text);
                Vector2 textPosition = new Vector2(
                    menuStartPos.X + i * _menuItemSpacing - textSize.X / 2f,
                    menuStartPos.Y - 50
                );

                bool isSelected = (i == selectedIndex);

                if (isSelected)
                {
                    float padding = 10f;
                    float outlineThickness = 2f;

                    Rectangle backgroundRect = new Rectangle(
                        (int)(textPosition.X - padding),
                        (int)(textPosition.Y - padding),
                        (int)(textSize.X + padding * 2),
                        (int)(textSize.Y + padding * 2)
                    );

                    Rectangle outlineRect = new Rectangle(
                        backgroundRect.X - (int)outlineThickness,
                        backgroundRect.Y - (int)outlineThickness,
                        backgroundRect.Width + (int)(outlineThickness * 2),
                        backgroundRect.Height + (int)(outlineThickness * 2)
                    );

                    spriteBatch.Draw(_whitePixelTexture, outlineRect, Color.White);
                    spriteBatch.Draw(_whitePixelTexture, backgroundRect, new Color(0, 0, 139));
                    spriteBatch.DrawString(_spriteFont, text, textPosition, Color.White);
                }
                else
                {
                    spriteBatch.DrawString(_spriteFont, text, textPosition, Color.Black);
                }
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

        public void Draw3D(GameTime gameTime, GraphicsDevice graphicsDevice)
        {
            // This screen has no 3D elements
        }

    }
}