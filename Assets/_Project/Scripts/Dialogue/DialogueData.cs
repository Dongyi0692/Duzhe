using System;
using System.Collections.Generic;
using UnityEngine;

namespace Duzhe.Dialogue
{
    /// Runtime mirror of the delivered TXT_CHxx.json schema (delivery contract
    /// section 2). The JSON files are the single source of truth; this layer
    /// only parses and indexes them by id. Task flow is NEVER derived from
    /// dialogue text, only from these ids.
    [Serializable]
    public class ChapterDialogue
    {
        public string schemaVersion = "";
        public string chapterId = "";
        public string displayName = "";
        public List<DialogueSegment> segments = new List<DialogueSegment>();
        public List<ObjectiveEntry> objectives = new List<ObjectiveEntry>();
        public List<ItemEntry> items = new List<ItemEntry>();
    }

    [Serializable]
    public class DialogueSegment
    {
        public string segmentId = "";
        public string stageId = "";
        public List<DialogueLine> lines = new List<DialogueLine>();
    }

    [Serializable]
    public class DialogueLine
    {
        public string lineId = "";
        public string chapterId = "";
        public string stageId = "";
        public string speakerId = "";
        public string text = "";
    }

    [Serializable]
    public class ObjectiveEntry
    {
        public string stageId = "";
        public string text = "";
    }

    [Serializable]
    public class ItemEntry
    {
        public string itemId = "";
        public string displayName = "";
        public string description = "";
    }

    [Serializable]
    public class UiStringEntry
    {
        public string key = "";
        public string text = "";
    }

    [Serializable]
    public class UiStringsFile
    {
        public string schemaVersion = "";
        public List<UiStringEntry> strings = new List<UiStringEntry>();
    }

    /// Runtime mirror of the delivered PresentationMap.json (contract section
    /// 2): presentationId / sceneId / galleryTitle / orderedSegmentIds /
    /// visualNotes. visualNotes are Chinese production notes for humans - the
    /// code never parses them.
    [Serializable]
    public class PresentationEntry
    {
        public string presentationId = "";
        public string sceneId = "";
        public string galleryTitle = "";
        public string[] orderedSegmentIds = new string[0];
        public string visualNotes = "";
    }

    [Serializable]
    public class PresentationMapFile
    {
        public string schemaVersion = "";
        public List<PresentationEntry> presentations = new List<PresentationEntry>();
    }

    /// Runtime mirror of the delivered CharacterDisplayNames.json.
    [Serializable]
    public class ActorDisplayNameEntry
    {
        public string actorId = "";
        public string displayName = "";
    }

    [Serializable]
    public class CharacterDisplayNamesFile
    {
        public string schemaVersion = "";
        public List<ActorDisplayNameEntry> actors = new List<ActorDisplayNameEntry>();
    }

    /// Loads every TXT_*.json from Resources/Data/Dialogue once per run and
    /// builds id indexes. Broken files are reported with the concrete file /
    /// id and are skipped, never silently accepted.
    public class DialogueLibrary
    {
        public static DialogueLibrary Instance { get; private set; }

        private readonly Dictionary<string, ChapterDialogue> chapters =
            new Dictionary<string, ChapterDialogue>();
        private readonly Dictionary<string, Dictionary<string, DialogueSegment>> segmentsByChapter =
            new Dictionary<string, Dictionary<string, DialogueSegment>>();
        private readonly Dictionary<string, Dictionary<string, ObjectiveEntry>> objectivesByChapter =
            new Dictionary<string, Dictionary<string, ObjectiveEntry>>();
        private readonly Dictionary<string, ItemEntry> items =
            new Dictionary<string, ItemEntry>();
        private readonly Dictionary<string, string> uiStrings =
            new Dictionary<string, string>();
        private readonly Dictionary<string, PresentationEntry> presentations =
            new Dictionary<string, PresentationEntry>();
        private readonly Dictionary<string, string> globalActorNames =
            new Dictionary<string, string>();

        public static void Load()
        {
            if (Instance != null) return; // load once per domain
            Instance = new DialogueLibrary();
            Instance.LoadAll();
        }

        private void LoadAll()
        {
            TextAsset[] assets = Resources.LoadAll<TextAsset>("Data/Dialogue");
            foreach (TextAsset asset in assets)
            {
                if (asset.name == "TXT_UI") { LoadUiFile(asset); continue; }
                if (!asset.name.StartsWith("TXT_CH", StringComparison.OrdinalIgnoreCase)) continue;
                LoadChapterFile(asset);
            }
            LoadPresentations();
            LoadActorDisplayNames();
        }

        private void LoadPresentations()
        {
            TextAsset asset = Resources.Load<TextAsset>("Data/Dialogue/PresentationMap");
            if (asset == null)
            {
                Debug.LogWarning("[Duzhe] PresentationMap.json not imported yet; presentations unavailable.");
                return;
            }
            PresentationMapFile data;
            try
            {
                data = JsonUtility.FromJson<PresentationMapFile>(asset.text);
            }
            catch (Exception e)
            {
                Debug.LogError("[Duzhe] PresentationMap.json failed to parse: " + e.Message);
                return;
            }
            if (data == null || data.presentations == null) return;
            foreach (PresentationEntry p in data.presentations)
            {
                if (string.IsNullOrEmpty(p.presentationId))
                {
                    Debug.LogError("[Duzhe] PresentationMap.json: entry with empty presentationId skipped.");
                    continue;
                }
                if (presentations.ContainsKey(p.presentationId))
                {
                    Debug.LogError("[Duzhe] PresentationMap.json: duplicate presentationId " + p.presentationId + "; second one ignored.");
                    continue;
                }
                if (p.orderedSegmentIds == null || p.orderedSegmentIds.Length == 0)
                {
                    Debug.LogError("[Duzhe] PresentationMap.json: " + p.presentationId + " has no orderedSegmentIds.");
                    continue;
                }
                presentations.Add(p.presentationId, p);
            }
        }

        private void LoadActorDisplayNames()
        {
            TextAsset asset = Resources.Load<TextAsset>("Data/Dialogue/CharacterDisplayNames");
            if (asset == null)
            {
                Debug.LogWarning("[Duzhe] CharacterDisplayNames.json not imported yet; using chapter-config names.");
                return;
            }
            CharacterDisplayNamesFile data;
            try
            {
                data = JsonUtility.FromJson<CharacterDisplayNamesFile>(asset.text);
            }
            catch (Exception e)
            {
                Debug.LogError("[Duzhe] CharacterDisplayNames.json failed to parse: " + e.Message);
                return;
            }
            if (data == null || data.actors == null) return;
            foreach (ActorDisplayNameEntry a in data.actors)
            {
                if (string.IsNullOrEmpty(a.actorId) || string.IsNullOrEmpty(a.displayName)) continue;
                globalActorNames[a.actorId] = a.displayName;
            }
        }

        private void LoadChapterFile(TextAsset asset)
        {
            ChapterDialogue data;
            try
            {
                data = JsonUtility.FromJson<ChapterDialogue>(asset.text);
            }
            catch (Exception e)
            {
                Debug.LogError("[Duzhe] Dialogue file " + asset.name + ".json failed to parse: " + e.Message);
                return;
            }
            if (data == null || string.IsNullOrEmpty(data.chapterId))
            {
                Debug.LogError("[Duzhe] Dialogue file " + asset.name + ".json is missing root field 'chapterId'.");
                return;
            }
            if (chapters.ContainsKey(data.chapterId))
            {
                Debug.LogError("[Duzhe] Duplicate chapterId " + data.chapterId + " in " + asset.name + ".json; second file ignored.");
                return;
            }

            var segIndex = new Dictionary<string, DialogueSegment>();
            foreach (DialogueSegment seg in data.segments)
            {
                if (string.IsNullOrEmpty(seg.segmentId))
                {
                    Debug.LogError("[Duzhe] " + asset.name + ".json: segment with empty segmentId skipped.");
                    continue;
                }
                if (segIndex.ContainsKey(seg.segmentId))
                {
                    Debug.LogError("[Duzhe] " + asset.name + ".json: duplicate segmentId " + seg.segmentId + "; second one ignored.");
                    continue;
                }
                foreach (DialogueLine line in seg.lines)
                {
                    if (string.IsNullOrEmpty(line.lineId) || string.IsNullOrEmpty(line.speakerId) ||
                        string.IsNullOrEmpty(line.text))
                    {
                        Debug.LogError("[Duzhe] " + asset.name + ".json segment " + seg.segmentId +
                            ": line missing lineId/speakerId/text; it will still be shown but must be fixed before release.");
                    }
                }
                segIndex.Add(seg.segmentId, seg);
            }

            chapters.Add(data.chapterId, data);
            segmentsByChapter.Add(data.chapterId, segIndex);

            var objIndex = new Dictionary<string, ObjectiveEntry>();
            foreach (ObjectiveEntry o in data.objectives)
            {
                if (!string.IsNullOrEmpty(o.stageId) && !objIndex.ContainsKey(o.stageId))
                    objIndex.Add(o.stageId, o);
            }
            objectivesByChapter.Add(data.chapterId, objIndex);

            foreach (ItemEntry it in data.items)
            {
                if (string.IsNullOrEmpty(it.itemId)) continue;
                if (items.ContainsKey(it.itemId))
                {
                    Debug.LogError("[Duzhe] " + asset.name + ".json: duplicate itemId " + it.itemId + "; second one ignored.");
                    continue;
                }
                items.Add(it.itemId, it);
            }
        }

        private void LoadUiFile(TextAsset asset)
        {
            UiStringsFile data;
            try
            {
                data = JsonUtility.FromJson<UiStringsFile>(asset.text);
            }
            catch (Exception e)
            {
                Debug.LogError("[Duzhe] TXT_UI.json failed to parse: " + e.Message);
                return;
            }
            if (data == null) return;
            foreach (UiStringEntry e in data.strings)
            {
                if (string.IsNullOrEmpty(e.key))
                {
                    Debug.LogError("[Duzhe] TXT_UI.json: entry with empty key skipped.");
                    continue;
                }
                if (uiStrings.ContainsKey(e.key))
                {
                    Debug.LogError("[Duzhe] TXT_UI.json: duplicate key " + e.key + "; second one ignored.");
                    continue;
                }
                uiStrings.Add(e.key, e.text ?? "");
            }
        }

        public bool TryGetSegment(string chapterId, string segmentId, out DialogueSegment segment)
        {
            segment = null;
            Dictionary<string, DialogueSegment> segIndex;
            if (chapterId == null || !segmentsByChapter.TryGetValue(chapterId, out segIndex)) return false;
            return segIndex.TryGetValue(segmentId ?? "", out segment);
        }

        public bool TryGetChapter(string chapterId, out ChapterDialogue chapter)
        {
            return chapters.TryGetValue(chapterId ?? "", out chapter);
        }

        public bool TryGetPresentation(string presentationId, out PresentationEntry presentation)
        {
            return presentations.TryGetValue(presentationId ?? "", out presentation);
        }

        public int PresentationCount { get { return presentations.Count; } }

        /// Display name from the delivered CharacterDisplayNames.json; empty
        /// when the actor is unknown there.
        public string GetGlobalActorDisplayName(string actorId)
        {
            string name;
            return globalActorNames.TryGetValue(actorId ?? "", out name) ? name : "";
        }

        /// All actor ids known to the delivered CharacterDisplayNames.json.
        public IEnumerable<string> GlobalActorIds { get { return globalActorNames.Keys; } }

        public string GetObjectiveText(string chapterId, string stageId, string fallback)
        {
            Dictionary<string, ObjectiveEntry> objIndex;
            if (chapterId != null && objectivesByChapter.TryGetValue(chapterId, out objIndex))
            {
                ObjectiveEntry o;
                if (objIndex.TryGetValue(stageId ?? "", out o) && !string.IsNullOrEmpty(o.text))
                    return o.text;
            }
            return fallback;
        }

        public string GetItemDisplayName(string itemId, string fallback)
        {
            ItemEntry entry;
            return items.TryGetValue(itemId ?? "", out entry) && !string.IsNullOrEmpty(entry.displayName)
                ? entry.displayName
                : fallback;
        }

        public string GetUiString(string key, string fallback)
        {
            string value;
            return uiStrings.TryGetValue(key ?? "", out value) && !string.IsNullOrEmpty(value) ? value : fallback;
        }

        public int ChapterCount { get { return chapters.Count; } }
    }
}
