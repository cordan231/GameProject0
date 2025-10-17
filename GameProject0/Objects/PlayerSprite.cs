using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameProject0.Collisions;

namespace GameProject0
{
    public enum CurrentState
    {
        Idle,
        Running,
        Attacking,
        Rolling,
        Hurt,
        Dead
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
        private Texture2D _attackTexture;
        private Texture2D _rollTexture;
        private Texture2D _hurtTexture;
        private Texture2D _deathTexture;


        private Vector2 _position;
        public Direction _currentDirection;
        private CurrentState _currentState;
        public CurrentState CurrentPlayerState => _currentState;
        private Texture2D _currentTexture;
        private int _currentFrame;
        private int _totalFrames;
        private double _frameTimer;

        private double _stateTimer;
        private double _hurtCooldown;
        public bool IsAttacking => _currentState == CurrentState.Attacking;
        public BoundingRectangle AttackBox { get; private set; }

        public float Scale { get; set; } = 2.0f;

        private const int FRAME_WIDTH = 128;
        private const int FRAME_HEIGHT = 128;
        private const double FRAME_TIME_MS = 100;

        public int Health { get; private set; } = 3;
        public bool IsInvincible { get; private set; } = false;
        public bool IsDead { get; private set; } = false;

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
                UpdateAttackBox();
            }
        }

        public float Width => FRAME_WIDTH * Scale;
        public float Height => FRAME_HEIGHT * Scale;

        public void LoadContent(ContentManager content)
        {
            _idleTexture = content.Load<Texture2D>("Stop_Running");
            _runningTexture = content.Load<Texture2D>("Running");
            _attackTexture = content.Load<Texture2D>("player_punch1");
            _rollTexture = content.Load<Texture2D>("player_roll");
            _hurtTexture = content.Load<Texture2D>("player_hurt");
            _deathTexture = content.Load<Texture2D>("player_death");

            _currentState = CurrentState.Idle;
            _currentTexture = _idleTexture;
            _totalFrames = 5;
            _currentFrame = 0;
            _frameTimer = 0;
            _currentDirection = Direction.Right;
        }

        public void Update(GameTime gameTime)
        {
            if (_hurtCooldown > 0)
            {
                _hurtCooldown -= gameTime.ElapsedGameTime.TotalSeconds;
            }

            _frameTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_frameTimer > FRAME_TIME_MS)
            {
                _currentFrame++;
                if (_currentFrame >= _totalFrames)
                {
                    if (_currentState == CurrentState.Attacking || _currentState == CurrentState.Rolling || _currentState == CurrentState.Hurt)
                    {
                        SetState(CurrentState.Idle);
                    }
                    if (_currentState == CurrentState.Dead)
                    {
                        IsDead = true;
                        _currentFrame = _totalFrames - 1;
                    }
                    else
                    {
                        _currentFrame = 0;
                    }
                }
                _frameTimer = 0;
            }

            if (_currentState == CurrentState.Rolling)
            {
                IsInvincible = (_currentFrame >= 1 && _currentFrame <= 6);
                float rollSpeed = 200f;
                float moveDirection = (_currentDirection == Direction.Right) ? 1 : -1;
                _position.X += moveDirection * rollSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            else
            {
                IsInvincible = false;
            }

            if (_stateTimer > 0)
            {
                _stateTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                if (_stateTimer <= 0)
                {
                    SetState(CurrentState.Idle);
                }
            }
            UpdateAttackBox();
        }

        public void Attack()
        {
            if (_currentState == CurrentState.Idle || _currentState == CurrentState.Running)
            {
                SetState(CurrentState.Attacking);
            }
        }

        public void Roll()
        {
            if (_currentState == CurrentState.Idle || _currentState == CurrentState.Running)
            {
                SetState(CurrentState.Rolling);
            }
        }

        public void TakeDamage(Direction hitDirection)
        {
            if (IsInvincible || _currentState == CurrentState.Hurt || _currentState == CurrentState.Dead || _hurtCooldown > 0) return;

            Health--;
            _hurtCooldown = 1.0; // 1 second of invincibility after getting hit
            Game1.Instance.BloodSplatters.Splatter(Bounds.Center);

            if (Health <= 0)
            {
                SetState(CurrentState.Dead);
            }
            else
            {
                SetState(CurrentState.Hurt);
                float knockback = 150f;
                _position.X += (hitDirection == Direction.Right) ? knockback : -knockback;
            }
        }


        private void UpdateAttackBox()
        {
            float attackWidth = 60 * Scale;
            float attackHeight = 30 * Scale;
            float yOffset = 40 * Scale;

            if (_currentDirection == Direction.Right)
            {
                float xOffset = 50 * Scale;
                AttackBox = new BoundingRectangle(Position.X + xOffset, Position.Y + yOffset, attackWidth, attackHeight);
            }
            else
            {
                float xOffset = (FRAME_WIDTH * Scale) - (50 * Scale) - attackWidth;
                AttackBox = new BoundingRectangle(Position.X + xOffset, Position.Y + yOffset, attackWidth, attackHeight);
            }
        }

        public void SetState(CurrentState state)
        {
            if (_currentState == state || _currentState == CurrentState.Dead) return;

            _currentState = state;
            _currentFrame = 0;
            _frameTimer = 0;
            _stateTimer = 0;

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
                case CurrentState.Attacking:
                    _currentTexture = _attackTexture;
                    _totalFrames = 4;
                    _stateTimer = (_totalFrames * FRAME_TIME_MS) / 1000.0;
                    break;
                case CurrentState.Rolling:
                    _currentTexture = _rollTexture;
                    _totalFrames = 9;
                    _stateTimer = (_totalFrames * FRAME_TIME_MS) / 1000.0;
                    break;
                case CurrentState.Hurt:
                    _currentTexture = _hurtTexture;
                    _totalFrames = 3;
                    _stateTimer = (_totalFrames * FRAME_TIME_MS) / 1000.0 * 2;
                    break;
                case CurrentState.Dead:
                    _currentTexture = _deathTexture;
                    _totalFrames = 5;
                    break;
            }
        }

        public void SetDirection(Direction direction)
        {
            if (_currentState != CurrentState.Attacking && _currentState != CurrentState.Rolling && _currentState != CurrentState.Hurt)
            {
                _currentDirection = direction;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsDead && _currentFrame >= _totalFrames - 1) return;

            var effects = (_currentDirection == Direction.Left) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Rectangle rect = new Rectangle(_currentFrame * FRAME_WIDTH, 0, FRAME_WIDTH, FRAME_HEIGHT);

            spriteBatch.Draw(_currentTexture, Position, rect, Color.White, 0f, Vector2.Zero, Scale, effects, 0f);
        }
    }
}

