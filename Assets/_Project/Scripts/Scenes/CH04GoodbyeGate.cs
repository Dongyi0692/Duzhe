using Duzhe.Core;
using Duzhe.Interaction;
using Duzhe.Minigames;
using UnityEngine;

namespace Duzhe.Scenes
{
    /// CH04 blackboard: the "write goodbye" panel may only open when the
    /// classroom lesson finished. It opens ONCE automatically when the
    /// writing stage starts, and can be reopened with E afterwards (closing
    /// it never re-opens it every frame).
    public class CH04GoodbyeGate : MonoBehaviour, IPropInteractable
    {
        private string autoOpenedRunId;

        private static bool Allowed(GameSession s)
        {
            return s != null && !s.Paused && s.Chapters != null &&
                (s.Mode == GameMode.MainRun || s.Mode == GameMode.ChapterReplay) &&
                s.Chapters.CurrentChapterId == "CH04" &&
                s.Chapters.CurrentStageId == "CH04_WriteGoodbye" && s.Chapters.LessonFinished;
        }

        private void Update()
        {
            GameSession s = GameSession.Instance;
            if (!Allowed(s) || autoOpenedRunId == s.RunId) return;
            TryOpen(s);
        }

        public bool OnPropInteract(Interactable self)
        {
            return TryOpen(GameSession.Instance);
        }

        private bool TryOpen(GameSession s)
        {
            MinigameHost host = MinigameHost.Instance;
            if (!Allowed(s) || host == null || host.IsOpen) return false;
            if (s.Dialogue != null && s.Dialogue.IsPlaying) return false;
            MinigameConfig cfg = MinigameCatalog.Get("MG07_Goodbye");
            if (cfg == null)
            {
                Debug.LogError("[Duzhe] Missing MG07_Goodbye config");
                return false;
            }
            host.Open(cfg);
            if (host.IsOpen && autoOpenedRunId != s.RunId)
            {
                autoOpenedRunId = s.RunId;
                if (s.Audio != null) s.Audio.PlaySfx("SFX10_SchoolBell");
            }
            return host.IsOpen;
        }
    }
}
