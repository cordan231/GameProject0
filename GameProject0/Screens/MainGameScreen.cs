using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using GameProject0.Enemies;
using GameProject0.Particles;

namespace GameProject0
{
    public class MainGameScreen : IGameScreen
    {
        private ScreenManager _screenManager;
        private PlayerSprite _playerSprite;
        private Texture2D _backgroundTexture;
        private List<Coin> _coins;
        private Random _random;
        private double _coinSpawnTimer;
        private int _score;
        private SpriteFont _spriteFont;
        private ContentManager _content;
        private GraphicsDeviceManager _graphicsDeviceManager;
        private SoundEffect _coinPickup;

        private Minotaur _minotaur;
        private bool _attackCooldown = false;
        private double _attackCooldownTimer = 0;
        private bool _minotaurWasAlive = true;

        public void Initialize(ScreenManager screenManager, ContentManager content, GraphicsDeviceManager graphicsDeviceManager)
        {
            _screenManager = screenManager;
            _content = content;
            _graphicsDeviceManager = graphicsDeviceManager;
            _playerSprite = new PlayerSprite();
            _coins = new List<Coin>();
            _random = new Random();
            _score = 0;
            _minotaur = new Minotaur();
        }

        public void LoadContent()
        {
            _playerSprite.LoadContent(_content);
            _minotaur.LoadContent(_content);
            _backgroundTexture = _content.Load<Texture2D>("platform-background");
            _spriteFont = _content.Load<SpriteFont>("vcr");
            _coinPickup = _content.Load<SoundEffect>("pickup-coin");

            _playerSprite.Scale = 1.5f;
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            float groundY = viewport.Height * 0.83f;
            _playerSprite.Position = new Vector2(viewport.Width / 2 - _playerSprite.Width / 2, groundY - _playerSprite.Height);
            _minotaur.Position = new Vector2(100, groundY - _minotaur.Height);
        }

        public void Update(GameTime gameTime, InputManager inputManager)
        {

            if (inputManager.Exit)
            {
                _screenManager.LoadScreen(new TitleScreen());
                return;
            }

            if (_attackCooldown)
            {
                _attackCooldownTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                if (_attackCooldownTimer <= 0)
                {
                    _attackCooldown = false;
                }
            }

            _playerSprite.Update(gameTime);
            if (!_minotaur.IsRemoved) _minotaur.Update(gameTime);

            if (inputManager.Attack && !_playerSprite.IsAttacking)
            {
                _playerSprite.Attack();
            }

            if (_playerSprite.IsAttacking && !_attackCooldown && _minotaur.Health > 0)
            {
                if (_playerSprite.AttackBox.CollidesWith(_minotaur.Bounds))
                {
                    _attackCooldown = true;
                    _attackCooldownTimer = 0.5;
                    _minotaur.TakeDamage();
                    Vector2 splatterPosition = _minotaur.Bounds.Center;
                    Game1.Instance.BloodSplatters.Splatter(splatterPosition);
                }
            }

            if (inputManager.Damage && _minotaur.Health > 0)
            {
                _minotaur.TakeDamage();
                Vector2 splatterPosition = _minotaur.Bounds.Center;
                Game1.Instance.BloodSplatters.Splatter(splatterPosition);
            }


            if (!_playerSprite.IsAttacking)
            {
                if (inputManager.Direction.X != 0)
                {
                    _playerSprite.SetState(CurrentState.Running);
                    _playerSprite.SetDirection(inputManager.Direction.X > 0 ? Direction.Right : Direction.Left);
                }
                else
                {
                    _playerSprite.SetState(CurrentState.Idle);
                }

                var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
                Vector2 newPosition = _playerSprite.Position + inputManager.Direction * 200f * (float)gameTime.ElapsedGameTime.TotalSeconds;

                newPosition.X = Math.Clamp(newPosition.X, 0, viewport.Width - _playerSprite.Width);
                _playerSprite.Position = newPosition;
            }


            _coinSpawnTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (_coinSpawnTimer > 1.0)
            {
                _coinSpawnTimer = 0;
                var coin = new Coin();
                coin.LoadContent(_content);
                coin.Position = new Vector2(_random.Next(0, _graphicsDeviceManager.GraphicsDevice.Viewport.Width - 64), -64);
                _coins.Add(coin);
            }

            for (int i = _coins.Count - 1; i >= 0; i--)
            {
                _coins[i].Update(gameTime);
                if (_coins[i].CollidesWith(_playerSprite))
                {
                    _score++;
                    _coins.RemoveAt(i);
                    _coinPickup.Play();
                }
                else if (_coins[i].Position.Y > _graphicsDeviceManager.GraphicsDevice.Viewport.Height)
                {
                    _coins.RemoveAt(i);
                }
            }

            if (_minotaurWasAlive && _minotaur.IsRemoved)
            {
                Vector2 deathPosition = _minotaur.Bounds.Center;
                Game1.Instance.BloodSplatters.Splatter(deathPosition);
                _minotaurWasAlive = false;
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;

            spriteBatch.Draw(_backgroundTexture, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
            _playerSprite.Draw(spriteBatch);
            _minotaur.Draw(spriteBatch);
            foreach (var coin in _coins)
            {
                coin.Draw(spriteBatch);
            }
            spriteBatch.DrawString(_spriteFont, $"Score: {_score}", new Vector2(10, 10), Color.White);
            if (!_minotaur.IsRemoved)
            {
                spriteBatch.DrawString(_spriteFont, $"Minotaur HP: {_minotaur.Health}", new Vector2(10, 30), Color.White);
            }
        }
    }
}

