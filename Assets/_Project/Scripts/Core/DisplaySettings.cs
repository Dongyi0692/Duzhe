using UnityEngine;

namespace Duzhe.Core
{
    /// Persisted window mode + resolution for the desktop build (C12 playtest
    /// feedback: the player must be able to switch windowed/fullscreen, pick a
    /// resolution and resize the window).
    ///
    /// Stored in PlayerPrefs, NOT in the save file: display preferences are
    /// machine-local and must never touch chapter progress or unlocks.
    public static class DisplaySettings
    {
        private const string KeyFullscreen = "DZ_Display_Fullscreen";
        private const string KeyWidth = "DZ_Display_Width";
        private const string KeyHeight = "DZ_Display_Height";

        public const int ReferenceWidth = 1280;
        public const int ReferenceHeight = 720;

        /// Offered resolutions, ascending. The first entry is the reference
        /// composition (16:9); every entry keeps the 16:9 ratio so the
        /// CanvasScaler and the camera framing stay predictable.
        public static readonly Vector2Int[] Resolutions =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080)
        };

        public static bool Fullscreen { get; private set; }
        public static Vector2Int Resolution { get; private set; } = new Vector2Int(ReferenceWidth, ReferenceHeight);

        /// Reads the stored preference (never applies it; Apply does that).
        public static void Load()
        {
            Fullscreen = PlayerPrefs.GetInt(KeyFullscreen, 0) == 1;
            int w = PlayerPrefs.GetInt(KeyWidth, ReferenceWidth);
            int h = PlayerPrefs.GetInt(KeyHeight, ReferenceHeight);
            Resolution = FindNearest(w, h);
        }

        /// Applies the current values and writes them back.
        public static void Apply()
        {
            Screen.SetResolution(Resolution.x, Resolution.y, Fullscreen);
            PlayerPrefs.SetInt(KeyFullscreen, Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(KeyWidth, Resolution.x);
            PlayerPrefs.SetInt(KeyHeight, Resolution.y);
            PlayerPrefs.Save();
        }

        public static void ToggleFullscreen()
        {
            Fullscreen = !Fullscreen;
            Apply();
        }

        /// dir = +1 / -1 cycles through the offered resolutions.
        public static void CycleResolution(int dir)
        {
            int index = 0;
            for (int i = 0; i < Resolutions.Length; i++)
            {
                if (Resolutions[i] == Resolution) { index = i; break; }
            }
            index = (index + (dir < 0 ? Resolutions.Length - 1 : 1)) % Resolutions.Length;
            Resolution = Resolutions[index];
            Apply();
        }

        public static string ResolutionLabel()
        {
            return Resolution.x + " × " + Resolution.y;
        }

        public static string WindowLabel()
        {
            return Fullscreen ? "全屏" : "窗口化";
        }

        private static Vector2Int FindNearest(int w, int h)
        {
            for (int i = 0; i < Resolutions.Length; i++)
            {
                if (Resolutions[i].x == w && Resolutions[i].y == h) return Resolutions[i];
            }
            return new Vector2Int(ReferenceWidth, ReferenceHeight);
        }
    }
}
