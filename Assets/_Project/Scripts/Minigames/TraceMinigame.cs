using System.Collections.Generic;
using Duzhe.Core;
using Duzhe.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Duzhe.Minigames
{
    /// The drawing minigame panel (MG01_Cane / MG04_Heart / MG07_Goodbye).
    /// Template guide lines are rendered under the player's ink; the current
    /// stroke is evaluated with TraceEvaluator when the pen is lifted. Redraw
    /// clears only the CURRENT stroke (finished strokes stay done, shown as
    /// solid green guides). Lifting the pen keeps progress (spec: allowed).
    public class TraceMinigame : MinigamePanelBase, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const int TextureW = 480;
        private const int TextureH = 420; // drawing area texture size (y up)

        private RectTransform drawArea;
        private RawImage inkImage;
        private Texture2D inkTex;
        private Text statusText;

        private List<TraceStroke> strokes;
        private int strokesDone;
        private readonly List<Vector2> currentInk = new List<Vector2>(); // area pixels
        private readonly List<int> inkBreaks = new List<int>();
        private bool newPenSegment;
        private bool drawing;
        private Vector2 lastInkPoint;
        private bool currentStrokeDirty;

        protected override void BuildUi(Transform parent, MinigameConfig cfg)
        {
            float left = 640f - cfg.panelW * 0.5f;
            float top = 360f - cfg.panelH * 0.5f;
            bool goodbye = cfg.minigameId == "MG07_Goodbye";
            // Card + chrome are centred on their own width; the drawing area
            // and the delivered board art keep their own coordinates.
            float cardW = cfg.panelW + 40f;
            float cardLeft = 640f - cardW * 0.5f;

            // Panel card (y is negative: it hangs DOWN from the canvas top).
            DuzheUiFactory.AddPanel(parent, "TraceCard",
                new Vector2(cardLeft, -(top - 56f)),
                new Vector2(cardW, cfg.panelH + 180f),
                new Color32(24, 22, 30, 246));
            DuzheUiFactory.AddLabel(parent, "TraceTitle",
                new Vector2(cardLeft, -(top - 48f)),
                new Vector2(cardW, 36f), 28,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text =
                MinigameCatalog.Title(cfg);

            // Formal board art (PRP07 for MG07). The writing area is clipped
            // to the visible board and stops above the button band instead of
            // overflowing the 360px image by 40px.
            float drawY = top + 60f;
            float drawW = cfg.panelW;
            float drawH = Mathf.Min(cfg.panelH, cfg.panelW * TextureH / TextureW);
            if (goodbye)
            {
                Sprite board = Resources.Load<Sprite>("Art/Props/PRP07_BlackboardCloseup");
                if (board != null)
                {
                    DuzheUiFactory.AddSpritePanel(parent, "BlackboardArt",
                        new Vector2(left, -top), new Vector2(cfg.panelW, cfg.panelH), board);
                }
                // Declared writeRect [0,80,640,320] ∩ board [0,0,640,360] =
                // [0,80,640,280]; the button band starts at y=300, so the
                // usable writing area is y 80..294 (214 tall, 6px clear of
                // the buttons).
                drawY = top + 80f;
                drawW = 640f;
                drawH = 214f;
            }

            // Drawing area: RawImage showing the ink texture.
            GameObject drawGo = new GameObject("DrawArea");
            drawGo.transform.SetParent(parent, false);
            drawArea = drawGo.AddComponent<RectTransform>();
            drawArea.anchorMin = new Vector2(0f, 1f);
            drawArea.anchorMax = new Vector2(0f, 1f);
            drawArea.pivot = new Vector2(0.5f, 1f);
            drawArea.anchoredPosition = new Vector2(640f, -drawY);
            drawArea.sizeDelta = new Vector2(drawW, drawH);
            inkTex = new Texture2D(TextureW, TextureH, TextureFormat.RGBA32, false);
            inkTex.filterMode = FilterMode.Bilinear;
            ClearInkTexture();
            inkImage = drawGo.AddComponent<RawImage>();
            inkImage.texture = inkTex;
            inkImage.raycastTarget = true; // receives the pen

            // Buttons: redraw (current stroke only) + close. Positions use the
            // top-left origin convention, so y is NEGATIVE (down).
            float bottomY = goodbye ? -(top + 300f) : -(360f + (cfg.panelH * 0.5f) - 40f);
            DuzheUiFactory.AddButton(parent, "Redraw",
                new Vector2(left + 30f, bottomY), new Vector2(180f, 44f),
                UiText.Get("button.clear_stroke", "重画当前笔画"), 20,
                () => RedrawCurrentStroke());
            DuzheUiFactory.AddButton(parent, "CloseTrace",
                new Vector2(left + cfg.panelW - 210f, bottomY), new Vector2(180f, 44f),
                UiText.Get("button.close", "关闭"), 20,
                () => { if (MinigameHost.Instance != null) MinigameHost.Instance.Close(); });

            float statusY = goodbye ? -(top + cfg.panelH + 8f) : -(360f + cfg.panelH * 0.5f) - 10f;
            statusText = DuzheUiFactory.AddLabel(parent, "TraceStatus",
                new Vector2(cardLeft, statusY),
                new Vector2(cardW, 30f), 20,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleCenter);

            strokes = TraceTemplates.Get(cfg.templateId);
            RefreshStatus();
        }

        // -- pointer handling (the pen) ------------------------------------

        public void OnPointerDown(PointerEventData e)
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Paused) return;
            Vector2 area = ToAreaPixels(e.position);
            if (area.x < 0f || area.y < 0f || area.x > TextureW || area.y > TextureH) return;
            List<TraceStroke> strokes = TraceTemplates.Get(Config.templateId);
            if (strokesDone >= strokes.Count) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Duzhe] TRACE down at " + area.ToString("F0") + " strokeIdx=" + strokesDone);
#endif
            // Free start: the player may begin anywhere on the stroke; the
            // coverage/corridor/endpoint checks still block scribbles and
            // shortcuts (guidance dot shows the suggested start).
            drawing = true;
            newPenSegment = true;
            if (currentInk.Count > 0) inkBreaks.Add(currentInk.Count);
            currentStrokeDirty = true;
            // C10: chalk loop only for the blackboard minigame.
            if (Config != null && Config.minigameId == "MG07_Goodbye")
            {
                GameSession chalkSession = GameSession.Instance;
                if (chalkSession != null && chalkSession.Audio != null)
                {
                    chalkSession.Audio.PlaySfxLoop("SFX09_Chalk");
                }
            }
            AddInkPoint(area);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!drawing) return;
            GameSession session = GameSession.Instance;
            if (session != null && session.Paused) return;
            Vector2 area = ToAreaPixels(e.position);
            AddInkPoint(area);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!drawing) return;
            AddInkPoint(ToAreaPixels(e.position));
            drawing = false;
            StopChalkLoop();
            EvaluateCurrentStroke();
        }

        private void StopChalkLoop()
        {
            GameSession session = GameSession.Instance;
            if (session != null && session.Audio != null) session.Audio.StopSfxLoop("SFX09_Chalk");
        }

        private void OnDisable()
        {
            StopChalkLoop();
        }

        private Vector2 ToAreaPixels(Vector3 screenPos)
        {
            // ScreenPointToLocalPointInRectangle handles the overlay-canvas
            // coordinate transform (screen origin/units vs canvas units) for
            // us. local origin = pivot (top-center), y axis points UP.
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                drawArea, screenPos, null, out local);
            // Map: x center -> texture center; texture TOP (y=H) at local.y=0.
            float texX = TextureW * (0.5f + local.x / drawArea.rect.width);
            float texY = TextureH * (1f + local.y / drawArea.rect.height);
            return new Vector2(
                Mathf.Clamp(texX, 0f, TextureW),
                Mathf.Clamp(texY, 0f, TextureH));
        }

        private void AddInkPoint(Vector2 areaPx)
        {
            // Instant visual feedback: ink drawn INSIDE the template corridor
            // is green, outside it is red - the player sees correctness in
            // real time while drawing.
            List<TraceStroke> strokes = TraceTemplates.Get(Config.templateId);
            bool inCorridor = strokesDone < strokes.Count &&
                TraceEvaluator.DistanceToStrokePixels(areaPx, strokes[strokesDone].points, TextureW, TextureH)
                    <= TraceEvaluator.CorridorHalfPx(TextureW, TextureH);
            Color inkColor = inCorridor
                ? new Color32(140, 220, 140, 255)
                : new Color32(235, 110, 110, 255);

            if (currentInk.Count > 0 && !newPenSegment)
            {
                Vector2 last = currentInk[currentInk.Count - 1];
                if (Vector2.Distance(last, areaPx) < 2f) return; // dedupe
                DrawLine(inkTex, last, areaPx, inkColor, 3);
            }
            else
            {
                DrawDot(inkTex, areaPx, inkColor, 2);
            }
            inkTex.Apply(false);
            currentInk.Add(areaPx);
            newPenSegment = false;
            lastInkPoint = areaPx;
        }

        // -- evaluation -----------------------------------------------------

        private void EvaluateCurrentStroke()
        {
            if (!currentStrokeDirty) return;
            List<TraceStroke> strokes = TraceTemplates.Get(Config.templateId);
            TraceStroke stroke = strokes[strokesDone];
            TraceEvaluator.Result result = TraceEvaluator.EvaluateStroke(
                stroke.points, currentInk, TextureW, TextureH, inkBreaks);
            if (result.passed)
            {
                strokesDone++;
                currentInk.Clear();
                inkBreaks.Clear();
                currentStrokeDirty = false;
                RedrawGuideTexture(); // finished stroke now shown as solid green
                PlayFeedbackTone(true);
                if (strokesDone >= strokes.Count)
                {
                    statusText.text = TraceSuccessText();
                    if (Host != null) Host.Complete();
                    return;
                }
                statusText.text = UiText.Get("minigame.trace.stroke_complete", "这一笔完成了。") + "  " +
                    UiText.Get("r03.trace.progress", "当前笔画 {current} / {total}")
                        .Replace("{current}", strokesDone.ToString())
                        .Replace("{total}", strokes.Count.ToString());
            }
            else
            {
                statusText.text = "已保留当前笔画，可抬笔后继续补画，或重画。";
                // Keep the ink so the player can continue/adjust the stroke.
            }
        }

        private AudioSource feedbackSource;
        private AudioClip toneUp;
        private AudioClip toneDown;

        /// Short program-generated tones: rising = stroke accepted, falling =
        /// rejected. Placeholder feedback until real SFX arrive.
        private void PlayFeedbackTone(bool positive)
        {
            if (feedbackSource == null)
            {
                feedbackSource = gameObject.AddComponent<AudioSource>();
                feedbackSource.playOnAwake = false;
            }
            const int rate = 44100;
            int samples = (int)(rate * 0.18f);
            AudioClip clip = AudioClip.Create("trace_feedback", samples, 1, rate, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float f = positive ? 660f + 340f * (t / 0.18f) : 520f - 260f * (t / 0.18f);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * 0.3f * Mathf.Exp(-4f * t);
            }
            clip.SetData(data, 0);
            if (positive) toneUp = clip; else toneDown = clip;
            feedbackSource.PlayOneShot(clip);
        }

        private void RedrawCurrentStroke()
        {
            currentInk.Clear();
            inkBreaks.Clear();
            currentStrokeDirty = false;
            ClearInkTexture();
            RedrawGuideTexture();
            statusText.text = "";
        }

        // -- texture painting ------------------------------------------------

        private void ClearInkTexture()
        {
            Color[] clear = new Color[TextureW * TextureH];
            for (int i = 0; i < clear.Length; i++) clear[i] = new Color(0f, 0f, 0f, 0f);
            inkTex.SetPixels(clear);
            RedrawGuideTexture();
            inkTex.Apply(false);
        }

        /// Draws the template guides into the ink texture: finished strokes as
        /// solid green, the current stroke bright yellow, upcoming ones dim.
        private void RedrawGuideTexture()
        {
            Color doneColor = new Color32(120, 200, 120, 160);
            Color currentColor = new Color32(240, 220, 110, 200);
            Color upcomingColor = new Color32(120, 120, 130, 90);
            List<TraceStroke> strokes = TraceTemplates.Get(Config.templateId);
            for (int i = 0; i < strokes.Count; i++)
            {
                Color c = i < strokesDone ? doneColor : (i == strokesDone ? currentColor : upcomingColor);
                List<Vector2> pts = strokes[i].points;
                for (int s = 1; s < pts.Count; s++)
                {
                    DrawLine(inkTex, ToTex(pts[s - 1]), ToTex(pts[s]), c, i == strokesDone ? 3 : 2);
                }
                // Start dot of the current stroke.
                if (i == strokesDone && pts.Count > 0)
                {
                    DrawDot(inkTex, ToTex(pts[0]), new Color32(255, 240, 160, 255), 4);
                }
            }
        }

        private Vector2 ToTex(Vector2 normalized)
        {
            // Template normalized (0..1, y up) -> texture pixels. This must
            // match TraceEvaluator EXACTLY (it maps p.x*areaW / p.y*areaH):
            // an earlier 8px inset drew the guide slightly smaller than the
            // judged line, so short strokes could never reach their end.
            return new Vector2(
                Mathf.Clamp(normalized.x, 0f, 1f) * TextureW,
                Mathf.Clamp(normalized.y, 0f, 1f) * TextureH);
        }

        private static void DrawLine(Texture2D tex, Vector2 a, Vector2 b, Color c, int width)
        {
            int steps = Mathf.Max(1, (int)Vector2.Distance(a, b));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, (float)i / steps);
                DrawDot(tex, p, c, width);
            }
        }

        private static void DrawDot(Texture2D tex, Vector2 p, Color c, int radius)
        {
            int cx = (int)p.x, cy = (int)p.y;
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) > radius * radius) continue;
                    tex.SetPixel(x, y, c);
                }
            }
        }

        private void RefreshStatus()
        {
            if (statusText != null)
            {
                statusText.text = MinigameCatalog.TraceInstructions(Config != null ? Config.templateId : "");
            }
        }

        private string TraceSuccessText()
        {
            switch (Config != null ? Config.templateId : "")
            {
                case "Cane": return UiText.Get("minigame.cane.success", "拐杖做好了。带回老人身边吧。");
                case "Goodbye": return UiText.Get("minigame.goodbye.finished", "再见。");
                default: return "完成了！";
            }
        }

        public override void OnGamePaused()
        {
            // Unfinished stroke is discarded (restore to origin); finished
            // strokes stay done.
            drawing = false;
            StopChalkLoop();
            // Keep the unfinished ink and let the next press resume it.
        }
    }
}
