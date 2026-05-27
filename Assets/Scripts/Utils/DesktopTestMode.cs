using UnityEngine;
using UnityEngine.InputSystem;

namespace ClickerAdventure
{
    public static class DesktopTestMode
    {
        public static bool IsEnabled =>
#if UNITY_EDITOR || UNITY_STANDALONE
            true;
#else
            false;
#endif

        public static void Configure()
        {
            if (!IsEnabled)
            {
                return;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Application.targetFrameRate = 60;
        }

        public static bool Pressed(KeyCode key)
        {
            if (!IsEnabled || Keyboard.current == null)
            {
                return false;
            }

            var inputKey = ToInputSystemKey(key);
            return inputKey.HasValue && Keyboard.current[inputKey.Value].wasPressedThisFrame;
        }

        public static bool PressedAny(params KeyCode[] keys)
        {
            if (!IsEnabled)
            {
                return false;
            }

            foreach (var key in keys)
            {
                if (Pressed(key))
                {
                    return true;
                }
            }

            return false;
        }

        private static Key? ToInputSystemKey(KeyCode key)
        {
            return key switch
            {
                KeyCode.Tab => Key.Tab,
                KeyCode.Return => Key.Enter,
                KeyCode.KeypadEnter => Key.NumpadEnter,
                KeyCode.F1 => Key.F1,
                KeyCode.F5 => Key.F5,
                KeyCode.Escape => Key.Escape,
                KeyCode.Space => Key.Space,
                KeyCode.Q => Key.Q,
                KeyCode.E => Key.E,
                KeyCode.G => Key.G,
                KeyCode.R => Key.R,
                KeyCode.Alpha1 => Key.Digit1,
                KeyCode.Alpha2 => Key.Digit2,
                KeyCode.Alpha3 => Key.Digit3,
                KeyCode.Alpha4 => Key.Digit4,
                KeyCode.Alpha5 => Key.Digit5,
                KeyCode.Alpha6 => Key.Digit6,
                KeyCode.Alpha7 => Key.Digit7,
                KeyCode.Alpha8 => Key.Digit8,
                KeyCode.Alpha9 => Key.Digit9,
                KeyCode.Keypad1 => Key.Numpad1,
                KeyCode.Keypad2 => Key.Numpad2,
                KeyCode.Keypad3 => Key.Numpad3,
                KeyCode.Keypad4 => Key.Numpad4,
                KeyCode.Keypad5 => Key.Numpad5,
                KeyCode.Keypad6 => Key.Numpad6,
                KeyCode.Keypad7 => Key.Numpad7,
                KeyCode.Keypad8 => Key.Numpad8,
                KeyCode.Keypad9 => Key.Numpad9,
                _ => null
            };
        }
    }
}
