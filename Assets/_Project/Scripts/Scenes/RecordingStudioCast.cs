using System.Collections;
using Duzhe.Core;
using Duzhe.Ritual;
using UnityEngine;

namespace Duzhe.Scenes
{
    /// RecordingStudio scene behaviour for CH03 (R09 BG05 anchors):
    /// - the drummer's ghost follows the player from scene start (he rode the
    ///   boat across);
    /// - the three living bandmates wait at their spots (the drummer's spot
    ///   stays empty until the photo);
    /// - on MinigameCompleted(MG06_Photo) the drummer walks to his spot and
    ///   the shutter fires EXACTLY ONCE (the successful drop, not a frame,
    ///   a dialogue line or the CG).
    public class RecordingStudioCast : MonoBehaviour
    {
        [Header("R09 BG05 anchors (world)")]
        public Vector2 ghostStart = new Vector2(-13.6f, -6.4f); // beside the door threshold (224,576)
        public Transform drummerSpot;                            // band spot 4: (896,384) -> (8,-0.75)

        [Header("Wired by ProjectSetupUtility")]
        public SpiritEscort drummerGhost;
        public GameObject[] bandmates;

        private bool ghostActivated;
        private bool shutterPlayed;

        /// Evidence for the play-mode test: how many times the shutter fired.
        public int ShutterFireCount { get; private set; }

        private IEnumerator Start()
        {
            while (GameBootstrap.Instance == null || GameBootstrap.Instance.Player == null ||
                   !GameBootstrap.Instance.Player.gameObject.activeSelf)
            {
                yield return null;
            }
            ActivateGhost();
        }

        private void ActivateGhost()
        {
            if (ghostActivated) return;
            ghostActivated = true;
            if (drummerGhost != null)
            {
                // C12: the drummer appears next to the ferryman inside the
                // camera view (the fixed -13.6/-6.4 start could end up under
                // the door frame / at the very edge), and the route gate is
                // marked as runtime-shown so the chapter-entry sweep can never
                // hide him again on the same frame.
                Vector3 start = new Vector3(ghostStart.x, ghostStart.y, 0f);
                GameBootstrap boot = GameBootstrap.Instance;
                if (boot != null && boot.Player != null)
                {
                    Vector3 p = boot.Player.transform.position;
                    start = new Vector3(p.x + 1.2f, p.y + 0.25f, 0f);
                }
                drummerGhost.transform.position = start;
                drummerGhost.gameObject.SetActive(true);
                Chapters.ChapterObjectVisibility gate =
                    drummerGhost.GetComponent<Chapters.ChapterObjectVisibility>();
                if (gate != null) gate.ShowRuntime();
                drummerGhost.BeginFollow();
            }
        }

        private void OnEnable()
        {
            GameSession session = GameSession.Instance;
            if (session != null) session.OnGameEvent += HandleEvent;
        }

        private void OnDisable()
        {
            GameSession session = GameSession.Instance;
            if (session != null) session.OnGameEvent -= HandleEvent;
        }

        private void HandleEvent(GameEvent e)
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Chapters == null) return;
            if (e.runId != session.RunId) return;
            if (e.chapterId != "CH03") return;
            // The stage check is explicit: at callback time CurrentStageId may
            // already have advanced, so the event's own stageId is the truth.
            if (e.type != GameEventType.MinigameCompleted) return;
            if (e.sourceId != "MG06_Photo" || e.stageId != "CH03_Photo") return;
            NotifyPhotoCompleted();
        }

        /// Called once on the successful photo drop (and by the play-mode
        /// test). Idempotent: the shutter never fires twice.
        public void NotifyPhotoCompleted()
        {
            // The drummer takes his place in the group for the farewell.
            if (drummerGhost != null && drummerSpot != null) drummerGhost.Seat(drummerSpot);
            PlayShutterOnce();
        }

        private void PlayShutterOnce()
        {
            if (shutterPlayed) return;
            shutterPlayed = true;
            ShutterFireCount++;
            GameSession session = GameSession.Instance;
            // SFX08_Shutter ships with R16; audio routing/import is C10, so
            // this resolves to the documented placeholder silence until then.
            if (session != null && session.Audio != null) session.Audio.PlaySfx("SFX08_Shutter");
            Debug.Log("[Duzhe] CH03 shutter fired once.");
        }

        /// Used by the farewell cast: the drummer stands at his spot.
        public void SeatDrummer()
        {
            if (drummerGhost != null && drummerSpot != null) drummerGhost.Seat(drummerSpot);
        }

        public SpiritEscort DrummerGhost { get { return drummerGhost; } }
    }
}
