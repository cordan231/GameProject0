using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace GameProject0
{
    public class InputManager
    {
        // Store current and previous keyboard states
        private KeyboardState _currentKeyboardState;
        private KeyboardState _priorKeyboardState;

        // Stores current and previous gamepad states
        private GamePadState _currentGamePadState;
        private GamePadState _priorGamePadState;

        public Vector2 Direction { get; private set; }
        public bool Exit { get; private set; } = false;
        public bool Pause { get; private set; } = false;
        public bool MenuBack { get; private set; } = false;
        public bool Select { get; private set; } = false;
        public bool UsePotion { get; private set; } = false;
        public bool Attack { get; private set; } = false;
        public bool Roll { get; private set; } = false;

        public bool Save { get; private set; } = false;
        public bool Load { get; private set; } = false;

        public bool CheatActivated { get; private set; } = false;

        // Cheat Buffers
        private List<Keys> _keyBuffer = new List<Keys>();
        private List<Buttons> _btnBuffer = new List<Buttons>();

        // Keyboard cheat code: F, I, A, N, C, E, E
        private readonly Keys[] _cheatKeys = { Keys.F, Keys.I, Keys.A, Keys.N, Keys.C, Keys.E, Keys.E };
        // Controller cheat code: Up, Up, Down, Down, Left, Right, Left, Right
        private readonly Buttons[] _cheatBtns = {
            Buttons.DPadUp, Buttons.DPadUp,
            Buttons.DPadDown, Buttons.DPadDown,
            Buttons.DPadLeft, Buttons.DPadRight,
            Buttons.DPadLeft, Buttons.DPadRight
        };


        // Update input states
        public void Update(GameTime gameTime)
        {
            _priorKeyboardState = _currentKeyboardState;
            _currentKeyboardState = Keyboard.GetState();

            _priorGamePadState = _currentGamePadState;
            _currentGamePadState = GamePad.GetState(PlayerIndex.One);

            Direction = Vector2.Zero;
            Exit = false;
            Pause = false;
            MenuBack = false;
            Select = false;
            Attack = false;
            UsePotion = false;
            Roll = false;
            Save = false;
            Load = false;
            CheatActivated = false;

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

            if (_currentGamePadState.IsConnected)
            {
                Direction += new Vector2(_currentGamePadState.ThumbSticks.Left.X, -_currentGamePadState.ThumbSticks.Left.Y);

                if (_currentGamePadState.DPad.Left == ButtonState.Pressed) Direction += new Vector2(-1, 0);
                if (_currentGamePadState.DPad.Right == ButtonState.Pressed) Direction += new Vector2(1, 0);
                if (_currentGamePadState.DPad.Up == ButtonState.Pressed) Direction += new Vector2(0, -1);
                if (_currentGamePadState.DPad.Down == ButtonState.Pressed) Direction += new Vector2(0, 1);
            }

            // Single press checks
            // Select
            if ((_currentKeyboardState.IsKeyDown(Keys.E) && _priorKeyboardState.IsKeyUp(Keys.E)) ||
                (_currentGamePadState.IsButtonDown(Buttons.A) && _priorGamePadState.IsButtonUp(Buttons.A)))
            {
                Select = true;
            }

            // Exit
            if (_currentKeyboardState.IsKeyDown(Keys.Escape) && _priorKeyboardState.IsKeyUp(Keys.Escape))
            {
                Exit = true;
            }

            // Pause
            if ((_currentKeyboardState.IsKeyDown(Keys.Escape) && _priorKeyboardState.IsKeyUp(Keys.Escape)) ||
                (_currentGamePadState.IsButtonDown(Buttons.Start) && _priorGamePadState.IsButtonUp(Buttons.Start)))
            {
                Pause = true;
            }

            // MenuBack
            if ((_currentKeyboardState.IsKeyDown(Keys.Escape) && _priorKeyboardState.IsKeyUp(Keys.Escape)) ||
                (_currentGamePadState.IsButtonDown(Buttons.B) && _priorGamePadState.IsButtonUp(Buttons.B)))
            {
                MenuBack = true;
            }

            // Attack (E or X)
            if ((_currentKeyboardState.IsKeyDown(Keys.E) && _priorKeyboardState.IsKeyUp(Keys.E)) ||
                (_currentGamePadState.IsButtonDown(Buttons.X) && _priorGamePadState.IsButtonUp(Buttons.X)))
            {
                Attack = true;
            }

            // Roll (Space or B)
            if ((_currentKeyboardState.IsKeyDown(Keys.Space) && _priorKeyboardState.IsKeyUp(Keys.Space)) ||
                (_currentGamePadState.IsButtonDown(Buttons.B) && _priorGamePadState.IsButtonUp(Buttons.B)))
            {
                Roll = true;
            }

            // Potion (Q or Y)
            if ((_currentKeyboardState.IsKeyDown(Keys.Q) && _priorKeyboardState.IsKeyUp(Keys.Q)) ||
                (_currentGamePadState.IsButtonDown(Buttons.Y) && _priorGamePadState.IsButtonUp(Buttons.Y)))
            {
                UsePotion = true;
            }

            // Save/Load
            if (_currentKeyboardState.IsKeyDown(Keys.F5) && _priorKeyboardState.IsKeyUp(Keys.F5))
            {
                Save = true;
            }
            if (_currentKeyboardState.IsKeyDown(Keys.F9) && _priorKeyboardState.IsKeyUp(Keys.F9))
            {
                Load = true;
            }

            // Cheat Detection
            DetectCheats();
        }

        private void DetectCheats()
        {
            // Keyboard Detection
            var pressedKeys = _currentKeyboardState.GetPressedKeys();
            foreach (var key in pressedKeys)
            {
                if (_priorKeyboardState.IsKeyUp(key))
                {
                    _keyBuffer.Add(key);
                    if (_keyBuffer.Count > _cheatKeys.Length) _keyBuffer.RemoveAt(0);
                    if (_keyBuffer.SequenceEqual(_cheatKeys)) CheatActivated = true;
                }
            }

            // Gamepad Detection
            CheckButton(Buttons.DPadUp);
            CheckButton(Buttons.DPadDown);
            CheckButton(Buttons.DPadLeft);
            CheckButton(Buttons.DPadRight);
        }

        private void CheckButton(Buttons btn)
        {
            if (_currentGamePadState.IsButtonDown(btn) && _priorGamePadState.IsButtonUp(btn))
            {
                _btnBuffer.Add(btn);
                if (_btnBuffer.Count > _cheatBtns.Length) _btnBuffer.RemoveAt(0);
                if (_btnBuffer.SequenceEqual(_cheatBtns)) CheatActivated = true;
            }
        }

    }
}