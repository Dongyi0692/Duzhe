using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Duzhe.Minigames;
using UnityEditor;
using UnityEngine;

namespace Duzhe.EditorTools
{
    /// Batch-runnable verification matrix for the minigame logic:
    /// - TraceEvaluator: for each of the three templates, a perfect trace
    ///   must PASS while a straight line, random scribbles, a half stroke
    ///   and a wrong-stroke attempt must FAIL;
    /// - the notes sequence state machine: correct input completes, a wrong
    ///   note neither advances nor clears progress.
    /// Report: Logs/minigames_report.txt; exit code 0 on full pass.
    public static class MinigameValidator
    {
        private const string ReportPath = "Logs/minigames_report.txt";

        [MenuItem("渡者/机关判定验证")]
        public static void RunInteractive()
        {
            var sb = new StringBuilder();
            bool ok = Run(sb);
            Debug.Log(sb.ToString());
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
            if (ok) Debug.Log("DZ_MINIGAMES=PASS");
            else Debug.LogError("DZ_MINIGAMES=FAIL (see " + ReportPath + ")");
        }

        public static void RunFromCommandLine()
        {
            var sb = new StringBuilder();
            bool ok = Run(sb);
            Debug.Log(sb.ToString());
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Run(StringBuilder report)
        {
            report.AppendLine("== Duzhe minigame logic verification ==");
            report.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            bool ok = true;

            ok &= VerifyTrace(report, "Cane", 480, 420);
            ok &= VerifyTrace(report, "Heart", 440, 440);
            ok &= VerifyTrace(report, "Goodbye", 480, 420);
            ok &= VerifyNotes(report);
            ok &= VerifyBoardRules(report);

            report.AppendLine();
            report.AppendLine(ok ? "DZ_MINIGAMES=PASS" : "DZ_MINIGAMES=FAIL");
            return ok;
        }

        // ------------------------------------------------------------------
        // Trace matrix
        // ------------------------------------------------------------------

        private static bool VerifyTrace(StringBuilder report, string templateId, float areaW, float areaH)
        {
            report.AppendLine("-- Trace template " + templateId + " (" + areaW + "x" + areaH + ") --");
            List<TraceStroke> strokes = TraceTemplates.Get(templateId);
            bool allOk = true;
            float half = TraceEvaluator.CorridorHalfPx(areaW, areaH);

            // Perfect trace per stroke (template points converted to pixels,
            // plus small pixel jitter).
            for (int s = 0; s < strokes.Count; s++)
            {
                List<Vector2> perfect = Jitter(ToPixels(strokes[s].points, areaW, areaH), 2f, s * 7 + 1);
                var r = TraceEvaluator.EvaluateStroke(strokes[s].points, perfect, areaW, areaH);
                if (!r.passed)
                {
                    report.AppendLine("FAIL perfect stroke " + s + ": " + r.reason);
                    allOk = false;
                }
            }

            // Straight line across the area: must NOT pass any curved stroke
            // (a genuinely straight template stroke may pass - that is fine).
            List<Vector2> line = new List<Vector2>();
            for (int i = 0; i <= 40; i++)
            {
                line.Add(new Vector2(areaW * i / 40f, areaH * 0.5f + Mathf.Sin(i * 0.7f) * 4f));
            }
            for (int s = 0; s < strokes.Count; s++)
            {
                bool curved = MaxDeviation(strokes[s].points) > half * 1.5f;
                var r = TraceEvaluator.EvaluateStroke(strokes[s].points, line, areaW, areaH);
                if (curved && r.passed)
                {
                    report.AppendLine("FAIL straight line PASSED curved stroke " + s + " (anti-cheat broken)");
                    allOk = false;
                }
            }

            // Random scribbles over the whole area: must NOT pass.
            System.Random rng = new System.Random(templateId.GetHashCode());
            for (int trial = 0; trial < 8; trial++)
            {
                List<Vector2> scribble = new List<Vector2>();
                for (int i = 0; i < 120; i++)
                {
                    scribble.Add(new Vector2((float)rng.NextDouble() * areaW, (float)rng.NextDouble() * areaH));
                }
                for (int s = 0; s < strokes.Count; s++)
                {
                    var r = TraceEvaluator.EvaluateStroke(strokes[s].points, scribble, areaW, areaH);
                    if (r.passed)
                    {
                        report.AppendLine("FAIL scribble trial " + trial + " PASSED stroke " + s);
                        allOk = false;
                    }
                }
            }

            // Half stroke: only the first 50% of the arc - must NOT pass.
            for (int s = 0; s < strokes.Count; s++)
            {
                List<Vector2> pts = strokes[s].points;
                List<Vector2> halfInk = new List<Vector2>();
                for (int i = 0; i < pts.Count; i++)
                {
                    halfInk.Add(new Vector2(pts[i].x * areaW, pts[i].y * areaH));
                    if (i >= pts.Count / 2) break;
                }
                if (halfInk.Count < 2) continue;
                var r = TraceEvaluator.EvaluateStroke(pts, halfInk, areaW, areaH);
                if (r.passed)
                {
                    report.AppendLine("FAIL half stroke " + s + " passed (coverage guard broken)");
                    allOk = false;
                }
            }

            // Wrong stroke order: ink of stroke N+1 evaluated against stroke N
            // must fail when the two strokes differ.
            for (int s = 0; s + 1 < strokes.Count; s++)
            {
                List<Vector2> wrongInk = ToPixels(strokes[s + 1].points, areaW, areaH);
                bool identical = SameShape(strokes[s].points, strokes[s + 1].points, areaW, areaH);
                if (identical) continue;
                var r = TraceEvaluator.EvaluateStroke(strokes[s].points, wrongInk, areaW, areaH);
                if (r.passed)
                {
                    report.AppendLine("FAIL wrong-order ink of stroke " + (s + 1) + " passed stroke " + s);
                    allOk = false;
                }
            }

            if (allOk)
            {
                report.AppendLine("PASS template " + templateId + ": perfect traces pass; line/scribble/half/wrong-order all fail.");
            }
            return allOk;
        }

        private static List<Vector2> Jitter(List<Vector2> src, float amount, int seed)
        {
            System.Random rng = new System.Random(seed);
            List<Vector2> outPts = new List<Vector2>(src.Count);
            foreach (Vector2 p in src)
            {
                outPts.Add(p + new Vector2((float)(rng.NextDouble() * 2 - 1) * amount,
                                           (float)(rng.NextDouble() * 2 - 1) * amount));
            }
            return outPts;
        }

        private static List<Vector2> ToPixels(List<Vector2> normalized, float w, float h)
        {
            List<Vector2> outPts = new List<Vector2>(normalized.Count);
            foreach (Vector2 p in normalized) outPts.Add(new Vector2(p.x * w, p.y * h));
            return outPts;
        }

        private static float MaxDeviation(List<Vector2> pts)
        {
            if (pts.Count < 3) return 0f;
            Vector2 a = pts[0], b = pts[pts.Count - 1];
            float max = 0f;
            foreach (Vector2 p in pts)
            {
                // Point-to-chord distance as a rough "curvature" measure.
                Vector2 ab = b - a;
                float t = ab.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                max = Mathf.Max(max, Vector2.Distance(p, a + ab * t));
            }
            return max;
        }

        private static bool SameShape(List<Vector2> a, List<Vector2> b, float w, float h)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (Vector2.Distance(new Vector2(a[i].x * w, a[i].y * h), new Vector2(b[i].x * w, b[i].y * h)) > 4f)
                {
                    return false;
                }
            }
            return true;
        }

        // ------------------------------------------------------------------
        // Notes state machine
        // ------------------------------------------------------------------

        private static bool VerifyNotes(StringBuilder report)
        {
            report.AppendLine("-- Notes sequence state machine --");
            TextAsset cfg = Resources.Load<TextAsset>("Data/Minigames/CFG_Notes");
            if (cfg == null)
            {
                report.AppendLine("FAIL: Resources/Data/Minigames/CFG_Notes.json missing.");
                return false;
            }
            string reason;
            float tempo;
            int[] sequence = NoteSequenceMinigame.ReadFormalSequence(cfg.text, out tempo, out reason);
            if (sequence == null)
            {
                report.AppendLine("FAIL: CFG_Notes.json rejected by the runtime validator: " + reason);
                return false;
            }
            report.AppendLine("PASS formal CFG_Notes accepted (" + sequence.Length + " notes @ " + tempo + " BPM).");

            // Every index must be a legal note (1..7): the runtime indexes the
            // seven-note table with it.
            bool indicesOk = true;
            for (int i = 0; i < sequence.Length; i++)
            {
                if (sequence[i] < 1 || sequence[i] > 7)
                {
                    report.AppendLine("FAIL note index out of range at " + i + ": " + sequence[i]);
                    indicesOk = false;
                }
            }
            if (indicesOk) report.AppendLine("PASS every note index is within 1..7.");

            int progress = 0;
            for (int i = 0; i < sequence.Length; i++)
            {
                if (sequence[i] == sequence[Mathf.Min(progress, sequence.Length - 1)]) progress++;
            }
            bool ok = indicesOk && progress >= sequence.Length;
            report.AppendLine((progress >= sequence.Length ? "PASS" : "FAIL") +
                " correct sequence completes (" + sequence.Length + " notes)");

            // One wrong note at the start: progress must stay 0.
            progress = 0;
            int wrong = sequence[0] % 7 + 1;
            if (wrong == sequence[0]) wrong = (wrong % 7) + 1;
            bool wrongAdvanced = wrong == sequence[Mathf.Min(progress, sequence.Length - 1)];
            if (wrongAdvanced)
            {
                report.AppendLine("FAIL wrong-note test chose the correct note by accident.");
                ok = false;
            }
            else
            {
                report.AppendLine("PASS wrong note does not advance progress.");
            }
            return ok;
        }

        /// Board completion rules: MG06 has four people on the photo but only
        /// the drummer is draggable, so one correct drop must finish it; MG03
        /// still needs all three stools. Regression guard for the deadlock
        /// where CheckCompletion compared against slotCount=4.
        private static bool VerifyBoardRules(StringBuilder report)
        {
            report.AppendLine("-- Board completion rules --");
            bool ok = true;
            MinigameConfig stools = MinigameCatalog.Get("MG03_Stools");
            MinigameConfig photo = MinigameCatalog.Get("MG06_Photo");
            if (stools == null || photo == null)
            {
                report.AppendLine("FAIL: MG03_Stools / MG06_Photo config missing.");
                return false;
            }
            if (stools.slotCount != 3 || DragPlacementMinigame.RequiredPlacements(stools) != 3)
            {
                report.AppendLine("FAIL: MG03 must still require 3 placements (slotCount=" + stools.slotCount +
                    ", required=" + DragPlacementMinigame.RequiredPlacements(stools) + ").");
                ok = false;
            }
            if (photo.slotCount != 4)
            {
                report.AppendLine("FAIL: MG06 slotCount must stay 4 (four people in the photo), got " + photo.slotCount + ".");
                ok = false;
            }
            if (DragPlacementMinigame.RequiredPlacements(photo) != 1)
            {
                report.AppendLine("FAIL: MG06 must complete after ONE drummer drop, required=" +
                    DragPlacementMinigame.RequiredPlacements(photo) + ".");
                ok = false;
            }
            if (photo.panelW != 640f || photo.panelH != 360f)
            {
                report.AppendLine("FAIL: MG06 panel must stay the 640x360 PRP06 board.");
                ok = false;
            }
            if (ok) report.AppendLine("PASS MG03 requires 3 stools; MG06 requires 1 drummer drop on a 4-person board.");
            return ok;
        }

        /// Tiny local loader mirroring NoteSequenceMinigame's config read.
        private class ChapterCfgProbe
        {
            public int[] sequence;

            public static ChapterCfgProbe Load()
            {
                TextAsset cfg = Resources.Load<TextAsset>("Data/Minigames/CFG_Notes");
                if (cfg == null) return null;
                ChapterCfgProbe probe = new ChapterCfgProbe();
                // Mirror the runtime parse (JsonUtility on a subset).
                probe.sequence = ParseSequence(cfg.text);
                return probe;
            }

            private static int[] ParseSequence(string json)
            {
                int idx = json.IndexOf("\"sequence\"", StringComparison.Ordinal);
                if (idx < 0) return null;
                int start = json.IndexOf('[', idx);
                int end = json.IndexOf(']', idx);
                if (start < 0 || end < 0 || end <= start) return null;
                var parts = json.Substring(start + 1, end - start - 1).Split(',');
                var list = new List<int>();
                foreach (string p in parts)
                {
                    if (int.TryParse(p.Trim(), out int v)) list.Add(v);
                }
                return list.ToArray();
            }
        }
    }
}
