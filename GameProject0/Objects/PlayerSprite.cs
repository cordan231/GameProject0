using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameProject0.Collisions;

namespace GameProject0
{
    public enum CurrentState
    {
        Idle,
        Running
    }

    public enum Direction
    {
        Left,
        Right
    }

    public class PlayerSprite
    {
        private Texture2D _idleTexture;
        private Texture2D _runningTexture;
        private Vector2 _position;
        public Direction _currentDirection;
        private CurrentState _currentState;
        private Texture2D _currentTexture;
        private int _currentFrame;
        private int _totalFrames;
        private double _frameTimer;

        public float Scale { get; set; } = 2.0f;

        private const int FRAME_WIDTH = 128;
        private const int FRAME_HEIGHT = 128;
        private const double FRAME_TIME_MS = 100;

        public BoundingRectangle Bounds { get; private set; }

        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;

                float boxWidth = (FRAME_WIDTH * Scale) * 0.35f;
                float boxHeight = (FRAME_HEIGHT * Scale) * 0.6f;

                float xOffset = (Width - boxWidth) / 2;
                float yOffset = (FRAME_HEIGHT * Scale) * 0.4f;

                Bounds = new BoundingRectangle(
                    new Vector2(_position.X + xOffset, _position.Y + yOffset),
                    boxWidth,
                    boxHeight
                );
            }
        }

        public float Width => FRAME_WIDTH * Scale;
        public float Height => FRAME_HEIGHT * Scale;

        public void LoadContent(ContentManager content)
        {
            _idleTexture = content.Load<Texture2D>("Stop_Running");
            _runningTexture = content.Load<Texture2D>("Running");
            _currentState = CurrentState.Idle;
            _currentTexture = _idleTexture;
            _totalFrames = 5;
            _currentFrame = 0;
            _frameTimer = 0;
            _currentDirection = Direction.Right;
        }

        public void Update(GameTime gameTime)
        {
            _frameTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_frameTimer > FRAME_TIME_MS)
            {
                _currentFrame = (_currentFrame + 1) % _totalFrames;
                _frameTimer = 0;
            }
        }

        public void SetState(CurrentState state)
        {
            if (_currentState == state) return;

            _currentState = state;
            _currentFrame = 0;
            _frameTimer = 0;

            switch (state)
            {
                case CurrentState.Idle:
                    _currentTexture = _idleTexture;
                    _totalFrames = 5;
                    break;
                case CurrentState.Running:
                    _currentTexture = _runningTexture;
                    _totalFrames = 12;
                    break;
            }
        }

        public void SetDirection(Direction direction)
        {
            _currentDirection = direction;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var effects = (_currentDirection == Direction.Left) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Rectangle rect = new Rectangle(_currentFrame * FRAME_WIDTH, 0, FRAME_WIDTH, FRAME_HEIGHT);

            spriteBatch.Draw(_currentTexture, Position, rect, Color.White, 0f, Vector2.Zero, Scale, effects, 0f);
        }
    }
}