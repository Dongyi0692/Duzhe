using UnityEngine;

namespace Duzhe.Core
{
    /// Marks a scene with its stable sceneId. Exactly one per game scene.
    public class SceneIdentity : MonoBehaviour
    {
        public string sceneId = "";
    }
}
