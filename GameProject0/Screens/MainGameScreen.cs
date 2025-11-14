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
        // ##### BOSS FIGHT TEST #####
        // Set to true to fight the boss immediately.
        // This will disable Minotaur, Skeleton, and Coin spawns.
        private bool _bossFightMode = true;
        // ###########################


        // Reference to the screen manager
        private ScreenManager _screenManager;
        // The main player character
        private PlayerSprite _playerSprite;
        // The level tilemap
        private Tilemap _tilemap;
        // List of active coins
        private List<Coin> _coins;
        private Random _random;
        private double _coinSpawnTimer;
        private int _score;
        private SpriteFont _spriteFont;
        private ContentManager _content;
        private GraphicsDeviceManager _graphicsDeviceManager;
        private SoundEffect _coinPickup;
        private Texture2D _whitePixelTexture;

        // Enemy references
        private Minotaur _minotaur;
        private Skeleton _skeleton;
        private Knight _knight; // The new boss

        // Cooldown to prevent spamming attacks instantly
        private bool _attackCooldown = false;
        private double _attackCooldownTimer = 0;

        // Tracks which enemy spawns next
        private SpawnState _nextSpawn = SpawnState.Minotaur;

        // Y-coordinate for the ground level
        private const float GROUND_Y = 3 * 64 * 2.0f;

        // Lists for 3D heart displays
        private List<Heart> _minotaurHearts;
        private List<Heart> _playerHearts;
        private List<Heart> _skeletonHearts;
        private List<Heart> _knightHearts; // Boss hearts

        // Pause menu state
        private bool _isPaused = false;
        private int _pauseSelection = 0;
        private List<string> _pauseOptions = new List<string> { "RESUME", "EXIT TO MENU" };

        // Initialize variables and lists
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
            _minotaurHearts = new List<Heart>();
            _playerHearts = new List<Heart>();
            _skeletonHearts = new List<Heart>();
            _knightHearts = new List<Heart>(); // Init boss hearts list
        }

        // Load assets and set up initial game state
        public void LoadContent()
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            _tilemap.LoadContent(_content);

            _playerSprite.LoadContent(_content);
            _spriteFont = _content.Load<SpriteFont>("vcr");
            _coinPickup = _content.Load<SoundEffect>("pickup-coin");

            // Texture for UI boxes
            _whitePixelTexture = new Texture2D(_graphicsDeviceManager.GraphicsDevice, 1, 1);
            _whitePixelTexture.SetData(new[] { Color.White });

            _playerSprite.Scale = 1.5f;

            // Set initial player position
            float yOffset = (_playerSprite.Height * 0.4f);
            float boxHeight = (_playerSprite.Height * 0.6f);
            _playerSprite.Position = new Vector2(
                viewport.Width / 2 - _playerSprite.Width / 2,
                GROUND_Y - (yOffset + boxHeight)
            );

            // Initialize hearts
            _playerHearts.Clear();
            for (int i = 0; i < _playerSprite.Health; i++)
            {
                _playerHearts.Add(new Heart(Game1.Instance, Color.Blue));
            }
            _skeletonHearts.Clear();
            _minotaurHearts.Clear();
            _knightHearts.Clear();

            // Spawn the first enemy
            if (_bossFightMode)
            {
                SpawnKnight();
            }
            else
            {
                SpawnMinotaur();
            }
        }

        // Main update loop for gameplay
        public void Update(GameTime gameTime, InputManager inputManager)
        {
            // Toggle pause if requested
            if (inputManager.Exit)
            {
                _isPaused = !_isPaused;
                _pauseSelection = 0;
                return;
            }

            // Handle Pause Menu Logic
            if (_isPaused)
            {
                if (inputManager.Direction.Y > 0) _pauseSelection = 1;
                if (inputManager.Direction.Y < 0) _pauseSelection = 0;

                if (inputManager.Attack || inputManager.Select)
                {
                    if (_pauseSelection == 0) _isPaused = false; // Resume
                    else _screenManager.LoadScreen(new TitleScreen()); // Exit
                }
                return;
            }

            // Check for Game Over
            if (_playerSprite.IsDead)
            {
                _screenManager.LoadScreen(new GameOverScreen(_score));
                return;
            }

            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;

            // Update attack cooldown
            if (_attackCooldown)
            {
                _attackCooldownTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                if (_attackCooldownTimer <= 0)
                {
                    _attackCooldown = false;
                }
            }

            // Save/Load handling
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

            // Check if enemies died and spawn new ones
            if (!_bossFightMode)
            {
                if (_minotaur != null && _minotaur.IsRemoved)
                {
                    _minotaur = null;
                    if (_nextSpawn == SpawnState.Skeleton)
                    {
                        _score += 25;
                        SpawnSkeleton();
                    }
                }
                if (_skeleton != null && _skeleton.IsRemoved)
                {
                    _skeleton = null;
                    if (_nextSpawn == SpawnState.Minotaur)
                    {
                        _score += 25;
                        SpawnMinotaur();
                    }
                }
            }
            else
            {
                // Boss fight mode: Check if boss is defeated
                if (_knight != null && _knight.IsRemoved)
                {
                    _knight = null;
                    _score += 1000; // Big score for boss
                    // You could transition to a victory screen or back to normal mode here
                    // For now, we'll just stop spawning
                }
            }


            _minotaur?.Update(gameTime, _playerSprite);
            _skeleton?.Update(gameTime, viewport);
            _knight?.Update(gameTime, _playerSprite);

            // Player Input Handling
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

            // Collision Detection
            // Minotaur Collisions
            if (_minotaur != null && !_minotaur.IsRemoved)
            {
                // Player hits Minotaur
                if (_playerSprite.IsAttacking && !_attackCooldown && _playerSprite.AttackBox.CollidesWith(_minotaur.Bounds))
                {
                    _attackCooldown = true;
                    _attackCooldownTimer = 0.5;
                    _minotaur.TakeDamage(Game1.GunModeActive ? 100 : 1, _playerSprite);
                    Game1.Instance.BloodSplatters.Splatter(_minotaur.Bounds.Center);
                }
                // Minotaur hits Player
                if (_minotaur.IsAttackHitboxActive && _minotaur.AttackBox.CollidesWith(_playerSprite.Bounds))
                {
                    _playerSprite.TakeDamage(_minotaur.Direction);
                }
            }
            // Skeleton Collisions
            if (_skeleton != null && !_skeleton.IsRemoved)
            {
                // Player hits Skeleton
                if (_playerSprite.IsAttacking && !_attackCooldown && _playerSprite.AttackBox.CollidesWith(_skeleton.Bounds))
                {
                    _attackCooldown = true;
                    _attackCooldownTimer = 0.5;
                    _skeleton.TakeDamage(Game1.GunModeActive ? 100 : 1);
                }
                // Arrows hit Player
                foreach (var arrow in _skeleton.Arrows)
                {
                    if (arrow.CollidesWith(_playerSprite))
                    {
                        _playerSprite.TakeDamage(arrow.Direction);
                        if (!_playerSprite.IsInvincible)
                        {
                            arrow.IsRemoved = true;
                        }
                    }
                }
            }
            // Knight Boss Collisions
            if (_knight != null && !_knight.IsRemoved)
            {
                // Player hits Knight
                if (_playerSprite.IsAttacking && !_attackCooldown && _playerSprite.AttackBox.CollidesWith(_knight.Bounds))
                {
                    _attackCooldown = true;
                    _attackCooldownTimer = 0.5;
                    _knight.TakeDamage(1);
                }
                // Knight hits Player
                if (_knight.IsAttackHitboxActive && _knight.AttackBox.CollidesWith(_playerSprite.Bounds))
                {
                    _playerSprite.TakeDamage(_knight.Direction);
                }
            }


            // Apply Player Movement & Bounds Checking
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

            // Clamp player to screen
            float scale = _playerSprite.Scale;
            float frameWidth = 128 * scale;
            float boxWidth = frameWidth * 0.35f;
            float xOffset = (frameWidth - boxWidth) / 2;
            float minX = -xOffset;
            float maxX = viewport.Width - xOffset - boxWidth;
            playerNewPos.X = Math.Clamp(playerNewPos.X, minX, maxX);
            _playerSprite.Position = playerNewPos;

            // Coin Spawning and Collection (Disabled in Boss Mode)
            if (!_bossFightMode)
            {
                _coinSpawnTimer += gameTime.ElapsedGameTime.TotalSeconds;
                if (_coinSpawnTimer > 1.0)
                {
                    _coinSpawnTimer = 0;
                    var coin = new Coin();
                    coin.LoadContent(_content);
                    coin.Position = new Vector2(_random.Next(0, _graphicsDeviceManager.GraphicsDevice.Viewport.Width - 64), -64);
                    _coins.Add(coin);
                }
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

            // Update 3D Heart Positions
            float angle = (float)gameTime.TotalGameTime.TotalSeconds * 1.5f;
            float worldHalfHeight = 10f * (float)Math.Tan(MathHelper.Pi / 8f);
            float worldHalfWidth = worldHalfHeight * viewport.AspectRatio;
            float heartScale = 0.2f;
            float heartSpacing = 1f;

            // Calculate heart positions for Minotaur
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
                // Hide hearts if enemy is gone
                for (int i = 0; i < _minotaurHearts.Count; i++)
                {
                    _minotaurHearts[i].World = Matrix.CreateTranslation(-1000, -1000, 0);
                }
            }

            // Calculate heart positions for Skeleton
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

            // Calculate heart positions for Knight Boss
            if (_knight != null && !_knight.IsRemoved)
            {
                float pixelX = _knight.Position.X + _knight.Width / 2;
                float pixelY = _knight.Position.Y + 30;
                float worldX = (pixelX - viewport.Width / 2) / (viewport.Width / 2) * worldHalfWidth;
                float worldY = -(pixelY - viewport.Height / 2) / (viewport.Height / 2) * worldHalfHeight;
                Vector3 basePosition = new Vector3(worldX, worldY, 0);
                for (int i = 0; i < _knightHearts.Count; i++)
                {
                    xOffset = (i - (_knightHearts.Count - 1) / 2.0f) * heartSpacing;
                    _knightHearts[i].World = Matrix.CreateScale(heartScale) * Matrix.CreateRotationY(angle) * Matrix.CreateTranslation(basePosition + new Vector3(xOffset, 0, 0));
                }
            }
            else
            {
                for (int i = 0; i < _knightHearts.Count; i++)
                {
                    _knightHearts[i].World = Matrix.CreateTranslation(-1000, -1000, 0);
                }
            }


            // Calculate heart positions for player
            float topEdgeOfView = worldHalfHeight - 0.75f;
            float rightEdgeOfView = worldHalfWidth - 1.5f;
            for (int i = 0; i < _playerHearts.Count; i++)
            {
                xOffset = rightEdgeOfView - (i * (heartSpacing + 0.2f));
                _playerHearts[i].World = Matrix.CreateScale(heartScale) * Matrix.CreateRotationY(angle) * Matrix.CreateTranslation(new Vector3(xOffset, topEdgeOfView, 0));
            }

        }

        // Helper to spawn a new Minotaur on left or right
        private void SpawnMinotaur()
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            _minotaur = new Minotaur();
            _minotaur.LoadContent(_content);

            Vector2 spawnPos;
            Vector2 targetPos;
            Direction walkInDir;

            if (_random.Next(2) == 0) // Spawn Right
            {
                spawnPos = new Vector2(viewport.Width + 100, GROUND_Y - _minotaur.Height);
                targetPos = new Vector2(viewport.Width - _minotaur.Width, GROUND_Y - _minotaur.Height);
                walkInDir = Direction.Left;
            }
            else // Spawn Left
            {
                spawnPos = new Vector2(-100 - _minotaur.Width, GROUND_Y - _minotaur.Height);
                targetPos = new Vector2(0, GROUND_Y - _minotaur.Height);
                walkInDir = Direction.Right;
            }

            _minotaur.WalkIn(spawnPos, targetPos, walkInDir);
            _nextSpawn = SpawnState.Skeleton;

            // Reset hearts
            _minotaurHearts.Clear();
            for (int i = 0; i < _minotaur.Health; i++)
            {
                _minotaurHearts.Add(new Heart(Game1.Instance, Color.Red));
            }
        }

        // Helper to spawn a new Skeleton on left or right
        private void SpawnSkeleton()
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            _skeleton = new Skeleton();
            _skeleton.LoadContent(_content);

            Vector2 spawnPos;
            Vector2 targetPos;
            Direction walkInDir;

            if (_random.Next(2) == 0) // Spawn Right
            {
                spawnPos = new Vector2(viewport.Width + 100, GROUND_Y - _skeleton.Height);
                targetPos = new Vector2(viewport.Width - _skeleton.Width, GROUND_Y - _skeleton.Height);
                walkInDir = Direction.Left;
            }
            else // Spawn Left
            {
                spawnPos = new Vector2(-100 - _skeleton.Width, GROUND_Y - _skeleton.Height);
                targetPos = new Vector2(0, GROUND_Y - _skeleton.Height);
                walkInDir = Direction.Right;
            }

            _skeleton.WalkIn(spawnPos, targetPos, walkInDir);
            _nextSpawn = SpawnState.Minotaur;

            // Reset hearts
            _skeletonHearts.Clear();
            for (int i = 0; i < _skeleton.Health; i++)
            {
                _skeletonHearts.Add(new Heart(Game1.Instance, Color.Green));
            }
        }

        // Helper to spawn the Knight Boss
        private void SpawnKnight()
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            _knight = new Knight();
            _knight.LoadContent(_content);

            Vector2 spawnPos = new Vector2(viewport.Width + 100, GROUND_Y - _knight.Height);
            Vector2 targetPos = new Vector2(viewport.Width - _knight.Width - 100, GROUND_Y - _knight.Height);
            _knight.WalkIn(spawnPos, targetPos, Direction.Left);

            _knightHearts.Clear();
            for (int i = 0; i < _knight.Health; i++)
            {
                _knightHearts.Add(new Heart(Game1.Instance, Color.Purple));
            }
        }

        // Draw 2D game elements
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var viewport = _graphicsDeviceManager.GraphicsDevice.Viewport;
            _tilemap.Draw(gameTime, spriteBatch);

            _playerSprite.Draw(spriteBatch);
            _minotaur?.Draw(spriteBatch);
            _skeleton?.Draw(spriteBatch);
            _knight?.Draw(spriteBatch);

            foreach (var coin in _coins)
            {
                coin.Draw(spriteBatch);
            }

            // Draw Score Box
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

            // Draw Pause Overlay
            if (_isPaused)
            {
                spriteBatch.Draw(_whitePixelTexture, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.7f);

                Vector2 center = new Vector2(viewport.Width / 2, viewport.Height / 2);

                string title = "PAUSED";
                Vector2 titleSize = _spriteFont.MeasureString(title);
                spriteBatch.DrawString(_spriteFont, title, center - titleSize / 2 - new Vector2(0, 60), Color.White);

                for (int i = 0; i < _pauseOptions.Count; i++)
                {
                    Color color = (i == _pauseSelection) ? Color.Yellow : Color.Gray;
                    string text = (i == _pauseSelection) ? $"> {_pauseOptions[i]} <" : _pauseOptions[i];
                    Vector2 textSize = _spriteFont.MeasureString(text);
                    spriteBatch.DrawString(_spriteFont, text, center - textSize / 2 + new Vector2(0, i * 40), color);
                }
            }

        }

        // Draw 3D game elements
        public void Draw3D(GameTime gameTime, GraphicsDevice graphicsDevice)
        {
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            graphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            // Draw Minotaur Hearts
            int minotaurHeartsToDraw = 0;
            if (_minotaur != null && !_minotaur.IsRemoved) minotaurHeartsToDraw = _minotaur.Health;
            for (int i = 0; i < minotaurHeartsToDraw; i++)
            {
                if (i < _minotaurHearts.Count) _minotaurHearts[i].Draw();
            }

            // Draw Skeleton Hearts
            int skeletonHeartsToDraw = 0;
            if (_skeleton != null && !_skeleton.IsRemoved) skeletonHeartsToDraw = _skeleton.Health;
            for (int i = 0; i < skeletonHeartsToDraw; i++)
            {
                if (i < _skeletonHearts.Count) _skeletonHearts[i].Draw();
            }

            // Draw Knight Hearts
            int knightHeartsToDraw = 0;
            if (_knight != null && !_knight.IsRemoved) knightHeartsToDraw = _knight.Health;
            for (int i = 0; i < knightHeartsToDraw; i++)
            {
                if (i < _knightHearts.Count) _knightHearts[i].Draw();
            }


            // Draw Player Hearts
            int playerHeartsToDraw = _playerSprite.Health;
            for (int i = 0; i < playerHeartsToDraw; i++)
            {
                if (i < _playerHearts.Count) _playerHearts[i].Draw();
            }
        }

        // Serialize current state to JSON and save
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
                },
                NextSpawn = _nextSpawn
            };
            // Note: Knight state is not saved in this example.
            // You would add a KnightData class to GameState.cs to save it.

            SaveManager.Save(state);
            Console.WriteLine("Game Saved!");
        }

        // Deserialize state from JSON and restore game
        private void LoadGame()
        {
            var state = SaveManager.Load();
            if (state == null)
            {
                Console.WriteLine("Load failed or no save found.");
                return;
            }

            // Disable boss fight mode when loading a normal game
            _bossFightMode = false;

            _score = state.Score;
            _nextSpawn = state.NextSpawn;

            // Restore Player
            _playerSprite.SetHealth(state.Player.Health);
            _playerSprite.SetState(state.Player.State);
            _playerSprite.KnockbackVelocity = state.Player.KnockbackVelocity.ToVector2();
            _playerSprite._currentDirection = state.Player.Direction;
            _playerSprite.Position = state.Player.Position.ToVector2();

            // Restore Coins
            _coins.Clear();
            foreach (var pos in state.CoinPositions)
            {
                var coin = new Coin();
                coin.LoadContent(_content);
                coin.Position = pos.ToVector2();
                _coins.Add(coin);
            }

            // Restore Minotaur
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

            // Restore Skeleton
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

            // Clear boss
            _knight = null;

            // Re-initialize hearts after load
            _playerHearts.Clear();
            for (int i = 0; i < _playerSprite.Health; i++)
            {
                _playerHearts.Add(new Heart(Game1.Instance, Color.Blue));
            }
            _minotaurHearts.Clear();
            if (_minotaur != null)
            {
                for (int i = 0; i < _minotaur.Health; i++)
                {
                    _minotaurHearts.Add(new Heart(Game1.Instance, Color.Red));
                }
            }
            _skeletonHearts.Clear();
            if (_skeleton != null)
            {
                for (int i = 0; i < _skeleton.Health; i++)
                {
                    _skeletonHearts.Add(new Heart(Game1.Instance, Color.Green));
                }
            }
            _knightHearts.Clear();

            Console.WriteLine("Game Loaded!");
        }
    }
}