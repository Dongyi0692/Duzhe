using Duzhe.Core;
using UnityEngine;

namespace Duzhe.Interaction
{
    /// A world-space guidance marker ("!" in gold, gently bobbing) shown ABOVE
    /// an interactable whenever it is currently actionable - regardless of
    /// how far the player is. The old E-prompt only appears inside the 1.2u
    /// interaction radius, so players could not see where to go. This marker
    /// is visible across the whole camera view and obeys the same stage gate:
    /// - items/props: visible while their stage is active;
    /// - NPC auto-talk: visible while the segment may auto-start this run;
    /// - NPC hand-over: visible only once EVERY required item is collected.
    /// The marker hides itself on pickup/minigame completion/stage change.
    [RequireComponent(typeof(Interactable))]
    public class InteractableHintMarker : MonoBehaviour
    {
        private Interactable it;
        private TextMesh label;
        private float baseY = 1.05f;

        private void Awake()
        {
            it = GetComponent<Interactable>();
            GameObject go = new GameObject("HintMark");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, baseY, 0f);
            label = go.AddComponent<TextMesh>();
            label.text = "!";
            label.fontSize = 44;
            label.characterSize = 0.2f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color32(240, 220, 120, 255);
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingLayerName = DuzheSortingLayers.Effects;
            go.SetActive(false);
        }

        private void Update()
        {
            GameSession session = GameSession.Instance;
            bool show = false;
            if (session != null && !session.Paused && session.Chapters != null &&
                session.InputMode == InputMode.Exploring)
            {
                string stageId = session.Chapters.CurrentStageId;
                if (it.IsActiveInStage(stageId))
                {
                    switch (it.kind)
                    {
                        case InteractableKind.Npc:
                            show = NpcActionable(session);
                            break;
                        default:
                            show = true; // items/props: stage gate already checked
                            break;
                    }
                }
            }

            if (label.gameObject.activeSelf != show) label.gameObject.SetActive(show);
            if (show)
            {
                float y = baseY + Mathf.Sin(Time.time * 3f) * 0.08f;
                label.transform.localPosition = new Vector3(0f, y, 0f);
            }
        }

        private bool NpcActionable(GameSession session)
        {
            // Hand-over point: only once every required item is collected.
            string[] required = it.RequiredReturnItems;
            if (required.Length > 0)
            {
                foreach (string itemId in required)
                {
                    if (!session.Chapters.HasItemCollected(itemId)) return false;
                }
                return true;
            }
            // Auto-talk point: while the segment may still auto-start.
            if (!string.IsNullOrEmpty(it.autoTalkSegmentId))
            {
                return session.Chapters.CanAutoStartSegment(it.autoTalkSegmentId);
            }
            return false;
        }
    }
}
