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
        Idle,
        Attack1,
        Attack2,
        Evasion,
        Hurt,
        Dead
    }

    // Snapshot for the after-image trail
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
        private Texture2D _attack1Texture;
        private Texture2D _attack2Texture;
        private Texture2D _evasionTexture;
        private Texture2D _hurtTexture;
        private Texture2D _deadTexture;
        private Texture2D _arrowTexture;
        private Texture2D _currentTexture;

        private Vector2 _position;
        private SkeletonState _currentState;
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
        private double _postAttackTimer = 2.0;
        private bool _attackFired = false;

        public BoundingRectangle Bounds { get; private set; }
        public List<Arrow> Arrows { get; private set; }

        private float _evasionTargetX;
        private const float EVASION_SPEED = 700f;
        private List<AfterImageSnapshot> _afterImages;
        private double _afterImageTimer;

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
            _attack1Texture = content.Load<Texture2D>("skeleton_shot1");
            _attack2Texture = content.Load<Texture2D>("skeleton_shot2");
            _evasionTexture = content.Load<Texture2D>("skeleton_evasion");
            _hurtTexture = content.Load<Texture2D>("skeleton_hurt");
            _deadTexture = content.Load<Texture2D>("skeleton_dead");
            _arrowTexture = content.Load<Texture2D>("arrow_sprite");

            SetState(SkeletonState.Idle);
        }

        public void Update(GameTime gameTime, Viewport viewport)
        {
            if (IsRemoved) return;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // --- Update Timers ---
            if (_hurtFlashTimer > 0)
            {
                _hurtFlashTimer -= dt;
                _color = _isFlashingWhite ? Color.White : Color.Red;
                _isFlashingWhite = !_isFlashingWhite;
                if (_hurtFlashTimer <= 0) _color = Color.White;
            }

            // --- State Machine ---
            switch (_currentState)
            {
                case SkeletonState.Idle:
                    _idleTimer -= dt;
                    if (_idleTimer <= 0)
                    {
                        // Randomly pick an attack
                        SetState(Random.Shared.Next(2) == 0 ? SkeletonState.Attack1 : SkeletonState.Attack2);
                    }
                    break;

                case SkeletonState.Attack1:
                case SkeletonState.Attack2:
                    _stateTimer -= dt;
                    // Fire arrow at specific frame
                    if (!_attackFired && _currentFrame >= 3)
                    {
                        SpawnArrow();
                        _attackFired = true;
                    }
                    if (_stateTimer <= 0)
                    {
                        _postAttackTimer = 2.0; // Start post-attack wait
                        SetState(SkeletonState.Idle); // Go to idle to wait
                    }
                    break;

                case SkeletonState.Evasion:
                    _stateTimer -= dt;

                    // Move towards target
                    if (Direction == Direction.Right) // Moving right
                    {
                        _position.X += EVASION_SPEED * dt;
                        if (_position.X >= _evasionTargetX)
                        {
                            _position.X = _evasionTargetX;
                            SetState(SkeletonState.Idle);
                        }
                    }
                    else // Moving left
                    {
                        _position.X -= EVASION_SPEED * dt;
                        if (_position.X <= _evasionTargetX)
                        {
                            _position.X = _evasionTargetX;
                            SetState(SkeletonState.Idle);
                        }
                    }
                    Position = _position; // Update bounds

                    // After-image logic
                    _afterImageTimer -= dt;
                    if (_afterImageTimer <= 0)
                    {
                        _afterImageTimer = 0.02; // Spawn new image
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
                            Effects = (_direction == Direction.Left) ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                            Alpha = 0.5f,
                            Tint = tint
                        });
                    }

                    if (_stateTimer <= 0) SetState(SkeletonState.Idle); // Failsafe
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

            // Check for post-attack evasion
            if (_currentState == SkeletonState.Idle && _postAttackTimer > 0)
            {
                _postAttackTimer -= dt;
                if (_postAttackTimer <= 0)
                {
                    StartEvasion(viewport);
                }
            }

            // --- Update Animation ---
            _animationTimer += dt;
            if (_animationTimer > _animationFrameTime)
            {
                _currentFrame++;
                if (_currentFrame >= _totalFrames)
                {
                    if (_currentState == SkeletonState.Dead)
                    {
                        _currentFrame = _totalFrames - 1; // Hold last frame
                    }
                    else
                    {
                        _currentFrame = 0;
                    }
                }
                _animationTimer -= _animationFrameTime;
            }

            // Update Arrows
            for (int i = Arrows.Count - 1; i >= 0; i--)
            {
                Arrows[i].Update(gameTime, viewport);
                if (Arrows[i].IsRemoved) Arrows.RemoveAt(i);
            }

            // Update After-images
            for (int i = _afterImages.Count - 1; i >= 0; i--)
            {
                var image = _afterImages[i];
                image.Alpha -= dt * 2.0f; // Fade over 0.5s
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

        private void SpawnArrow()
        {
            float x = (Direction == Direction.Right) ? Position.X + 80 * Scale : Position.X + 10 * Scale;
            float y = Position.Y + 45 * Scale;
            Arrow arrow = new Arrow(new Vector2(x, y), Direction);
            arrow.LoadContent(Game1.Instance.Content);
            Arrows.Add(arrow);
        }

        private void StartEvasion(Viewport viewport)
        {
            // Flip direction and set target
            if (Direction == Direction.Right)
            {
                Direction = Direction.Left;
                _evasionTargetX = 20; // Left edge
            }
            else
            {
                Direction = Direction.Right;
                _evasionTargetX = viewport.Width - Width - 20; // Right edge
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
            _attackFired = false;

            switch (state)
            {
                case SkeletonState.Idle:
                    _currentTexture = _idleTexture;
                    _totalFrames = 7;
                    _animationFrameTime = 0.15;
                    _idleTimer = 2.0; // Reset idle timer
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
                    _stateTimer = _totalFrames * _animationFrameTime * 1.5; // Give it time to cross
                    _afterImages.Clear();
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

            // Draw after-images
            foreach (var image in _afterImages)
            {
                spriteBatch.Draw(_evasionTexture, image.Position, image.SourceRect, image.Tint * image.Alpha, 0f, Vector2.Zero, Scale, image.Effects, 0f);
            }

            // Draw main sprite
            var effects = (_direction == Direction.Left) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var sourceRect = new Rectangle(_currentFrame * FRAME_WIDTH, 0, FRAME_WIDTH, FRAME_HEIGHT);
            spriteBatch.Draw(_currentTexture, Position, sourceRect, _color, 0f, Vector2.Zero, Scale, effects, 0f);

            // Draw arrows
            foreach (var arrow in Arrows)
            {
                arrow.Draw(spriteBatch);
            }
        }
    }
}