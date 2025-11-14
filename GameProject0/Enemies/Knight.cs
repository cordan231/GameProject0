using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameProject0.Collisions;
using System;
using Microsoft.Xna.Framework.Audio;

namespace GameProject0.Enemies
{
    public enum KnightState
    {
        Idle,
        Walk,
        Run,
        RunAttack,
        ComboAttack1,
        ComboAttack2,
        ComboAttack3,
        Defend,
        Jump,
        Hurt,
        Dead
    }

    public class Knight
    {
        // Textures
        private Texture2D _idleTexture;
        private Texture2D _walkTexture;
        private Texture2D _runTexture;
        private Texture2D _runAttackTexture;
        private Texture2D _attack1Texture;
        private Texture2D _attack2Texture;
        private Texture2D _attack3Texture;
        private Texture2D _defendTexture;
        private Texture2D _jumpTexture;
        private Texture2D _hurtTexture;
        private Texture2D _deadTexture;
        private Texture2D _currentTexture;

        private SoundEffect _enemyHurtSound;

        private Vector2 _position;
        private KnightState _currentState;
        private Direction _direction;

        // Animation
        private int _currentFrame;
        private int _totalFrames;
        private double _animationTimer;
        private double _animationFrameTime = 0.1;

        // Stats
        public int Health { get; private set; } = 10;
        public bool IsRemoved { get; private set; } = false;
        public float Scale { get; set; } = 2.0f;
        private const int FRAME_WIDTH = 128;
        private const int FRAME_HEIGHT = 128;

        // State Management
        private Color _color = Color.White;
        private double _hurtFlashTimer;
        private bool _isFlashingWhite = false;
        private bool _isInvulnerable = false;
        private double _stateTimer;

        // AI Logic
        private const float CHASE_SPEED = 120f;
        private const float RUN_SPEED = 300f;
        private const float JUMP_SPEED = 250f;
        private const float ATTACK_RANGE = 120f;
        private const float RUN_ATTACK_RANGE = 300f;
        private const float PLAYER_CLOSE_RANGE = 200f;

        // Vulnerability Windows
        private bool _isVulnerableWindow = false;
        private int _hitsTakenInWindow = 0;
        private double _vulnerabilityTimer = 0;
        private int _hitsToTriggerDefend = 0;

        public BoundingRectangle Bounds { get; private set; }
        public BoundingRectangle AttackBox { get; private set; }
        public bool IsAttackHitboxActive { get; private set; } = false;

        public KnightState CurrentState => _currentState;
        public Vector2 Position
        {
            get => _position;
            set { _position = value; UpdateBounds(); UpdateAttackBox(); }
        }
        public Direction Direction
        {
            get => _direction;
            set { _direction = value; }
        }
        public float Width => FRAME_WIDTH * Scale;
        public float Height => FRAME_HEIGHT * Scale;

        public void LoadContent(ContentManager content)
        {
            _idleTexture = content.Load<Texture2D>("knight_idle");
            _walkTexture = content.Load<Texture2D>("knight_walk");
            _runTexture = content.Load<Texture2D>("knight_run");
            _runAttackTexture = content.Load<Texture2D>("knight_run_attack");
            _attack1Texture = content.Load<Texture2D>("knight_attack1");
            _attack2Texture = content.Load<Texture2D>("knight_attack2");
            _attack3Texture = content.Load<Texture2D>("knight_attack3");
            _defendTexture = content.Load<Texture2D>("knight_defend");
            _jumpTexture = content.Load<Texture2D>("knight_jump");
            _hurtTexture = content.Load<Texture2D>("knight_hurt");
            _deadTexture = content.Load<Texture2D>("knight_dead");

            _enemyHurtSound = content.Load<SoundEffect>("enemy-hurt");

            SetState(KnightState.Idle);
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

            if (_currentState == KnightState.Dead)
            {
                Animate(dt);
                if (_currentFrame == _totalFrames - 1) IsRemoved = true;
                return;
            }

            // Handle Vulnerability Window Logic
            if (_isVulnerableWindow)
            {
                _vulnerabilityTimer -= dt;
                if (_vulnerabilityTimer <= 0)
                {
                    _isVulnerableWindow = false;
                    _hitsTakenInWindow = 0;
                    SetState(KnightState.Walk); // Back to chase
                }
            }

            // AI State Machine
            float distance = float.MaxValue;
            if (player != null && !player.IsDead)
            {
                distance = Vector2.Distance(player.Bounds.Center, Bounds.Center);
                if (_currentState != KnightState.Run && _currentState != KnightState.RunAttack &&
                    _currentState != KnightState.ComboAttack1 && _currentState != KnightState.ComboAttack2 &&
                    _currentState != KnightState.ComboAttack3 && _currentState != KnightState.Defend &&
                    _currentState != KnightState.Jump && _currentState != KnightState.Hurt)
                {
                    Direction = (player.Bounds.Center.X < Bounds.Center.X) ? Direction.Left : Direction.Right;
                }
            }

            switch (_currentState)
            {
                case KnightState.Idle:
                    if (!_isVulnerableWindow)
                    {
                        _stateTimer -= dt;
                        if (_stateTimer <= 0) SetState(KnightState.Walk);
                    }
                    break;

                case KnightState.Walk:
                    if (distance > RUN_ATTACK_RANGE)
                    {
                        SetState(KnightState.Run);
                    }
                    else if (distance <= ATTACK_RANGE)
                    {
                        SetState(KnightState.ComboAttack1);
                    }
                    else
                    {
                        _position.X += (Direction == Direction.Right ? CHASE_SPEED : -CHASE_SPEED) * dt;
                    }
                    break;

                case KnightState.Run:
                    if (distance <= RUN_ATTACK_RANGE - 50)
                    {
                        SetState(KnightState.RunAttack);
                    }
                    else
                    {
                        _position.X += (Direction == Direction.Right ? RUN_SPEED : -RUN_SPEED) * dt;
                    }
                    break;

                case KnightState.RunAttack:
                    if (AnimationFinished())
                    {
                        StartVulnerabilityWindow(2.0, 1);
                    }
                    break;

                case KnightState.ComboAttack1:
                    if (AnimationFinished()) SetState(KnightState.ComboAttack2);
                    break;
                case KnightState.ComboAttack2:
                    if (AnimationFinished()) SetState(KnightState.ComboAttack3);
                    break;
                case KnightState.ComboAttack3:
                    if (AnimationFinished())
                    {
                        StartVulnerabilityWindow(5.0, 2);
                    }
                    break;

                case KnightState.Defend:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0)
                    {
                        if (distance <= PLAYER_CLOSE_RANGE)
                        {
                            SetState(KnightState.Jump);
                        }
                        else
                        {
                            SetState(KnightState.Walk);
                        }
                    }
                    break;

                case KnightState.Jump:
                    // Jump away from player
                    float jumpDir = (Direction == Direction.Right) ? -1 : 1;
                    _position.X += jumpDir * JUMP_SPEED * dt;
                    if (AnimationFinished())
                    {
                        SetState(KnightState.Walk);
                    }
                    break;

                case KnightState.Hurt:
                    if (AnimationFinished())
                    {
                        // Return to vulnerability window
                        _isVulnerableWindow = true;
                        SetState(KnightState.Idle);
                    }
                    break;
            }

            Position = _position;
            Animate(dt);
            UpdateAttackBox();
        }

        private void StartVulnerabilityWindow(double duration, int hitThreshold)
        {
            _isVulnerableWindow = true;
            _vulnerabilityTimer = duration;
            _hitsTakenInWindow = 0;
            _hitsToTriggerDefend = hitThreshold;
            SetState(KnightState.Idle);
        }

        private void Animate(float dt)
        {
            _animationTimer += dt;
            if (_animationTimer > _animationFrameTime)
            {
                _currentFrame++;
                if (_currentFrame >= _totalFrames)
                {
                    if (_currentState == KnightState.Dead || _currentState == KnightState.Defend)
                    {
                        _currentFrame = _totalFrames - 1; // Hold last frame
                    }
                    else
                    {
                        _currentFrame = 0; // Loop
                    }
                }
                _animationTimer -= _animationFrameTime;
            }

            // Update hitbox activation
            IsAttackHitboxActive = _currentState switch
            {
                KnightState.ComboAttack1 => _currentFrame == 3,
                KnightState.ComboAttack2 => _currentFrame == 2,
                KnightState.ComboAttack3 => _currentFrame == 2,
                KnightState.RunAttack => _currentFrame == 3 || _currentFrame == 4,
                _ => false
            };
        }

        private bool AnimationFinished()
        {
            return _currentFrame >= _totalFrames - 1 && _animationTimer >= _animationFrameTime;
        }

        public void TakeDamage(int damage)
        {
            if (_isInvulnerable || _currentState == KnightState.Dead) return;

            Health -= damage;
            _enemyHurtSound.Play();
            _hurtFlashTimer = 0.2;
            _isFlashingWhite = true;
            Game1.Instance.BloodSplatters.Splatter(Bounds.Center);

            if (Health <= 0)
            {
                SetState(KnightState.Dead);
                return;
            }

            // Check vulnerability window logic
            if (_isVulnerableWindow)
            {
                _hitsTakenInWindow++;
                if (_hitsTakenInWindow >= _hitsToTriggerDefend)
                {
                    _isVulnerableWindow = false;
                    _hitsTakenInWindow = 0;
                    SetState(KnightState.Defend);
                }
                else
                {
                    SetState(KnightState.Hurt);
                }
            }
            // Do not interrupt attacks
            else if (_currentState == KnightState.Idle || _currentState == KnightState.Walk || _currentState == KnightState.Run)
            {
                SetState(KnightState.Hurt);
            }
        }

        public void SetState(KnightState state)
        {
            if (_currentState == state) return;
            if (_currentState == KnightState.Dead) return;
            if (_currentState == KnightState.Hurt && state != KnightState.Idle) return; // Can only exit Hurt back to Idle

            _currentState = state;
            _currentFrame = 0;
            _animationTimer = 0;
            _isInvulnerable = false;
            IsAttackHitboxActive = false;

            switch (state)
            {
                case KnightState.Idle:
                    _currentTexture = _idleTexture;
                    _totalFrames = 4;
                    _animationFrameTime = 0.15;
                    break;
                case KnightState.Walk:
                    _currentTexture = _walkTexture;
                    _totalFrames = 8;
                    _animationFrameTime = 0.1;
                    break;
                case KnightState.Run:
                    _currentTexture = _runTexture;
                    _totalFrames = 7;
                    _animationFrameTime = 0.1;
                    break;
                case KnightState.RunAttack:
                    _currentTexture = _runAttackTexture;
                    _totalFrames = 6;
                    _animationFrameTime = 0.1;
                    break;
                case KnightState.ComboAttack1:
                    _currentTexture = _attack1Texture;
                    _totalFrames = 5;
                    _animationFrameTime = 0.1;
                    break;
                case KnightState.ComboAttack2:
                    _currentTexture = _attack2Texture;
                    _totalFrames = 4;
                    _animationFrameTime = 0.1;
                    break;
                case KnightState.ComboAttack3:
                    _currentTexture = _attack3Texture;
                    _totalFrames = 4;
                    _animationFrameTime = 0.1;
                    break;
                case KnightState.Defend:
                    _currentTexture = _defendTexture;
                    _totalFrames = 5;
                    _animationFrameTime = 0.1;
                    _stateTimer = 3.0; // Defend for 3 seconds
                    _isInvulnerable = true;
                    break;
                case KnightState.Jump:
                    _currentTexture = _jumpTexture;
                    _totalFrames = 6;
                    _animationFrameTime = 0.1;
                    break;
                case KnightState.Hurt:
                    _currentTexture = _hurtTexture;
                    _totalFrames = 2;
                    _animationFrameTime = 0.1;
                    break;
                case KnightState.Dead:
                    _currentTexture = _deadTexture;
                    _totalFrames = 6;
                    _animationFrameTime = 0.15;
                    break;
            }
        }

        private void UpdateBounds()
        {
            float boxWidth = (FRAME_WIDTH * Scale) * 0.3f;
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
            float xOffset;

            if (Direction == Direction.Right)
            {
                xOffset = 60 * Scale;
            }
            else
            {
                xOffset = (FRAME_WIDTH * Scale) - (60 * Scale) - attackWidth;
            }
            AttackBox = new BoundingRectangle(Position.X + xOffset, Position.Y + yOffset, attackWidth, attackHeight);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsRemoved) return;

            var effects = (_direction == Direction.Left) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var sourceRect = new Rectangle(_currentFrame * FRAME_WIDTH, 0, FRAME_WIDTH, FRAME_HEIGHT);
            spriteBatch.Draw(_currentTexture, Position, sourceRect, _color, 0f, Vector2.Zero, Scale, effects, 0f);
        }
    }
}