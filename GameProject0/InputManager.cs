using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameProject0
{
    public class InputManager
    {
        private KeyboardState _currentKeyboardState;
        private KeyboardState _priorKeyboardState;

        public Vector2 Direction { get; private set; }

        public bool Exit { get; private set; } = false;

        public bool Select { get; private set; } = false;

        public void Update(GameTime gameTime)
        {
            _priorKeyboardState = _currentKeyboardState;
            _currentKeyboardState = Keyboard.GetState();

            Direction = Vector2.Zero;
            Select = false;

            if (_currentKeyboardState.IsKeyDown(Keys.Left))
            {
                Direction += new Vector2(-1, 0);
            }
            if (_currentKeyboardState.IsKeyDown(Keys.Right))
            {
                Direction += new Vector2(1, 0);
            }

            if (_currentKeyboardState.IsKeyDown(Keys.Enter) && _priorKeyboardState.IsKeyUp(Keys.Enter))
            {
                Select = true;
            }

            if(_currentKeyboardState.IsKeyDown(Keys.Escape))
            {
                Exit = true;
            }

        }

    }
}
