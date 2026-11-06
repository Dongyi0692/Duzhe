using UnityEngine;

namespace Duzhe.Dialogue
{
    /// Plays one fixed dialogue segment line by line; the view shows each
    /// line in full (no typewriter). When the last line is passed a
    /// DialogueFinished event is published for ChapterDirector. This runner
    /// never decides story flow itself.
    [DefaultExecutionOrder(-30)]
    public class DialogueRunner : MonoBehaviour
    {
        public bool IsPlaying { get; private set; }

        /// Fired after a line is shown. Purely a visual notification: no
        /// subscriber may advance the dialogue or complete a chapter with it.
        public event System.Action<DialogueLine> LineShown;

        private ChapterDialogue chapter;
        private DialogueSegment segment;
        private int lineIndex;
        private DialogueView view;
        private int lastAdvanceFrame = -1;
        private string capturedRunId = "";

        private void Start()
        {
            if (DialogueLibrary.Instance == null) DialogueLibrary.Load();
        }

        public void BindView(DialogueView dialogueView)
        {
            view = dialogueView;
        }

        /// Starts a segment. Requests while another segment is playing are
        /// rejected (logged) rather than queued - the director owns ordering.
        public void StartSegment(string chapterId, string segmentId)
        {
            if (IsPlaying)
            {
                Debug.LogWarning("[Duzhe] StartSegment(" + segmentId + ") ignored: another segment is playing.");
                return;
            }
            DialogueSegment seg;
            ChapterDialogue ch;
            DialogueLibrary lib = DialogueLibrary.Instance;
            if (lib == null || !lib.TryGetSegment(chapterId, segmentId, out seg))
            {
                Debug.LogError("[Duzhe] Dialogue segment not found: chapter=" + chapterId + " segment=" + segmentId);
                return;
            }
            lib.TryGetChapter(chapterId, out ch);
            chapter = ch;
            segment = seg;
            lineIndex = 0;
            // Remember which run owns this segment: a completion published
            // after a run reset must not reach the next run's listeners.
            capturedRunId = Core.GameSession.Instance != null
                ? Core.GameSession.Instance.RunId : "";

            Core.GameSession.Instance.SetInputMode(Core.InputMode.Dialogue);
            IsPlaying = true;
            ShowCurrentLine();
        }

        /// One press = one line. Both the UI click and the space/return key
        /// funnel here; a same-frame duplicate is dropped.
        public void Advance()
        {
            if (!IsPlaying || segment == null) return;
            if (lastAdvanceFrame == Time.frameCount) return; // same-frame duplicate press
            lastAdvanceFrame = Time.frameCount;

            lineIndex++;
            if (lineIndex >= segment.lines.Count)
            {
                FinishSegment();
            }
            else
            {
                ShowCurrentLine();
            }
        }

        private void ShowCurrentLine()
        {
            if (view == null)
            {
                // A missing view is an error, NOT a finished segment: calling
                // FinishSegment here would publish a completion for dialogue
                // the player never saw.
                Debug.LogError("[Duzhe] DialogueView is not bound; stopping segment without completion.");
                IsPlaying = false;
                segment = null;
                chapter = null;
                return;
            }
            DialogueLine line = segment.lines[lineIndex];
            string speakerName = Chapters.ChapterCatalog.GetActorDisplayName(line.speakerId, line.speakerId);
            view.ShowLine(speakerName, line.text, lineIndex, segment.lines.Count);
            if (LineShown != null) LineShown(line);
        }

        private void FinishSegment()
        {
            IsPlaying = false;
            if (view != null) view.HideLine();
            Core.GameSession session = Core.GameSession.Instance;
            string stageId = segment != null ? segment.stageId : "";
            string segmentId = segment != null ? segment.segmentId : "";
            string chapterId = chapter != null ? chapter.chapterId : "";
            segment = null;
            chapter = null;
            if (session == null) return;
            if (!string.IsNullOrEmpty(capturedRunId) && session.RunId != capturedRunId)
            {
                // The run was reset while this segment played: publishing now
                // would hand the old dialogue to the new run.
                Debug.Log("[Duzhe] Dialogue finished after a run reset; completion dropped (" +
                    segmentId + ").");
                if (!session.Paused) session.SetInputMode(Core.InputMode.Exploring);
                return;
            }
            // Restores exploring; the closing press is consumed by the mode
            // switch so it cannot leak into the world.
            session.SetInputMode(Core.InputMode.Exploring);
            session.PublishEvent(new Core.GameEvent(Core.GameEventType.DialogueFinished,
                chapterId, stageId, segmentId, capturedRunId));
        }

        /// Hard stop with NO DialogueFinished event (pause-menu leave). The
        /// dialogue panel must not leak into the main menu.
        public void ForceStop()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            segment = null;
            chapter = null;
            if (view != null) view.HideLine();
            Core.GameSession session = Core.GameSession.Instance;
            if (session != null && !session.Paused) session.SetInputMode(Core.InputMode.Exploring);
        }
    }
}
