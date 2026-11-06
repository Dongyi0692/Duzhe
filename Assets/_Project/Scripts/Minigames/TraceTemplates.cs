using System;
using System.Collections.Generic;
using UnityEngine;

namespace Duzhe.Minigames
{
    /// One drawing unit (a stroke) of a trace template: normalized points
    /// (0..1 in the drawing area, y up).
    [Serializable]
    public class TraceStroke
    {
        public List<Vector2> points = new List<Vector2>();
    }

    /// Placeholder trace templates for MG01_Cane / MG04_Heart / MG07_Goodbye
    /// (marked Placeholder on purpose). They are real, playable stroke data -
    /// formal CFG_TraceCane/Heart/Goodbye JSON from the resource side replaces
    /// the Goodbye stroke shapes and tolerance values in C06+ without
    /// changing IDs. Resampled at load to ~24 points per stroke.
    public static class TraceTemplates
    {
        private static readonly Dictionary<string, List<TraceStroke>> cache =
            new Dictionary<string, List<TraceStroke>>(StringComparer.Ordinal);

        public static List<TraceStroke> Get(string templateId)
        {
            List<TraceStroke> strokes;
            if (cache.TryGetValue(templateId ?? "", out strokes)) return strokes;
            switch (templateId)
            {
                case "Cane": strokes = BuildCane(); break;
                case "Heart": strokes = BuildHeart(); break;
                case "Goodbye": strokes = BuildGoodbye(); break;
                default:
                    Debug.LogError("[Duzhe] Unknown trace template: " + templateId);
                    strokes = new List<TraceStroke> { new TraceStroke { points = Resample(new List<Vector2> { new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f) }, 12) } };
                    break;
            }
            cache.Add(templateId, strokes);
            return strokes;
        }

        private static List<TraceStroke> BuildCane()
        {
            // Cane: straight grip shaft + top hook curve (two units).
            List<TraceStroke> strokes = new List<TraceStroke>();
            strokes.Add(new TraceStroke
            {
                points = Resample(new List<Vector2>
                {
                    new Vector2(0.50f, 0.06f),
                    new Vector2(0.50f, 0.30f),
                    new Vector2(0.50f, 0.55f)
                }, 14)
            });
            strokes.Add(new TraceStroke
            {
                points = Resample(new List<Vector2>
                {
                    new Vector2(0.50f, 0.55f),
                    new Vector2(0.62f, 0.62f),
                    new Vector2(0.74f, 0.74f),
                    new Vector2(0.74f, 0.88f),
                    new Vector2(0.64f, 0.94f),
                    new Vector2(0.54f, 0.90f)
                }, 20)
            });
            return strokes;
        }

        private static List<TraceStroke> BuildHeart()
        {
            // Heart: single closed unit from the parametric heart curve.
            List<Vector2> raw = new List<Vector2>();
            const int n = 36;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n * Mathf.PI * 2f;
                float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                // Curve spans x -16..16, y about -17..12 -> normalize to 0..1
                // keeping the heart upright (y up).
                raw.Add(new Vector2(x / 32f + 0.5f, (y + 17f) / 30f));
            }
            return new List<TraceStroke>
            {
                new TraceStroke { points = Resample(raw, 36) }
            };
        }

        private static List<TraceStroke> BuildGoodbye()
        {
            // C12 redesign: the two characters "再见" drawn with the REAL
            // stroke order and stroke count (再 6 strokes, 见 4 strokes), so
            // the guide can be followed stroke by stroke instead of the old
            // 14-piece skeleton (which drew 见 as a closed box with an inner
            // bar and could not be read as a character).
            //
            // Coordinates are normalized (0..1, y UP) into the 480x420 ink
            // texture, which is displayed in the 640x214 blackboard area: the
            // x span per character is kept at ~0.34 so each glyph reads
            // roughly square on screen (the area is ~3:1 wide).
            //
            // Cornered strokes (横折 / 竖弯钩) keep their raw polyline so the
            // guide draws the real corner; the evaluator resamples internally.
            List<TraceStroke> strokes = new List<TraceStroke>();

            // ---- 再 (left, x 0.06..0.40) ----
            // 1. 横 (top horizontal), left -> right.
            strokes.Add(new TraceStroke { points = Line(0.07f, 0.94f, 0.39f, 0.94f) });
            // 2. 竖 (left vertical of the frame), top -> bottom.
            strokes.Add(new TraceStroke { points = Line(0.11f, 0.82f, 0.11f, 0.08f) });
            // 3. 横折钩 (frame top, right side, then inward hook).
            strokes.Add(new TraceStroke
            {
                points = new List<Vector2>
                {
                    new Vector2(0.11f, 0.82f), new Vector2(0.37f, 0.82f),
                    new Vector2(0.37f, 0.08f), new Vector2(0.34f, 0.15f)
                }
            });
            // 4. 竖 (centre vertical, inside the frame).
            strokes.Add(new TraceStroke { points = Line(0.24f, 0.94f, 0.24f, 0.28f) });
            // 5. 横 (middle horizontal, crossing the centre vertical).
            strokes.Add(new TraceStroke { points = Line(0.11f, 0.48f, 0.37f, 0.48f) });
            // 6. 横 (crossbar; the two outer verticals extend below it).
            strokes.Add(new TraceStroke { points = Line(0.07f, 0.28f, 0.39f, 0.28f) });

            // ---- 见 (right, x 0.54..0.88) ----
            // 7. 竖 (left vertical of the 冂).
            strokes.Add(new TraceStroke { points = Line(0.57f, 0.94f, 0.57f, 0.52f) });
            // 8. 横折 (top horizontal, then down the right side).
            strokes.Add(new TraceStroke
            {
                points = new List<Vector2>
                {
                    new Vector2(0.57f, 0.94f), new Vector2(0.87f, 0.94f), new Vector2(0.87f, 0.52f)
                }
            });
            // 9. 撇 (left leg of 儿).
            strokes.Add(new TraceStroke { points = new List<Vector2> {
                new Vector2(0.73f, 0.76f), new Vector2(0.72f, 0.52f),
                new Vector2(0.66f, 0.25f), new Vector2(0.55f, 0.10f) } });
            // 10. 竖弯钩 (right leg of 儿 with the hook).
            strokes.Add(new TraceStroke
            {
                points = new List<Vector2>
                {
                    new Vector2(0.77f, 0.52f), new Vector2(0.77f, 0.20f),
                    new Vector2(0.79f, 0.12f), new Vector2(0.88f, 0.12f), new Vector2(0.89f, 0.23f)
                }
            });
            return strokes;
        }

        /// Two-point straight stroke helper (the evaluator resamples anyway).
        private static List<Vector2> Line(float x0, float y0, float x1, float y1)
        {
            return Resample(new List<Vector2> { new Vector2(x0, y0), new Vector2(x1, y1) }, 16);
        }

        /// Resamples a polyline into exactly count points by arc length.
        private static List<Vector2> Resample(List<Vector2> raw, int count)
        {
            List<Vector2> result = new List<Vector2>(count);
            float total = 0f;
            for (int i = 1; i < raw.Count; i++) total += Vector2.Distance(raw[i - 1], raw[i]);
            if (total < 0.0001f)
            {
                for (int i = 0; i < count; i++) result.Add(raw[0]);
                return result;
            }
            for (int i = 0; i < count; i++)
            {
                float target = total * i / (count - 1);
                float acc = 0f;
                for (int s = 1; s < raw.Count; s++)
                {
                    float seg = Vector2.Distance(raw[s - 1], raw[s]);
                    if (acc + seg >= target || s == raw.Count - 1)
                    {
                        float t = seg > 0.0001f ? (target - acc) / seg : 0f;
                        result.Add(Vector2.Lerp(raw[s - 1], raw[s], Mathf.Clamp01(t)));
                        break;
                    }
                    acc += seg;
                }
            }
            return result;
        }
    }
}
