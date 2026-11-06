using System;
using System.Collections.Generic;
using UnityEngine;

namespace Duzhe.Presentation
{
    /// Runtime mirror of the delivered PresentationLayers.json (R14): which
    /// actors are spirits (fade), which persist, whether a full-screen fade
    /// is allowed and whether text is engine-rendered. The scene casts
    /// implement the behaviour; this catalog is the contract reference used
    /// by validation and by later chapters (CH02-CH04 arrive with C05-C07).
    [Serializable]
    public class PresentationLayerItem
    {
        public string assetId = "";
        public string chapterId = "";
        public string presentationId = "";
        public string mainImagePath = "";
        public string[] spiritActorIds = new string[0];
        public string[] persistentActorIds = new string[0];
        public string[] fadeOnlyActorIds = new string[0];
        public bool neverFadeEntireCGForSpiritDeparture;
        public bool fullScreenFadeAllowed;
        public bool keepCompleteFourPersonPhoto;
        public bool blackboardTextRenderedByProgram;
    }

    [Serializable]
    public class PresentationLayersFile
    {
        public string schemaVersion = "";
        public List<PresentationLayerItem> items = new List<PresentationLayerItem>();
    }

    public static class PresentationLayerCatalog
    {
        private static Dictionary<string, PresentationLayerItem> byAssetId;
        private static bool loaded;

        public static bool TryGet(string assetId, out PresentationLayerItem item)
        {
            EnsureLoaded();
            return byAssetId.TryGetValue(assetId ?? "", out item);
        }

        public static int Count
        {
            get { EnsureLoaded(); return byAssetId.Count; }
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            byAssetId = new Dictionary<string, PresentationLayerItem>();
            TextAsset asset = Resources.Load<TextAsset>("Data/Presentations/PresentationLayers");
            if (asset == null)
            {
                Debug.LogWarning("[Duzhe] PresentationLayers.json not imported; catalog empty.");
                return;
            }
            try
            {
                PresentationLayersFile data = JsonUtility.FromJson<PresentationLayersFile>(asset.text);
                if (data == null || data.items == null) return;
                foreach (PresentationLayerItem it in data.items)
                {
                    if (it == null || string.IsNullOrEmpty(it.assetId)) continue;
                    byAssetId[it.assetId] = it;
                }
                Debug.Log("[Duzhe] PresentationLayerCatalog: " + byAssetId.Count + " items loaded.");
            }
            catch (Exception e)
            {
                Debug.LogError("[Duzhe] PresentationLayers.json parse failed: " + e.Message);
            }
        }
    }
}
