using UnityEngine;

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
            return IsEnabled && Input.GetKeyDown(key);
        }

        public static bool PressedAny(params KeyCode[] keys)
        {
            if (!IsEnabled)
            {
                return false;
            }

            foreach (var key in keys)
            {
                if (Input.GetKeyDown(key))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
