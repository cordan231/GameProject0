using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameProject0.Collisions;
using GameProject0.Objects;
using System;
using System.Collections.Generic;

namespace GameProject0.Enemies
{
    public enum SkeletonState
    {
        WalkingIn,
        Idle,
        Attack1,
        Attack2,
        Evasion,
        Hurt,
        Dead
    }

    struct AfterImageSnapshot
    {
        public Vector2 Position;
        public Rectangle SourceRect;
        public SpriteEffects Effects;
        public float Alpha;
        public Color Tint;
    }

    public class Skeleton
    {
        private Texture2D _idleTexture;
        private Texture2D _walkTexture;
        private Texture2D _attack1Texture;
        private Texture2D _attack2Texture;
        private Texture2D _evasionTexture;
        private Texture2D _hurtTexture;
        private Texture2D _deadTexture;
        private Texture2D _arrowTexture;
        private Texture2D _currentTexture;

        private Vector2 _position;
        private SkeletonState _currentState;
        private SkeletonState _lastAttackState;
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

        private const int FRAME_WIDTH = 128;
        private const int FRAME_HEIGHT = 128;
        private double _animationFrameTime = 0.1;

        private double _idleTimer = 2.0;
        private double _postAttackTimer = 0;
        private bool _postEvasionAttack = false;

        public BoundingRectangle Bounds { get; private set; }
        public List<Arrow> Arrows { get; private set; }

        private float _evasionTargetX;
        private const float EVASION_SPEED = 700f;
        private List<AfterImageSnapshot> _afterImages;
        private double _afterImageTimer;

        private Vector2 _walkInTargetPosition;
        private const float WALK_IN_SPEED = 200f;

        public SkeletonState CurrentState => _currentState;

        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;
                UpdateBounds();
            }
        }

        public Direction Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                UpdateBounds();
            }
        }

        public float Width => FRAME_WIDTH * Scale;
        public float Height => FRAME_HEIGHT * Scale;

        public Skeleton()
        {
            Arrows = new List<Arrow>();
            _afterImages = new List<AfterImageSnapshot>();
        }

        public void LoadContent(ContentManager content)
        {
            _idleTexture = content.Load<Texture2D>("skeleton_idle");
            _walkTexture = content.Load<Texture2D>("skeleton_walk");
            _attack1Texture = content.Load<Texture2D>("skeleton_shot1");
            _attack2Texture = content.Load<Texture2D>("skeleton_shot2");
            _evasionTexture = content.Load<Texture2D>("skeleton_evasion");
            _hurtTexture = content.Load<Texture2D>("skeleton_hurt");
            _deadTexture = content.Load<Texture2D>("skeleton_dead");
            _arrowTexture = content.Load<Texture2D>("arrow_sprite");

            SetState(SkeletonState.Idle);
        }

        public void WalkIn(Vector2 spawnPosition, Vector2 targetPosition, Direction direction)
        {
            Position = spawnPosition;
            _walkInTargetPosition = targetPosition;
            Direction = direction;
            SetState(SkeletonState.WalkingIn);
        }

        public void Update(GameTime gameTime, Viewport viewport)
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

            switch (_currentState)
            {
                case SkeletonState.WalkingIn:
                    if (Direction == Direction.Left)
                    {
                        _position.X -= WALK_IN_SPEED * dt;
                        if (_position.X <= _walkInTargetPosition.X)
                        {
                            _position.X = _walkInTargetPosition.X;
                            SetState(Random.Shared.Next(2) == 0 ? SkeletonState.Attack1 : SkeletonState.Attack2);
                        }
                    }
                    else // Direction is Right
                    {
                        _position.X += WALK_IN_SPEED * dt;
                        if (_position.X >= _walkInTargetPosition.X)
                        {
                            _position.X = _walkInTargetPosition.X;
                            SetState(Random.Shared.Next(2) == 0 ? SkeletonState.Attack1 : SkeletonState.Attack2);
                        }
                    }
                    Position = _position;
                    break;

                case SkeletonState.Idle:
                    _idleTimer -= dt;
                    if (_idleTimer <= 0 && _postAttackTimer <= 0)
                    {
                        SetState(Random.Shared.Next(2) == 0 ? SkeletonState.Attack1 : SkeletonState.Attack2);
                    }

                    if (_postAttackTimer > 0)
                    {
                        _postAttackTimer -= dt;
                        if (_postAttackTimer <= 0)
                        {
                            StartEvasion(viewport);
                        }
                    }
                    break;

                case SkeletonState.Attack1:
                case SkeletonState.Attack2:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0)
                    {
                        _lastAttackState = _currentState;
                        SpawnArrow(_lastAttackState);

                        if (_postEvasionAttack)
                        {
                            _idleTimer = 3.0;
                            _postAttackTimer = 0;
                            _postEvasionAttack = false;
                        }
                        else
                        {
                            _postAttackTimer = 3.0;
                        }
                        SetState(SkeletonState.Idle);
                    }
                    break;

                case SkeletonState.Evasion:
                    _stateTimer -= dt;

                    if (_evasionTargetX > _position.X)
                    {
                        _position.X += EVASION_SPEED * dt;
                        if (_position.X >= _evasionTargetX)
                        {
                            _position.X = _evasionTargetX;
                            Direction = Direction.Left;
                            SetState(Random.Shared.Next(2) == 0 ? SkeletonState.Attack1 : SkeletonState.Attack2);
                        }
                    }
                    else // Target is to the left
                    {
                        _position.X -= EVASION_SPEED * dt;
                        if (_position.X <= _evasionTargetX)
                        {
                            _position.X = _evasionTargetX;
                            Direction = Direction.Right;
                            SetState(Random.Shared.Next(2) == 0 ? SkeletonState.Attack1 : SkeletonState.Attack2);
                        }
                    }
                    Position = _position;

                    _afterImageTimer -= dt;
                    if (_afterImageTimer <= 0)
                    {
                        _afterImageTimer = 0.02;
                        Color tint = Random.Shared.Next(3) switch
                        {
                            0 => Color.Cyan,
                            1 => Color.Magenta,
                            _ => Color.Yellow
                        };
                        _afterImages.Add(new AfterImageSnapshot
                        {
                            Position = _position,
                            SourceRect = new Rectangle(_currentFrame * FRAME_WIDTH, 0, FRAME_WIDTH, FRAME_HEIGHT),
                            Effects = (Direction == Direction.Left) ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                            Alpha = 0.5f,
                            Tint = tint
                        });
                    }

                    if (_stateTimer <= 0)
                    {
                        _position.X = _evasionTargetX;
                        Direction = (_evasionTargetX > 0) ? Direction.Left : Direction.Right;
                        SetState(Random.Shared.Next(2) == 0 ? SkeletonState.Attack1 : SkeletonState.Attack2);
                    }
                    break;

                case SkeletonState.Hurt:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0)
                    {
                        StartEvasion(viewport);
                    }
                    break;

                case SkeletonState.Dead:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0)
                    {
                        IsRemoved = true;
                    }
                    break;
            }

            _animationTimer += dt;
            if (_animationTimer > _animationFrameTime)
            {
                _currentFrame++;
                if (_currentFrame >= _totalFrames)
                {
                    if (_currentState == SkeletonState.Dead)
                    {
                        _currentFrame = _totalFrames - 1;
                    }
                    else
                    {
                        _currentFrame = 0;
                    }
                }
                _animationTimer -= _animationFrameTime;
            }

            for (int i = Arrows.Count - 1; i >= 0; i--)
            {
                Arrows[i].Update(gameTime, viewport);
                if (Arrows[i].IsRemoved) Arrows.RemoveAt(i);
            }

            for (int i = _afterImages.Count - 1; i >= 0; i--)
            {
                var image = _afterImages[i];
                image.Alpha -= dt * 2.0f;
                if (image.Alpha <= 0)
                {
                    _afterImages.RemoveAt(i);
                }
                else
                {
                    _afterImages[i] = image;
                }
            }
        }

        private void SpawnArrow(SkeletonState attackType)
        {
            float yOffset;
            if (attackType == SkeletonState.Attack1)
            {
                yOffset = 50 * Scale;
            }
            else // Attack2
            {
                yOffset = 65 * Scale;
            }

            float arrowWidth = 48 * 2.0f;
            float x = (Direction == Direction.Right) ? Position.X + 80 * Scale : Position.X + (FRAME_WIDTH * Scale - 80 * Scale) - arrowWidth;
            float y = Position.Y + yOffset;

            Arrow arrow = new Arrow(new Vector2(x, y), Direction);
            arrow.LoadContent(Game1.Instance.Content);
            Arrows.Add(arrow);
        }

        private void StartEvasion(Viewport viewport)
        {
            if (Direction == Direction.Right)
            {
                _evasionTargetX = viewport.Width - Width;
            }
            else
            {
                _evasionTargetX = 0;
            }
            SetState(SkeletonState.Evasion);
        }

        public void TakeDamage(int damage)
        {
            if (_currentState == SkeletonState.Dead || _currentState == SkeletonState.Hurt || _currentState == SkeletonState.Evasion) return;

            Health -= damage;
            _hurtFlashTimer = 0.2;
            _isFlashingWhite = true;
            Game1.Instance.BloodSplatters.Splatter(Bounds.Center);

            if (Health <= 0)
            {
                SetState(SkeletonState.Dead);
            }
            else
            {
                SetState(SkeletonState.Hurt);
            }
        }

        public void SetState(SkeletonState state)
        {
            if (_currentState == state && state != SkeletonState.Idle) return;
            if (_currentState == SkeletonState.Dead) return;

            _currentState = state;
            _currentFrame = 0;
            _animationTimer = 0;

            switch (state)
            {
                case SkeletonState.WalkingIn:
                    _currentTexture = _walkTexture;
                    _totalFrames = 8;
                    _animationFrameTime = 0.1;
                    break;
                case SkeletonState.Idle:
                    _currentTexture = _idleTexture;
                    _totalFrames = 7;
                    _animationFrameTime = 0.15;
                    _idleTimer = 3.0;
                    break;
                case SkeletonState.Attack1:
                    _currentTexture = _attack1Texture;
                    _totalFrames = 15;
                    _animationFrameTime = 0.08;
                    _stateTimer = _totalFrames * _animationFrameTime;
                    break;
                case SkeletonState.Attack2:
                    _currentTexture = _attack2Texture;
                    _totalFrames = 15;
                    _animationFrameTime = 0.08;
                    _stateTimer = _totalFrames * _animationFrameTime;
                    break;
                case SkeletonState.Evasion:
                    _currentTexture = _evasionTexture;
                    _totalFrames = 6;
                    _animationFrameTime = 0.1;
                    _stateTimer = 1.0;
                    _afterImages.Clear();
                    _postEvasionAttack = true;
                    break;
                case SkeletonState.Hurt:
                    _currentTexture = _hurtTexture;
                    _totalFrames = 2;
                    _animationFrameTime = 0.1;
                    _stateTimer = _totalFrames * _animationFrameTime * 2;
                    break;
                case SkeletonState.Dead:
                    _currentTexture = _deadTexture;
                    _totalFrames = 5;
                    _animationFrameTime = 0.15;
                    _stateTimer = _totalFrames * _animationFrameTime;
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

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsRemoved) return;

            foreach (var image in _afterImages)
            {
                spriteBatch.Draw(_evasionTexture, image.Position, image.SourceRect, image.Tint * image.Alpha, 0f, Vector2.Zero, Scale, image.Effects, 0f);
            }

            var effects = (_direction == Direction.Left) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var sourceRect = new Rectangle(_currentFrame * FRAME_WIDTH, 0, FRAME_WIDTH, FRAME_HEIGHT);
            spriteBatch.Draw(_currentTexture, Position, sourceRect, _color, 0f, Vector2.Zero, Scale, effects, 0f);

            foreach (var arrow in Arrows)
            {
                arrow.Draw(spriteBatch);
            }
        }
    }
}