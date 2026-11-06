using Duzhe.Core;
using Duzhe.Interaction;
using UnityEngine;

namespace Duzhe.Ritual
{
    /// Placed next to a Prop Interactable (a ritual anchor at the ferry dock).
    /// Pressing E on it forwards to the scene's RitualController that owns
    /// THIS anchor id - a scene may host several anchors/chapters (C04 adds
    /// CH01 next to the DEV route), so the controller is matched by id.
    public class RitualTrigger : MonoBehaviour, IPropInteractable
    {
        public bool OnPropInteract(Interactable self)
        {
            RitualController[] controllers = FindObjectsOfType<RitualController>();
            foreach (RitualController rc in controllers)
            {
                if (rc != null && rc.ritualAnchorId == self.interactableId)
                {
                    return rc.TryBeginRitual(self.interactableId);
                }
            }
            Debug.LogWarning("[Duzhe] RitualTrigger: no RitualController owns anchor '" + self.interactableId + "'.");
            return false;
        }
    }
}
