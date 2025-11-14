using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameProject0.Collisions;
using System;
using Microsoft.Xna.Framework.Audio;

namespace GameProject0.Enemies
{
    // Enemy behavior states
    public enum MinotaurState
    {
        WalkingIn,
        Idle,
        Walk,
        Hurt,
        Dead,
        Attack
    }

    public class Minotaur
    {
        // Textures for different states
        private Texture2D _idleTexture;
        private Texture2D _walkTexture;
        private Texture2D _hurtTexture;
        private Texture2D _deadTexture;
        private Texture2D _attackTexture;
        private Texture2D _currentTexture;

        // Sound effects
        private SoundEffect _enemyHurtSound;

        private Vector2 _position;
        private MinotaurState _currentState;
        private Direction _direction;

        // Animation variables
        private int _currentFrame;
        private int _totalFrames;
        private double _animationTimer;
        private double _stateTimer;

        // Flashing effect when damaged
        private Color _color = Color.White;
        private double _hurtFlashTimer;
        private bool _isFlashingWhite = false;

        public int Health { get; set; } = 3;
        public bool IsRemoved { get; private set; } = false;
        // Flag for when the attack frame is active (to deal damage)
        public bool IsAttackHitboxActive { get; private set; } = false;

        public float Scale { get; set; } = 2.0f;

        private int FRAME_WIDTH = 128;
        private int FRAME_HEIGHT = 128;
        private double _animationFrameTime = 0.1;

        // AI Variables
        private double _attackCooldownTimer = 0;
        private const double ATTACK_COOLDOWN = 3.0;
        private const float CHASE_SPEED = 100f;
        private const float ATTACK_RANGE = 150f;

        public BoundingRectangle Bounds { get; private set; }
        public BoundingRectangle AttackBox { get; private set; }

        // Spawn animation target
        private Vector2 _walkInTargetPosition;
        private const float WALK_IN_SPEED = 150f;

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

            _enemyHurtSound = content.Load<SoundEffect>("enemy-hurt");

            SetState(MinotaurState.Idle);
        }

        // Starts the spawn-in animation
        public void WalkIn(Vector2 spawnPosition, Vector2 targetPosition, Direction direction)
        {
            Position = spawnPosition;
            _walkInTargetPosition = targetPosition;
            Direction = direction;
            SetState(MinotaurState.WalkingIn);
        }

        // Update Enemy Logic & AI
        public void Update(GameTime gameTime, PlayerSprite player)
        {
            if (IsRemoved) return;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Handle damage flash
            if (_hurtFlashTimer > 0)
            {
                _hurtFlashTimer -= dt;
                _color = _isFlashingWhite ? Color.White : Color.Red;
                _isFlashingWhite = !_isFlashingWhite;
                if (_hurtFlashTimer <= 0) _color = Color.White;
            }

            float distance = float.MaxValue;
            if (player != null && !player.IsDead)
            {
                distance = Vector2.Distance(player.Bounds.Center, Bounds.Center);
            }

            switch (_currentState)
            {
                case MinotaurState.WalkingIn:
                    if (Direction == Direction.Left)
                    {
                        _position.X -= WALK_IN_SPEED * dt;
                        if (_position.X <= _walkInTargetPosition.X)
                        {
                            _position.X = _walkInTargetPosition.X;
                            SetState(MinotaurState.Walk);
                        }
                    }
                    else
                    {
                        _position.X += WALK_IN_SPEED * dt;
                        if (_position.X >= _walkInTargetPosition.X)
                        {
                            _position.X = _walkInTargetPosition.X;
                            SetState(MinotaurState.Walk);
                        }
                    }
                    Position = _position;
                    break;

                case MinotaurState.Idle:
                    if (_attackCooldownTimer > 0)
                    {
                        _attackCooldownTimer -= dt;
                    }

                    if (_attackCooldownTimer <= 0)
                    {
                        // If player is close, attack; otherwise chase
                        if (distance < ATTACK_RANGE)
                        {
                            SetState(MinotaurState.Attack);
                        }
                        else
                        {
                            SetState(MinotaurState.Walk);
                        }
                    }
                    break;

                case MinotaurState.Walk:
                    if (player == null || player.IsDead)
                    {
                        SetState(MinotaurState.Idle);
                        _attackCooldownTimer = 0;
                        break;
                    }

                    if (distance < ATTACK_RANGE)
                    {
                        // Face player and attack
                        Direction = (player.Bounds.Center.X < Bounds.Center.X) ? Direction.Left : Direction.Right;
                        SetState(MinotaurState.Attack);
                    }
                    else
                    {
                        // Move towards player
                        if (player.Bounds.Center.X < Bounds.Center.X)
                        {
                            Direction = Direction.Left;
                            _position.X -= CHASE_SPEED * dt;
                        }
                        else
                        {
                            Direction = Direction.Right;
                            _position.X += CHASE_SPEED * dt;
                        }
                        Position = _position;
                    }
                    break;

                case MinotaurState.Hurt:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0)
                    {
                        SetState(MinotaurState.Idle);
                        _attackCooldownTimer = ATTACK_COOLDOWN;
                    }
                    break;
            }

            // Animation Loop
            _animationTimer += dt;

            // Determine frame duration
            double currentFrameDuration = _animationFrameTime;
            if (_currentState == MinotaurState.Attack && _currentFrame >= 3)
            {
                currentFrameDuration = 0.15;
            }


            if (_animationTimer > currentFrameDuration)
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
                        SetState(MinotaurState.Idle);
                        _attackCooldownTimer = ATTACK_COOLDOWN;
                    }
                    else
                    {
                        _currentFrame = 0;
                    }
                }
                _animationTimer -= currentFrameDuration;
            }

            // Active hitbox only during specific frames of attack
            IsAttackHitboxActive = (_currentState == MinotaurState.Attack && _currentFrame >= 3);
        }

        // Draw sprite
        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsRemoved) return;

            var effects = (_direction == Direction.Left) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var sourceRect = new Rectangle(_currentFrame * FRAME_WIDTH, 0, FRAME_WIDTH, FRAME_HEIGHT);
            spriteBatch.Draw(_currentTexture, Position, sourceRect, _color, 0f, Vector2.Zero, Scale, effects, 0f);
        }

        // Handle damage taken
        public void TakeDamage(int damage, PlayerSprite player)
        {
            if (_currentState == MinotaurState.Dead) return;

            Health -= damage;
            _enemyHurtSound.Play();
            _hurtFlashTimer = 0.2;
            _isFlashingWhite = true;

            if (Health <= 0)
            {
                SetState(MinotaurState.Dead);
                return;
            }

            // If attacked while idle, counter-attack
            if (_currentState == MinotaurState.Idle)
            {
                if (player != null)
                {
                    Direction = (player.Bounds.Center.X < Bounds.Center.X) ? Direction.Left : Direction.Right;
                }
                SetState(MinotaurState.Attack);
            }
            else if (_currentState != MinotaurState.Attack)
            {
                SetState(MinotaurState.Hurt);
            }
        }

        // Set State and load textures
        public void SetState(MinotaurState state)
        {
            if (_currentState == state) return;
            if (_currentState == MinotaurState.Dead) return;
            if (_currentState == MinotaurState.Hurt && _stateTimer > 0 && state != MinotaurState.Dead && state != MinotaurState.Attack) return;
            if (state == MinotaurState.Attack && (_currentState == MinotaurState.Hurt || _currentState == MinotaurState.Dead)) return;


            _currentState = state;
            _currentFrame = 0;
            _animationTimer = 0;

            switch (state)
            {
                case MinotaurState.WalkingIn:
                    _currentTexture = _walkTexture;
                    _totalFrames = 12;
                    _color = Color.White;
                    _animationFrameTime = 0.1;
                    break;
                case MinotaurState.Idle:
                    _currentTexture = _idleTexture;
                    _totalFrames = 10;
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
                    _animationFrameTime = 0.1;
                    _stateTimer = _animationFrameTime * _totalFrames * 2;
                    break;
                case MinotaurState.Dead:
                    _currentTexture = _deadTexture;
                    _totalFrames = 5;
                    _animationFrameTime = 0.1;
                    break;
                case MinotaurState.Attack:
                    _currentTexture = _attackTexture;
                    _totalFrames = 5;
                    _animationFrameTime = 0.25;
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