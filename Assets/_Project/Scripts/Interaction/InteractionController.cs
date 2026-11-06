using System.Collections.Generic;
using Duzhe.Core;
using UnityEngine;

namespace Duzhe.Interaction
{
    /// Lives on the player. Each frame it finds the closest legal interactable
    /// (interaction distance 1.2 by default) and shows the E-prompt; E performs
    /// it. NPCs with autoTalk start a dialogue automatically at 1.4 when the
    /// current stage allows it and the segment has not played this run, so
    /// standing in range never re-triggers it (spec section 5).
    [DefaultExecutionOrder(-10)]
    public class InteractionController : MonoBehaviour
    {
        [Tooltip("Manual interaction radius in world units.")]
        public float interactDistance = 1.2f;
        [Tooltip("Auto-dialogue radius in world units.")]
        public float autoTalkDistance = 1.4f;

        [Tooltip("World-space prompt label shown above the target.")]
        public TextMesh promptLabel;

        private readonly List<Interactable> nearby = new List<Interactable>();
        private Interactable currentTarget;
        private Interactable autoTalkCandidate;

        private void Start()
        {
            EnsurePromptLabel();
        }

        private void EnsurePromptLabel()
        {
            if (promptLabel != null) return;
            GameObject labelGo = new GameObject("DZ_InteractPrompt");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            TextMesh tm = labelGo.AddComponent<TextMesh>();
            tm.text = "";
            tm.fontSize = 28;
            tm.characterSize = 0.12f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color32(238, 230, 210, 255);
            promptLabel = tm;
            // A TextMesh is a renderer; keep it above actors in sorting.
            MeshRenderer mr = labelGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingLayerName = Core.DuzheSortingLayers.Effects;
        }

        private void Update()
        {
            Core.GameSession session = Core.GameSession.Instance;
            if (session == null || session.Paused)
            {
                HidePrompt();
                return;
            }
            bool exploring = session.InputMode == Core.InputMode.Exploring;
            if (!exploring)
            {
                HidePrompt();
                return;
            }

            ScanNearby(session);
            UpdatePromptLabel();

            // Auto-talk: legal stage + segment not played this run.
            if (autoTalkCandidate != null && session.Dialogue != null && !session.Dialogue.IsPlaying)
            {
                string segmentId = autoTalkCandidate.autoTalkSegmentId;
                string chapterId = session.Chapters != null ? session.Chapters.CurrentChapterId : "";
                if (session.Chapters != null && session.Chapters.CanAutoStartSegment(segmentId))
                {
                    session.Chapters.MarkSegmentPlayed(segmentId);
                    session.Dialogue.StartSegment(chapterId, segmentId);
                }
            }

            // Manual E interaction.
            if (currentTarget != null && Core.InputRouter.Instance != null && Core.InputRouter.Instance.InteractPressed)
            {
                Interactable target = currentTarget; // captured before possible destroy
                PerformInteraction(session, target);
            }
        }

        private void ScanNearby(Core.GameSession session)
        {
            nearby.Clear();
            currentTarget = null;
            autoTalkCandidate = null;
            if (session.Chapters == null) return;
            string stageId = session.Chapters.CurrentStageId;

            int mask = Core.DuzheLayers.LayerMaskOf(Core.DuzheLayers.Interactable, Core.DuzheLayers.Trigger);
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, autoTalkDistance, mask);
            float bestDist = float.MaxValue;
            foreach (Collider2D hit in hits)
            {
                Interactable it = hit.GetComponentInParent<Interactable>();
                if (it == null) continue;
                if (!it.IsActiveInStage(stageId)) continue; // locked hotspot: no response at all
                nearby.Add(it);

                float d = Vector2.Distance(transform.position, it.transform.position);
                if (d <= interactDistance && d < bestDist && CanManuallyInteract(session, it))
                {
                    bestDist = d;
                    currentTarget = it;
                }
                if (it.kind == InteractableKind.Npc && !string.IsNullOrEmpty(it.autoTalkSegmentId))
                {
                    autoTalkCandidate = it; // at most one NPC with autoTalk expected in range
                }
            }
        }

        private bool CanManuallyInteract(Core.GameSession session, Interactable it)
        {
            if (it.kind == InteractableKind.Npc)
            {
                // NPCs accept E only to hand over EVERY required item
                // (C04 CH01: glasses AND cane together, one hand-over).
                string[] required = it.RequiredReturnItems;
                if (required.Length == 0 || session.Chapters == null) return false;
                foreach (string itemId in required)
                {
                    if (!session.Chapters.HasItemCollected(itemId)) return false;
                }
                return true;
            }
            return true; // items/props: stage gate already checked
        }

        private void PerformInteraction(Core.GameSession session, Interactable target)
        {
            switch (target.kind)
            {
                case InteractableKind.Item:
                    CollectItem(session, target);
                    break;
                case InteractableKind.Npc:
                    HandOverItem(session, target);
                    break;
                default:
                    // Props forward to an optional IPropInteractable behaviour
                    // (ritual anchor, boarding spot, later minigame hooks).
                    IPropInteractable prop = target.GetComponent<IPropInteractable>();
                    if (prop != null)
                    {
                        prop.OnPropInteract(target);
                    }
                    else
                    {
                        Debug.Log("[Duzhe] Interacted with prop " + target.interactableId + " (no behaviour bound yet).");
                    }
                    break;
            }
        }

        private void CollectItem(Core.GameSession session, Interactable target)
        {
            string chapterId = string.IsNullOrEmpty(target.chapterId)
                ? (session.Chapters != null ? session.Chapters.CurrentChapterId : "")
                : target.chapterId;
            session.Chapters.MarkItemCollected(target.interactableId);
            session.PublishEvent(Core.GameEventType.ItemCollected, chapterId,
                session.Chapters != null ? session.Chapters.CurrentStageId : "", target.interactableId);
            if (session.Hud != null)
            {
                session.Hud.ShowToast(GetPickupToast(session, target));
                session.Hud.ShowQuestItem(target.interactableId);
            }
            if (session.Audio != null) session.Audio.PlaySfx("SFX03_Pickup");
            Destroy(target.gameObject);
        }

        private string GetPickupToast(Core.GameSession session, Interactable target)
        {
            return target.displayName + "：" + Duzhe.UI.UiText.Get("feedback.item_collected", "已找到物品。");
        }

        private void HandOverItem(Core.GameSession session, Interactable target)
        {
            string[] required = target.RequiredReturnItems;
            if (session.Chapters == null || required.Length == 0)
            {
                Debug.LogWarning("[Duzhe] Hand-over rejected: NPC " + target.interactableId + " accepts no items.");
                return;
            }
            foreach (string itemId in required)
            {
                if (!session.Chapters.HasItemCollected(itemId))
                {
                    Debug.LogWarning("[Duzhe] Hand-over rejected: item not collected: " + itemId);
                    return;
                }
            }
            string stageId = session.Chapters.CurrentStageId;
            string chapterId = session.Chapters.CurrentChapterId;
            foreach (string itemId in required)
            {
                session.Chapters.MarkItemReturned(itemId);
            }
            session.PublishEvent(Core.GameEventType.ItemsReturned, chapterId, stageId, target.interactableId);
            if (session.Hud != null)
            {
                session.Hud.ShowToast(Duzhe.UI.UiText.Get("feedback.items_returned", "物品已交还。"));
                session.Hud.HideQuestItems(); // 收起任务物图标
            }
            if (session.Audio != null) session.Audio.PlaySfx("SFX04_ReturnItem");
            if (!string.IsNullOrEmpty(target.returnSegmentId) && session.Dialogue != null)
            {
                session.Chapters.MarkSegmentPlayed(target.returnSegmentId);
                session.Dialogue.StartSegment(chapterId, target.returnSegmentId);
            }
        }

        private void UpdatePromptLabel()
        {
            if (promptLabel == null) return;
            if (currentTarget == null)
            {
                promptLabel.text = "";
                return;
            }
            promptLabel.transform.position = new Vector3(
                currentTarget.transform.position.x,
                currentTarget.transform.position.y + 1.05f,
                currentTarget.transform.position.z);
            // Context prompt from TXT_UI (hint.pickup / hint.return_items /
            // hint.ritual / ...); the generic "[E] 名称" is the fallback.
            promptLabel.text = string.IsNullOrEmpty(currentTarget.promptKey)
                ? Duzhe.UI.UiText.Get("interact_prompt", "[E] ") + currentTarget.displayName
                : Duzhe.UI.UiText.Get(currentTarget.promptKey, "[E] " + currentTarget.displayName);
        }

        private void HidePrompt()
        {
            if (promptLabel != null) promptLabel.text = "";
            currentTarget = null;
            autoTalkCandidate = null;
        }
    }
}
