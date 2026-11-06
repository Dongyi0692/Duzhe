using Duzhe.Core;
using UnityEngine;

namespace Duzhe.Player
{
    /// Eight-direction movement, four-direction facing, for the one and only
    /// controllable character (the Ferryman). Diagonal input is normalized by
    /// the InputRouter so diagonals are never faster. When both axes are
    /// equally strong the previous valid facing is kept to avoid animation
    /// flip-flopping (spec section 1).
    [DefaultExecutionOrder(-20)]
    public class PlayerController2D : MonoBehaviour
    {
        [Tooltip("Movement speed in world units per second.")]
        public float moveSpeed = 4.5f;

        public Direction4 Facing { get; private set; } = Direction4.Down;
        /// True while the player actually moves this physics frame (walk
        /// animation input; false whenever input is blocked or paused).
        public bool IsMoving { get; private set; }
        public event System.Action<Direction4> OnFacingChanged;

        private Rigidbody2D rb;
        private Core.InputRouter input;
        private float footstepTimer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            input = Core.InputRouter.Instance;
        }

        private void Update()
        {
            // C10: footsteps on a fixed interval while actually moving (never
            // every frame), silenced by pause and by non-exploring modes.
            Core.GameSession session = Core.GameSession.Instance;
            bool walking = IsMoving && session != null && !session.Paused &&
                           session.InputMode == Core.InputMode.Exploring;
            if (!walking)
            {
                footstepTimer = 0f;
                return;
            }
            footstepTimer -= Time.deltaTime;
            if (footstepTimer > 0f) return;
            footstepTimer = 0.32f;
            if (session.Audio != null) session.Audio.PlaySfx("SFX02_Footstep");
        }

        private void FixedUpdate()
        {
            Core.GameSession session = Core.GameSession.Instance;
            if (session == null || session.Paused) { rb.velocity = Vector2.zero; IsMoving = false; return; }
            if (session.InputMode != Core.InputMode.Exploring) { rb.velocity = Vector2.zero; IsMoving = false; return; }
            if (input == null) { rb.velocity = Vector2.zero; IsMoving = false; return; }

            Vector2 move = input.Move;
            IsMoving = move != Vector2.zero;
            rb.velocity = move * moveSpeed;
            UpdateFacing(move);
        }

        private void UpdateFacing(Vector2 move)
        {
            if (move == Vector2.zero) return;
            float ax = Mathf.Abs(move.x);
            float ay = Mathf.Abs(move.y);
            Direction4 newFacing = Facing;
            if (ax > ay) newFacing = move.x > 0f ? Direction4.Right : Direction4.Left;
            else if (ay > ax) newFacing = move.y > 0f ? Direction4.Up : Direction4.Down;
            // ax == ay keeps the previous facing (anti-jitter rule).

            if (newFacing != Facing)
            {
                Facing = newFacing;
                if (OnFacingChanged != null) OnFacingChanged(Facing);
            }
        }

        public void ResetFacing()
        {
            Facing = Direction4.Down;
        }

        /// Immediate reposition for scripted beats (no interpolation). Keeps
        /// the Rigidbody2D and Transform in sync so the next physics step does
        /// not pull the player back to the old position.
        public void TeleportTo(Vector3 position)
        {
            position.z = 0f;
            if (rb != null) rb.position = position;
            transform.position = position;
        }
    }
}
