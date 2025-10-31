using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject0
{
    public class Background
    {
        private Texture2D _tilesetTexture;
        private Rectangle _groundTileSrc;
        private Rectangle _mountainSrc;
        private Rectangle _sandHillsSrc;

        public float TileScale { get; set; } = 2.0f;
        public float GroundY { get; private set; }

        public void LoadContent(ContentManager content, Viewport viewport)
        {
            _tilesetTexture = content.Load<Texture2D>("Dungeon Ruins Tileset Day");


            _groundTileSrc = new Rectangle(16, 16, 48, 48);
            _sandHillsSrc = new Rectangle(321, 143, 192, 16);
            _mountainSrc = new Rectangle(287, 172, 128, 36);

            // Calculate the ground position based on tile size and scale
            GroundY = viewport.Height - (_groundTileSrc.Height * TileScale);
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport)
        {
            // Background scales
            float mountainScale = 7.5f;
            float hillScale = 4.0f;

            // 1. Draw Mountains (Farthest back)
            float mountainHeight = _mountainSrc.Height * mountainScale;
            float mountainY = GroundY - mountainHeight + 10; // Position relative to ground

            // Draw a single mountain texture stretched to the screen width
            spriteBatch.Draw(
                _tilesetTexture,
                new Rectangle(0, (int)mountainY, viewport.Width, (int)mountainHeight),
                _mountainSrc,
                Color.White
            );

            // 2. Draw Sand Hills (Middle layer)
            float hillHeight = _sandHillsSrc.Height * hillScale;
            float hillsY = GroundY - hillHeight;
            float hillWidth = _sandHillsSrc.Width * hillScale;

            for (int i = 0; i <= viewport.Width / hillWidth; i++)
            {
                spriteBatch.Draw(
                    _tilesetTexture,
                    new Vector2(i * hillWidth, hillsY),
                    _sandHillsSrc,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    hillScale, // Use the new hillScale
                    SpriteEffects.None,
                    0f
                );
            }

            // 3. Draw Ground (Front layer)
            for (int i = 0; i <= viewport.Width / (_groundTileSrc.Width * TileScale); i++)
            {
                spriteBatch.Draw(
                    _tilesetTexture,
                    new Vector2(i * _groundTileSrc.Width * TileScale, GroundY),
                    _groundTileSrc,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    TileScale,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }
}