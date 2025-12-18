using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace GameProject0
{
    public class ControlScreen : IGameScreen
    {
        private ScreenManager _screenManager;
        private SpriteFont _spriteFont;
        private ContentManager _content;
        private GraphicsDeviceManager _graphicsDeviceManager;
        private Texture2D _whitePixelTexture;

        private string _statusMessage;
        private double _statusMessageTimer;

        public void Initialize(ScreenManager screenManager, ContentManager content, GraphicsDeviceManager graphicsDeviceManager)
        {
            _screenManager = screenManager;
            _content = content;
            _graphicsDeviceManager = graphicsDeviceManager;
        }

        public void LoadContent()
        {
            _spriteFont = _content.Load<SpriteFont>("vcr");

            _whitePixelTexture = new Texture2D(_graphicsDeviceManager.GraphicsDevice, 1, 1);
            _whitePixelTexture.SetData(new[] { Color.White });
        }

        public void Update(GameTime gameTime, InputManager inputManager)
        {
            if (inputManager.MenuBack)
            {
                _screenManager.LoadScreen(new TitleScreen());
            }

            if (_statusMessageTimer > 0)
            {
                _statusMessageTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                if (_statusMessageTimer <= 0)
                {
                    _statusMessage = null;
                }
            }

            if (inputManager.CheatActivated)
            {
                Game1.GunModeActive = !Game1.GunModeActive;
                _statusMessage = "CHEAT ACTIVATED";
                _statusMessageTimer = 2.0;
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            Vector2 viewportCenter = new Vector2(viewport.Width / 2f, viewport.Height / 2f);

            string[] controls = {
                "CONTROLS",
                "",
                "A/D or <-/-> or L-Stick : MOVE",
                "E or X : ATTACK",
                "SPACE or B : DODGE",
                "ENTER or A : SELECT",
                "Q or Y : USE POTION",
                "",
                "F5 : SAVE GAME",
                "F9 : LOAD GAME",
                "",
                "ESC or Circle : BACK / EXIT"
            };

            float lineSpacing = 40f;
            float padding = 20f;
            float outlineThickness = 2f;

            float maxWidth = controls.Max(line => _spriteFont.MeasureString(line).X);
            float totalTextHeight = (controls.Length - 1) * lineSpacing + _spriteFont.MeasureString(controls.Last()).Y;

            float boxWidth = maxWidth + (padding * 2);
            float boxHeight = totalTextHeight + (padding * 2);

            Rectangle backgroundRect = new Rectangle(
                (int)(viewportCenter.X - boxWidth / 2),
                (int)(viewportCenter.Y - boxHeight / 2),
                (int)boxWidth,
                (int)boxHeight
            );

            Rectangle outlineRect = new Rectangle(
                backgroundRect.X - (int)outlineThickness,
                backgroundRect.Y - (int)outlineThickness,
                backgroundRect.Width + (int)(outlineThickness * 2),
                backgroundRect.Height + (int)(outlineThickness * 2)
            );

            spriteBatch.Draw(_whitePixelTexture, outlineRect, Color.White);
            spriteBatch.Draw(_whitePixelTexture, backgroundRect, new Color(0, 0, 139));

            float textStartY = backgroundRect.Y + padding;

            for (int i = 0; i < controls.Length; i++)
            {
                Vector2 textSize = _spriteFont.MeasureString(controls[i]);
                Vector2 textPosition = new Vector2(
                    backgroundRect.X + (boxWidth / 2f) - (textSize.X / 2f),
                    textStartY + (i * lineSpacing)
                );
                spriteBatch.DrawString(_spriteFont, controls[i], textPosition, Color.White);
            }

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                float msgPadding = 10f;
                float msgOutlineThickness = 2f;

                Vector2 textSize = _spriteFont.MeasureString(_statusMessage);
                Vector2 msgPosition = new Vector2(viewport.Width / 2 - textSize.X / 2, viewport.Height - 100);

                Rectangle msgBackgroundRect = new Rectangle(
                    (int)(msgPosition.X - msgPadding),
                    (int)(msgPosition.Y - msgPadding),
                    (int)(textSize.X + msgPadding * 2),
                    (int)(textSize.Y + msgPadding * 2)
                );
                Rectangle msgOutlineRect = new Rectangle(
                    msgBackgroundRect.X - (int)msgOutlineThickness,
                    msgBackgroundRect.Y - (int)msgOutlineThickness,
                    msgBackgroundRect.Width + (int)(msgOutlineThickness * 2),
                    msgBackgroundRect.Height + (int)(msgOutlineThickness * 2)
                );

                spriteBatch.Draw(_whitePixelTexture, msgOutlineRect, Color.White);
                spriteBatch.Draw(_whitePixelTexture, msgBackgroundRect, new Color(0, 0, 139));
                spriteBatch.DrawString(_spriteFont, _statusMessage, msgPosition, Color.White);
            }

        }

        public void Draw3D(GameTime gameTime, GraphicsDevice graphicsDevice)
        {
        }

    }
}