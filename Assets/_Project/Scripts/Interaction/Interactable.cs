using Duzhe.Core;
using UnityEngine;

namespace Duzhe.Interaction
{
    /// A world entity the player can interact with: quest items, NPCs, props.
    /// All progression goes through ids; this component never decides story
    /// logic on its own (that is ChapterDirector's job).
    public class Interactable : MonoBehaviour
    {
        [Tooltip("Stable entity id, e.g. DEV_GLASSES or an actorId from the delivery contract.")]
        public string interactableId = "";

        [Tooltip("Shown in the E-prompt; display text only, never used for logic.")]
        public string displayName = "";

        public InteractableKind kind = InteractableKind.Item;

        [Tooltip("Stages in which this entity responds. Empty = every stage of its chapter.")]
        public string[] activeStageIds = new string[0];

        [Header("NPC options")]
        [Tooltip("Segment automatically started when the player walks close, if allowed by the current stage and not played yet this run.")]
        public string autoTalkSegmentId = "";

        [Tooltip("Segment started when the player presses E to hand a collected item over.")]
        public string returnSegmentId = "";

        [Tooltip("itemId this NPC accepts being handed over (items' targetActorId).")]
        public string returnItemId = "";

        [Tooltip("ALL items this NPC accepts in ONE hand-over (C04 CH01: glasses + cane). When non-empty it replaces returnItemId.")]
        public string[] returnItemIds = new string[0];

        [Header("Item options")]
        [Tooltip("Chapter this item belongs to (itemBindings).")]
        public string chapterId = "";

        [Tooltip("TXT_UI key of the E-prompt text (e.g. hint.pickup / hint.ritual). Empty = the generic [E] + displayName prompt.")]
        public string promptKey = "";

        /// Items required before the E hand-over is offered.
        public string[] RequiredReturnItems
        {
            get
            {
                if (returnItemIds != null && returnItemIds.Length > 0) return returnItemIds;
                return string.IsNullOrEmpty(returnItemId) ? new string[0] : new[] { returnItemId };
            }
        }

        public bool IsActiveInStage(string stageId)
        {
            if (activeStageIds == null || activeStageIds.Length == 0) return true;
            foreach (string s in activeStageIds)
            {
                if (s == stageId) return true;
            }
            return false;
        }
    }
}
