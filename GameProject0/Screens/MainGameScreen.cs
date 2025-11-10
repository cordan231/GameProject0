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
using GameProject0.Objects;

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
        private Texture2D _whitePixelTexture;

        private Minotaur _minotaur;
        private Skeleton _skeleton;
        private bool _attackCooldown = false;
        private double _attackCooldownTimer = 0;

        // private double _minotaurSpawnTimer; // Disabled for Skeleton testing
        // private const double MINOTAUR_SPAWN_TIME = 10.0;

        private const float GROUND_Y = 3 * 64 * 2.0f;

        private List<Heart> _minotaurHearts;
        private List<Heart> _playerHearts;
        private List<Heart> _skeletonHearts;

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
            // _minotaurSpawnTimer = MINOTAUR_SPAWN_TIME; // Disabled
            _minotaurHearts = new List<Heart>();
            _playerHearts = new List<Heart>();
            _skeletonHearts = new List<Heart>();
        }

        public void LoadContent()
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            _tilemap.LoadContent(_content);

            _playerSprite.LoadContent(_content);
            _spriteFont = _content.Load<SpriteFont>("vcr");
            _coinPickup = _content.Load<SoundEffect>("pickup-coin");

            _whitePixelTexture = new Texture2D(_graphicsDeviceManager.GraphicsDevice, 1, 1);
            _whitePixelTexture.SetData(new[] { Color.White });

            _playerSprite.Scale = 1.5f;

            float yOffset = (_playerSprite.Height * 0.4f);
            float boxHeight = (_playerSprite.Height * 0.6f);
            _playerSprite.Position = new Vector2(
                viewport.Width / 2 - _playerSprite.Width / 2,
                GROUND_Y - (yOffset + boxHeight)
            );

            // --- Hearts ---
            _minotaurHearts.Clear();
            _playerHearts.Clear();
            _skeletonHearts.Clear();
            for (int i = 0; i < 3; i++)
            {
                _minotaurHearts.Add(new Heart(Game1.Instance, Color.Red));
                _playerHearts.Add(new Heart(Game1.Instance, Color.Blue));
            }
            for (int i = 0; i < 2; i++)
            {
                _skeletonHearts.Add(new Heart(Game1.Instance, Color.Green));
            }

            // --- Spawn Skeleton for Testing ---
            SpawnSkeleton();
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
                return;
            }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _playerSprite.Update(gameTime);

            // --- Enemy Updates ---
            _minotaur?.Update(gameTime, viewport.Width); // Update if minotaur exists
            _skeleton?.Update(gameTime, viewport); // Update if skeleton exists

            // HandleMinotaurSpawning(gameTime); // Disabled for testing

            // --- Player Input ---
            if (_playerSprite.CurrentPlayerState == CurrentState.Idle || _playerSprite.CurrentPlayerState == CurrentState.Running)
            {
                if (inputManager.Roll) _playerSprite.Roll();
                else if (inputManager.Attack) _playerSprite.Attack();
                else if (inputManager.Direction.X != 0)
                {
                    _playerSprite.SetState(CurrentState.Running);
                    _playerSprite.SetDirection(inputManager.Direction.X > 0 ? Direction.Right : Direction.Left);
                }
                else _playerSprite.SetState(CurrentState.Idle);
            }

            // --- Handle Collisions ---
            // Minotaur
            if (_minotaur != null && !_minotaur.IsRemoved)
            {
                if (_playerSprite.IsAttacking && !_attackCooldown && _playerSprite.AttackBox.CollidesWith(_minotaur.Bounds))
                {
                    _attackCooldown = true;
                    _attackCooldownTimer = 0.5;
                    _minotaur.TakeDamage(Game1.GunModeActive ? 100 : 1);
                    Game1.Instance.BloodSplatters.Splatter(_minotaur.Bounds.Center);
                }
                if (_minotaur.IsAttackHitboxActive && _minotaur.AttackBox.CollidesWith(_playerSprite.Bounds))
                {
                    _playerSprite.TakeDamage(_minotaur.Direction);
                }
            }
            // Skeleton
            if (_skeleton != null && !_skeleton.IsRemoved)
            {
                if (_playerSprite.IsAttacking && !_attackCooldown && _playerSprite.AttackBox.CollidesWith(_skeleton.Bounds))
                {
                    _attackCooldown = true;
                    _attackCooldownTimer = 0.5;
                    _skeleton.TakeDamage(Game1.GunModeActive ? 100 : 1);
                }
                foreach (var arrow in _skeleton.Arrows)
                {
                    if (arrow.CollidesWith(_playerSprite))
                    {
                        _playerSprite.TakeDamage(arrow.Direction);
                        arrow.IsRemoved = true;
                    }
                }
            }

            // --- Player Movement ---
            Vector2 playerNewPos = _playerSprite.Position;
            if (_playerSprite.CurrentPlayerState == CurrentState.Running || _playerSprite.CurrentPlayerState == CurrentState.Idle)
            {
                playerNewPos += inputManager.Direction * 200f * dt;
                playerNewPos.Y = GROUND_Y - _playerSprite.Height;
            }
            else if (_playerSprite.CurrentPlayerState == CurrentState.Rolling)
            {
                float rollSpeed = 200f;
                float moveDirection = (_playerSprite._currentDirection == Direction.Right) ? 1 : -1;
                playerNewPos.X += moveDirection * rollSpeed * dt;
                playerNewPos.Y = GROUND_Y - _playerSprite.Height;
            }
            else if (_playerSprite.CurrentPlayerState == CurrentState.Hurt)
            {
                playerNewPos += _playerSprite.KnockbackVelocity * dt;
            }

            float scale = _playerSprite.Scale;
            float frameWidth = 128 * scale;
            float boxWidth = frameWidth * 0.35f;
            float xOffset = (frameWidth - boxWidth) / 2;
            float minX = -xOffset;
            float maxX = viewport.Width - xOffset - boxWidth;
            playerNewPos.X = Math.Clamp(playerNewPos.X, minX, maxX);
            _playerSprite.Position = playerNewPos;

            // --- Coin Logic ---
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

            // --- 3D Heart Positioning ---
            float angle = (float)gameTime.TotalGameTime.TotalSeconds * 1.5f;
            float worldHalfHeight = 10f * (float)Math.Tan(MathHelper.Pi / 8f);
            float worldHalfWidth = worldHalfHeight * viewport.AspectRatio;
            float heartScale = 0.2f;
            float heartSpacing = 0.8f;

            // Minotaur Hearts
            if (_minotaur != null && !_minotaur.IsRemoved)
            {
                float pixelX = _minotaur.Position.X + _minotaur.Width / 2;
                float pixelY = _minotaur.Position.Y + 30;
                float worldX = (pixelX - viewport.Width / 2) / (viewport.Width / 2) * worldHalfWidth;
                float worldY = -(pixelY - viewport.Height / 2) / (viewport.Height / 2) * worldHalfHeight;
                Vector3 basePosition = new Vector3(worldX, worldY, 0);
                for (int i = 0; i < _minotaurHearts.Count; i++)
                {
                    xOffset = (i - (_minotaurHearts.Count - 1) / 2.0f) * heartSpacing;
                    _minotaurHearts[i].World = Matrix.CreateScale(heartScale) * Matrix.CreateRotationY(angle) * Matrix.CreateTranslation(basePosition + new Vector3(xOffset, 0, 0));
                }
            }
            else
            {
                for (int i = 0; i < _minotaurHearts.Count; i++)
                {
                    _minotaurHearts[i].World = Matrix.CreateTranslation(-1000, -1000, 0);
                }
            }

            // Skeleton Hearts
            if (_skeleton != null && !_skeleton.IsRemoved)
            {
                float pixelX = _skeleton.Position.X + _skeleton.Width / 2;
                float pixelY = _skeleton.Position.Y + 80;
                float worldX = (pixelX - viewport.Width / 2) / (viewport.Width / 2) * worldHalfWidth;
                float worldY = -(pixelY - viewport.Height / 2) / (viewport.Height / 2) * worldHalfHeight;
                Vector3 basePosition = new Vector3(worldX, worldY, 0);
                for (int i = 0; i < _skeletonHearts.Count; i++)
                {
                    xOffset = (i - (_skeletonHearts.Count - 1) / 2.0f) * heartSpacing;
                    _skeletonHearts[i].World = Matrix.CreateScale(heartScale) * Matrix.CreateRotationY(angle) * Matrix.CreateTranslation(basePosition + new Vector3(xOffset, 0, 0));
                }
            }
            else
            {
                for (int i = 0; i < _skeletonHearts.Count; i++)
                {
                    _skeletonHearts[i].World = Matrix.CreateTranslation(-1000, -1000, 0);
                }
            }


            // Player Hearts (Top-Right)
            float topEdgeOfView = 3.5f;
            float rightEdgeOfView = 5.5f;
            for (int i = 0; i < _playerHearts.Count; i++)
            {
                xOffset = rightEdgeOfView - (i * (heartSpacing + 0.2f)); // Player hearts slightly more spaced
                _playerHearts[i].World = Matrix.CreateScale(heartScale) * Matrix.CreateRotationY(angle) * Matrix.CreateTranslation(new Vector3(xOffset, topEdgeOfView, 0));
            }

        }

        private void SpawnSkeleton()
        {
            _skeleton = new Skeleton();
            _skeleton.LoadContent(_content);
            _skeleton.Position = new Vector2(0, GROUND_Y - _skeleton.Height);
            _skeleton.Direction = Direction.Right;
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            _tilemap.Draw(gameTime, spriteBatch);

            _playerSprite.Draw(spriteBatch);
            _minotaur?.Draw(spriteBatch);
            _skeleton?.Draw(spriteBatch);

            foreach (var coin in _coins)
            {
                coin.Draw(spriteBatch);
            }

            // --- Draw Score Box (Back to Top-Left) ---
            string scoreText = $"Score: {_score}";
            Vector2 scoreTextSize = _spriteFont.MeasureString(scoreText);
            float padding = 10f;
            float outlineThickness = 2f;
            Vector2 textPosition = new Vector2(10, 10);

            Rectangle backgroundRect = new Rectangle(
                (int)(textPosition.X - padding),
                (int)(textPosition.Y - padding),
                (int)(scoreTextSize.X + padding * 2),
                (int)(scoreTextSize.Y + padding * 2)
            );
            Rectangle outlineRect = new Rectangle(
                backgroundRect.X - (int)outlineThickness,
                backgroundRect.Y - (int)outlineThickness,
                backgroundRect.Width + (int)(outlineThickness * 2),
                backgroundRect.Height + (int)(outlineThickness * 2)
            );

            spriteBatch.Draw(_whitePixelTexture, outlineRect, Color.White);
            spriteBatch.Draw(_whitePixelTexture, backgroundRect, new Color(0, 0, 139));
            spriteBatch.DrawString(_spriteFont, scoreText, textPosition, Color.White);
        }

        public void Draw3D(GameTime gameTime, GraphicsDevice graphicsDevice)
        {
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            graphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            // Minotaur Hearts
            int minotaurHeartsToDraw = 0;
            if (_minotaur != null && !_minotaur.IsRemoved) minotaurHeartsToDraw = _minotaur.Health;
            for (int i = 0; i < minotaurHeartsToDraw; i++)
            {
                if (i < _minotaurHearts.Count) _minotaurHearts[i].Draw();
            }

            // Skeleton Hearts
            int skeletonHeartsToDraw = 0;
            if (_skeleton != null && !_skeleton.IsRemoved) skeletonHeartsToDraw = _skeleton.Health;
            for (int i = 0; i < skeletonHeartsToDraw; i++)
            {
                if (i < _skeletonHearts.Count) _skeletonHearts[i].Draw();
            }

            // Player Hearts
            int playerHeartsToDraw = _playerSprite.Health;
            for (int i = 0; i < playerHeartsToDraw; i++)
            {
                if (i < _playerHearts.Count) _playerHearts[i].Draw();
            }
        }

        private void SaveGame()
        {
            var state = new GameState
            {
                Score = _score,
                Player = new PlayerData
                {
                    Position = new VectorData(_playerSprite.Position),
                    Health = _playerSprite.Health,
                    State = _playerSprite.CurrentPlayerState,
                    KnockbackVelocity = new VectorData(_playerSprite.KnockbackVelocity),
                    Direction = _playerSprite._currentDirection
                },
                CoinPositions = _coins.Select(c => new VectorData(c.Position)).ToList(),
                Minotaur = _minotaur == null ? new MinotaurData { IsRemoved = true } : new MinotaurData
                {
                    IsRemoved = _minotaur.IsRemoved,
                    Position = new VectorData(_minotaur.Position),
                    Health = _minotaur.Health,
                    Direction = _minotaur.Direction,
                    State = _minotaur.CurrentState
                },
                Skeleton = _skeleton == null ? new SkeletonData { IsRemoved = true } : new SkeletonData
                {
                    IsRemoved = _skeleton.IsRemoved,
                    Position = new VectorData(_skeleton.Position),
                    Health = _skeleton.Health,
                    Direction = _skeleton.Direction,
                    State = _skeleton.CurrentState
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

            _score = state.Score;

            _playerSprite.SetHealth(state.Player.Health);
            _playerSprite.SetState(state.Player.State);
            _playerSprite.KnockbackVelocity = state.Player.KnockbackVelocity.ToVector2();
            _playerSprite._currentDirection = state.Player.Direction;
            _playerSprite.Position = state.Player.Position.ToVector2();

            _coins.Clear();
            foreach (var pos in state.CoinPositions)
            {
                var coin = new Coin();
                coin.LoadContent(_content);
                coin.Position = pos.ToVector2();
                _coins.Add(coin);
            }

            if (state.Minotaur != null && !state.Minotaur.IsRemoved)
            {
                _minotaur = new Minotaur();
                _minotaur.LoadContent(_content);
                _minotaur.Health = state.Minotaur.Health;
                _minotaur.Direction = state.Minotaur.Direction;
                _minotaur.SetState(state.Minotaur.State);
                _minotaur.Position = state.Minotaur.Position.ToVector2();
            }
            else _minotaur = null;

            if (state.Skeleton != null && !state.Skeleton.IsRemoved)
            {
                _skeleton = new Skeleton();
                _skeleton.LoadContent(_content);
                _skeleton.Health = state.Skeleton.Health;
                _skeleton.Direction = state.Skeleton.Direction;
                _skeleton.SetState(state.Skeleton.State);
                _skeleton.Position = state.Skeleton.Position.ToVector2();
            }
            else _skeleton = null;

            Console.WriteLine("Game Loaded!");
        }
    }
}