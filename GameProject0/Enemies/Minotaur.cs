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
        Dead,
        Attack
    }

    public class Minotaur
    {
        private Texture2D _idleTexture;
        private Texture2D _walkTexture;
        private Texture2D _hurtTexture;
        private Texture2D _deadTexture;
        private Texture2D _attackTexture;
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

        public int Health { get; set; } = 3;
        public bool IsRemoved { get; private set; } = false;
        public bool IsAttackHitboxActive { get; private set; } = false;

        public float Scale { get; set; } = 2.0f;

        private int FRAME_WIDTH = 128;
        private int FRAME_HEIGHT = 128;
        private double _animationFrameTime = 0.1;
        private const double IDLE_DURATION = 1.5;

        private double _attackTimer = 4.0;

        public BoundingRectangle Bounds { get; private set; }
        public BoundingRectangle AttackBox { get; private set; }

        public MinotaurState CurrentState => _currentState;

        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;
                UpdateBounds();
                UpdateAttackBox();
            }
        }

        public Direction Direction
        {
            get => _direction;
            set => _direction = value;
        }

        public float Width => FRAME_WIDTH * Scale;
        public float Height => FRAME_HEIGHT * Scale;

        public void LoadContent(ContentManager content)
        {
            _walkTexture = content.Load<Texture2D>("minotaur_walk");
            _hurtTexture = content.Load<Texture2D>("minotaur_hurt");
            _deadTexture = content.Load<Texture2D>("minotaur_dead");
            _idleTexture = content.Load<Texture2D>("minotaur_idle");
            _attackTexture = content.Load<Texture2D>("minotaur_attack");

            SetState(MinotaurState.Walk);
        }

        public void Update(GameTime gameTime, int screenWidth)
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

            if (_currentState != MinotaurState.Walk && _currentState != MinotaurState.Attack && _currentState != MinotaurState.Dead)
            {
                _stateTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                if (_stateTimer <= 0)
                {
                    SetState(MinotaurState.Walk);
                }
            }

            if (_currentState == MinotaurState.Walk)
            {
                _attackTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                if (_attackTimer <= 0)
                {
                    SetState(MinotaurState.Attack);
                    _attackTimer = 4.0;
                }

            }

            _animationTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (_animationTimer > _animationFrameTime)
            {
                _currentFrame++;
                if (_currentFrame >= _totalFrames)
                {
                    if (_currentState == MinotaurState.Dead)
                    {
                        IsRemoved = true;
                        _currentFrame = _totalFrames - 1;
                    }
                    else if (_currentState == MinotaurState.Attack)
                    {
                        SetState(MinotaurState.Walk);
                    }
                    else
                    {
                        _currentFrame = 0;
                    }
                }
                _animationTimer -= _animationFrameTime;
            }

            IsAttackHitboxActive = (_currentState == MinotaurState.Attack && _currentFrame >= 3);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsRemoved) return;

            var effects = (_direction == Direction.Left) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
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

        public void SetState(MinotaurState state)
        {
            if (_currentState == state) return;
            if (_currentState == MinotaurState.Dead) return;
            if (_currentState == MinotaurState.Hurt && _stateTimer > 0 && state != MinotaurState.Dead) return;

            _currentState = state;
            _currentFrame = 0;
            _animationTimer = 0;

            switch (state)
            {
                case MinotaurState.Idle:
                    _currentTexture = _idleTexture;
                    _totalFrames = 10;
                    _stateTimer = IDLE_DURATION;
                    _animationFrameTime = 0.1;
                    break;
                case MinotaurState.Walk:
                    _currentTexture = _walkTexture;
                    _totalFrames = 12;
                    _color = Color.White;
                    _animationFrameTime = 0.1;
                    break;
                case MinotaurState.Hurt:
                    _currentTexture = _hurtTexture;
                    _totalFrames = 3;
                    _stateTimer = _animationFrameTime * _totalFrames * 2;
                    _animationFrameTime = 0.1;
                    break;
                case MinotaurState.Dead:
                    _currentTexture = _deadTexture;
                    _totalFrames = 5;
                    _animationFrameTime = 0.1;
                    break;
                case MinotaurState.Attack:
                    _currentTexture = _attackTexture;
                    _totalFrames = 5;
                    _animationFrameTime = 0.2;
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

        private void UpdateAttackBox()
        {
            float attackWidth = 100 * Scale;
            float attackHeight = 50 * Scale;
            float yOffset = 50 * Scale;

            if (Direction == Direction.Right)
            {
                float xOffset = 60 * Scale;
                AttackBox = new BoundingRectangle(Position.X + xOffset, Position.Y + yOffset, attackWidth, attackHeight);
            }
            else
            {
                float xOffset = (FRAME_WIDTH * Scale) - (60 * Scale) - attackWidth;
                AttackBox = new BoundingRectangle(Position.X + xOffset, Position.Y + yOffset, attackWidth, attackHeight);
            }
        }
    }
}