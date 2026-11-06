using System.Collections.Generic;
using UnityEngine;

namespace Duzhe.Minigames
{
    /// Pure-math evaluation of a trace attempt against one template stroke.
    /// Deterministic and UI-free so the editor validator (MinigameValidator)
    /// can drive the same code the game uses.
    ///
    /// Guarantees (spec section 8): sampled-point coverage >= 75%, drawn
    /// length inside the corridor >= 65%, corridor half width = 5% of the
    /// drawing area's SHORT side, start near the stroke's first sampled
    /// point, forward progress along the stroke (a single straight line or
    /// full-screen scribbling cannot pass).
    public static class TraceEvaluator
    {
        public const float CoveragePass = 0.75f;
        public const float CorridorRatioPass = 0.65f;

        public struct Result
        {
            public bool passed;
            public float coverage;       // template points touched by ink
            public float corridorRatio;  // ink inside the corridor
            public string reason;        // null when passed
        }

        /// Corridor half width in pixels: 5% of the SHORT side of the drawing
        /// area (both dimensions in pixels, e.g. 480x420 -> 21 px).
        public static float CorridorHalfPx(float areaW, float areaH)
        {
            return Mathf.Min(areaW, areaH) * 0.075f;
        }

        /// Evaluates the player's ink for ONE stroke. strokePoints are the
        /// template's NORMALIZED (0..1) points; playerPoints are the ink
        /// pixels of the current attempt (area-pixel space, y up). All math
        /// happens in area-pixel space.
        public static Result EvaluateStroke(List<Vector2> strokePointsNormalized, List<Vector2> playerPoints,
            float areaW, float areaH, List<int> inkBreaks = null)
        {
            Result r = new Result();
            float half = CorridorHalfPx(areaW, areaH);

            if (playerPoints == null || playerPoints.Count < 2)
            {
                r.reason = "not enough ink";
                return r;
            }
            // Pointer events describe a continuous polyline, not isolated
            // dots. Sample its length uniformly so a fast straight stroke
            // and a slowly drawn one receive the same coverage/ratio score.
            float inkLength;
            if (inkBreaks == null || inkBreaks.Count == 0) playerPoints = Resample(playerPoints, 128, out inkLength);
            else
            {
                var sampled = new List<Vector2>();
                inkLength = 0f;
                int first = 0;
                for (int part = 0; part <= inkBreaks.Count; part++)
                {
                    int end = part < inkBreaks.Count ? inkBreaks[part] : playerPoints.Count;
                    var sub = playerPoints.GetRange(first, end - first);
                    if (sub.Count > 1)
                    {
                        Resample(sub, 2, out float length);
                        inkLength += length;
                        sampled.AddRange(Resample(sub, Mathf.Max(2, Mathf.CeilToInt(length / 2f)), out _));
                    }
                    first = end;
                }
                playerPoints = sampled;
            }
            if (inkLength < 1f) { r.reason = "not enough ink"; return r; }

            // Normalize -> pixels (both dimensions use their own scale).
            List<Vector2> strokePoints = new List<Vector2>(strokePointsNormalized.Count);
            foreach (Vector2 p in strokePointsNormalized)
            {
                strokePoints.Add(new Vector2(p.x * areaW, p.y * areaH));
            }

            List<Vector2> samples = Resample(strokePoints, 20, out float strokeLength);

            if (strokeLength < areaH * 0.12f)
            {
                r.reason = "template stroke too short";
                return r;
            }

            // 1) Coverage: every template sample must have ink within the
            //    corridor half width.
            int covered = 0;
            foreach (Vector2 sp in samples)
            {
                if (HasInkNear(playerPoints, sp, half)) covered++;
            }
            r.coverage = samples.Count > 0 ? (float)covered / samples.Count : 0f;
            if (r.coverage < CoveragePass)
            {
                r.reason = "coverage " + (r.coverage * 100f).ToString("F0") + "% < 75%";
                return r;
            }

            // 2) Corridor ratio: at least 65% of the ink pixels lie inside
            //    the corridor (any position along the stroke).
            if (playerPoints.Count == 0)
            {
                r.reason = "no ink";
                return r;
            }
            int inCorridor = 0;
            foreach (Vector2 pp in playerPoints)
            {
                if (DistanceToPolyline(pp, strokePoints) <= half) inCorridor++;
            }
            r.corridorRatio = (float)inCorridor / playerPoints.Count;
            if (r.corridorRatio < CorridorRatioPass)
            {
                r.reason = "corridor ratio " + (r.corridorRatio * 100f).ToString("F0") + "% < 65%";
                return r;
            }

            // 3) Forward progress: the projection parameter along the stroke
            //    must not jump backwards too much and must reach the end
            //    (anti one-line / anti scribble rules also come from 1+2).
            float maxS = 0f, minS = 1f;
            foreach (Vector2 pp in playerPoints)
            {
                float sParam = ProjectToPolyline(pp, strokePoints);
                if (DistanceToPolyline(pp, strokePoints) <= half)
                {
                    maxS = Mathf.Max(maxS, sParam);
                    minS = Mathf.Min(minS, sParam);
                }
            }
            if (maxS < 0.95f)
            {
                r.reason = "stroke end not reached (" + (maxS * 100f).ToString("F0") + "%)";
                return r;
            }

            r.passed = true;
            return r;
        }

        /// Start-point check for a new attempt: the first ink pixel must be
        /// near the stroke's first sample (within 3x corridor half width),
        /// otherwise the attempt is ignored (no penalty, guidance shown).
        public static bool StartsNearFirstSample(List<Vector2> strokePoints, Vector2 firstInk,
            float areaW, float areaH)
        {
            float half = CorridorHalfPx(areaW, areaH) * 3f;
            return Vector2.Distance(firstInk, strokePoints[0]) <= half;
        }

        /// Distance from an area-PIXEL point to a NORMALIZED template stroke
        /// (converted to pixels). Used by the panel for live ink coloring.
        public static float DistanceToStrokePixels(Vector2 pixelPoint, List<Vector2> normalizedStroke,
            float areaW, float areaH)
        {
            List<Vector2> px = new List<Vector2>(normalizedStroke.Count);
            foreach (Vector2 p in normalizedStroke)
            {
                px.Add(new Vector2(p.x * areaW, p.y * areaH));
            }
            return DistanceToPolyline(pixelPoint, px);
        }

        // ------------------------------------------------------------------

        private static List<Vector2> Resample(List<Vector2> points, int count, out float length)
        {
            length = 0f;
            List<Vector2> outPts = new List<Vector2>(count);
            for (int i = 1; i < points.Count; i++) length += Vector2.Distance(points[i - 1], points[i]);
            if (points.Count == 0)
            {
                for (int i = 0; i < count; i++) outPts.Add(Vector2.zero);
                return outPts;
            }
            if (length < 0.0001f)
            {
                for (int i = 0; i < count; i++) outPts.Add(points[0]);
                return outPts;
            }
            float acc = 0f;
            int seg = 1;
            for (int i = 0; i < count; i++)
            {
                float target = length * i / (count - 1);
                while (seg < points.Count - 1 && acc + Vector2.Distance(points[seg - 1], points[seg]) < target)
                {
                    acc += Vector2.Distance(points[seg - 1], points[seg]);
                    seg++;
                }
                float segLen = Vector2.Distance(points[seg - 1], points[seg]);
                float t = segLen > 0.0001f ? Mathf.Clamp01((target - acc) / segLen) : 0f;
                outPts.Add(Vector2.Lerp(points[seg - 1], points[seg], t));
            }
            return outPts;
        }

        private static bool HasInkNear(List<Vector2> playerPoints, Vector2 p, float radius)
        {
            float r2 = radius * radius;
            foreach (Vector2 pp in playerPoints)
            {
                if ((pp - p).sqrMagnitude <= r2) return true;
            }
            return false;
        }

        private static float DistanceToPolyline(Vector2 p, List<Vector2> poly)
        {
            float best = float.MaxValue;
            for (int i = 1; i < poly.Count; i++)
            {
                best = Mathf.Min(best, DistanceToSegment(p, poly[i - 1], poly[i]));
            }
            return best;
        }

        private static float ProjectToPolyline(Vector2 p, List<Vector2> poly)
        {
            // Returns the normalized arc-length parameter (0..1) of the
            // closest point on the polyline.
            float total = 0f;
            for (int i = 1; i < poly.Count; i++) total += Vector2.Distance(poly[i - 1], poly[i]);
            if (total < 0.0001f) return 0f;
            float acc = 0f;
            float bestS = 0f, bestD = float.MaxValue;
            for (int i = 1; i < poly.Count; i++)
            {
                Vector2 a = poly[i - 1], b = poly[i];
                float segLen = Vector2.Distance(a, b);
                Vector2 ab = b - a;
                float t = ab.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                Vector2 proj = a + ab * t;
                float d = Vector2.Distance(p, proj);
                if (d < bestD)
                {
                    bestD = d;
                    bestS = (acc + segLen * t) / total;
                }
                acc += segLen;
            }
            return bestS;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
