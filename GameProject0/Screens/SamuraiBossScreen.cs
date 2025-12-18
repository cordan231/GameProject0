using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameProject0.Enemies;
using System.Collections.Generic;

namespace GameProject0
{
    public class SamuraiBossScreen : IGameScreen
    {
        private ScreenManager _screenManager;
        private ContentManager _content;
        private GraphicsDeviceManager _graphics;

        private PlayerSprite _player;
        private Samurai _boss;
        private Tilemap _tilemap;
        private SpriteFont _font;

        // Simple hearts list for UI
        private List<Heart> _playerHearts = new List<Heart>();

        private const float GROUND_Y = 3 * 64 * 2.0f;
        private bool _cooldown = false;
        private double _cooldownTimer = 0;

        public void Initialize(ScreenManager sm, ContentManager cm, GraphicsDeviceManager gdm)
        {
            _screenManager = sm; _content = cm; _graphics = gdm;
            _player = new PlayerSprite();
            _tilemap = new Tilemap("map.txt");
            _boss = new Samurai();
        }

        public void LoadContent()
        {
            _tilemap.LoadContent(_content);
            _player.LoadContent(_content);
            _player.Scale = 1.5f;
            var vp = _graphics.GraphicsDevice.Viewport;
            _player.Position = new Vector2(100, GROUND_Y - _player.Height);

            _boss.LoadContent(_content);
            _boss.WalkIn(new Vector2(vp.Width - 200, GROUND_Y - _boss.Height));

            _font = _content.Load<SpriteFont>("vcr");

            for (int i = 0; i < _player.Health; i++) _playerHearts.Add(new Heart(Game1.Instance, Color.Blue));
        }

        public void Update(GameTime gameTime, InputManager input)
        {
            if (_player.IsDead) { _screenManager.LoadScreen(new TitleScreen()); return; }
            if (_boss.IsRemoved) { _screenManager.LoadScreen(new TitleScreen()); return; }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_cooldown) { _cooldownTimer -= dt; if (_cooldownTimer <= 0) _cooldown = false; }

            _player.Update(gameTime);
            _boss.Update(gameTime, _player, _graphics.GraphicsDevice.Viewport);

            // Collisions
            if (_player.IsAttacking && !_cooldown && _player.AttackBox.CollidesWith(_boss.Bounds))
            {
                _cooldown = true; _cooldownTimer = 0.5;
                _boss.TakeDamage(1);
                Game1.Instance.BloodSplatters.Splatter(_boss.Bounds.Center);
            }
            if (_boss.IsAttackHitboxActive && _boss.AttackBox.CollidesWith(_player.Bounds)) _player.TakeDamage(_boss.Direction);
            foreach (var arrow in _boss.Arrows) if (arrow.CollidesWith(_player)) { _player.TakeDamage(arrow.Direction); arrow.IsRemoved = true; }

            // Player Input
            if (_player.CurrentPlayerState == CurrentState.Idle || _player.CurrentPlayerState == CurrentState.Running)
            {
                if (input.Roll) _player.Roll();
                else if (input.Attack) _player.Attack();
                else if (input.Direction.X != 0) { _player.SetState(CurrentState.Running); _player.SetDirection(input.Direction.X > 0 ? Direction.Right : Direction.Left); }
                else _player.SetState(CurrentState.Idle);
            }

            // Movement Physics
            Vector2 pos = _player.Position;
            if (_player.CurrentPlayerState == CurrentState.Running || _player.CurrentPlayerState == CurrentState.Idle) pos.X += input.Direction.X * 200f * dt;
            else if (_player.CurrentPlayerState == CurrentState.Rolling) pos.X += (_player._currentDirection == Direction.Right ? 1 : -1) * 200f * dt;
            else if (_player.CurrentPlayerState == CurrentState.Hurt) pos += _player.KnockbackVelocity * dt;

            pos.Y = GROUND_Y - _player.Height;
            pos.X = MathHelper.Clamp(pos.X, 0, _graphics.GraphicsDevice.Viewport.Width - _player.Width);
            _player.Position = pos;
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            _graphics.GraphicsDevice.Clear(new Color(50, 0, 0));
            _tilemap.Draw(gameTime, spriteBatch);
            _player.Draw(spriteBatch);
            _boss.Draw(spriteBatch);

            // Draw Boss HP text
            spriteBatch.DrawString(_font, $"SAMURAI HP: {_boss.Health}", new Vector2(500, 50), Color.White);
        }

        public void Draw3D(GameTime gameTime, GraphicsDevice gd)
        {
            // Draw Player Hearts
            float angle = (float)gameTime.TotalGameTime.TotalSeconds * 1.5f;
            float heartScale = 0.2f;
            for (int i = 0; i < _player.Health; i++)
            {
                if (i < _playerHearts.Count)
                {
                    _playerHearts[i].World = Matrix.CreateScale(heartScale) * Matrix.CreateRotationY(angle) * Matrix.CreateTranslation(new Vector3(4 - i * 1.2f, 3, 0));
                    _playerHearts[i].Draw();
                }
            }
        }
    }
}