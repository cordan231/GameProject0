using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GameProject0
{
    public class InputManager
    {
        // Store current and previous keyboard states
        private KeyboardState _currentKeyboardState;
        private KeyboardState _priorKeyboardState;

        public Vector2 Direction { get; private set; }
        public bool Exit { get; private set; } = false;
        public bool Select { get; private set; } = false;
        public bool Damage { get; private set; } = false;
        public bool Attack { get; private set; } = false;
        public bool Roll { get; private set; } = false;

        public bool Save { get; private set; } = false;
        public bool Load { get; private set; } = false;

        public bool GunModeToggle { get; private set; } = false;

        // Update input states
        public void Update(GameTime gameTime)
        {
            _priorKeyboardState = _currentKeyboardState;
            _currentKeyboardState = Keyboard.GetState();

            Direction = Vector2.Zero;
            Select = false;
            Attack = false;
            Damage = false;
            Roll = false;
            Save = false;
            Load = false;
            GunModeToggle = false;

            // Movement
            if (_currentKeyboardState.IsKeyDown(Keys.Left) || _currentKeyboardState.IsKeyDown(Keys.A))
            {
                Direction += new Vector2(-1, 0);
            }
            if (_currentKeyboardState.IsKeyDown(Keys.Right) || _currentKeyboardState.IsKeyDown(Keys.D))
            {
                Direction += new Vector2(1, 0);
            }
            if (_currentKeyboardState.IsKeyDown(Keys.Up) || _currentKeyboardState.IsKeyDown(Keys.W))
            {
                Direction += new Vector2(0, -1);
            }
            if (_currentKeyboardState.IsKeyDown(Keys.Down) || _currentKeyboardState.IsKeyDown(Keys.S))
            {
                Direction += new Vector2(0, 1);
            }

            // Single press checks
            if (_currentKeyboardState.IsKeyDown(Keys.Enter) && _priorKeyboardState.IsKeyUp(Keys.Enter))
            {
                Select = true;
            }

            if (_currentKeyboardState.IsKeyDown(Keys.Escape) && _priorKeyboardState.IsKeyUp(Keys.Escape))
            {
                Exit = true;
            }
            else
            {
                Exit = false;
            }

            if (_currentKeyboardState.IsKeyDown(Keys.E) && _priorKeyboardState.IsKeyUp(Keys.E))
            {
                Attack = true;
            }

            if (_currentKeyboardState.IsKeyDown(Keys.Space) && _priorKeyboardState.IsKeyUp(Keys.Space))
            {
                Roll = true;
            }

            if (_currentKeyboardState.IsKeyDown(Keys.Q) && _priorKeyboardState.IsKeyUp(Keys.Q))
            {
                Damage = true;
            }
            if (_currentKeyboardState.IsKeyDown(Keys.F5) && _priorKeyboardState.IsKeyUp(Keys.F5))
            {
                Save = true;
            }

            if (_currentKeyboardState.IsKeyDown(Keys.F9) && _priorKeyboardState.IsKeyUp(Keys.F9))
            {
                Load = true;
            }

            if (_currentKeyboardState.IsKeyDown(Keys.C) && _priorKeyboardState.IsKeyUp(Keys.C))
            {
                GunModeToggle = true;
            }

        }
    }
}