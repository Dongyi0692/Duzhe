using System;
using System.Collections.Generic;

namespace Duzhe.Minigames
{
    /// The four reusable minigame interaction families (spec section 8).
    public enum MinigameType
    {
        Trace,   // MG01_Cane, MG04_Heart, MG07_Goodbye
        Drag,    // MG03_Stools, MG06_Photo
        Door,    // MG02_Door
        Notes    // MG05_Notes
    }

    /// Static per-minigame configuration. Template/sequence data is placeholder
    /// development data (marked below); formal CFG_* JSON replaces it in
    /// C04-C08 without changing IDs.
    [Serializable]
    public class MinigameConfig
    {
        public string minigameId;
        public string displayName;
        public MinigameType type;
        /// Trace template id (Cane / Heart / Goodbye).
        public string templateId = "";
        /// Drag slot / item count.
        public int slotCount = 1;
        /// Notes sequence (indices 1..7).
        public int[] sequence = new int[0];
        /// Panel reference size in UI pixels (1280x720 space).
        public float panelW = 640f;
        public float panelH = 360f;
        /// MG06_Photo only: the single empty slot on the 640x360 photo board
        /// (x, y, w, h - TOP-LEFT origin), and where the drummer starts.
        /// Photo order: drummer gap first, purple jacket second, blue third, yellow fourth.
        /// These are the replaceable seam for the upcoming photo art: when the
        /// resource side delivers the final frame, only these two rects (and
        /// the PRP06 image itself) change - no code edits.
        public float photoGapX = 197f;
        public float photoGapY = 155f;
        public float photoGapW = 54f;
        public float photoGapH = 84f;
        public float photoDrummerX = 296f;
        public float photoDrummerY = 60f;
        public float photoDrummerW = 54f;
        public float photoDrummerH = 84f;
    }

    public static class MinigameCatalog
    {
        public static readonly MinigameConfig[] All =
        {
            new MinigameConfig
            {
                minigameId = "MG01_Cane",
                displayName = "描画拐杖（Placeholder）",
                type = MinigameType.Trace,
                templateId = "Cane",
                panelW = 240f, panelH = 440f
            },
            new MinigameConfig
            {
                minigameId = "MG02_Door",
                displayName = "敲门（Placeholder）",
                type = MinigameType.Door,
                panelW = 640f, panelH = 360f
            },
            new MinigameConfig
            {
                minigameId = "MG03_Stools",
                displayName = "摆凳子（Placeholder）",
                type = MinigameType.Drag,
                slotCount = 3,
                panelW = 640f, panelH = 360f
            },
            new MinigameConfig
            {
                minigameId = "MG04_Heart",
                displayName = "描画爱心（Placeholder）",
                type = MinigameType.Trace,
                templateId = "Heart",
                panelW = 440f, panelH = 440f
            },
            new MinigameConfig
            {
                minigameId = "MG05_Notes",
                displayName = "七音引导（Placeholder 序列）",
                type = MinigameType.Notes,
                // Placeholder development sequence (spec section 8) - formal
                // CFG_Notes.json replaces it in C06 without changing IDs.
                sequence = new[] { 1, 3, 5, 6, 5, 3, 2, 4, 6, 7, 6, 4, 2, 1 },
                panelW = 720f, panelH = 300f
            },
            new MinigameConfig
            {
                minigameId = "MG06_Photo",
                displayName = "补拍合照（Placeholder）",
                type = MinigameType.Drag,
                slotCount = 4,
                panelW = 640f, panelH = 360f
            },
            new MinigameConfig
            {
                minigameId = "MG07_Goodbye",
                displayName = "写再见（Placeholder 笔画）",
                type = MinigameType.Trace,
                templateId = "Goodbye",
                panelW = 640f, panelH = 360f
            }
        };

        public static MinigameConfig Get(string minigameId)
        {
            foreach (MinigameConfig c in All)
            {
                if (c.minigameId == minigameId) return c;
            }
            return null;
        }

        /// Panel title from the delivered TXT_UI (minigame.*.title); the
        /// static displayName is the fallback when the key is missing.
        public static string Title(MinigameConfig cfg)
        {
            if (cfg == null) return "";
            string key;
            switch (cfg.minigameId)
            {
                case "MG01_Cane": key = "minigame.cane.title"; break;
                case "MG02_Door": key = "minigame.door.title"; break;
                case "MG03_Stools": key = "minigame.stools.title"; break;
                case "MG04_Heart": key = "minigame.heart.title"; break;
                case "MG05_Notes": key = "minigame.notes.title"; break;
                case "MG06_Photo": key = "minigame.photo.title"; break;
                case "MG07_Goodbye": key = "minigame.goodbye.title"; break;
                default: return cfg.displayName;
            }
            return Duzhe.UI.UiText.Get(key, cfg.displayName);
        }

        /// Per-template drawing instructions (trace minigames).
        public static string TraceInstructions(string templateId)
        {
            switch (templateId)
            {
                case "Cane": return Duzhe.UI.UiText.Get("minigame.cane.instructions",
                    "按住鼠标左键，沿轮廓描出拐杖。可以抬笔，再接着画。");
                case "Heart": return Duzhe.UI.UiText.Get("minigame.heart.instructions",
                    "按住鼠标左键，沿引导描出完整的爱心。可以抬笔。");
                case "Goodbye": return Duzhe.UI.UiText.Get("minigame.goodbye.instructions",
                    "沿亮起的笔画，用鼠标写下“再见”。一笔一笔慢慢来。");
                default: return "";
            }
        }
    }
}
