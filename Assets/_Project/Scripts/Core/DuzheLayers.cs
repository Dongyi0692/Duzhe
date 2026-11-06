using UnityEngine;

namespace Duzhe.Core
{
    /// Physics2D layer indices, written into TagManager by ProjectSetupUtility.
    /// Keep in sync with Docs/ProductionLogs/ProjectEnvironment.md.
    public static class DuzheLayers
    {
        public const int Player = 8;
        public const int WorldSolid = 9;
        public const int Interactable = 10;
        public const int Trigger = 11;
        public const int Boat = 12;
        public const int RiverHazard = 13;

        public static int LayerMaskOf(params int[] layers)
        {
            int mask = 0;
            foreach (int l in layers) mask |= 1 << l;
            return mask;
        }
    }

    /// Sorting layer names, written into TagManager by ProjectSetupUtility.
    public static class DuzheSortingLayers
    {
        public const string Background = "Background";
        public const string GroundProps = "GroundProps";
        public const string Actors = "Actors";
        public const string Foreground = "Foreground";
        public const string Effects = "Effects";
    }

    /// Fixed ids used across the whole project. Display names may change,
    /// ids never do (Delivery contract section 1).
    public static class DuzheIds
    {
        public const string SceneMainMenu = "MainMenu";
        public const string SceneUnderworld = "Underworld";
        public const string SceneRiver = "River";
        public const string SceneFarmCourtyard = "FarmCourtyard";
        public const string ScenePark = "Park";
        public const string SceneRecordingStudio = "RecordingStudio";
        public const string SceneClassroom = "Classroom";
    }
}
