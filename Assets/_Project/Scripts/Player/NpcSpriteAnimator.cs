using Duzhe.Core;
using UnityEngine;

namespace Duzhe.Player
{
    /// Four-direction walk animation for supporting NPCs / spirits, driven by
    /// their own world-position delta (no controller needed). Frame layout is
    /// the R12 contract: rows Down/Left/Right/Up, cols WalkA/Idle/WalkB;
    /// walking cycles WalkA->Idle->WalkB->Idle at 6 fps, standing holds the
    /// Idle column. Freezes with the pause overlay and never touches the
    /// spirit's alpha (SpiritEscort owns the fade).
    public class NpcSpriteAnimator : MonoBehaviour
    {
        [Tooltip("12 frames, [row*3+col]: rows Down/Left/Right/Up, cols WalkA/Idle/WalkB.")]
        [SerializeField] private Sprite[] frames;
        [Tooltip("Walk cycle frames per second (delivery default 6).")]
        public float framesPerSecond = 6f;

        private static readonly int[] WalkCycle = { 0, 1, 2, 1 };

        private SpriteRenderer sr;
        private float timer;
        private int cycleIndex;
        private Vector3 lastPosition;
        private bool hasLastPosition;
        private Direction4 facing = Direction4.Down;
        private bool moving;
        private bool debugActive;
        private Direction4 debugDirection;
        private bool debugMoving;

        /// Binds the 12 sliced frames and the renderer to drive. Returns false
        /// when the sheet is missing (the caller then keeps the static stand).
        public bool Bind(Sprite[] sheetFrames, SpriteRenderer target)
        {
            if (sheetFrames == null || sheetFrames.Length != 12 || target == null) return false;
            frames = sheetFrames;
            sr = target;
            lastPosition = transform.position;
            hasLastPosition = true;
            sr.sprite = frames[(int)facing * 3 + 1]; // Idle
            return true;
        }

        /// The renderer reference is a runtime binding; resolve it again after
        /// a scene load (the serialized frames survive, the object ref does not
        /// have to be re-assembled).
        private void Awake()
        {
            if (sr == null) sr = GetComponentInChildren<SpriteRenderer>(true);
        }

        public Direction4 Facing { get { return facing; } }
        public bool IsWalking { get { return moving; } }

        /// Play-mode test hook: drives direction/motion without moving.
        public void DebugDrive(Direction4 direction, bool walking)
        {
            debugActive = true;
            debugDirection = direction;
            debugMoving = walking;
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

            if (debugActive)
            {
                facing = debugDirection;
                moving = debugMoving;
            }
            else
            {
                Vector3 pos = transform.position;
                if (!hasLastPosition)
                {
                    lastPosition = pos;
                    hasLastPosition = true;
                    moving = false;
                }
                else
                {
                    Vector3 delta = pos - lastPosition;
                    moving = delta.sqrMagnitude > 1e-8f;
                    if (moving) facing = DirectionFromDelta(delta);
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
            sr.sprite = frames[(int)facing * 3 + col];
        }

        private static Direction4 DirectionFromDelta(Vector3 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                return delta.x < 0f ? Direction4.Left : Direction4.Right;
            }
            return delta.y < 0f ? Direction4.Down : Direction4.Up;
        }
    }
}
