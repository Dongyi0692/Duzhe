using Duzhe.Core;
using Duzhe.Interaction;
using UnityEngine;

namespace Duzhe.Ritual
{
    /// Scene-level owner of the farewell ritual performance at the dock:
    /// spirits appear and take their spots during a short silent cut, then
    /// the RitualCompleted event is published exactly once. The ritual has no
    /// dialogue and no timer - it simply pauses input for a short beat.
    /// Pressing E repeatedly cannot restart it (phase guard + stage gating).
    public class RitualController : MonoBehaviour
    {
        [Tooltip("interactableId of the ritual anchor this controller reacts to.")]
        public string ritualAnchorId = "DEV_RITUAL_ANCHOR";

        [Tooltip("Duration of the appearance beat, in seconds of game time.")]
        public float performDuration = 2.2f;

        [Tooltip("Spirits (passengers) that appear when the ritual starts.")]
        public SpiritEscort[] spirits;

        [Tooltip("Standing spots, one per spirit (same order).")]
        public Transform[] spiritSpots;

        [Tooltip("Scene bodies that must vanish when the ritual starts - the departed has turned into the escorting spirits, so the old standing self must not stay visible.")]
        public Interactable[] hideOnRitual;

        private enum Phase { None, Performing, Done }
        private Phase phase = Phase.None;
        private float timer;

        public bool RitualPerformed { get { return phase == Phase.Done; } }

        /// Starts the ritual. Returns false when it is not possible (already
        /// running/performed, wrong anchor, wrong mode).
        public bool TryBeginRitual(string anchorId)
        {
            if (anchorId != ritualAnchorId) return false;
            if (phase != Phase.None) return false;

            GameSession session = GameSession.Instance;
            if (session == null || session.Paused) return false;
            if (session.InputMode != InputMode.Exploring) return false;

            phase = Phase.Performing;
            timer = 0f;
            session.SetInputMode(InputMode.Ritual); // silent beat: no input, no dialogue
            if (session.Audio != null) session.Audio.PlaySfx("SFX05_Ritual");

            // The departed now exists as the escorting spirits; their old
            // standing body must not linger on screen.
            if (hideOnRitual != null)
            {
                foreach (Interactable body in hideOnRitual)
                {
                    if (body != null && body.gameObject.activeSelf)
                    {
                        body.gameObject.SetActive(false);
                    }
                }
            }

            if (spirits != null)
            {
                for (int i = 0; i < spirits.Length; i++)
                {
                    SpiritEscort sp = spirits[i];
                    if (sp == null) continue;
                    Transform spot = spiritSpots != null && i < spiritSpots.Length ? spiritSpots[i] : null;
                    sp.Appear(spot);
                }
            }
            Debug.Log("[Duzhe] Ritual started: " + anchorId);
            return true;
        }

        private void Update()
        {
            if (phase != Phase.Performing) return;
            GameSession session = GameSession.Instance;
            if (session == null || session.Paused) return; // freeze with the pause overlay
            timer += Time.deltaTime;
            if (timer < performDuration) return;

            phase = Phase.Done;
            if (spirits != null)
            {
                foreach (SpiritEscort sp in spirits)
                {
                    if (sp != null) sp.BeginFollow();
                }
            }
            session.SetInputMode(InputMode.Exploring);
            if (session.Hud != null)
            {
                session.Hud.ShowToast(Duzhe.UI.UiText.Get("feedback.ritual_complete", "仪式完成，可以前往渡口。"));
            }
            session.PublishEvent(GameEventType.RitualCompleted,
                session.Chapters != null ? session.Chapters.CurrentChapterId : "",
                session.Chapters != null ? session.Chapters.CurrentStageId : "",
                ritualAnchorId);
            Debug.Log("[Duzhe] Ritual completed: " + ritualAnchorId);
        }
}
}
