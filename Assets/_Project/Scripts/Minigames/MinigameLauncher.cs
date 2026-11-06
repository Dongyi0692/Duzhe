using Duzhe.Core;
using Duzhe.Interaction;
using UnityEngine;

namespace Duzhe.Minigames
{
    /// Placed next to a Prop Interactable in a reunion scene: pressing E opens
    /// the configured minigame. Stage authorisation is enforced upstream by
    /// the Interactable's activeStageIds, so the launcher itself only routes.
    /// MG07 (CH04 "write goodbye") additionally requires the completed
    /// classroom lesson - the writing must never be reachable before the
    /// class ends, and a presentation replay never opens a minigame.
    public class MinigameLauncher : MonoBehaviour, IPropInteractable
    {
        public string minigameId = "";

        public bool OnPropInteract(Interactable self)
        {
            GameSession session = GameSession.Instance;
            if (session != null && session.Mode == GameMode.PresentationReplay)
            {
                // A gallery replay only re-watches the presentation.
                Debug.Log("[Duzhe] Minigame " + minigameId + " blocked: presentation replay never opens minigames.");
                return false;
            }
            if (minigameId == "MG07_Goodbye" && session != null && session.Chapters != null &&
                (session.Mode == GameMode.MainRun || session.Mode == GameMode.ChapterReplay))
            {
                Chapters.ChapterDirector chapters = session.Chapters;
                if (chapters.CurrentChapterId != "CH04" ||
                    chapters.CurrentStageId != "CH04_WriteGoodbye" ||
                    !chapters.LessonFinished)
                {
                    Debug.Log("[Duzhe] MG07 blocked: chapter=" + chapters.CurrentChapterId +
                        " stage=" + chapters.CurrentStageId + " lessonFinished=" + chapters.LessonFinished);
                    return false;
                }
            }

            MinigameHost host = MinigameHost.Instance;
            if (host == null) return false;
            MinigameConfig cfg = MinigameCatalog.Get(minigameId);
            if (cfg == null)
            {
                Debug.LogError("[Duzhe] MinigameLauncher: unknown minigameId '" + minigameId + "'.");
                return false;
            }
            host.Open(cfg);
            return true;
        }
    }
}
