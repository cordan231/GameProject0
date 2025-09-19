using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameProject0.Collisions;

namespace GameProject0
{
    public class Coin
    {
        private Texture2D _texture;
        public Vector2 Position { get; set; }
        private BoundingCircle _bounds;

        private int _currentFrame;
        private int _totalFrames;
        private double _frameTimer;
        private const int FRAME_WIDTH = 64;
        private const int FRAME_HEIGHT = 64;
        private const double FRAME_TIME_MS = 30;
        private const float COIN_SCALE = 0.75f;

        public void LoadContent(ContentManager content)
        {
            _texture = content.Load<Texture2D>("coin-64x64");

            float radius = (FRAME_WIDTH * COIN_SCALE) / 2;
            Vector2 center = new Vector2(Position.X + radius, Position.Y + radius);
            _bounds = new BoundingCircle(center, radius);

            _totalFrames = 25;
            _currentFrame = 0;
            _frameTimer = 0;
        }

        public void Update(GameTime gameTime)
        {
            _frameTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_frameTimer > FRAME_TIME_MS)
            {
                _currentFrame = (_currentFrame + 1) % _totalFrames;
                _frameTimer = 0;
            }

            Position = new Vector2(Position.X, Position.Y + 100f * (float)gameTime.ElapsedGameTime.TotalSeconds);

            _bounds.Center = new Vector2(Position.X + _bounds.Radius, Position.Y + _bounds.Radius);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var sourceRect = new Rectangle(_currentFrame * FRAME_WIDTH, 0, FRAME_WIDTH, FRAME_HEIGHT);
            spriteBatch.Draw(_texture, Position, sourceRect, Color.White, 0f, Vector2.Zero, COIN_SCALE, SpriteEffects.None, 0f);
        }

        public bool CollidesWith(PlayerSprite player)
        {
            return _bounds.CollidesWith(player.Bounds);
        }
    }
}