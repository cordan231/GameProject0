using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameProject0.Collisions;
using GameProject0.Objects;
using System;
using System.Collections.Generic;

namespace GameProject0.Enemies
{
    public enum SamuraiState
    {
        Idle,
        Walking,
        JumpingToEdge,
        Shooting,
        Attack2, // Combo 1
        ComboDash, // Mirage Dodge in combo
        Attack3, // Combo 2
        CounterStance,
        CounterAttack,
        DashingAway,
        Dead
    }

    public class Samurai
    {
        private Texture2D _idleTexture, _walkTexture, _jumpTexture, _shotTexture, _attack1Texture, _attack2Texture, _attack3Texture, _deadTexture, _currentTexture;
        private Texture2D _dashTexture;

        private SamuraiState _currentState;
        private Vector2 _position;
        public Direction Direction { get; private set; }
        public int Health { get; private set; } = 20;
        public bool IsRemoved { get; private set; } = false;

        private int _currentFrame;
        private double _animationTimer;
        private double _frameTime = 0.1;
        private int _totalFrames;
        public float Scale { get; set; } = 2.0f;

        private double _stateTimer;
        private double _idleWaitTimer;
        private double _poseHoldTimer;

        // Jump Logic
        private Vector2 _jumpStartPos, _jumpTargetPos;
        private float _jumpProgress;
        private SamuraiState _nextStateAfterJump = SamuraiState.Shooting;

        // Counter Logic
        private bool _counterTriggered;

        // Dash Logic
        private float _dashTargetX;
        private const float DASH_SPEED = 900f;
        private const float COMBO_DASH_SPEED = 1200f;
        private List<AfterImageSnapshot> _afterImages = new List<AfterImageSnapshot>();
        private double _afterImageTimer;

        // Pattern Logic
        private int _patternPhase = 0; // 0: Combo, 1: Bow, 2: Counter

        public List<SamuraiArrow> Arrows { get; private set; } = new List<SamuraiArrow>();
        public BoundingRectangle Bounds { get; private set; }
        public BoundingRectangle AttackBox { get; private set; }
        public bool IsAttackHitboxActive { get; private set; }

        private const int FRAME_WIDTH = 128;
        private const int FRAME_HEIGHT = 128;
        private Color _color = Color.White;
        private double _hurtTimer;

        public float Width => FRAME_WIDTH * Scale;
        public float Height => FRAME_HEIGHT * Scale;

        public void LoadContent(ContentManager content)
        {
            _idleTexture = content.Load<Texture2D>("samurai_Idle");
            _walkTexture = content.Load<Texture2D>("samurai_Walk");
            _jumpTexture = content.Load<Texture2D>("samurai_Jump");
            _shotTexture = content.Load<Texture2D>("samurai_Shot");
            _attack1Texture = content.Load<Texture2D>("samurai_Attack_1");
            _attack2Texture = content.Load<Texture2D>("samurai_Attack_2");
            _attack3Texture = content.Load<Texture2D>("samurai_Attack_3");
            _deadTexture = content.Load<Texture2D>("samurai_Dead");
            _dashTexture = _walkTexture;

            _patternPhase = 0;
        }

        public void WalkIn(Vector2 startPos)
        {
            _position = startPos;
            _patternPhase = 0;
            SetState(SamuraiState.Idle);
            _idleWaitTimer = 0.5;
        }

        public void Update(GameTime gameTime, PlayerSprite player, Viewport viewport)
        {
            if (IsRemoved) return;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_hurtTimer > 0) { _hurtTimer -= dt; _color = (_hurtTimer * 20 % 2 > 1) ? Color.Red : Color.White; }
            else _color = Color.White;

            switch (_currentState)
            {
                case SamuraiState.Idle:
                    _idleWaitTimer -= dt;
                    if (_idleWaitTimer <= 0) ExecutePatternBehavior(player, viewport);
                    break;

                case SamuraiState.DashingAway:
                    if (UpdateDash(dt, DASH_SPEED)) SetState(SamuraiState.Walking);
                    break;

                case SamuraiState.ComboDash:
                    // Dash through player
                    if (UpdateDash(dt, COMBO_DASH_SPEED))
                    {
                        float dist = player.Position.X - _position.X;
                        Direction = dist > 0 ? Direction.Right : Direction.Left;

                        SetState(SamuraiState.Attack3);
                    }
                    break;

                case SamuraiState.Walking:
                    float d = player.Position.X - _position.X;
                    Direction = d > 0 ? Direction.Right : Direction.Left;
                    _position.X += (Direction == Direction.Right ? 1 : -1) * 250f * dt;
                    if (Math.Abs(d) < 150) SetState(SamuraiState.Attack2);
                    break;

                case SamuraiState.JumpingToEdge:
                    _jumpProgress += dt * 1.5f;
                    if (_jumpProgress >= 1f)
                    {
                        _jumpProgress = 1f;
                        _position = _jumpTargetPos;
                        SetState(_nextStateAfterJump);
                    }
                    else
                    {
                        Vector2 pos = Vector2.Lerp(_jumpStartPos, _jumpTargetPos, _jumpProgress);
                        pos.Y -= 300f * (float)Math.Sin(_jumpProgress * Math.PI);
                        _position = pos;
                    }
                    break;

                case SamuraiState.Attack2:
                    if (_currentFrame >= _totalFrames - 1)
                    {
                        if (_poseHoldTimer == 0) _poseHoldTimer = 0.5;
                        _poseHoldTimer -= dt;
                        if (_poseHoldTimer <= 0)
                        {
                            PrepareComboDash(viewport, player);
                        }
                    }
                    break;

                case SamuraiState.Attack3:
                    if (_currentFrame == 4)
                    {
                        float lungeSpeed = 400f;
                        _position.X += (Direction == Direction.Right ? 1 : -1) * lungeSpeed * dt;
                    }
                    if (_currentFrame >= _totalFrames - 1)
                    {
                        if (_poseHoldTimer == 0) _poseHoldTimer = 0.5;
                        _poseHoldTimer -= dt;
                        if (_poseHoldTimer <= 0)
                        {
                            _patternPhase = 1;
                            _idleWaitTimer = 2.0;
                            SetState(SamuraiState.Idle);
                        }
                    }
                    break;

                case SamuraiState.Shooting:
                    if (_currentFrame >= _totalFrames - 1)
                    {
                        _patternPhase = 2;
                        _idleWaitTimer = 3.0;
                        SetState(SamuraiState.Idle);
                    }
                    break;

                case SamuraiState.CounterStance:
                    _stateTimer -= dt;
                    if (_currentFrame >= 2) { _currentFrame = 2; _animationTimer = 0; }
                    if (Vector2.Distance(_position, player.Position) < 300)
                    {
                        _counterTriggered = true;
                        SetState(SamuraiState.CounterAttack);
                    }
                    if (_stateTimer <= 0 && !_counterTriggered)
                    {
                        _patternPhase = 0;
                        _idleWaitTimer = 3.0;
                        SetState(SamuraiState.Idle);
                    }
                    break;

                case SamuraiState.CounterAttack:
                    if (_poseHoldTimer <= 0)
                    {
                        float lungeSpeed = 3500f;
                        _position.X += (Direction == Direction.Right ? 1 : -1) * lungeSpeed * dt;
                    }
                    if (_currentFrame >= _totalFrames - 1)
                    {
                        _currentFrame = _totalFrames - 1;
                        _animationTimer = 0;
                        if (_poseHoldTimer == 0) _poseHoldTimer = 1.0;
                        _poseHoldTimer -= dt;
                        if (_poseHoldTimer <= 0)
                        {
                            _patternPhase = 0;
                            _idleWaitTimer = 3.0;
                            SetState(SamuraiState.Idle);
                        }
                    }
                    break;

                case SamuraiState.Dead:
                    if (_currentFrame >= _totalFrames - 1) IsRemoved = true;
                    break;
            }

            _animationTimer += dt;
            bool frozen = (_currentState == SamuraiState.Attack2 && _poseHoldTimer > 0) ||
                          (_currentState == SamuraiState.Attack3 && _poseHoldTimer > 0) ||
                          (_currentState == SamuraiState.CounterAttack && _poseHoldTimer > 0) ||
                          (_currentState == SamuraiState.CounterStance && _currentFrame >= 2);

            if (_animationTimer >= _frameTime && !frozen)
            {
                _animationTimer = 0;
                _currentFrame++;

                if (_currentState == SamuraiState.Walking || _currentState == SamuraiState.Idle || _currentState == SamuraiState.DashingAway || _currentState == SamuraiState.ComboDash)
                    _currentFrame %= _totalFrames;
                else
                    _currentFrame = Math.Min(_currentFrame, _totalFrames - 1);

                if (_currentState == SamuraiState.Shooting && _currentFrame == 8) SpawnArrow();
            }

            UpdateAfterImages(dt);

            for (int i = Arrows.Count - 1; i >= 0; i--) { Arrows[i].Update(gameTime, viewport); if (Arrows[i].IsRemoved) Arrows.RemoveAt(i); }
            UpdateBounds();
        }

        // Returns true if destination reached
        private bool UpdateDash(float dt, float speed)
        {
            bool reached = false;
            if (_dashTargetX > _position.X)
            {
                _position.X += speed * dt;
                if (_position.X >= _dashTargetX) { _position.X = _dashTargetX; reached = true; }
            }
            else
            {
                _position.X -= speed * dt;
                if (_position.X <= _dashTargetX) { _position.X = _dashTargetX; reached = true; }
            }

            _afterImageTimer -= dt;
            if (_afterImageTimer <= 0)
            {
                _afterImageTimer = 0.05;
                _afterImages.Add(new AfterImageSnapshot
                {
                    Position = _position,
                    SourceRect = new Rectangle(_currentFrame * FRAME_WIDTH, 0, FRAME_WIDTH, FRAME_HEIGHT),
                    Effects = (Direction == Direction.Left) ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    Alpha = 0.5f,
                    Tint = Color.Red
                });
            }
            return reached;
        }

        private void UpdateAfterImages(float dt)
        {
            for (int i = _afterImages.Count - 1; i >= 0; i--)
            {
                var image = _afterImages[i];
                image.Alpha -= dt * 2.0f;
                if (image.Alpha <= 0) _afterImages.RemoveAt(i);
                else _afterImages[i] = image;
            }
        }

        private void ExecutePatternBehavior(PlayerSprite player, Viewport viewport)
        {
            if (player == null) { SetState(SamuraiState.Idle); return; }

            switch (_patternPhase)
            {
                case 0: // Combo Phase
                    PrepareDashAway(viewport, player);
                    break;
                case 1: // Bow Phase
                    _nextStateAfterJump = SamuraiState.Shooting;
                    PrepareJump(viewport, player);
                    break;
                case 2: // Counter Phase
                    float dist = Vector2.Distance(_position, player.Position);
                    if (dist < 400)
                    {
                        _nextStateAfterJump = SamuraiState.CounterStance;
                        PrepareJump(viewport, player);
                    }
                    else SetState(SamuraiState.CounterStance);
                    break;
            }
        }

        private void PrepareDashAway(Viewport viewport, PlayerSprite player)
        {
            if (player.Position.X < _position.X) _dashTargetX = viewport.Width - Width - 50;
            else _dashTargetX = 50;

            Direction = (_dashTargetX > _position.X) ? Direction.Right : Direction.Left;
            SetState(SamuraiState.DashingAway);
        }

        private void PrepareComboDash(Viewport viewport, PlayerSprite player)
        {
            float dashDist = 200f;
            float dirToPlayer = (player.Position.X > _position.X) ? 1 : -1;

            _dashTargetX = player.Position.X + (dirToPlayer * dashDist);
            _dashTargetX = MathHelper.Clamp(_dashTargetX, 50, viewport.Width - 50);

            Direction = (_dashTargetX > _position.X) ? Direction.Right : Direction.Left;
            SetState(SamuraiState.ComboDash);
        }

        private void PrepareJump(Viewport viewport, PlayerSprite player)
        {
            _jumpStartPos = _position;
            if (player.Position.X < viewport.Width / 2)
                _jumpTargetPos = new Vector2(viewport.Width - Width - 50, _position.Y);
            else
                _jumpTargetPos = new Vector2(50, _position.Y);

            Direction = (_jumpTargetPos.X < player.Position.X) ? Direction.Right : Direction.Left;
            _jumpProgress = 0;
            SetState(SamuraiState.JumpingToEdge);
        }

        private void SpawnArrow()
        {
            Vector2 arrowPos = new Vector2(_position.X + (Direction == Direction.Right ? 100 : 20), _position.Y + 100);
            var arrow = new SamuraiArrow(arrowPos, Direction);
            arrow.LoadContent(Game1.Instance.Content);
            Arrows.Add(arrow);
        }

        public void TakeDamage(int damage)
        {
            if (_currentState == SamuraiState.Dead) return;
            Health -= damage;
            _hurtTimer = 0.5;
            if (Health <= 0) SetState(SamuraiState.Dead);
        }

        private void SetState(SamuraiState state)
        {
            if (_currentState == state && state != SamuraiState.Idle) return;

            _currentState = state;
            _currentFrame = 0;
            _animationTimer = 0;
            _poseHoldTimer = 0;
            IsAttackHitboxActive = false;

            switch (state)
            {
                case SamuraiState.Idle:
                    _currentTexture = _idleTexture;
                    _totalFrames = 6;
                    _frameTime = 0.15;
                    break;
                case SamuraiState.Walking:
                    _currentTexture = _walkTexture;
                    _totalFrames = 8;
                    _frameTime = 0.1;
                    break;
                case SamuraiState.DashingAway:
                case SamuraiState.ComboDash:
                    _currentTexture = _dashTexture;
                    _totalFrames = 8;
                    _frameTime = 0.05;
                    _afterImages.Clear();
                    break;
                case SamuraiState.JumpingToEdge:
                    _currentTexture = _jumpTexture;
                    _totalFrames = 1;
                    _frameTime = 1.0;
                    break;
                case SamuraiState.Shooting:
                    _currentTexture = _shotTexture;
                    _totalFrames = 13;
                    _frameTime = 0.08;
                    break;
                case SamuraiState.Attack2:
                    _currentTexture = _attack2Texture;
                    _totalFrames = 5;
                    _frameTime = 0.1;
                    IsAttackHitboxActive = true;
                    break;
                case SamuraiState.Attack3:
                    _currentTexture = _attack3Texture;
                    _totalFrames = 6;
                    _frameTime = 0.1;
                    IsAttackHitboxActive = true;
                    break;
                case SamuraiState.CounterStance:
                    _currentTexture = _attack1Texture;
                    _totalFrames = 5;
                    _frameTime = 0.1;
                    _stateTimer = 5.0;
                    _counterTriggered = false;
                    break;
                case SamuraiState.CounterAttack:
                    _currentTexture = _attack1Texture;
                    _totalFrames = 5;
                    _currentFrame = 3;
                    _frameTime = 0.05;
                    IsAttackHitboxActive = true;
                    break;
                case SamuraiState.Dead:
                    _currentTexture = _deadTexture;
                    _totalFrames = 6;
                    _frameTime = 0.15;
                    break;
            }
        }

        private void UpdateBounds()
        {
            float w = FRAME_WIDTH * Scale * 0.4f, h = FRAME_HEIGHT * Scale * 0.7f;
            Bounds = new BoundingRectangle(_position.X + (Width - w) / 2, _position.Y + (Height - h), w, h);

            AttackBox = new BoundingRectangle(0, 0, 0, 0);
            if (_poseHoldTimer > 0) return;

            if (IsAttackHitboxActive)
            {
                bool activeFrame = false;
                if (_currentState == SamuraiState.Attack2 && _currentFrame >= 3) activeFrame = true;
                if (_currentState == SamuraiState.Attack3 && _currentFrame >= 4) activeFrame = true;
                if (_currentState == SamuraiState.CounterAttack) activeFrame = true;

                if (activeFrame)
                {
                    float range = 160f;
                    float atkX = Direction == Direction.Right ? Bounds.Right : Bounds.Left - range;
                    AttackBox = new BoundingRectangle(atkX, Bounds.Top, range, Bounds.Height);
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsRemoved) return;
            foreach (var image in _afterImages) spriteBatch.Draw(_dashTexture, image.Position, image.SourceRect, image.Tint * image.Alpha, 0f, Vector2.Zero, Scale, image.Effects, 0f);
            var effect = Direction == Direction.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            if (_currentTexture != null) spriteBatch.Draw(_currentTexture, _position, new Rectangle(_currentFrame * FRAME_WIDTH, 0, FRAME_WIDTH, FRAME_HEIGHT), _color, 0f, Vector2.Zero, Scale, effect, 0f);
            foreach (var arrow in Arrows) arrow.Draw(spriteBatch);
        }
    }
}