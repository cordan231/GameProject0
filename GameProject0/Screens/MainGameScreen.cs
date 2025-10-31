using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using GameProject0.Enemies;
using GameProject0.Particles;
using GameProject0.Collisions;
using System.Linq;

namespace GameProject0
{
    public class MainGameScreen : IGameScreen
    {
        private ScreenManager _screenManager;
        private PlayerSprite _playerSprite;
        private Tilemap _tilemap;
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

        private double _minotaurSpawnTimer;
        private const double MINOTAUR_SPAWN_TIME = 10.0;

        private const float GROUND_Y = 3 * 64 * 2.0f;

        public void Initialize(ScreenManager screenManager, ContentManager content, GraphicsDeviceManager graphicsDeviceManager)
        {
            _screenManager = screenManager;
            _content = content;
            _graphicsDeviceManager = graphicsDeviceManager;
            _playerSprite = new PlayerSprite();
            _tilemap = new Tilemap("map.txt");
            _coins = new List<Coin>();
            _random = new Random();
            _score = 0;
            _minotaurSpawnTimer = MINOTAUR_SPAWN_TIME;
        }

        public void LoadContent()
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            _tilemap.LoadContent(_content);

            _playerSprite.LoadContent(_content);
            _spriteFont = _content.Load<SpriteFont>("vcr");
            _coinPickup = _content.Load<SoundEffect>("pickup-coin");

            _playerSprite.Scale = 1.5f;

            float yOffset = (_playerSprite.Height * 0.4f);
            float boxHeight = (_playerSprite.Height * 0.6f);
            _playerSprite.Position = new Vector2(
                viewport.Width / 2 - _playerSprite.Width / 2,
                GROUND_Y - (yOffset + boxHeight)
            );
        }

        public void Update(GameTime gameTime, InputManager inputManager)
        {
            if (_playerSprite.IsDead)
            {
                _screenManager.LoadScreen(new TitleScreen());
                return;
            }

            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;

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
            if (inputManager.Save)
            {
                SaveGame();

            }

            if (inputManager.Load)
            {
                LoadGame();

            }

            _playerSprite.Update(gameTime);
            // Update Minotaur position based on ground
            if (_minotaur != null)
            {
                _minotaur.Update(gameTime, viewport.Width);
                _minotaur.Position = new Vector2(_minotaur.Position.X, GROUND_Y - _minotaur.Height);
            }

            HandleMinotaurSpawning(gameTime);

            // Only process new actions if the player is in a state that allows it
            if (_playerSprite.CurrentPlayerState == CurrentState.Idle || _playerSprite.CurrentPlayerState == CurrentState.Running)
            {
                if (inputManager.Roll)
                {
                    _playerSprite.Roll();
                }
                else if (inputManager.Attack)
                {
                    _playerSprite.Attack();
                }
                else if (inputManager.Direction.X != 0)
                {
                    _playerSprite.SetState(CurrentState.Running);
                    _playerSprite.SetDirection(inputManager.Direction.X > 0 ? Direction.Right : Direction.Left);
                }
                else
                {
                    _playerSprite.SetState(CurrentState.Idle);
                }
            }


            if (_minotaur != null && !_minotaur.IsRemoved)
            {
                // Player attacks minotaur
                if (_playerSprite.IsAttacking && !_attackCooldown && _playerSprite.AttackBox.CollidesWith(_minotaur.Bounds))
                {
                    _attackCooldown = true;
                    _attackCooldownTimer = 0.5;
                    _minotaur.TakeDamage();
                    Game1.Instance.BloodSplatters.Splatter(_minotaur.Bounds.Center);
                }

                // Minotaur attacks player
                if (_minotaur.IsAttackHitboxActive && _minotaur.AttackBox.CollidesWith(_playerSprite.Bounds))
                {
                    _playerSprite.TakeDamage(_minotaur.Direction);
                }
            }

            // Player movement is now conditional on state
            if (_playerSprite.CurrentPlayerState == CurrentState.Running || _playerSprite.CurrentPlayerState == CurrentState.Idle)
            {
                Vector2 newPosition = _playerSprite.Position + inputManager.Direction * 200f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                newPosition.X = Math.Clamp(newPosition.X, 0, viewport.Width - _playerSprite.Width);
                // Keep Y position locked to the ground
                newPosition.Y = GROUND_Y - _playerSprite.Height;
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
        }

        private void HandleMinotaurSpawning(GameTime gameTime)
        {
            if (_minotaur == null || _minotaur.IsRemoved)
            {
                _minotaurSpawnTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                if (_minotaurSpawnTimer <= 0)
                {
                    SpawnMinotaur();
                    _minotaurSpawnTimer = MINOTAUR_SPAWN_TIME;
                }
            }
        }

        private void SpawnMinotaur()
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            _minotaur = new Minotaur();
            _minotaur.LoadContent(_content);

            int side = _random.Next(2); // 0 for left, 1 for right
            if (side == 0)
            {
                _minotaur.Position = new Vector2(-_minotaur.Width, GROUND_Y - _minotaur.Height);
                _minotaur.Direction = Direction.Right;
            }
            else
            {
                _minotaur.Position = new Vector2(viewport.Width, GROUND_Y - _minotaur.Height);
                _minotaur.Direction = Direction.Left;
            }
            Game1.Instance.ShakeScreen(10f, 0.5f);
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;

            _tilemap.Draw(gameTime, spriteBatch);

            _playerSprite.Draw(spriteBatch);
            _minotaur?.Draw(spriteBatch);
            foreach (var coin in _coins)
            {
                coin.Draw(spriteBatch);
            }
            spriteBatch.DrawString(_spriteFont, $"Score: {_score}", new Vector2(10, 10), Color.White);
            spriteBatch.DrawString(_spriteFont, $"Player HP: {_playerSprite.Health}", new Vector2(10, 50), Color.White);
            if (_minotaur != null && !_minotaur.IsRemoved)
            {
                spriteBatch.DrawString(_spriteFont, $"Minotaur HP: {_minotaur.Health}", new Vector2(10, 30), Color.White);
            }

            string instructions = "E TO ATTACK   SPACE TO DODGE";
            Vector2 instructionsSize = _spriteFont.MeasureString(instructions);
            Vector2 instructionsPosition = new Vector2(
                viewport.Width - instructionsSize.X - 10,
                viewport.Height - instructionsSize.Y - 10
            );
            spriteBatch.DrawString(_spriteFont, instructions, instructionsPosition, Color.White);
        }
        private void SaveGame()
        {
            var state = new GameState
            {
                Score = _score,
                Player = new PlayerData
                {
                    Position = _playerSprite.Position,
                    Health = _playerSprite.Health
                },
                CoinPositions = _coins.Select(c => c.Position).ToList(),
                Minotaur = new EnemyData
                {
                    IsRemoved = _minotaur == null || _minotaur.IsRemoved,
                    Position = _minotaur?.Position ?? Vector2.Zero,
                    Health = _minotaur?.Health ?? 0,
                    Direction = _minotaur?.Direction ?? Direction.Right
                }
            };

            SaveManager.Save(state);
            Console.WriteLine("Game Saved!");
        }

        private void LoadGame()
        {
            var state = SaveManager.Load();
            if (state == null)
            {
                Console.WriteLine("Load failed or no save found.");
                return;
            }

            // Restore game state
            _score = state.Score;
            _playerSprite.Position = state.Player.Position;
            _playerSprite.SetHealth(state.Player.Health);

            // Restore coins
            _coins.Clear();
            foreach (var pos in state.CoinPositions)
            {
                var coin = new Coin();
                coin.LoadContent(_content);
                coin.Position = pos;
                _coins.Add(coin);
            }

            // Restore minotaur
            if (state.Minotaur != null && !state.Minotaur.IsRemoved)
            {
                _minotaur = new Minotaur();
                _minotaur.LoadContent(_content);
                _minotaur.Position = state.Minotaur.Position;
                _minotaur.Health = state.Minotaur.Health;
                _minotaur.Direction = state.Minotaur.Direction;
            }
            else
            {
                _minotaur = null;
            }

            Console.WriteLine("Game Loaded!");
        }
    }
}