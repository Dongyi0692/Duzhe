using System;
using System.Collections.Generic;
using UnityEngine;

namespace Duzhe.Chapters
{
    /// Static per-chapter configuration (spec section 4). Built in code from
    /// the delivery specs; stage ids, chapter ids and event semantics are
    /// part of the frozen contract and never change with display names.
    [Serializable]
    public class ChapterConfig
    {
        public string chapterId;
        public string displayName;
        /// Scene the chapter starts in; CH05 uses no living-world scene.
        public string startSceneId;
        public string spawnPointId;
        /// Chapter dialogue chapterId (TXT_CHxx.json root chapterId).
        public string dialogueChapterId;
        public StageDefinition[] stages;
        /// actorId -> display name (speaker names for the dialogue view).
        public Dictionary<string, string> actorDisplayNames = new Dictionary<string, string>();
        /// itemId -> target actorId for hand-over interactions.
        public Dictionary<string, string> itemTargetActors = new Dictionary<string, string>();
        public string[] presentationIds = new string[0];
        public bool enablePerformance = true;
        /// River crossing parameters (TechnicalAndInteractionSpec section 7).
        public RiverSettings riverSettings = new RiverSettings();

        public StageDefinition GetStage(string stageId)
        {
            if (stages == null) return null;
            foreach (StageDefinition s in stages)
            {
                if (s.stageId == stageId) return s;
            }
            return null;
        }

        public string GetActorDisplayName(string actorId)
        {
            string name;
            return actorId != null && actorDisplayNames.TryGetValue(actorId, out name) ? name : null;
        }
    }

    /// One all-of completion condition of a stage (C04): the stage advances
    /// only after EVERY condition has fired at least once this run, in any
    /// order (e.g. CH01_Prepare needs glasses AND the traced cane).
    [Serializable]
    public class StageCondition
    {
        public Core.GameEventType type = Core.GameEventType.DialogueFinished;
        /// Event sourceId that satisfies this condition. Empty = any source.
        public string sourceId = "";
        /// Optional itemId granted when this condition fires (the traced cane
        /// becomes the IT02_Cane item without a world pickup).
        public string grantsItemId = "";
    }

    /// One stage of one chapter (spec section 4). entryConditions beyond the
    /// implicit chain order are explicit item requirements; completionEvent
    /// with optional expectedSourceId uniquely advances to nextStageId.
    [Serializable]
    public class StageDefinition
    {
        public string stageId;
        public string sceneId;
        public Core.InputMode inputMode = Core.InputMode.Exploring;
        public string objectiveText;
        /// Segment automatically played when the stage is entered (empty = none;
        /// used by later chapters for scripted beats).
        public string autoStartSegmentId = "";
        /// Presentation automatically started when the stage is entered
        /// (empty = none; C04 uses it for PR_CH01_Farewell).
        public string autoStartPresentationId = "";
        /// When > 0 the stage auto-advances after this many seconds of game
        /// time (pause-frozen) - used by the closing fade stage CH01_Complete.
        public float autoAdvanceSeconds = 0f;
        /// Optional HUD toast key shown once when the stage is entered
        /// (first-stage movement tutorial etc.).
        public string entryHintKey = "";
        /// Items that must already be collected before this stage can run.
        public string[] requiredItemIds = new string[0];
        /// Non-empty replaces the single completionEvent: ALL conditions must
        /// fire (any order) before the stage advances.
        public StageCondition[] requireAll = new StageCondition[0];
        public Core.GameEventType completionEvent = Core.GameEventType.DialogueFinished;
        /// Optional id the completing event's sourceId must match (e.g. the
        /// exact dialogue segment id). Empty = any source accepted.
        public string expectedSourceId = "";
        /// Next stage; empty/whitespace = chapter ends (ChapterCompleted).
        public string nextStageId = "";
    }

    /// Per-run mutable state of the current chapter. Never persisted:
    /// in-chapter precision progress is deliberately not saved (spec 9).
    public class ChapterRuntimeState
    {
        public string chapterId;
        public int stageIndex = -1;
        public readonly HashSet<string> playedSegmentIds = new HashSet<string>();
        public readonly HashSet<string> collectedItemIds = new HashSet<string>();
        public readonly HashSet<string> returnedItemIds = new HashSet<string>();
        public readonly HashSet<string> completedMinigameIds = new HashSet<string>();
        /// Condition keys ("type#sourceId") already fired for the CURRENT
        /// multi-condition stage; cleared on every stage change.
        public readonly HashSet<string> stageConditionKeys = new HashSet<string>();
        public bool chapterCompleted;
        /// CH04: set only when the COMPLETE classroom lesson dialogue ended.
        /// The "write goodbye" stage refuses to run before this is true
        /// (runtime-only; never written to the save file).
        public bool lessonFinished;
        /// CH05: the recall presentation finished / the "let go" dialogue
        /// finished. Both gate boarding; a river retry never clears them.
        public bool memoryRestored;
        public bool letGoFinished;

        /// River seed picked once per chapter run: reused on every retry of
        /// the crossing within this run (learnable), regenerated when the
        /// chapter is replayed (spec section 7).
        public int riverSeed;

        public bool HasItemCollected(string itemId)
        {
            return collectedItemIds.Contains(itemId);
        }

        public bool HasSegmentPlayed(string segmentId)
        {
            return playedSegmentIds.Contains(segmentId);
        }
    }

    /// River crossing defaults (TechnicalAndInteractionSpec section 7).
    /// Everything is tunable per chapter via ChapterConfig.
    [Serializable]
    public class RiverSettings
    {
        [Tooltip("How many passengers ride along (CH01 two, CH02-04 one, CH05 none).")]
        public int passengerCount = 1;
        [Tooltip("Max lanes a single wave may block (CH01 one, other chapters two).")]
        public int maxBannedLanesPerWave = 1;
        public float sailDuration = 10f;
        public float forwardSpeed = 5f;
        public float steerSpeed = 4f;
        public float steerRangeX = 4.5f;
        public float laneSpacing = 3f;
        public float[] waveArrivalTimes = new float[] { 3f, 5f, 7f, 9f };
        [Tooltip("Seconds a whirlpool exists before reaching the boat line (visibility lead).")]
        public float spawnLeadSeconds = 2.5f;
        [Tooltip("Half sizes of the boat collision box (boat visual is wider).")]
        public float boatHalfWidth = 0.4f;
        public float boatHalfHeight = 0.25f;
        [Tooltip("Half sizes of a whirlpool's collision box (visual is larger).")]
        public float whirlHalfWidth = 0.45f;
        public float whirlHalfHeight = 0.45f;
        [Tooltip("Random visual-only x jitter for whirlpools inside a lane.")]
        public float whirlJitterX = 0.4f;
        public float whirlDriftSpeed = 0f;
        [Tooltip("Camera vertical offset during the crossing (raises the visible band so whirlpools appear >=1.5s early).")]
        public float cameraYOffset = 2f;
    }
}
