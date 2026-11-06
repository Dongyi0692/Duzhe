using UnityEngine;

namespace Duzhe.Core
{
    /// Loads formally delivered art (R11/R12/R14) from Resources. Placeholders
    /// remain the fallback so a missing formal asset never breaks the build;
    /// the missing assetId is logged loudly for the handoff instead.
    public static class FormalArt
    {
        public const string FerrymanSheetPath = "Art/Animations/CHR01_Ferryman";
        public const string IllustrationPathPrefix = "Art/Illustrations/";

        /// The 12 sliced frames of the CHR01 walk sheet, ordered
        /// [row * 3 + col]: rows Down/Left/Right/Up, cols WalkA/Idle/WalkB.
        /// Returns null when the sheet is missing or not correctly sliced.
        public static Sprite[] LoadFerrymanFrames()
        {
            Sprite[] all = Resources.LoadAll<Sprite>(FerrymanSheetPath);
            if (all == null || all.Length != 12) return null;
            Sprite[] frames = new Sprite[12];
            foreach (Sprite s in all)
            {
                if (s == null) return null;
                int col = Mathf.RoundToInt(s.rect.x / 32f);
                int row = Mathf.RoundToInt((192f - s.rect.y - s.rect.height) / 48f);
                if (row < 0 || row > 3 || col < 0 || col > 2) return null;
                frames[row * 3 + col] = s;
            }
            foreach (Sprite f in frames)
            {
                if (f == null) return null;
            }
            return frames;
        }

        /// Formal ferry (R13 PRP01, 96x64 top-down) with the painted
        /// placeholder only as a fallback.
        public static Sprite LoadBoatSprite()
        {
            Sprite s = Resources.Load<Sprite>("Art/Props/PRP01_Boat");
            return s != null ? s : PlaceholderArt.LoadBoatSprite();
        }

        /// Formal whirlpool (R13 PRP02) with the painted placeholder fallback.
        public static Sprite LoadWhirlpoolSprite()
        {
            Sprite s = Resources.Load<Sprite>("Art/Props/PRP02_Whirlpool");
            return s != null ? s : PlaceholderArt.LoadWhirlpoolSprite();
        }

        public static Sprite LoadIllustration(string assetId)        {
            Sprite s = Resources.Load<Sprite>(IllustrationPathPrefix + assetId);
            if (s == null) Debug.LogWarning("[Duzhe] Formal illustration missing: " + assetId);
            return s;
        }

        /// Formal standing sprite by character assetId (R11), with a
        /// placeholder fallback so a missing delivery never breaks a scene.
        public static Sprite LoadCharacterStand(string assetId)
        {
            Sprite s = Resources.Load<Sprite>("Art/Characters/" + assetId);
            if (s == null) Debug.LogWarning("[Duzhe] Formal character stand missing: " + assetId + "; placeholder used.");
            return s;
        }

        /// The 12 sliced frames of ANY 3x4 walk sheet (R12 contract: rows
        /// Down/Left/Right/Up, cols WalkA/Idle/WalkB), ordered [row * 3 + col].
        /// Returns null when the sheet is missing or not correctly sliced.
        /// Never relies on LoadAll order (this Tuanjie build does not
        /// guarantee it).
        public static Sprite[] LoadWalkFrames(string assetId)
        {
            Sprite[] all = Resources.LoadAll<Sprite>("Art/Animations/" + assetId);
            if (all == null || all.Length != 12) return null;
            Sprite[] frames = new Sprite[12];
            foreach (Sprite s in all)
            {
                if (s == null) return null;
                int col = Mathf.RoundToInt(s.rect.x / 32f);
                int row = Mathf.RoundToInt((192f - s.rect.y - s.rect.height) / 48f);
                if (row < 0 || row > 3 || col < 0 || col > 2) return null;
                frames[row * 3 + col] = s;
            }
            foreach (Sprite f in frames)
            {
                if (f == null) return null;
            }
            return frames;
        }
    }
}
