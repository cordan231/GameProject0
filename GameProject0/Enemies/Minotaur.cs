using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameProject0.Collisions;
using System;

namespace GameProject0.Enemies
{
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

        // --- AI Variables ---
        private double _attackCooldownTimer = 0;
        private const double ATTACK_COOLDOWN = 3.0; // Set to 3 seconds per your request
        private const float CHASE_SPEED = 100f;
        private const float ATTACK_RANGE = 150f;

        public BoundingRectangle Bounds { get; private set; }
        public BoundingRectangle AttackBox { get; private set; }

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

            SetState(MinotaurState.Idle);
        }

        public void WalkIn(Vector2 spawnPosition, Vector2 targetPosition)
        {
            Position = spawnPosition;
            _walkInTargetPosition = targetPosition;
            Direction = Direction.Left;
            SetState(MinotaurState.WalkingIn);
        }

        public void Update(GameTime gameTime, PlayerSprite player)
        {
            if (IsRemoved) return;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_hurtFlashTimer > 0)
            {
                _hurtFlashTimer -= dt;
                _color = _isFlashingWhite ? Color.White : Color.Red;
                _isFlashingWhite = !_isFlashingWhite;
                if (_hurtFlashTimer <= 0) _color = Color.White;
            }

            // Player distance check
            float distance = float.MaxValue;
            if (player != null && !player.IsDead)
            {
                distance = Vector2.Distance(player.Bounds.Center, Bounds.Center);
            }

            switch (_currentState)
            {
                case MinotaurState.WalkingIn:
                    // Rule 1: Walk in
                    _position.X -= WALK_IN_SPEED * dt;
                    if (_position.X <= _walkInTargetPosition.X)
                    {
                        _position.X = _walkInTargetPosition.X;
                        SetState(MinotaurState.Walk); // Start chasing
                    }
                    Position = _position;
                    break;

                case MinotaurState.Idle: // This is the 3-second "attack cooldown" state
                    if (_attackCooldownTimer > 0)
                    {
                        _attackCooldownTimer -= dt;
                    }

                    if (_attackCooldownTimer <= 0)
                    {
                        // Cooldown is over.
                        // Rule 5: If player is close, attack again.
                        if (distance < ATTACK_RANGE)
                        {
                            SetState(MinotaurState.Attack);
                        }
                        else
                        {
                            // Player is not close, resume chasing.
                            SetState(MinotaurState.Walk);
                        }
                    }
                    // If cooldown is still active, we just stay in Idle state.
                    // Rule 4 (attacked during idle) is handled in TakeDamage.
                    break;

                case MinotaurState.Walk: // This is the "Chase" state
                    if (player == null || player.IsDead)
                    {
                        SetState(MinotaurState.Idle); // No player, go idle
                        _attackCooldownTimer = 0; // Clear cooldown
                        break;
                    }

                    // Rule 2: Check for attack
                    if (distance < ATTACK_RANGE)
                    {
                        SetState(MinotaurState.Attack);
                    }
                    else // Not in range, keep chasing
                    {
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
                    // Flinch for a short time
                    _stateTimer -= dt;
                    if (_stateTimer <= 0)
                    {
                        // After flinching, go into cooldown (Idle)
                        SetState(MinotaurState.Idle);
                        _attackCooldownTimer = ATTACK_COOLDOWN;
                    }
                    break;

                case MinotaurState.Attack:
                    // Animation completion is handled below
                    break;

                case MinotaurState.Dead:
                    // Rule 6: Animation completion is handled below
                    break;
            }

            // Animation logic
            _animationTimer += dt;
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
                        // Rule 3: After attacking, idle for 3 seconds
                        SetState(MinotaurState.Idle);
                        _attackCooldownTimer = ATTACK_COOLDOWN;
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

        public void TakeDamage(int damage)
        {
            if (_currentState == MinotaurState.Dead) return;

            Health -= damage;
            _hurtFlashTimer = 0.2;
            _isFlashingWhite = true;

            if (Health <= 0)
            {
                SetState(MinotaurState.Dead);
                return; // Stop further logic if dead
            }

            // Rule 4: If attacked during Idle (cooldown), immediately attack.
            if (_currentState == MinotaurState.Idle)
            {
                SetState(MinotaurState.Attack);
                // The cooldown will be reset to 3.0 when this new attack animation finishes (handled in Update)
            }
            // If attacked during any other non-attack state (like Walk)
            else if (_currentState != MinotaurState.Attack)
            {
                SetState(MinotaurState.Hurt);
            }
        }

        public void SetState(MinotaurState state)
        {
            if (_currentState == state) return;
            if (_currentState == MinotaurState.Dead) return;
            // Allow Hurt to interrupt Walk/Idle
            if (_currentState == MinotaurState.Hurt && _stateTimer > 0 && state != MinotaurState.Dead && state != MinotaurState.Attack) return;
            // Allow Attack to interrupt anything except Dead or Hurt
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
                    // _stateTimer is not used here; _attackCooldownTimer is
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
                    _stateTimer = _animationFrameTime * _totalFrames * 2; // Short flinch duration
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