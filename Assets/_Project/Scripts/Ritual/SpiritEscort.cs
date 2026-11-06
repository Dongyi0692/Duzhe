using Duzhe.Core;
using UnityEngine;

namespace Duzhe.Ritual
{
    /// A ghost passenger: hidden until the ritual starts, walks to its spot
    /// during the performance, then follows the player with a simple offset
    /// queue (no pathfinding - positions only). Spirits have no collider, so
    /// they never block the player and never take river damage themselves.
    public class SpiritEscort : MonoBehaviour
    {
        [Tooltip("Walk speed while moving to the spot or following.")]
        public float moveSpeed = 2.4f;

        [Tooltip("Follow offset relative to the player (queue slot).")]
        public Vector2 followOffset = new Vector2(-0.65f, -0.2f);

        [Tooltip("Ghost alpha target after appearing.")]
        [Range(0f, 1f)] public float ghostAlpha = 0.8f;

        public enum EscortState { Hidden, MovingToSpot, Following, Seated }

        private EscortState state = EscortState.Hidden;
        private bool seatedMode;
        private SpriteRenderer sr;
        private Transform followTarget;
        private Transform spot;

        public bool IsSeated { get { return state == EscortState.Seated; } }

        private void Awake()
        {
            // The renderer may live on a child "Visual" (formal feet-pivot art
            // carrier) or on this object (placeholder assemblies).
            sr = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 0f);
        }

        /// Called by RitualController when the ritual starts.
        public void Appear(Transform targetSpot)
        {
            spot = targetSpot;
            if (spot != null)
            {
                // Rise from slightly below the spot, facing up.
                transform.position = spot.position + new Vector3(0f, -1.6f, 0f);
                state = EscortState.MovingToSpot;
            }
            else
            {
                state = EscortState.Following;
            }
            GameBootstrap boot = GameBootstrap.Instance;
            if (boot != null && boot.Player != null) followTarget = boot.Player.transform;
            gameObject.SetActive(true);
        }

        /// Called when the ritual beat ends: start tailing the player.
        public void BeginFollow()
        {
            GameBootstrap boot = GameBootstrap.Instance;
            if (boot != null && boot.Player != null) followTarget = boot.Player.transform;
            seatedMode = false;
            if (state == EscortState.Seated || state == EscortState.Hidden) state = EscortState.Following;
        }

        /// Walks to a seat and STAYS there (farewell presentation seating).
        public void Seat(Transform targetSpot)
        {
            spot = targetSpot;
            seatedMode = true;
            if (spot != null)
            {
                state = EscortState.MovingToSpot;
            }
            else
            {
                state = EscortState.Seated;
            }
        }

        private void Update()
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Paused) return; // freeze with the pause overlay
            if (state == EscortState.Hidden) return;

            // Fade in as ghost alpha.
            if (sr != null)
            {
                Color c = sr.color;
                if (c.a < ghostAlpha)
                {
                    c.a = Mathf.MoveTowards(c.a, ghostAlpha, Time.deltaTime * 0.6f);
                    sr.color = c;
                }
            }

            Vector3 goal;
            if (state == EscortState.MovingToSpot && spot != null)
            {
                goal = spot.position;
            }
            else if (state == EscortState.Following && followTarget != null)
            {
                goal = followTarget.position + new Vector3(followOffset.x, followOffset.y, 0f);
            }
            else
            {
                return;
            }

            Vector3 next = Vector3.MoveTowards(transform.position, goal, moveSpeed * Time.deltaTime);
            transform.position = next;
            if (state == EscortState.MovingToSpot && (next - goal).sqrMagnitude < 0.001f)
            {
                state = seatedMode ? EscortState.Seated : EscortState.Following;
            }
        }
    }
}
