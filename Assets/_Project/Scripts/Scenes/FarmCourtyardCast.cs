using System.Collections;
using Duzhe.Core;
using Duzhe.Ritual;
using UnityEngine;

namespace Duzhe.Scenes
{
    /// FarmCourtyard scene behaviour for CH01 (R04 layout FarmCourtyard.layout.json):
    /// - on MinigameCompleted(MG02_Door) the grandson appears at the door and
    ///   walks out to his family spot;
    /// - on MinigameCompleted(MG03_Stools) the three faint seat markers are
    ///   replaced by the three locked stools;
    /// - the two elder spirits follow the player from scene start (they rode
    ///   the boat across the river).
    /// Door/table/stool anchors come from R04 (worldX=(px-640)/32,
    /// worldY=(360-px)/32); the reunion composition overrides them so the
    /// door, grandson and player read top-to-bottom without overlapping.
    public class FarmCourtyardCast : MonoBehaviour
    {
        [Header("CH01 reunion composition (world)")]
        public Vector2 doorPoint = new Vector2(0f, 6.5f);              // FC_Door (640,152)
        public Vector2 grandsonSpawnPoint = new Vector2(0f, 5.1f);     // just below the door
        public Vector2 grandsonReunionPoint = new Vector2(0f, 4.25f);  // between door and player
        public Vector2 playerReunionPoint = new Vector2(0f, 2.25f);    // stepped back from the door

        [Header("Wired by ProjectSetupUtility")]
        public GameObject[] stoolMarkers = new GameObject[0]; // faint, visible from start
        public GameObject[] stools = new GameObject[0];       // locked-in stools, hidden
        public GameObject grandson;                            // hidden until the knock
        public SpiritEscort elderA;
        public SpiritEscort elderB;
        public Vector2 elderStartA = new Vector2(-12.1f, -6.1f); // beside FC_Spawn
        public Vector2 elderStartB = new Vector2(-10.1f, -6.4f);

        private bool eldersActivated;
        private Coroutine grandsonWalk;

        private IEnumerator Start()
        {
            // The bootstrap places the player right after the scene is ready;
            // wait for it before the elders start tailing.
            while (GameBootstrap.Instance == null || GameBootstrap.Instance.Player == null ||
                   !GameBootstrap.Instance.Player.gameObject.activeSelf)
            {
                yield return null;
            }
            ActivateElders();
        }

        private void ActivateElders()
        {
            if (eldersActivated) return;
            eldersActivated = true;
            if (elderA != null)
            {
                elderA.transform.position = new Vector3(elderStartA.x, elderStartA.y, 0f);
                elderA.gameObject.SetActive(true);
                elderA.BeginFollow();
            }
            if (elderB != null)
            {
                elderB.transform.position = new Vector3(elderStartB.x, elderStartB.y, 0f);
                elderB.gameObject.SetActive(true);
                elderB.BeginFollow();
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

            if (e.type == GameEventType.MinigameCompleted && e.sourceId == "MG02_Door")
            {
                BringGrandsonOut();
            }
            else if (e.type == GameEventType.MinigameCompleted && e.sourceId == "MG03_Stools")
            {
                LockStools();
            }
        }

        /// Used by the farewell cast and the R14 live test: the player is
        /// pressed against the door when knocking, so step him back to the
        /// reunion point first; the grandson then emerges into the gap between
        /// the door and the player.
        public void BringGrandsonOut()
        {
            if (grandson == null || grandson.activeSelf) return;

            GameBootstrap boot = GameBootstrap.Instance;
            if (boot != null && boot.Player != null)
            {
                boot.Player.TeleportTo(new Vector3(playerReunionPoint.x, playerReunionPoint.y, 0f));
            }

            grandson.SetActive(true);
            grandson.transform.position = new Vector3(grandsonSpawnPoint.x, grandsonSpawnPoint.y, 0f);
            if (grandsonWalk != null) StopCoroutine(grandsonWalk);
            grandsonWalk = StartCoroutine(WalkGrandsonRoutine());
        }

        private IEnumerator WalkGrandsonRoutine()
        {
            Vector3 goal = new Vector3(grandsonReunionPoint.x, grandsonReunionPoint.y, 0f);
            while ((grandson.transform.position - goal).sqrMagnitude > 0.002f)
            {
                GameSession session = GameSession.Instance;
                if (session != null && !session.Paused)
                {
                    grandson.transform.position = Vector3.MoveTowards(
                        grandson.transform.position, goal, 2.2f * Time.deltaTime);
                }
                yield return null;
            }
        }

        private void LockStools()
        {
            foreach (GameObject marker in stoolMarkers)
            {
                if (marker != null) marker.SetActive(false);
            }
            foreach (GameObject stool in stools)
            {
                if (stool != null) stool.SetActive(true);
            }
        }

        /// Used by the farewell cast: stops the grandson at his seat.
        public void StopGrandsonWalk()
        {
            if (grandsonWalk != null)
            {
                StopCoroutine(grandsonWalk);
                grandsonWalk = null;
            }
        }
    }
}
