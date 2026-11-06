using System.Collections;
using Duzhe.Core;
using Duzhe.Presentation;
using UnityEngine;

namespace Duzhe.Scenes
{
    /// PR_CH02_Farewell cast: seats the girl and the boy at the reunion point
    /// (embrace), shows the CG02 tableau during the farewell dialogue, then
    /// fades ONLY the girl (PresentationLayers: fadeOnlyActorIds =
    /// CHR05_LoverSpirit; persistentActorIds = CHR06_PastSelfRomance).
    public class CH02FarewellCast : PresentationCast
    {
        public ParkCast park;
        public Transform embraceSpot; // PK_Reunion

        public float fadeDuration = 1.2f;
        public float lingerDuration = 0.8f;

        public override IEnumerator OnPresentationBegin(string presId)
        {
            // R14 rule: the farewell dialogue plays over the STATIC CG tableau.
            PresentationCG.Show("CG02_LoversFarewell");
            if (park != null) park.StopEmbraceWalk();
            // Ensure both are at the embrace spot (in case the heart completion
            // already moved them there, this is idempotent).
            if (park != null && park.loverGhost != null && embraceSpot != null)
            {
                park.loverGhost.Seat(embraceSpot);
            }
            if (park != null && park.boy != null && embraceSpot != null)
            {
                Vector3 goal = embraceSpot.position + new Vector3(0.3f, 0f, 0f);
                while ((park.boy.transform.position - goal).sqrMagnitude > 0.002f)
                {
                    GameSession session = GameSession.Instance;
                    if (session != null && !session.Paused)
                    {
                        park.boy.transform.position = Vector3.MoveTowards(
                            park.boy.transform.position, goal, 2.2f * Time.deltaTime);
                    }
                    yield return null;
                }
            }
            while (park != null && park.loverGhost != null && !park.loverGhost.IsSeated)
            {
                yield return null;
            }
        }

        public override IEnumerator OnPresentationVisuals(string presId)
        {
            // R14 rule: back to the clean scene FIRST (hide the CG tableau),
            // then fade ONLY the girl - the boy and the park stay untouched.
            PresentationCG.Hide();
            SpriteRenderer girl = null;
            if (park != null && park.loverGhost != null)
            {
                girl = park.loverGhost.GetComponentInChildren<SpriteRenderer>();
            }

            float t = 0f;
            while (t < fadeDuration)
            {
                GameSession session = GameSession.Instance;
                if (session != null && !session.Paused)
                {
                    t += Time.deltaTime;
                    float a = 1f - Mathf.Clamp01(t / fadeDuration);
                    if (girl != null) girl.color = new Color(1f, 1f, 1f, 0.85f * a);
                }
                yield return null;
            }
            if (girl != null) girl.gameObject.SetActive(false);
            if (park != null && park.loverGhost != null) park.loverGhost.gameObject.SetActive(false);

            // The boy stays at the reunion point for a final beat.
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
