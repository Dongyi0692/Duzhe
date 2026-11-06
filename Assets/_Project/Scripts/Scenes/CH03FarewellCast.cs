using System.Collections;
using Duzhe.Core;
using Duzhe.Presentation;
using UnityEngine;

namespace Duzhe.Scenes
{
    /// PR_CH03_Farewell cast: the drummer takes his place in the group before
    /// the dialogue and the CG03 tableau (four people, drummer included)
    /// shows during it; after the last line the CG is hidden FIRST, then ONLY
    /// the drummer fades - the three living bandmates and the studio stay.
    public class CH03FarewellCast : PresentationCast
    {
        public RecordingStudioCast studio;

        public float fadeDuration = 1.2f;
        public float lingerDuration = 0.8f;

        public override IEnumerator OnPresentationBegin(string presId)
        {
            // R14 rule: the farewell dialogue plays over the STATIC CG tableau.
            PresentationCG.Show("CG03_BandFarewell");
            if (studio != null) studio.SeatDrummer();
            // Bounded wait: a presentation must never hang because the ghost
            // could not reach the spot (e.g. hidden by a route gate).
            float waited = 0f;
            while (studio != null && studio.DrummerGhost != null && !studio.DrummerGhost.IsSeated &&
                   waited < 3f)
            {
                GameSession session = GameSession.Instance;
                if (session != null && !session.Paused) waited += Time.deltaTime;
                yield return null;
            }
        }

        public override IEnumerator OnPresentationVisuals(string presId)
        {
            // Back to the clean scene first, then fade ONLY the drummer.
            PresentationCG.Hide();
            SpriteRenderer drummer = null;
            if (studio != null && studio.DrummerGhost != null)
            {
                drummer = studio.DrummerGhost.GetComponentInChildren<SpriteRenderer>();
            }

            float t = 0f;
            while (t < fadeDuration)
            {
                GameSession session = GameSession.Instance;
                if (session != null && !session.Paused)
                {
                    t += Time.deltaTime;
                    float a = 1f - Mathf.Clamp01(t / fadeDuration);
                    if (drummer != null) drummer.color = new Color(1f, 1f, 1f, 0.85f * a);
                }
                yield return null;
            }
            if (drummer != null) drummer.gameObject.SetActive(false);
            if (studio != null && studio.DrummerGhost != null) studio.DrummerGhost.gameObject.SetActive(false);

            // The band stays in the studio for a final beat.
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
