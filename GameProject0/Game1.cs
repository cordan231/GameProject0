using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using GameProject0.Particles;
using System;

namespace GameProject0
{
    public class Game1 : Game
    {
        // Graphics manager for the game window
        private GraphicsDeviceManager _graphics;

        // Sprite batch for rendering 2D sprites
        private SpriteBatch _spriteBatch;

        // Manages different screens in the game
        private ScreenManager _screenManager;

        // Manages user input
        private InputManager _inputManager;

        // Background music for the game
        private Song _backgroundMusic;

        // Singleton instance of the game
        public static Game1 Instance { get; private set; }

        // Particle system for blood splatters
        public BloodSplatterParticleSystem BloodSplatters { get; private set; }

        // Indicates whether gun mode is active
        public static bool GunModeActive { get; set; } = false;

        // Screen shake variables
        private float _screenShakeTimer = 0;
        private float _screenShakeMagnitude = 0;
        private Random _random = new Random();

        public Game1()
        {
            Instance = this;
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _screenManager = new ScreenManager();
            _inputManager = new InputManager();

            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.ApplyChanges();
        }

        // Initialize game components
        protected override void Initialize()
        {
            _screenManager.Initialize(Content, _graphics);

            BloodSplatters = new BloodSplatterParticleSystem(this, 1000);
            Components.Add(BloodSplatters);

            base.Initialize();
        }

        // Load game content
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _screenManager.LoadScreen(new TitleScreen());
            _backgroundMusic = Content.Load<Song>("background-music");
            MediaPlayer.IsRepeating = true;

            // Tweak 2: Set music volume to 50%
            MediaPlayer.Volume = 0.25f;

            MediaPlayer.Play(_backgroundMusic);

        }

        // Update game logic
        protected override void Update(GameTime gameTime)
        {
            _inputManager.Update(gameTime);

            if (_screenManager.ShouldExit)
            {
                Exit();
            }

            if (_screenShakeTimer > 0)
            {
                _screenShakeTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            _screenManager.Update(gameTime, _inputManager);

            base.Update(gameTime);
        }

        // Trigger a screen shake effect
        public void ShakeScreen(float magnitude, float duration)
        {
            _screenShakeMagnitude = magnitude;
            _screenShakeTimer = duration;
        }

        // Draw game elements
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.PaleTurquoise);

            _screenManager.CurrentScreen?.Draw3D(gameTime, GraphicsDevice);

            Matrix cameraTransform = Matrix.Identity;
            if (_screenShakeTimer > 0)
            {
                float x = (_random.NextSingle() * 2 - 1) * _screenShakeMagnitude;
                float y = (_random.NextSingle() * 2 - 1) * _screenShakeMagnitude;
                cameraTransform = Matrix.CreateTranslation(x, y, 0);
            }

            _spriteBatch.Begin(
                transformMatrix: cameraTransform,
                samplerState: SamplerState.PointClamp
            );
            _screenManager.Draw(gameTime, _spriteBatch);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}