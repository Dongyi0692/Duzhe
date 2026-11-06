using Duzhe.Core;
using UnityEngine;

namespace Duzhe.Player
{
    /// Four-direction walk animation for the Ferryman, driven by
    /// PlayerController2D's facing + moving state. Frame layout matches the
    /// delivered R12 sheet contract: rows Down/Left/Right/Up, cols
    /// WalkA/Idle/WalkB; walking cycles WalkA->Idle->WalkB->Idle at 6 fps,
    /// idle holds the Idle column. Freezes with the pause overlay.
    public class PlayerSpriteAnimator : MonoBehaviour
    {
        [Tooltip("12 frames, [row*3+col]: rows Down/Left/Right/Up, cols WalkA/Idle/WalkB.")]
        [SerializeField] private Sprite[] frames;
        [Tooltip("Walk cycle frames per second (delivery default 6).")]
        public float framesPerSecond = 6f;

        private static readonly int[] WalkCycle = { 0, 1, 2, 1 };

        private SpriteRenderer sr;
        private PlayerController2D ctrl;
        private float timer;
        private int cycleIndex;

        // Position-based movement detection: Rigidbody2D velocity is written
        // in FixedUpdate while the animator ticks in Update, so reading
        // IsMoving across the FixedUpdate/Update boundary can sample a stale
        // flag. Comparing world positions frame to frame is order-independent.
        private Vector3 lastPosition;
        private bool hasLastPosition;

        // Editor/play-test debug drive (R14 live test): overrides the
        // controller inputs for direct visual verification.
        private bool debugActive;
        private Direction4 debugDirection;
        private bool debugMoving;

        public void Bind(Sprite[] sheetFrames)
        {
            frames = sheetFrames;
            sr = GetComponent<SpriteRenderer>();
            // ctrl is resolved lazily in Update: at Bind time the parent may
            // not yet have PlayerController2D attached (Bind runs before
            // GameBootstrap adds it in BuildPlayer).
        }

        public void DebugDrive(Direction4 direction, bool moving)
        {
            debugActive = true;
            debugDirection = direction;
            debugMoving = moving;
        }

        public void ClearDebug()
        {
            debugActive = false;
        }

        private void Update()
        {
            if (sr == null || frames == null || frames.Length != 12) return;
            GameSession session = GameSession.Instance;
            if (session != null && session.Paused) return; // freeze with pause overlay

            if (ctrl == null) ctrl = GetComponentInParent<PlayerController2D>();

            Direction4 dir;
            bool moving;
            if (debugActive)
            {
                dir = debugDirection;
                moving = debugMoving;
            }
            else if (session != null && session.InputMode == InputMode.River)
            {
                dir = Direction4.Up;
                moving = false; // The boat carries him: no walking on the deck.
            }
            else
            {
                dir = ctrl != null ? ctrl.Facing : Direction4.Down;
                // Movement by position delta (order-independent): >1e-4u this
                // frame means actually moving.
                Vector3 pos = transform.position;
                if (!hasLastPosition)
                {
                    lastPosition = pos;
                    hasLastPosition = true;
                    moving = false;
                }
                else
                {
                    moving = (pos - lastPosition).sqrMagnitude > 1e-8f;
                    lastPosition = pos;
                }
            }

            timer += Time.deltaTime;
            float step = 1f / Mathf.Max(1f, framesPerSecond);
            while (timer >= step)
            {
                timer -= step;
                if (moving) cycleIndex = (cycleIndex + 1) % WalkCycle.Length;
            }
            int col = moving ? WalkCycle[cycleIndex] : 1;
            sr.sprite = frames[(int)dir * 3 + col];
        }
    }
}
