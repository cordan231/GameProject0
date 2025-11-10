using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameProject0.Collisions;

namespace GameProject0.Objects
{
    public class Arrow
    {
        private Texture2D _texture;
        public Vector2 Position { get; set; }
        public BoundingRectangle Bounds { get; private set; }
        private Vector2 _velocity;
        public Direction Direction { get; private set; }
        public bool IsRemoved { get; set; } = false;

        private const float SPEED = 800f;
        private const float SCALE = 2.0f;

        public Arrow(Vector2 position, Direction direction)
        {
            Position = position;
            Direction = direction;
            float directionModifier = (direction == Direction.Right) ? 1 : -1;
            _velocity = new Vector2(SPEED * directionModifier, 0);
        }

        public void LoadContent(ContentManager content)
        {
            _texture = content.Load<Texture2D>("arrow_sprite");
            UpdateBounds();
        }

        private void UpdateBounds()
        {
            float width = _texture.Width * SCALE;
            float height = _texture.Height * SCALE;
            Bounds = new BoundingRectangle(Position.X, Position.Y, width, height);
        }

        public void Update(GameTime gameTime, Viewport viewport)
        {
            if (IsRemoved) return;

            Position += _velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            UpdateBounds();

            if (Position.X > viewport.Width || Position.X < -Bounds.Width)
            {
                IsRemoved = true;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsRemoved) return;

            var effects = (Direction == Direction.Left) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            spriteBatch.Draw(_texture, Position, null, Color.White, 0f, Vector2.Zero, SCALE, effects, 0f);
        }

        public bool CollidesWith(PlayerSprite player)
        {
            return !IsRemoved && Bounds.CollidesWith(player.Bounds);
        }
    }
}