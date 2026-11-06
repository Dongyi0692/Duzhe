using System;
using System.Collections.Generic;
using Duzhe.Core;
using Duzhe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.Minigames
{
    /// MG05_Notes: seven-note guided performance (Do..Si / C4..B4). The next
    /// target note is highlighted; a wrong note plays but neither advances
    /// nor clears progress; no beat/rhythm failure of any kind. The
    /// development sequence is the spec placeholder (14 notes, marked
    /// Placeholder); a formal CFG_Notes.json + 7 WAV files replace it in
    /// C06+ (loaded from Resources/Data/Minigames/CFG_Notes.json when
    /// present, silent sine-wave placeholders otherwise).
    public class NoteSequenceMinigame : MinigamePanelBase
    {
        public static readonly string[] NoteNames =
            { "Do", "Re", "Mi", "Fa", "Sol", "La", "Si" };

        private static readonly float[] NoteFrequencies =
            { 261.63f, 293.66f, 329.63f, 349.23f, 392.00f, 440.00f, 493.88f };

        private int[] sequence = new int[0];
        private float tempoBpm;
        private int progress;
        private readonly Button[] noteButtons = new Button[7];
        private readonly Image[] noteHighlights = new Image[7];
        private Text statusText;
        private readonly AudioClip[] placeholderClips = new AudioClip[7];

        protected override void BuildUi(Transform parent, MinigameConfig cfg)
        {
            float left = 640f - cfg.panelW * 0.5f;
            float top = 360f - cfg.panelH * 0.5f;
            // The card is 40px wider than the board, so it must be centred on
            // its own width (640 - cardW/2); anchoring it at "left" shifted
            // the card 20px right of its own content.
            float cardW = cfg.panelW + 40f;
            float cardLeft = 640f - cardW * 0.5f;

            DuzheUiFactory.AddPanel(parent, "NotesCard",
                new Vector2(cardLeft, -top), new Vector2(cardW, cfg.panelH + 100f),
                new Color32(24, 22, 30, 246));
            DuzheUiFactory.AddLabel(parent, "NotesTitle",
                new Vector2(cardLeft, -(top + 10f)),
                new Vector2(cardW, 36f), 28,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text =
                MinigameCatalog.Title(cfg);

            // Progress dots (one per sequence entry).
            LoadSequence();
            GameObject dots = new GameObject("ProgressDots");
            RectTransform dotsRt = dots.AddComponent<RectTransform>(); // explicit
            dots.transform.SetParent(parent, false);
            // Full-stretch container so the dot positions below use absolute
            // reference coordinates.
            dotsRt.anchorMin = Vector2.zero;
            dotsRt.anchorMax = Vector2.one;
            dotsRt.offsetMin = Vector2.zero;
            dotsRt.offsetMax = Vector2.zero;
            float dotX = 640f - (sequence.Length - 1) * 13f;
            for (int i = 0; i < sequence.Length; i++)
            {
                GameObject d = new GameObject("Dot_" + i);
                d.transform.SetParent(dots.transform, false);
                Image di = d.AddComponent<Image>();
                di.color = new Color32(110, 108, 120, 255);
                di.raycastTarget = false;
                RectTransform rt = di.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(dotX + i * 26f, -(top + 56f));
                rt.sizeDelta = new Vector2(14f, 14f);
            }

            // Seven note buttons with keyboard shortcut digits as labels.
            float btnW = 86f, gap = 8f;
            float startX = 640f - (7 * btnW + 6 * gap) * 0.5f;
            for (int i = 0; i < 7; i++)
            {
                int index = i + 1; // captured
                string label = UiText.Get("minigame.notes.note_" + index,
                    NoteNames[i] + "（数字键 " + index + "）");
                Button b = DuzheUiFactory.AddButton(parent, "Note_" + NoteNames[i],
                    new Vector2(startX + i * (btnW + gap), -(top + 80f)), new Vector2(btnW, 72f),
                    label, 18,
                    () => OnNotePressed(index));
                noteButtons[i] = b;
                noteHighlights[i] = b.GetComponent<Image>();
            }

            statusText = DuzheUiFactory.AddLabel(parent, "NotesStatus",
                new Vector2(cardLeft, -(top + cfg.panelH - 20f)),
                new Vector2(cardW, 30f), 20,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleCenter);

            // C10: notes play through the shared AudioService sfx bus (no
            // private AudioSource), and the music ducks while performing.
            GameSession session = GameSession.Instance;
            if (session != null && session.Audio != null) session.Audio.SetNotesActive(true);

            DuzheUiFactory.AddButton(parent, "CloseNotes",
                new Vector2(cardLeft + cardW - 150f, -(top + cfg.panelH + 40f)),
                new Vector2(150f, 44f), UiText.Get("button.close", "关闭"), 20,
                () => { if (MinigameHost.Instance != null) MinigameHost.Instance.Close(); });

            HighlightTarget();
            RefreshStatus();
        }

        private void LoadSequence()
        {
            // Formal CFG_Notes.json (R15) lives at
            // Resources/Data/Minigames/CFG_Notes.json. It is validated before
            // use: a malformed score falls back to the catalog placeholder
            // with a loud warning, and the handoff must report that the
            // formal sequence is NOT active.
            TextAsset cfg = Resources.Load<TextAsset>("Data/Minigames/CFG_Notes");
            if (cfg == null)
            {
                Debug.LogWarning("[Duzhe] MG05: Data/Minigames/CFG_Notes.json missing; " +
                    "using the placeholder sequence (formal score NOT active).");
                sequence = (int[])Config.sequence.Clone();
                return;
            }

            string reason;
            float tempo;
            int[] formal = ReadFormalSequence(cfg.text, out tempo, out reason);
            if (formal == null)
            {
                Debug.LogWarning("[Duzhe] MG05: CFG_Notes.json rejected (" + reason +
                    "); using the placeholder sequence (formal score NOT active).");
                sequence = (int[])Config.sequence.Clone();
                return;
            }

            sequence = formal;
            tempoBpm = tempo;
            Debug.Log("[Duzhe] MG05: formal CFG_Notes loaded (" + sequence.Length +
                " notes @ " + tempoBpm + " BPM).");
        }

        /// The delivered schema. Validation is deliberately strict: a wrong
        /// note index would read past the seven-note table, and a missing
        /// noteBeats entry means the file is not the delivered score.
        [Serializable]
        private class NotesConfig
        {
            public string schemaVersion;
            public string minigameId;
            public string demoAssetId;
            public float tempoBpm;
            public string[] noteAssetIds;
            public int[] sequence;
            public float[] noteBeats;
        }

        /// Single interpretation of the delivered schema for both the runtime
        /// loader and the editor validator (which passes the raw file text).
        public static bool ValidateNotesConfigText(string json, out string reason)
        {
            NotesConfig parsed = null;
            try
            {
                parsed = JsonUtility.FromJson<NotesConfig>(json);
            }
            catch (Exception e)
            {
                reason = "parse error: " + e.Message;
                return false;
            }
            return IsValidNotesConfig(parsed, out reason);
        }

        /// Reads and validates a delivered CFG_Notes.json body. Returns null
        /// (with the field-level reason) when the file is not the formal
        /// score - the caller must fall back to the placeholder explicitly.
        public static int[] ReadFormalSequence(string json, out float tempo, out string reason)
        {
            tempo = 0f;
            NotesConfig parsed = null;
            try
            {
                parsed = JsonUtility.FromJson<NotesConfig>(json);
            }
            catch (Exception e)
            {
                reason = "parse error: " + e.Message;
                return null;
            }
            if (!IsValidNotesConfig(parsed, out reason)) return null;
            tempo = parsed.tempoBpm;
            return (int[])parsed.sequence.Clone();
        }

        private static bool IsValidNotesConfig(NotesConfig data, out string reason)
        {
            string[] ids = { "N01_Do", "N02_Re", "N03_Mi", "N04_Fa",
                             "N05_Sol", "N06_La", "N07_Si" };
            reason = "";
            if (data == null) { reason = "root object is null"; return false; }
            if (data.schemaVersion != "1.0") { reason = "schemaVersion=" + data.schemaVersion; return false; }
            if (data.minigameId != "MG05_Notes") { reason = "minigameId=" + data.minigameId; return false; }
            if (data.demoAssetId != "MEL01_BandPhrase") { reason = "demoAssetId=" + data.demoAssetId; return false; }
            if (!(data.tempoBpm > 0f) || float.IsInfinity(data.tempoBpm))
            { reason = "tempoBpm=" + data.tempoBpm; return false; }
            if (data.noteAssetIds == null || data.noteAssetIds.Length != 7)
            { reason = "noteAssetIds length=" + (data.noteAssetIds == null ? 0 : data.noteAssetIds.Length); return false; }
            if (data.sequence == null || data.sequence.Length < 12 || data.sequence.Length > 16)
            { reason = "sequence length=" + (data.sequence == null ? 0 : data.sequence.Length); return false; }
            if (data.noteBeats == null || data.noteBeats.Length != data.sequence.Length)
            { reason = "noteBeats length mismatch"; return false; }
            for (int i = 0; i < 7; i++)
            {
                if (data.noteAssetIds[i] != ids[i])
                { reason = "noteAssetIds[" + i + "]=" + data.noteAssetIds[i]; return false; }
            }
            for (int i = 0; i < data.sequence.Length; i++)
            {
                if (data.sequence[i] < 1 || data.sequence[i] > 7)
                { reason = "sequence[" + i + "]=" + data.sequence[i] + " outside 1..7"; return false; }
                if (!(data.noteBeats[i] > 0f) || float.IsInfinity(data.noteBeats[i]))
                { reason = "noteBeats[" + i + "]=" + data.noteBeats[i]; return false; }
            }
            return true;
        }

        // ------------------------------------------------------------------

        private void Update()
        {
            if (Config == null) return;
            // Number keys 1..7 as auxiliary input (spec: keyboard helpers).
            for (int i = 0; i < 7; i++)
            {
                KeyCode k = KeyCode.Alpha1 + i;
                KeyCode keypad = KeyCode.Keypad1 + i;
                if (Input.GetKeyDown(k) || Input.GetKeyDown(keypad))
                {
                    OnNotePressed(i + 1);
                }
            }
        }

        private void OnNotePressed(int noteIndex)
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Paused) return;
            if (sequence.Length == 0 || progress >= sequence.Length) return;

            PlayNote(noteIndex);

            int target = sequence[progress];
            if (noteIndex == target)
            {
                progress++;
                RefreshStatus();
                if (progress >= sequence.Length)
                {
                    statusText.text = UiText.Get("minigame.notes.finished", "这段旋律，完整了。");
                    if (Host != null) Host.Complete();
                }
            }
            else
            {
                // Wrong note: it plays, but neither advances nor clears progress.
                statusText.text = UiText.Get("minigame.notes.wrong", "试试亮起的那个音。");
            }
        }

        private void PlayNote(int noteIndex)
        {
            int idx = Mathf.Clamp(noteIndex - 1, 0, 6);
            AudioClip clip = null;
            // Formal per-note WAVs (N01_Do..) take priority once delivered.
            clip = Resources.Load<AudioClip>("Audio/Notes/N0" + noteIndex + "_" + NoteNames[idx]);
            if (clip == null)
            {
                if (placeholderClips[idx] == null)
                {
                    placeholderClips[idx] = GenerateTone(NoteFrequencies[idx], 0.5f);
                }
                clip = placeholderClips[idx];
            }
            GameSession session = GameSession.Instance;
            if (session != null && session.Audio != null) session.Audio.PlayGameplayClip(clip);
        }

        /// Restores the music duck when the panel closes for any reason
        /// (completed, closed, pause-menu leave, chapter switch).
        private void OnDisable()
        {
            GameSession session = GameSession.Instance;
            if (session != null && session.Audio != null) session.Audio.SetNotesActive(false);
        }

        /// Generates a soft sine tone so the placeholder build gives audible
        /// feedback without the formal WAV files.
        private static AudioClip GenerateTone(float frequency, float seconds)
        {
            const int rate = 44100;
            int samples = (int)(rate * seconds);
            AudioClip clip = AudioClip.Create("tone_" + frequency.ToString("F0"), samples, 1, rate, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float envelope = Mathf.Min(1f, i / (rate * 0.02f)) *
                    Mathf.Exp(-3f * t);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.35f * envelope;
            }
            clip.SetData(data, 0);
            return clip;
        }

        private void HighlightTarget()
        {
            // The next target note is highlighted via the status text; the
            // target dot is brightened in RefreshStatus as well.
        }

        private void RefreshStatus()
        {
            if (statusText == null) return;
            if (sequence.Length == 0)
            {
                statusText.text = "（无序列配置）";
                return;
            }
            int target = sequence[Mathf.Min(progress, sequence.Length - 1)];
            bool finished = progress >= sequence.Length;
            statusText.text = finished
                ? UiText.Get("minigame.notes.finished", "这段旋律，完整了。")
                : UiText.Get("minigame.notes.next", "下一个音") + "：" +
                  UiText.Get("minigame.notes.note_" + target, NoteNames[target - 1] + "（数字键 " + target + "）");

            // Highlight the TARGET note button so the guidance is unmissable.
            Color idle = new Color32(52, 48, 62, 255);
            Color targetColor = new Color32(168, 140, 58, 255);
            for (int i = 0; i < 7; i++)
            {
                if (noteButtons[i] == null) continue;
                Image img = noteButtons[i].GetComponent<Image>();
                if (img == null) continue;
                bool isTarget = !finished && (i + 1) == target;
                img.color = isTarget ? targetColor : idle;
            }

            // Brighten the progress dot for the next entry via the dots group.
            Transform dots = PanelGO != null && PanelGO.transform.Find("ProgressDots")
                ? PanelGO.transform.Find("ProgressDots") : null;
            if (dots != null)
            {
                for (int i = 0; i < dots.childCount; i++)
                {
                    Image di = dots.GetChild(i).GetComponent<Image>();
                    if (di == null) continue;
                    di.color = i < progress
                        ? new Color32(150, 200, 150, 255)
                        : (i == progress ? new Color32(240, 220, 110, 255)
                                         : new Color32(110, 108, 120, 255));
                }
            }
        }

        public override void OnGamePaused()
        {
            // Nothing to restore: progress and highlights persist across the
            // pause overlay (spec: wrong notes never clear progress).
        }
    }
}
