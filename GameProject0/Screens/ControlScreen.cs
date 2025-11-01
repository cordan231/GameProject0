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
            if (inputManager.Exit)
            {
                _screenManager.LoadScreen(new TitleScreen());
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            Vector2 viewportCenter = new Vector2(viewport.Width / 2f, viewport.Height / 2f);

            string[] controls = {
                "CONTROLS",
                "",
                "A/D or <-/-> : MOVE",
                "E : ATTACK",
                "SPACE : DODGE",
                "ENTER : SELECT",
                "",
                "F5 : SAVE GAME",
                "F9 : LOAD GAME",
                "",
                "ESC : BACK / EXIT"
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
        }
    }
}