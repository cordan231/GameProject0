using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameProject0.Collisions;
using System;

namespace GameProject0.Enemies
{
    public enum MinotaurState
    {
        Idle,
        Walk,
        Hurt,
        Dead
    }

    public class Minotaur
    {
        private Texture2D _idleTexture;
        private Texture2D _walkTexture;
        private Texture2D _hurtTexture;
        private Texture2D _deadTexture;
        private Texture2D _currentTexture;

        private Vector2 _position;
        private MinotaurState _currentState;
        private Direction _direction;

        private int _currentFrame;
        private int _totalFrames;
        private double _animationTimer;
        private double _stateTimer;

        private Color _color = Color.White;
        private double _hurtFlashTimer;
        private bool _isFlashingWhite = false;

        public int Health { get; set; } = 2;
        public bool IsRemoved { get; private set; } = false;

        public float Scale { get; set; } = 2.0f;

        private const int FRAME_WIDTH = 96;
        private const int FRAME_HEIGHT = 96;
        private const double ANIMATION_FRAME_TIME = 0.1;
        private const double IDLE_DURATION = 1.5;

        public BoundingRectangle Bounds { get; private set; }

        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;
                UpdateBounds();
            }
        }

        public float Width => FRAME_WIDTH * Scale;
        public float Height => FRAME_HEIGHT * Scale;

        public void LoadContent(ContentManager content)
        {
            _idleTexture = content.Load<Texture2D>("minotaur_idle");
            _walkTexture = content.Load<Texture2D>("minotaur_walk");
            _hurtTexture = content.Load<Texture2D>("minotaur_hurt");
            _deadTexture = content.Load<Texture2D>("minotaur_dead");

            SetState(MinotaurState.Walk);
            _direction = Direction.Left;
        }

        public void Update(GameTime gameTime)
        {
            if (IsRemoved) return;

            if (_hurtFlashTimer > 0)
            {
                _hurtFlashTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                _color = _isFlashingWhite ? Color.White : Color.Red;
                _isFlashingWhite = !_isFlashingWhite;
                if (_hurtFlashTimer <= 0)
                {
                    _color = Color.White;
                }
            }

            if (_currentState == MinotaurState.Hurt || _currentState == MinotaurState.Idle)
            {
                _stateTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                if (_stateTimer <= 0)
                {
                    SetState(MinotaurState.Walk);
                }
            }

            if (_currentState == MinotaurState.Walk)
            {
                float speed = 100f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_direction == Direction.Left)
                {
                    _position.X -= speed;
                    if (_position.X < 0)
                    {
                        _position.X = 0;
                        _direction = Direction.Right;
                        SetState(MinotaurState.Idle);
                    }
                }
                else
                {
                    _position.X += speed;
                    if (_position.X > 800 - Width)
                    {
                        _position.X = 800 - Width;
                        _direction = Direction.Left;
                        SetState(MinotaurState.Idle);
                    }
                }
            }

            _animationTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (_animationTimer > ANIMATION_FRAME_TIME)
            {
                _currentFrame++;
                if (_currentFrame >= _totalFrames)
                {
                    if (_currentState == MinotaurState.Dead)
                    {
                        IsRemoved = true;
                        _currentFrame = _totalFrames - 1;
                    }
                    else
                    {
                        _currentFrame = 0;
                    }
                }
                _animationTimer -= ANIMATION_FRAME_TIME;
            }

            Position = _position;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsRemoved) return;

            var effects = (_direction == Direction.Right) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var sourceRect = new Rectangle(_currentFrame * FRAME_WIDTH, 0, FRAME_WIDTH, FRAME_HEIGHT);
            spriteBatch.Draw(_currentTexture, Position, sourceRect, _color, 0f, Vector2.Zero, Scale, effects, 0f);
        }

        public void TakeDamage()
        {
            if (_currentState == MinotaurState.Dead) return;

            Health--;
            _hurtFlashTimer = 0.2;
            _isFlashingWhite = true;

            if (Health <= 0)
            {
                SetState(MinotaurState.Dead);
            }
            else
            {
                SetState(MinotaurState.Hurt);
            }
        }

        private void SetState(MinotaurState state)
        {
            if ((_currentState == MinotaurState.Hurt && _stateTimer > 0) || _currentState == MinotaurState.Dead) return;
            if (_currentState == state) return;

            _currentState = state;
            _currentFrame = 0;
            _animationTimer = 0;

            switch (state)
            {
                case MinotaurState.Idle:
                    _currentTexture = _idleTexture;
                    _totalFrames = 6;
                    _stateTimer = IDLE_DURATION;
                    break;
                case MinotaurState.Walk:
                    _currentTexture = _walkTexture;
                    _totalFrames = 8;
                    _color = Color.White;
                    break;
                case MinotaurState.Hurt:
                    _currentTexture = _hurtTexture;
                    _totalFrames = 2;
                    _stateTimer = ANIMATION_FRAME_TIME * _totalFrames * 2;
                    break;
                case MinotaurState.Dead:
                    _currentTexture = _deadTexture;
                    _totalFrames = 6;
                    break;
            }
        }

        private void UpdateBounds()
        {
            float boxWidth = (FRAME_WIDTH * Scale) * 0.4f;
            float boxHeight = (FRAME_HEIGHT * Scale) * 0.7f;
            float xOffset = (Width - boxWidth) / 2;
            float yOffset = (Height - boxHeight);
            Bounds = new BoundingRectangle(
                new Vector2(_position.X + xOffset, _position.Y + yOffset),
                boxWidth,
                boxHeight
            );
        }
    }
}

