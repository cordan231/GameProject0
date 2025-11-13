using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace GameProject0
{
    public interface IGameScreen
    {
        void Initialize(ScreenManager screenManager, ContentManager content, GraphicsDeviceManager graphicsDeviceManager);
        void LoadContent();
        void Update(GameTime gameTime, InputManager inputManager);
        void Draw(GameTime gameTime, SpriteBatch spriteBatch);
        void Draw3D(GameTime gameTime, GraphicsDevice graphicsDevice);
    }
}