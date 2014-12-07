using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace Popgun.Input
{
    public class InputManager
    {
        private KeyboardState CurrentKeyState, PrevKeyState;
        private MouseState CurrentMouseState, PrevMouseState;

        private static InputManager instance;
        public static InputManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new InputManager();
                return instance;
            }
        }

        private InputManager()
        {
        }

        public void Update(GameTime time)
        {
            PrevKeyState = CurrentKeyState;
            PrevMouseState = CurrentMouseState;
            if (!ScreenManager.Instance.IsTransitioning)
            {
                CurrentKeyState = Keyboard.GetState();
                CurrentMouseState = Mouse.GetState();
            }
        }

        public bool LeftMouseButtonClicked()
        {
            return CurrentMouseState.LeftButton == ButtonState.Released && PrevMouseState.LeftButton == ButtonState.Pressed;
        }

        public bool KeyPressed(params Keys[] keys)
        {
            foreach (Keys key in keys)
                if (CurrentKeyState.IsKeyDown(key) && PrevKeyState.IsKeyUp(key))
                    return true;
            return false;
        }

        public bool KeyReleased(params Keys[] keys)
        {
            foreach (Keys key in keys)
                if (CurrentKeyState.IsKeyUp(key) && PrevKeyState.IsKeyDown(key))
                    return true;
            return false;
        }

        public bool KeyDown(params Keys[] keys)
        {
            foreach (Keys key in keys)
                if (CurrentKeyState.IsKeyDown(key))
                    return true;
            return false;
        }
    }
}
