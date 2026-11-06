using System.Collections;
using Duzhe.Core;
using Duzhe.Presentation;
using UnityEngine;

namespace Duzhe.Scenes
{
    /// PR_CH01_Farewell cast: seats the two elders and the grandson at the
    /// three stools before the dialogue; after the last line both elders fade
    /// out over ~1.2s while the grandson and the courtyard stay, then the
    /// presentation lingers ~0.8s (PresentationMap visualNotes).
    public class CH01FarewellCast : PresentationCast
    {
        public FarmCourtyardCast courtyard;

        [Header("R04 stool points (world)")]
        public Transform elderSeatA; // FC_StoolLeft (672,448) -> (1,-2.75)
        public Transform elderSeatB; // FC_StoolRight (800,448) -> (5,-2.75)
        public Transform grandsonSeat; // FC_StoolFront (736,496) -> (3,-4.25)

        public float fadeDuration = 1.2f;
        public float lingerDuration = 0.8f;

        public override IEnumerator OnPresentationBegin(string presId)
        {
            // R14 rule: the farewell dialogue plays over the STATIC CG
            // tableau (a baked image); the spirit fade happens afterwards in
            // the clean scene - never by fading the whole CG.
            PresentationCG.Show("CG01_FamilyFarewell");
            if (courtyard != null) courtyard.StopGrandsonWalk();
            if (elderSeatA != null && courtyard != null && courtyard.elderA != null)
            {
                courtyard.elderA.Seat(elderSeatA);
            }
            if (elderSeatB != null && courtyard != null && courtyard.elderB != null)
            {
                courtyard.elderB.Seat(elderSeatB);
            }
            if (grandsonSeat != null && courtyard != null && courtyard.grandson != null)
            {
                Vector3 goal = grandsonSeat.position;
                while ((courtyard.grandson.transform.position - goal).sqrMagnitude > 0.002f)
                {
                    GameSession session = GameSession.Instance;
                    if (session != null && !session.Paused)
                    {
                        courtyard.grandson.transform.position = Vector3.MoveTowards(
                            courtyard.grandson.transform.position, goal, 2.2f * Time.deltaTime);
                    }
                    yield return null;
                }
            }
            // Wait until both elders reached their stools (walking time only).
            while ((courtyard != null && courtyard.elderA != null && !courtyard.elderA.IsSeated) ||
                   (courtyard != null && courtyard.elderB != null && !courtyard.elderB.IsSeated))
            {
                yield return null;
            }
        }

        public override IEnumerator OnPresentationVisuals(string presId)
        {
            // R14 rule: back to the clean scene FIRST (hide the CG tableau),
            // then fade ONLY the two elder spirits - the grandson and the
            // courtyard stay untouched.
            PresentationCG.Hide();
            SpriteRenderer[] elders = new SpriteRenderer[2];
            if (courtyard != null && courtyard.elderA != null) elders[0] = courtyard.elderA.GetComponentInChildren<SpriteRenderer>();
            if (courtyard != null && courtyard.elderB != null) elders[1] = courtyard.elderB.GetComponentInChildren<SpriteRenderer>();

            float t = 0f;
            while (t < fadeDuration)
            {
                GameSession session = GameSession.Instance;
                if (session != null && !session.Paused)
                {
                    t += Time.deltaTime;
                    float a = 1f - Mathf.Clamp01(t / fadeDuration);
                    foreach (SpriteRenderer sr in elders)
                    {
                        if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.85f * a);
                    }
                }
                yield return null;
            }
            foreach (SpriteRenderer sr in elders)
            {
                if (sr != null) sr.gameObject.SetActive(false);
            }
            // The whole escort root goes too: no lingering followers after
            // the spirits have departed (keeps the scene clean for the
            // grandson's final beat).
            if (courtyard != null && courtyard.elderA != null) courtyard.elderA.gameObject.SetActive(false);
            if (courtyard != null && courtyard.elderB != null) courtyard.elderB.gameObject.SetActive(false);

            // The grandson stays in his seat for a final beat.
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
