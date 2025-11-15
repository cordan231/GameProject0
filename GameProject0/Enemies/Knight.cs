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
        WalkingIn,
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
        private double[] _animationFrameTimes;

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

        // ####################################################################
        // AI LOGIC & TWEAKABLE CONSTANTS
        // ####################################################################

        // --- General AI ---
        private const float CHASE_SPEED = 120f;
        private const float RUN_SPEED = 300f;
        private const float JUMP_SPEED = 600f;
        private const float ATTACK_RANGE = 150f;
        private const float RUN_ATTACK_RANGE = 200f;
        private const float PLAYER_CLOSE_RANGE = 200f;
        private Vector2 _walkInTargetPosition;
        private const float WALK_IN_SPEED = 200f;

        // --- Combo Attack 1: Timings & Speeds ---
        // Spec: 0.1, 0.1, 0.1, 0.1, 0.2 (using your provided values)
        private static readonly double[] COMBO1_FRAME_TIMINGS = new double[] { 0.15, 0.15, 0.15, 0.25, 0.25 };
        private const float COMBO1_LUNGE_SPEED = 300f;

        // --- Combo Attack 2: Timings & Speeds ---
        // Spec: 0.1, 0.1, 0.15, 0.15 (using your provided values)
        private static readonly double[] COMBO2_FRAME_TIMINGS = new double[] { 0.2, 0.2, 0.2, 0.3 };
        private const float COMBO2_LUNGE_SPEED = 300f;

        // --- Combo Attack 3: Timings & Speeds ---
        // Spec: 0.2, 0.2, (fast lunge), 0.1 (using your provided values)
        private static readonly double[] COMBO3_FRAME_TIMINGS = new double[] { 0.2, 0.2, 0.3, 0.1 };
        private const float COMBO3_RETREAT_SPEED = -500f;
        private const float COMBO3_LUNGE_SPEED = 2000f;

        // ####################################################################

        // Vulnerability Windows
        private bool _isVulnerableWindow = false;
        private int _hitsTakenInWindow = 0;
 a      private double _vulnerabilityTimer = 0;
        private int _hitsToTriggerDefend = 0;

        // Attack Cooldowns
        private double _runAttackCooldown = 0;
        private bool _lastAttackWasRunAttack = false;

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
            set { _direction = value; UpdateBounds(); }
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

        public void WalkIn(Vector2 spawnPosition, Vector2 targetPosition, Direction direction)
        {
            Position = spawnPosition;
            _walkInTargetPosition = targetPosition;
            Direction = direction;
            SetState(KnightState.WalkingIn);
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

            // Animate first, then do logic
            Animate(dt);

            if (_currentState == KnightState.Dead)
            {
                // AnimationFinished check is now (>= _totalFrames)
                if (AnimationFinished()) IsRemoved = true;
                return;
            }

            // Update cooldowns
            if (_runAttackCooldown > 0)
            {
                _runAttackCooldown -= dt;
            }

            // Handle Vulnerability Window Logic
            if (_isVulnerableWindow)
            {
                _vulnerabilityTimer -= dt;
                if (_vulnerabilityTimer <= 0)
                {
                    _isVulnerableWindow = false;
                    _hitsTakenInWindow = 0;
                    SetState(KnightState.Run);
                    Position = _position;
                    UpdateAttackBox();
                    return;
                }
            }

            // AI State Machine
            float distance = float.MaxValue;
            if (player != null && !player.IsDead)
            {
                distance = Vector2.Distance(player.Bounds.Center, Bounds.Center);
                if (_currentState != KnightState.RunAttack &&
                    _currentState != KnightState.ComboAttack1 && _currentState != KnightState.ComboAttack2 &&
                    _currentState != KnightState.ComboAttack3 && _currentState != KnightState.Defend &&
                    _currentState != KnightState.Jump && _currentState != KnightState.Hurt &&
                    _currentState != KnightState.Idle)
                {
                    Direction = (player.Bounds.Center.X < Bounds.Center.X) ? Direction.Left : Direction.Right;
                }
            }

            switch (_currentState)
            {
                case KnightState.WalkingIn:
                    if (Direction == Direction.Left)
                    {
                        _position.X -= WALK_IN_SPEED * dt;
                        if (_position.X <= _walkInTargetPosition.X)
                        {
                            _position.X = _walkInTargetPosition.X;
                            SetState(KnightState.Run);
                        }
                    }
                    else
                    {
                        _position.X += WALK_IN_SPEED * dt;
                        if (_position.X >= _walkInTargetPosition.X)
                        {
                            _position.X = _walkInTargetPosition.X;
                            SetState(KnightState.Run);
                        }
                    }
                    break;

                case KnightState.Idle:
                    if (!_isVulnerableWindow)
                    {
                        _stateTimer -= dt;
                        if (_stateTimer <= 0)
                        {
                            SetState(KnightState.Run);
                        }
                    }
                    break;

                case KnightState.Walk:
                    if (distance <= ATTACK_RANGE)
                    {
                        SetState(KnightState.ComboAttack1);
                    }
                    else if (distance > ATTACK_RANGE + 20)
                    {
                        SetState(KnightState.Run);
                    }
                    else
                    {
                        _position.X += (Direction == Direction.Right ? CHASE_SPEED : -CHASE_SPEED) * dt;
                    }
                    break;

                case KnightState.Run:
                    if (distance <= RUN_ATTACK_RANGE && _runAttackCooldown <= 0 && !_lastAttackWasRunAttack)
                    {
                        SetState(KnightState.RunAttack);
                    }
                    else if (distance <= ATTACK_RANGE + 20)
                    {
                        SetState(KnightState.Walk);
                    }
                    else
                    {
                        _position.X += (Direction == Direction.Right ? RUN_SPEED : -RUN_SPEED) * dt;
                    }
                    break;

                case KnightState.RunAttack:
                    _position.X += (Direction == Direction.Right ? RUN_SPEED : -RUN_SPEED) * dt;
                    if (AnimationFinished())
                    {
                        StartVulnerabilityWindow(3.0, 1);
                    }
                    break;

                case KnightState.ComboAttack1:
                    // Spec: Still frame 0
                    if (_currentFrame >= 1 && _currentFrame <= 3) // Spec: Move frames 1, 2, 3
                    {
                        _position.X += (Direction == Direction.Right ? COMBO1_LUNGE_SPEED : -COMBO1_LUNGE_SPEED) * dt;
                    }
                    // Spec: Still frame 4
                    if (AnimationFinished())
                    {
                        if (player != null && !player.IsDead) { Direction = (player.Bounds.Center.X < Bounds.Center.X) ? Direction.Left : Direction.Right; }
                        SetState(KnightState.ComboAttack2);
                    }
                    break;

                case KnightState.ComboAttack2:
                    if (_currentFrame == 0 || _currentFrame == 1) // Spec: Move frames 0, 1
                    {
                        _position.X += (Direction == Direction.Right ? COMBO2_LUNGE_SPEED : -COMBO2_LUNGE_SPEED) * dt;
                    }
                    // Spec: Still frames 2, 3
                    if (AnimationFinished())
                    {
                        if (player != null && !player.IsDead) { Direction = (player.Bounds.Center.X < Bounds.Center.X) ? Direction.Left : Direction.Right; }
                        SetState(KnightState.ComboAttack3);
                    }
                    break;

                case KnightState.ComboAttack3:
                    if (_currentFrame == 0 || _currentFrame == 1) // Spec: Move back frames 0, 1
                    {
                        _position.X += (Direction == Direction.Right ? COMBO3_RETREAT_SPEED : -COMBO3_RETREAT_SPEED) * dt;
                    }
                    else if (_currentFrame == 2) // Spec: Lunge hard frame 2
                    {
                        _position.X += (Direction == Direction.Right ? COMBO3_LUNGE_SPEED : -COMBO3_LUNGE_SPEED) * dt;
                    }
                    // Spec: Still frame 3
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
                            SetState(KnightState.Run);
                        }
                    }
                    break;

                case KnightState.Jump:
                    float jumpDir = (Direction == Direction.Right) ? -1 : 1;
                    _position.X += jumpDir * JUMP_SPEED * dt;
                    if (AnimationFinished())
                    {
                        SetState(KnightState.Run);
                    }
                    break;

                case KnightState.Hurt:
                    if (AnimationFinished())
                    {
                        _isVulnerableWindow = true;
                        SetState(KnightState.Idle);
                    }
                    break;
            }

            Position = _position;
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
            // If animation is finished (e.g. _currentFrame == _totalFrames), don't advance timer
            if (_currentFrame >= _totalFrames)
            {
                // But we still need to update hitbox logic for the held frame
                IsAttackHitboxActive = false; // Turn off hitbox once anim is done
                return;
            }

            _animationTimer += dt;
            if (_animationTimer > _animationFrameTimes[_currentFrame])
            {
                _animationTimer -= _animationFrameTimes[_currentFrame];
                _currentFrame++;

                if (_currentFrame >= _totalFrames)
                {
                    // Animation has *just* finished
                    switch (_currentState)
                    {
                        // Looping animations
                        case KnightState.WalkingIn:
                        case KnightState.Idle:
                        case KnightState.Walk:
                        case KnightState.Run:
                            _currentFrame = 0; // These loop
                            break;

                            // For non-looping, _currentFrame will now be == _totalFrames,
                            // which AnimationFinished() will detect.
                            // We don't cap it to _totalFrames - 1 anymore.
                    }
                }
            }

            // --- Hitbox Logic ---
            // Need to check against a valid frame index
            int frameToCheck = _currentFrame;
            if (frameToCheck >= _totalFrames)
            {
                frameToCheck = _totalFrames - 1;
            }

            IsAttackHitboxActive = _currentState switch
            {
                KnightState.ComboAttack1 => frameToCheck == 4, // Spec: Only last frame
                KnightState.ComboAttack2 => frameToCheck == 2, // Spec: Only third frame
                KnightState.ComboAttack3 => frameToCheck == 2, // Spec: Only third frame (lunge)
                KnightState.RunAttack => frameToCheck == 3 || frameToCheck == 4,
                _ => false
            };
        }


        private bool AnimationFinished()
        {
            // NEW: Animation is finished when the frame counter is AT OR BEYOND total frames
            return _currentFrame >= _totalFrames;
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
            else if (_currentState == KnightState.Idle || _currentState == KnightState.Walk || _currentState == KnightState.Run)
            {
                SetState(KnightState.Hurt);
            }
        }

        public void SetState(KnightState state)
        {
            if (_currentState == KnightState.Dead) return;
            if (_currentState == KnightState.Hurt && state != KnightState.Idle) return;

            _currentState = state;
            _currentFrame = 0;
            _animationTimer = 0;
            _isInvulnerable = false;
            IsAttackHitboxActive = false;

            double defaultFrameTime = 0.1;

            switch (state)
            {
                case KnightState.WalkingIn:
                    _currentTexture = _walkTexture;
                    _totalFrames = 8;
                    defaultFrameTime = 0.1;
                    break;
                case KnightState.Idle:
                    _currentTexture = _idleTexture;
                    _totalFrames = 4;
                    defaultFrameTime = 0.15;
                    if (!_isVulnerableWindow)
                    {
                        _stateTimer = 1.0;
                    }
                    break;
                case KnightState.Walk:
                    _currentTexture = _walkTexture;
                    _totalFrames = 8;
                    defaultFrameTime = 0.1;
                    break;
                case KnightState.Run:
                    _currentTexture = _runTexture;
                    _totalFrames = 7;
                    defaultFrameTime = 0.1;
                    break;
                case KnightState.RunAttack:
                    _currentTexture = _runAttackTexture;
                    _totalFrames = 6;
                    defaultFrameTime = 0.1;
                    _runAttackCooldown = 5.0;
                    _lastAttackWasRunAttack = true;
                    break;

                case KnightState.ComboAttack1:
                    _currentTexture = _attack1Texture;
                    _totalFrames = 5;
                    _animationFrameTimes = COMBO1_FRAME_TIMINGS;
                    _lastAttackWasRunAttack = false;
                    break;
                case KnightState.ComboAttack2:
                    _currentTexture = _attack2Texture;
                    _totalFrames = 4;
                    _animationFrameTimes = COMBO2_FRAME_TIMINGS;
                    break;
                case KnightState.ComboAttack3:
                    _currentTexture = _attack3Texture;
                    _totalFrames = 4;
                    _animationFrameTimes = COMBO3_FRAME_TIMINGS;
                    break;

                case KnightState.Defend:
                    _currentTexture = _defendTexture;
                    _totalFrames = 5;
                    defaultFrameTime = 0.1;
                    _stateTimer = 3.0;
                    _isInvulnerable = true;
                    break;
                case KnightState.Jump:
                    _currentTexture = _jumpTexture;
                    _totalFrames = 6;
                    defaultFrameTime = 0.1;
                    break;
                case KnightState.Hurt:
                    _currentTexture = _hurtTexture;
                    _totalFrames = 2;
                    defaultFrameTime = 0.1;
                    break;
                case KnightState.Dead:
                    _currentTexture = _deadTexture;
                    _totalFrames = 6;
                    defaultFrameTime = 0.15;
                    break;
            }

            if (state != KnightState.ComboAttack1 && state != KnightState.ComboAttack2 && state != KnightState.ComboAttack3)
            {
                _animationFrameTimes = new double[_totalFrames];
                for (int i = 0; i < _totalFrames; i++)
                {
                    _animationFrameTimes[i] = defaultFrameTime;
                }
            }
        }

        private void UpdateBounds()
        {
            float boxWidth = (FRAME_WIDTH * Scale) * 0.4f;
            float boxHeight = (FRAME_HEIGHT * Scale) * 0.7f;
            float yOffset = (Height - boxHeight);
            float xOffsetFromEdge = (FRAME_WIDTH * Scale) * 0.1f;
            float xOffset;

            if (Direction == Direction.Right)
            {
                xOffset = xOffsetFromEdge;
            }
            else // Direction.Left
            {
                xOffset = (FRAME_WIDTH * Scale) - xOffsetFromEdge - boxWidth;
            }

            Bounds = new BoundingRectangle(
                new Vector2(_position.X + xOffset, _position.Y + yOffset),
                boxWidth,
                boxHeight
            );
        }

        private void UpdateAttackBox()
        {
            float attackWidth = (FRAME_WIDTH * Scale) * 0.15f;
            float attackHeight = (FRAME_HEIGHT * Scale) * 0.3f;
            float yOffset = (FRAME_HEIGHT * Scale) * 0.45f;
            float xOffset;

            if (Direction == Direction.Right)
            {
                xOffset = (FRAME_WIDTH * Scale) * 0.6f;
            }
            else
            {
                xOffset = (FRAME_WIDTH * Scale) * 0.15f;
            }

            AttackBox = new BoundingRectangle(Position.X + xOffset, Position.Y + yOffset, attackWidth, attackHeight);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsRemoved) return;

            // NEW: Cap the frame here for drawing, so we don't go out of bounds
            int frameToDraw = _currentFrame;
            if (frameToDraw >= _totalFrames)
            {
                // If animation is done, draw the last frame
                frameToDraw = _totalFrames - 1;
            }

            var effects = (_direction == Direction.Left) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var sourceRect = new Rectangle(frameToDraw * FRAME_WIDTH, 0, FRAME_WIDTH, FRAME_HEIGHT);
            spriteBatch.Draw(_currentTexture, Position, sourceRect, _color, 0f, Vector2.Zero, Scale, effects, 0f);
        }
    }
}