using System.Collections.Generic;
using Duzhe.Interaction;
using UnityEngine;

namespace Duzhe.Core
{
    /// Per-scene registry assembled by ProjectSetupUtility: spawn points,
    /// interactables and the world bounds used by the camera clamp.
    /// The scene keeps static data only; all behaviour lives in services.
    public class SceneContent : MonoBehaviour
    {
        [Tooltip("Named spawn points, e.g. SP_Main. First entry is the default.")]
        public Transform[] spawnPoints = new Transform[0];

        [Tooltip("All interactables physically placed in this scene.")]
        public Interactable[] interactables = new Interactable[0];

        [Tooltip("Background renderer; its world bounds clamp the camera so no blank area outside the art is ever visible.")]
        public SpriteRenderer backgroundRenderer;

        public Transform GetSpawnPoint(string spawnPointId)
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return null;
            if (string.IsNullOrEmpty(spawnPointId)) return spawnPoints[0];
            foreach (Transform t in spawnPoints)
            {
                if (t != null && t.name == spawnPointId) return t;
            }
            return spawnPoints[0];
        }

        public Bounds GetWorldBounds()
        {
            Bounds b = new Bounds(Vector3.zero, Vector3.one);
            bool has = false;
            if (backgroundRenderer != null)
            {
                b = backgroundRenderer.bounds;
                has = true;
            }
            else
            {
                // Fallback: enclose all interactables, else a safe 40x22.5 box
                // (1280x720 px background at 32 PPU).
                foreach (Interactable it in interactables)
                {
                    if (it == null) continue;
                    Vector3 p = it.transform.position;
                    if (!has) { b = new Bounds(p, Vector3.zero); has = true; }
                    else b.Encapsulate(p);
                }
                if (!has) b = new Bounds(Vector3.zero, new Vector3(40f, 22.5f, 1f));
                else b.Expand(6f);
            }
            return b;
        }
    }
}
