using UnityEngine;

namespace Duzhe.Core
{
    /// Loads placeholder art from Assets/_Project/Resources/Art/... (generated
    /// by ProjectSetupUtility with stable file names). If a file is missing a
    /// runtime solid-colour sprite is produced so the game still runs; the
    /// missing assetId is logged loudly for the handoff, never silently
    /// treated as finished art.
    public static class PlaceholderArt
    {
        public const string PlayerSpritePath = "Art/Characters/Duke_Placeholder";
        public const string NpcSpritePath = "Art/Characters/Npc_Placeholder";
        public const string ItemSpritePath = "Art/Props/Item_Placeholder";
        public const string BoatSpritePath = "Art/Props/Boat_Placeholder";
        public const string WhirlpoolSpritePath = "Art/Props/Whirlpool_Placeholder";
        public const string RitualAnchorSpritePath = "Art/Props/RitualAnchor_Placeholder";

        public static Sprite LoadRitualAnchorSprite()
        {
            return LoadOrFallback(RitualAnchorSpritePath, new Color32(150, 150, 160, 255), 16, 24);
        }

        public static Sprite LoadStoolSprite()
        {
            return LoadOrFallback(StoolSpritePath, new Color32(139, 108, 74, 255), 32, 32);
        }

        public static Sprite LoadCaneSprite()
        {
            return LoadOrFallback(CaneSpritePath, new Color32(150, 112, 70, 255), 16, 32);
        }

        public static Sprite LoadDoorSprite()
        {
            return LoadOrFallback(DoorSpritePath, new Color32(104, 78, 52, 255), 48, 96);
        }

        public static Sprite LoadDiningTableSprite()
        {
            return LoadOrFallback(DiningTableSpritePath, new Color32(120, 92, 62, 255), 96, 64);
        }

        /// HUD quest-item icon by itemId (C04: IT01_Glasses / IT02_Cane;
        /// C05: IT03_Necklace; C06: IT04_Drumsticks).
        public static Sprite QuestIconSprite(string itemId)
        {
            switch (itemId)
            {
                case "IT01_Glasses": return LoadItemSprite();
                case "IT02_Cane": return LoadCaneSprite();
                case "IT03_Necklace": return LoadOrFallback(NecklaceSpritePath, ItemColor, 32, 32);
                case "IT04_Drumsticks": return LoadOrFallback(DrumsticksSpritePath, ItemColor, 32, 32);
                case "IT05_LessonPlan": return LoadOrFallback(LessonPlanSpritePath, ItemColor, 32, 32);
                default:
                    // C10 (R03): the delivered generic item icon when present.
                    Sprite uiItem = Resources.Load<Sprite>("Art/UI/UI02_Item");
                    return uiItem != null ? uiItem : LoadItemSprite();
            }
        }

        public const string NecklaceSpritePath = "Art/Props/IT03_Necklace";
        public const string DrumsticksSpritePath = "Art/Props/IT04_Drumsticks";
        public const string LessonPlanSpritePath = "Art/Props/IT05_LessonPlan";
        public const string StoolSpritePath = "Art/Props/Stool_Placeholder";
        public const string CaneSpritePath = "Art/Props/Cane_Placeholder";
        public const string DoorSpritePath = "Art/Props/Door_Placeholder";
        public const string DiningTableSpritePath = "Art/Props/DiningTable_Placeholder";
        public const string UnderworldBgPath = "Art/Backgrounds/BG_Underworld_Placeholder";
        public const string RiverBgPath = "Art/Backgrounds/BG_River_Placeholder";
        public const string FarmBgPath = "Art/Backgrounds/BG_FarmCourtyard_Placeholder";
        public const string ParkBgPath = "Art/Backgrounds/BG_Park_Placeholder";
        public const string StudioBgPath = "Art/Backgrounds/BG_RecordingStudio_Placeholder";
        public const string ClassroomBgPath = "Art/Backgrounds/BG_Classroom_Placeholder";
        public const string MenuBgPath = "Art/Backgrounds/BG_MainMenu_Placeholder";

        private static readonly Color PlayerColor = new Color32(56, 78, 132, 255);
        private static readonly Color NpcColor = new Color32(126, 106, 88, 255);
        private static readonly Color ItemColor = new Color32(212, 178, 74, 255);
        private static readonly Color BoatColor = new Color32(120, 92, 62, 255);
        private static readonly Color WhirlpoolColor = new Color32(70, 96, 128, 255);

        public static Sprite LoadPlayerSprite()
        {
            return LoadOrFallback(PlayerSpritePath, PlayerColor, 24, 32);
        }

        public static Sprite LoadNpcSprite()
        {
            return LoadOrFallback(NpcSpritePath, NpcColor, 24, 32);
        }

        public static Sprite LoadItemSprite()
        {
            return LoadOrFallback(ItemSpritePath, ItemColor, 16, 16);
        }

        public static Sprite LoadBoatSprite()
        {
            return LoadOrFallback(BoatSpritePath, BoatColor, 40, 24);
        }

        public static Sprite LoadWhirlpoolSprite()
        {
            return LoadOrFallback(WhirlpoolSpritePath, WhirlpoolColor, 32, 32);
        }

        public static Sprite LoadBackgroundSprite(string path)
        {
            Sprite s = Resources.Load<Sprite>(path);
            if (s == null)
            {
                Debug.LogWarning("[Duzhe] Placeholder background missing at Resources/" + path + " (expected assetId will be listed in CodelyHandoff).");
                s = MakeSolidSprite(new Color32(24, 20, 32, 255), 64, 36);
            }
            return s;
        }

        private static Sprite LoadOrFallback(string path, Color color, int w, int h)
        {
            Sprite s = Resources.Load<Sprite>(path);
            if (s == null)
            {
                Debug.LogWarning("[Duzhe] Placeholder sprite missing at Resources/" + path + "; using runtime solid sprite.");
                s = MakeSolidSprite(color, w, h);
            }
            return s;
        }

        private static Sprite MakeSolidSprite(Color color, int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = color;
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), GameBootstrap.PixelsPerUnit);
        }
    }
}
