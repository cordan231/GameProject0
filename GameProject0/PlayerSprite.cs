using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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

        public Vector2 Position { get; set; }

        public Direction _currentDirection;

        private CurrentState _currentState;

        private Texture2D _currentTexture;
        private int _currentFrame;
        private int _totalFrames;
        private double _frameTimer;

        private const int FRAME_WIDTH = 128;
        private const int FRAME_HEIGHT = 128;
        private const double FRAME_TIME_MS = 100;

        public float Width { get { return FRAME_WIDTH * 2.0f; } }
        public float Height { get { return FRAME_HEIGHT * 2.0f; } }

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

            switch(state)
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

            spriteBatch.Draw(_currentTexture, Position, rect, Color.White, 0f, Vector2.Zero, 2.0f, effects, 0f);

        }


    }
}
