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
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private ScreenManager _screenManager;
        private InputManager _inputManager;
        private Song _backgroundMusic;

        public static Game1 Instance { get; private set; }
        public BloodSplatterParticleSystem BloodSplatters { get; private set; }

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
        }

        protected override void Initialize()
        {
            _screenManager.Initialize(Content, _graphics);

            BloodSplatters = new BloodSplatterParticleSystem(this, 1000);
            Components.Add(BloodSplatters);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _screenManager.LoadScreen(new TitleScreen());
            _backgroundMusic = Content.Load<Song>("background-music");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(_backgroundMusic);

        }

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

        public void ShakeScreen(float magnitude, float duration)
        {
            _screenShakeMagnitude = magnitude;
            _screenShakeTimer = duration;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.PaleTurquoise);

            Matrix cameraTransform = Matrix.Identity;
            if (_screenShakeTimer > 0)
            {
                float x = (_random.NextSingle() * 2 - 1) * _screenShakeMagnitude;
                float y = (_random.NextSingle() * 2 - 1) * _screenShakeMagnitude;
                cameraTransform = Matrix.CreateTranslation(x, y, 0);
            }

            _spriteBatch.Begin(
                transformMatrix: cameraTransform,
                samplerState: SamplerState.PointClamp // Added for crisp pixel art
            );
            _screenManager.Draw(gameTime, _spriteBatch);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}