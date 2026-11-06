using UnityEngine;
using UnityEngine.EventSystems;

namespace Duzhe.Core
{
    /// Single input entry point (old Input Manager, frozen project baseline:
    /// activeInputHandler=0). Every frame it snapshots raw input into
    /// press-once flags; gameplay code must only read through this router so
    /// there is exactly one place deciding what a "press" means.
    /// Hold-to-repeat is impossible by construction (GetKeyDown semantics),
    /// which satisfies the "one press = one line" dialogue rule.
    [DefaultExecutionOrder(-90)]
    public class InputRouter : MonoBehaviour
    {
        public static InputRouter Instance { get; private set; }

        // --- per-frame snapshot, filled in Update ---
        public Vector2 Move { get; private set; }
        public bool InteractPressed { get; private set; }      // E, exploring only
        public bool AdvancePressed { get; private set; }       // Space/Return, dialogue/gallery only
        public bool EscapePressed { get; private set; }        // ESC, anywhere in gameplay
        public bool PointerOverUi { get; private set; }

        private bool consumeRequested;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            bool paused = GameSession.Instance != null && GameSession.Instance.Paused;
            InputMode mode = GameSession.Instance != null ? GameSession.Instance.InputMode : InputMode.Menu;

            float h = 0f, v = 0f;
            if (!paused && (mode == InputMode.Exploring || mode == InputMode.River))
            {
                h = Input.GetAxisRaw("Horizontal");
                // River mode steers the boat: horizontal only, no vertical.
                if (mode == InputMode.Exploring) v = Input.GetAxisRaw("Vertical");
            }
            Vector2 raw = new Vector2(h, v);
            if (raw.sqrMagnitude > 1f) raw.Normalize(); // no faster diagonals
            Move = raw;

            bool interact = !paused && mode == InputMode.Exploring && Input.GetKeyDown(KeyCode.E);
            bool advance = !paused && mode == InputMode.Dialogue &&
                (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter));
            // ESC works both while paused (closes the pause menu) and during
            // gameplay (opens it); consume requests still swallow it so the
            // close-press never re-triggers anything.
            bool escape = Input.GetKeyDown(KeyCode.Escape);

            PointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // Consume requests (mode switches) eat this frame's presses.
            if (consumeRequested)
            {
                interact = false;
                advance = false;
                escape = false;
                consumeRequested = false;
            }

            InteractPressed = interact;
            AdvancePressed = advance;
            EscapePressed = escape;
        }

        /// Called right after any input-mode change: the press that caused the
        /// switch must not also act inside the new mode.
        public void ConsumePendingPresses()
        {
            consumeRequested = true;
            InteractPressed = false;
            AdvancePressed = false;
            EscapePressed = false;
        }
    }
}
