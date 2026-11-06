using System.Collections;
using Duzhe.Core;
using Duzhe.Ritual;
using UnityEngine;

namespace Duzhe.Scenes
{
    /// Park scene behaviour for CH02 (R04 Park.layout.json):
    /// - the girl's ghost follows the player from scene start (she rode the
    ///   boat across);
    /// - on MinigameCompleted(MG04_Heart) the girl and the boy move into the
    ///   embrace pose at the reunion point;
    /// - the farewell presentation seats them, shows the CG02 tableau, and
    ///   fades ONLY the girl after the last line (boy + park stay).
    public class ParkCast : MonoBehaviour
    {
        [Header("R04 layout points (world)")]
        public Vector2 reunionPoint = new Vector2(6f, -0.75f); // PK_Reunion (832,384)
        public Vector2 ghostStart = new Vector2(-14.6f, -6.4f); // beside PK_Spawn

        [Header("Wired by ProjectSetupUtility")]
        public SpiritEscort loverGhost;
        public GameObject boy;

        private bool ghostActivated;
        private Coroutine embraceWalk;

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
            if (loverGhost != null)
            {
                // C12: appear NEXT TO the ferryman where the player actually
                // stands (the fixed ghostStart sat 14 units to the left of the
                // Park spawn, i.e. off-screen). ghostStart stays the fallback
                // for a missing player.
                Vector3 start = new Vector3(ghostStart.x, ghostStart.y, 0f);
                GameBootstrap boot = GameBootstrap.Instance;
                if (boot != null && boot.Player != null)
                {
                    Vector3 p = boot.Player.transform.position;
                    start = new Vector3(p.x + 1.2f, p.y + 0.25f, 0f);
                }
                loverGhost.transform.position = start;
                loverGhost.gameObject.SetActive(true);
                // Keep the route gate from re-hiding her on the next sweep.
                Chapters.ChapterObjectVisibility gate =
                    loverGhost.GetComponent<Chapters.ChapterObjectVisibility>();
                if (gate != null) gate.ShowRuntime();
                loverGhost.BeginFollow();
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
            if (e.chapterId != session.Chapters.CurrentChapterId) return;

            if (e.type == GameEventType.MinigameCompleted && e.sourceId == "MG04_Heart")
            {
                StartEmbrace();
            }
        }

        private void StartEmbrace()
        {
            if (embraceWalk != null) StopCoroutine(embraceWalk);
            embraceWalk = StartCoroutine(EmbraceRoutine());
        }

        private IEnumerator EmbraceRoutine()
        {
            // Move the girl and the boy into the embrace pose at the reunion
            // point (the boy walks a short step to meet her).
            Vector3 goal = new Vector3(reunionPoint.x, reunionPoint.y, 0f);
            while (true)
            {
                GameSession session = GameSession.Instance;
                if (session == null || session.Paused) { yield return null; continue; }
                if (loverGhost != null)
                {
                    loverGhost.transform.position = Vector3.MoveTowards(
                        loverGhost.transform.position, goal, 2.2f * Time.deltaTime);
                }
                if (boy != null)
                {
                    boy.transform.position = Vector3.MoveTowards(
                        boy.transform.position, goal + new Vector3(0.3f, 0f, 0f), 2.2f * Time.deltaTime);
                }
                bool ghostThere = loverGhost == null ||
                    (loverGhost.transform.position - goal).sqrMagnitude < 0.02f;
                bool boyThere = boy == null ||
                    (boy.transform.position - (goal + new Vector3(0.3f, 0f, 0f))).sqrMagnitude < 0.02f;
                if (ghostThere && boyThere) break;
                yield return null;
            }
        }

        /// Used by the farewell cast: stops the embrace walk coroutine.
        public void StopEmbraceWalk()
        {
            if (embraceWalk != null)
            {
                StopCoroutine(embraceWalk);
                embraceWalk = null;
            }
        }
    }
}
