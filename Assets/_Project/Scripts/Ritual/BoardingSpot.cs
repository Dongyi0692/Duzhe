using Duzhe.Core;
using Duzhe.Interaction;
using UnityEngine;

namespace Duzhe.Ritual
{
    /// Placed next to the boat's Prop Interactable at the dock. Boarding only
    /// works when the current stage authorises it (activeStageIds gate), so
    /// no ritual-skip is possible. Emits exactly one Boarded event; the
    /// ChapterDirector consumes it and loads the River scene.
    public class BoardingSpot : MonoBehaviour, IPropInteractable
    {
        public bool OnPropInteract(Interactable self)
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Chapters == null) return false;
            session.PublishEvent(GameEventType.Boarded,
                session.Chapters.CurrentChapterId,
                session.Chapters.CurrentStageId,
                self.interactableId);
            return true;
        }
    }
}
