using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using GameProject0.Particles;

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

            _screenManager.Update(gameTime, _inputManager);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();
            _screenManager.Draw(gameTime, _spriteBatch);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}

