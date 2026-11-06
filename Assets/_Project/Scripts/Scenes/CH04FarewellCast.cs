using System.Collections;
using Duzhe.Core;
using Duzhe.Presentation;
using UnityEngine;

namespace Duzhe.Scenes
{
    /// PR_CH04_Farewell cast. Main run / chapter replay: the class already
    /// happened in its own stage, so the farewell shows CG04 with the
    /// finished "再见" on the board. Gallery replay: the lesson is part of the
    /// presentation, so the board stays blank during the lesson and the
    /// writing appears (with the school bell) between the lesson and the
    /// farewell. After the last line the CG and its text layer are hidden
    /// FIRST, then ONLY the teacher fades - the student and the room stay.
    public class CH04FarewellCast : PresentationCast
    {
        public ClassroomCast classroom;

        public float fadeDuration = 1.2f;
        public float lingerDuration = 0.8f;

        private static readonly string[] GoodbyeLines = { "再见" };

        public override IEnumerator OnPresentationBegin(string presId)
        {
            // R14 rule: the farewell dialogue plays over the STATIC CG tableau.
            PresentationCG.Show("CG04_TeacherFarewell");
            GameSession session = GameSession.Instance;
            bool replay = session != null && session.Mode == GameMode.PresentationReplay;
            if (!replay)
            {
                // The player wrote these two characters in the previous stage.
                BlackboardTextLayer.Show(GoodbyeLines);
            }
            yield break;
        }

        public override IEnumerator OnPresentationSegmentFinished(string presId, string segmentId)
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Mode != GameMode.PresentationReplay) yield break;
            if (segmentId == "CH04_Lesson")
            {
                // Replay keeps the original order: class, completed writing,
                // three memories, then farewell. No minigame or save writes.
                if (session.Audio != null) session.Audio.PlaySfx("SFX10_SchoolBell");
                BlackboardTextLayer.Show(GoodbyeLines);
                float elapsed = 0f;
                while (elapsed < 0.8f)
                {
                    if (!session.Paused) elapsed += Time.deltaTime;
                    yield return null;
                }
                BlackboardTextLayer.Hide();
                PresentationCG.Show("CG15_TeacherMemory01");
            }
            else if (segmentId == "CH04_Memory01") PresentationCG.Show("CG16_TeacherMemory02");
            else if (segmentId == "CH04_Memory02") PresentationCG.Show("CG17_TeacherMemory03");
            else if (segmentId == "CH04_Memory03")
            {
                PresentationCG.Show("CG04_TeacherFarewell");
                BlackboardTextLayer.Show(GoodbyeLines);
            }
            yield break;
        }

        public override IEnumerator OnPresentationVisuals(string presId)
        {
            // Back to the clean scene first, then fade ONLY the teacher.
            PresentationCG.Hide();
            BlackboardTextLayer.Hide();
            SpriteRenderer teacher = classroom != null && classroom.teacher != null
                ? classroom.teacher.GetComponentInChildren<SpriteRenderer>()
                : null;

            float t = 0f;
            while (t < fadeDuration)
            {
                GameSession session = GameSession.Instance;
                if (session != null && !session.Paused)
                {
                    t += Time.deltaTime;
                    float a = 1f - Mathf.Clamp01(t / fadeDuration);
                    if (teacher != null) teacher.color = new Color(1f, 1f, 1f, 0.85f * a);
                }
                yield return null;
            }
            if (teacher != null) teacher.gameObject.SetActive(false);
            if (classroom != null && classroom.teacher != null) classroom.teacher.SetActive(false);

            // The student and the classroom stay for a final beat.
            t = 0f;
            while (t < lingerDuration)
            {
                GameSession session = GameSession.Instance;
                if (session != null && !session.Paused) t += Time.deltaTime;
                yield return null;
            }
        }
    }
}
