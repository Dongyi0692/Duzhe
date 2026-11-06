using System.Collections.Generic;
using Duzhe.Chapters;
using UnityEngine;

namespace Duzhe.River
{
    /// Creates, moves and clears whirlpool visuals for one crossing attempt.
    /// Objects are plain sprite renderers (no physics): the collision check is
    /// a deterministic AABB test owned by RiverController, so collision and
    /// the 10-second success can never both happen in the same frame.
    public class WhirlpoolSpawner : MonoBehaviour
    {
        private readonly List<Whirl> active = new List<Whirl>();
        private List<RiverWave> waves;
        private RiverSettings settings;
        private float boatY;
        private Sprite sprite;

        private class Whirl
        {
            public Transform transform;
            public float x;
            public float y;
            public float velocityX;
        }

        /// Loads the wave plan and local parameters for the crossing.
        public void Bind(List<RiverWave> plan, RiverSettings s, float boatLineY)
        {
            waves = plan;
            settings = s;
            boatY = boatLineY;
            if (sprite == null) sprite = Core.FormalArt.LoadWhirlpoolSprite();
            ClearAll();
        }

        // Keep seeded hazard arrival times identical in the moving boat frame.
        public void FollowBoat(float worldY)
        {
            float delta = worldY - boatY;
            boatY = worldY;
            foreach (Whirl w in active)
            {
                w.y += delta;
                if (w.transform != null) w.transform.position = new Vector3(w.x, w.y, 0f);
            }
        }

        /// Spawns every wave whose lead time has arrived. Returns the number
        /// spawned this frame.
        public int SpawnDue(RiverSession session)
        {
            int spawned = 0;
            for (int i = 0; i < waves.Count; i++)
            {
                if (session.spawnedWaves.Contains(i)) continue;
                RiverWave w = waves[i];
                float spawnAt = Mathf.Max(0f, w.arrivalTime - settings.spawnLeadSeconds);
                if (session.elapsed >= spawnAt)
                {
                    SpawnWave(w);
                    session.spawnedWaves.Add(i);
                    spawned++;
                }
            }
            return spawned;
        }

        private void SpawnWave(RiverWave w)
        {
            for (int i = 0; i < w.bannedLanes.Length; i++)
            {
                GameObject go = new GameObject("Whirlpool_" + w.arrivalTime.ToString("F0") + "_" + w.bannedLanes[i]);
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = Core.DuzheSortingLayers.Actors;
                sr.sortingOrder = -3; // behind the boat (-2) and the passengers
                Whirl whirl = new Whirl
                {
                    transform = go.transform,
                    x = w.whirlXs[i] - (w.whirlVelocitiesX.Length > i ? w.whirlVelocitiesX[i] : 0f) * settings.spawnLeadSeconds,
                    velocityX = (w.whirlVelocitiesX.Length > i ? w.whirlVelocitiesX[i] : 0f),
                    y = boatY + settings.forwardSpeed * settings.spawnLeadSeconds
                };
                go.transform.position = new Vector3(whirl.x, whirl.y, 0f);
                active.Add(whirl);
            }
        }

        /// Moves all whirlpools downward at the configured forward speed.
        /// dt is caller-supplied game time (0 while paused).
        public void MoveAll(float dt, RiverSettings s)
        {
            for (int i = 0; i < active.Count; i++)
            {
                active[i].y -= s.forwardSpeed * dt;
                active[i].x += active[i].velocityX * dt;
                active[i].transform.position = new Vector3(active[i].x, active[i].y, 0f);
            }
        }

        /// AABB overlap test against the boat box; visual sprites are larger
        /// than these collision sizes on purpose (spec: visual slightly
        /// bigger than effective collision).
        public bool HitsBoat(float boatX, float boatY, RiverSettings s)
        {
            for (int i = 0; i < active.Count; i++)
            {
                Whirl w = active[i];
                if (Mathf.Abs(w.x - boatX) < s.whirlHalfWidth + s.boatHalfWidth &&
                    Mathf.Abs(w.y - boatY) < s.whirlHalfHeight + s.boatHalfHeight)
                {
                    return true;
                }
            }
            return false;
        }

        public void ClearAll()
        {
            foreach (Whirl w in active)
            {
                if (w.transform != null) Destroy(w.transform.gameObject);
            }
            active.Clear();
        }

        public int ActiveCount { get { return active.Count; } }
    }
}
