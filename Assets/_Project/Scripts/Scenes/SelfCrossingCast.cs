using System.Collections;
using System.Collections.Generic;
using Duzhe.Core;
using Duzhe.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.Scenes
{
    /// CH05 「自渡」 presentation cast, reused for both presentations:
    /// - PR_CH05_Recall (Underworld): the four CGs appear ON the dialogue
    ///   lines that recognise each past identity, never on a timer, so the
    ///   image can never run ahead of the player's reading;
    /// - PR_CH05_Reincarnation (River): CG05 while the four lines play, then
    ///   the CG is hidden and the REAL boat + ferryman drift away into a warm
    ///   fade (the boat and the ferryman move together, nobody is left
    ///   behind and no extra passenger is invented).
    public class SelfCrossingCast : PresentationCast
    {
        private string boundRunId;
        private Dialogue.DialogueRunner boundRunner;
        private GameObject warmOverlay;
        private bool rowingMemory;
        private float memoryDrift;
        private Dictionary<string, VisualCue> cues;

        [System.Serializable]
        private class VisualCue { public string lineId; public string imageId; public bool blackboard; }
        [System.Serializable]
        private class VisualCueFile { public VisualCue[] cues; }


        [Header("Reincarnation drift (world units over the whole beat)")]
        public Vector3 departOffset = new Vector3(2.6f, 1.4f, 0f);
        public float departDuration = 3.5f;

        public override IEnumerator OnPresentationBegin(string presId)
        {
            GameSession session = GameSession.Instance;
            boundRunId = session != null ? session.RunId : "";
            if (presId == "PR_CH05_Recall")
            {
                PresentationCG.Hide();
                BlackboardTextLayer.Hide();
                if (session != null && session.Hud != null) session.Hud.SetCinematic(true);
                BindLines();
            }
            else if (presId == "PR_CH05_Reincarnation")
            {
                PresentationCG.ShowRecollection("CG18_FamilyRecollection");
                if (session != null && session.Hud != null) session.Hud.SetCinematic(true);
                rowingMemory = true;
                memoryDrift = 0f;
                if (session != null && session.Audio != null) session.Audio.PlaySfxLoop("SFX06_Rowing");
            }
            yield break;
        }

        // -- recall: image follows the dialogue lines -----------------------

        private void BindLines()
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Dialogue == null) return;
            UnbindLines();
            boundRunner = session.Dialogue;
            boundRunner.LineShown += HandleLine;
        }

        private void UnbindLines()
        {
            if (boundRunner != null) boundRunner.LineShown -= HandleLine;
            boundRunner = null;
        }

        private void OnDisable()
        {
            UnbindLines();
            StopMemoryRowing();
            DestroyWarmOverlay();
        }

        private void OnDestroy()
        {
            UnbindLines();
            StopMemoryRowing();
            DestroyWarmOverlay();
        }

        private void HandleLine(Dialogue.DialogueLine line)
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.RunId != boundRunId || presentationId != "PR_CH05_Recall" ||
                line == null || line.chapterId != "CH05" || line.stageId != "CH05_Recall") return;
            if (cues == null)
            {
                cues = new Dictionary<string, VisualCue>();
                TextAsset json = Resources.Load<TextAsset>("Data/Dialogue/StoryVisualCues");
                if (json == null) { Debug.LogError("[Duzhe] StoryVisualCues missing."); return; }
                VisualCueFile file = JsonUtility.FromJson<VisualCueFile>(json.text);
                if (file != null && file.cues != null)
                    foreach (VisualCue cue in file.cues) cues[cue.lineId] = cue;
            }
            VisualCue found;
            if (!cues.TryGetValue(line.lineId, out found)) return;
            BlackboardTextLayer.Hide();
            if (string.IsNullOrEmpty(found.imageId)) PresentationCG.Hide();
            else
            {
                PresentationCG.Show(found.imageId);
                if (found.blackboard) BlackboardTextLayer.Show(new[] { "再见" });
            }
        }

        public override IEnumerator OnPresentationSegmentFinished(string presId, string segmentId)
        {
            if (presId != "PR_CH05_Reincarnation") yield break;
            string next = null;
            switch (segmentId)
            {
                case "CH05_RecollectionFamily": next = "CG19_LoverRecollection"; break;
                case "CH05_RecollectionLover": next = "CG20_FriendsRecollection"; break;
                case "CH05_RecollectionFriends": next = "CG21_TeacherRecollection"; break;
                case "CH05_RecollectionTeacher": next = "CG05_SelfCrossing"; break;
            }
            if (next == null) yield break;
            yield return FadePicture(false);
            PresentationCG.ShowRecollection(next);
            PresentationCG.SetOpacity(0f);
            yield return FadePicture(true);
        }

        private IEnumerator FadePicture(bool appear)
        {
            float elapsed = 0f;
            const float duration = 0.35f;
            while (elapsed < duration)
            {
                GameSession session = GameSession.Instance;
                if (session == null || session.RunId != boundRunId) yield break;
                if (!session.Paused)
                {
                    elapsed += Time.deltaTime;
                    float k = Mathf.Clamp01(elapsed / duration);
                    PresentationCG.SetOpacity(appear ? k : 1f - k);
                }
                yield return null;
            }
            PresentationCG.SetOpacity(appear ? 1f : 0f);
        }

        private void Update()
        {
            if (!rowingMemory || memoryDrift >= 1.2f) return;
            GameSession session = GameSession.Instance;
            if (session == null || session.Paused || session.RunId != boundRunId) return;
            River.BoatController boat = FindObjectOfType<River.BoatController>();
            if (boat == null) return; // Gallery replay uses only the illustrated boat.
            float step = Mathf.Min(0.12f * Time.deltaTime, 1.2f - memoryDrift);
            memoryDrift += step;
            boat.transform.position += Vector3.up * step;
            GameBootstrap boot = GameBootstrap.Instance;
            if (boot != null && boot.Player != null) boot.Player.transform.position += Vector3.up * step;
        }

        private void StopMemoryRowing()
        {
            if (!rowingMemory) return;
            rowingMemory = false;
            GameSession session = GameSession.Instance;
            if (session != null && session.Audio != null) session.Audio.StopSfxLoop("SFX06_Rowing");
        }

        // -- post-dialogue visuals -----------------------------------------

        public override IEnumerator OnPresentationVisuals(string presId)
        {
            if (presId == "PR_CH05_Recall")
            {
                UnbindLines();
                BlackboardTextLayer.Hide();
                PresentationCG.Hide();
                yield break;
            }

            StopMemoryRowing();
            // Reincarnation: back to the live river FIRST, then depart.
            PresentationCG.Hide();
            BlackboardTextLayer.Hide();

            Transform boat = null;
            River.BoatController controller = FindObjectOfType<River.BoatController>();
            if (controller != null) boat = controller.transform;
            Transform player = null;
            GameBootstrap boot = GameBootstrap.Instance;
            if (boot != null && boot.Player != null) player = boot.Player.transform;

            CreateWarmOverlay();

            float t = 0f;
            float duration = Mathf.Max(0.5f, departDuration);
            while (t < duration)
            {
                GameSession session = GameSession.Instance;
                if (session != null && !session.Paused)
                {
                    float dt = Time.deltaTime;
                    t += dt;
                    float k = Mathf.Clamp01(t / duration);
                    Vector3 step = Vector3.up * (2.4f * dt / duration);
                    // Boat and ferryman move TOGETHER: the ferryman is never
                    // parented to a scene object (he survives scene loads).
                    if (boat != null) boat.position += step;
                    if (player != null) player.position += step;
                    SetWarmAlpha(Mathf.Lerp(0f, 0.92f, k));
                }
                yield return null;
            }
            SetWarmAlpha(0.92f);
        }

        // -- warm fade overlay (persistent canvas, cleaned on destroy) ------

        private void CreateWarmOverlay()
        {
            DestroyWarmOverlay();
            GameObject canvas = GameObject.Find(GameBootstrap.UiCanvasName);
            if (canvas == null) return;
            warmOverlay = new GameObject("DZ_CH05_WarmFade");
            warmOverlay.transform.SetParent(canvas.transform, false);
            RectTransform rt = warmOverlay.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = warmOverlay.AddComponent<Image>();
            img.color = new Color32(255, 214, 170, 0);
            img.raycastTarget = false; // never swallow the credits button
            // Above the scene and the CG, below the credits/pause panels.
            GameSession session = GameSession.Instance;
            if (session != null && session.Credits != null)
                warmOverlay.transform.SetSiblingIndex(session.Credits.transform.GetSiblingIndex());
            else warmOverlay.transform.SetAsFirstSibling();
        }

        private void SetWarmAlpha(float a)
        {
            if (warmOverlay == null) return;
            Image img = warmOverlay.GetComponent<Image>();
            if (img != null) img.color = new Color32(255, 214, 170, (byte)Mathf.Clamp(a * 255f, 0f, 255f));
        }

        private void DestroyWarmOverlay()
        {
            if (warmOverlay != null)
            {
                Destroy(warmOverlay);
                warmOverlay = null;
            }
        }
    }
}
