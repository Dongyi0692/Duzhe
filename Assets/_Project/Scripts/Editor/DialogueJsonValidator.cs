using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Duzhe.Chapters;
using UnityEditor;
using UnityEngine;

namespace Duzhe.EditorTools
{
    /// Validates every TXT_CHxx.json / TXT_UI.json under
    /// Assets/_Project/Resources/Data/Dialogue against the delivery contract
    /// (section 2). Reports the concrete file / field / id for every problem;
    /// the batch runner fails on any error. Uses the same JSON parse shape as
    /// the runtime so there is exactly one interpretation of the schema.
    public static class DialogueJsonValidator
    {
        private const string DataDir = "Assets/_Project/Resources/Data/Dialogue";

        public static bool ValidateAll(StringBuilder report)
        {
            string[] files = Directory.Exists(DataDir) ? Directory.GetFiles(DataDir, "TXT_*.json") : new string[0];
            if (files.Length == 0)
            {
                report.AppendLine("DIALOGUE VALIDATION: no TXT_*.json found in " + DataDir);
                return false;
            }

            bool ok = true;
            var seenLineIds = new HashSet<string>(StringComparer.Ordinal);
            var seenUiKeys = new HashSet<string>(StringComparer.Ordinal);
            var seenChapterIds = new HashSet<string>(StringComparer.Ordinal);
            var legalActors = CollectLegalActors();

            Array.Sort(files);
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string json = File.ReadAllText(file, Encoding.UTF8);
                ok &= ValidateOne(fileName, json, seenLineIds, seenUiKeys, seenChapterIds, legalActors, report);
            }
            return ok;
        }

        private static HashSet<string> CollectLegalActors()
        {
            var actors = new HashSet<string>(StringComparer.Ordinal);
            // The delivery contract states speakerId may only use ids defined
            // by the specs; chapter configs + the delivered
            // CharacterDisplayNames.json are the in-project sources.
            foreach (string key in AllChapterActorIds()) actors.Add(key);
            Dialogue.DialogueLibrary.Load();
            Dialogue.DialogueLibrary lib = Dialogue.DialogueLibrary.Instance;
            if (lib != null)
            {
                foreach (string actorId in lib.GlobalActorIds) actors.Add(actorId);
            }
            return actors;
        }

        private static IEnumerable<string> AllChapterActorIds()
        {
            // ChapterCatalog static ctor registers all chapters, and actor
            // display names enumerate their actors.
            var ids = new List<string>();
            foreach (ChapterConfig cfg in AllChapters())
            {
                foreach (var kv in cfg.actorDisplayNames) ids.Add(kv.Key);
            }
            return ids;
        }

        private static IEnumerable<ChapterConfig> AllChapters()
        {
            var list = new List<ChapterConfig>();
            for (int i = 0; i < ChapterCatalog.MainRunOrder.Count; i++)
            {
                ChapterConfig cfg = ChapterCatalog.GetChapter(ChapterCatalog.MainRunOrder[i]);
                if (cfg != null) list.Add(cfg);
            }
            return list;
        }

        private static bool ValidateOne(string fileName, string json,
            HashSet<string> seenLineIds, HashSet<string> seenUiKeys,
            HashSet<string> seenChapterIds, HashSet<string> legalActors,
            StringBuilder report)
        {
            if (fileName.Equals("TXT_UI.json", StringComparison.OrdinalIgnoreCase))
            {
                return ValidateUiFile(fileName, json, seenUiKeys, report);
            }

            Duzhe.Dialogue.ChapterDialogue data;
            try
            {
                data = JsonUtility.FromJson<Duzhe.Dialogue.ChapterDialogue>(json);
            }
            catch (Exception e)
            {
                report.AppendLine("FAIL " + fileName + ": JSON parse error: " + e.Message);
                return false;
            }
            if (data == null)
            {
                report.AppendLine("FAIL " + fileName + ": root object could not be parsed.");
                return false;
            }

            bool ok = true;
            if (data.schemaVersion != "1.0")
            {
                report.AppendLine("FAIL " + fileName + ": schemaVersion must be \"1.0\", got \"" + data.schemaVersion + "\".");
                ok = false;
            }
            if (string.IsNullOrEmpty(data.chapterId))
            {
                report.AppendLine("FAIL " + fileName + ": missing root field 'chapterId'.");
                ok = false;
            }
            else if (!seenChapterIds.Add(data.chapterId))
            {
                report.AppendLine("FAIL " + fileName + ": duplicate chapterId " + data.chapterId + " across TXT files.");
                ok = false;
            }
            if (string.IsNullOrEmpty(data.displayName))
            {
                report.AppendLine("FAIL " + fileName + ": missing root field 'displayName'.");
                ok = false;
            }

            ChapterConfig cfg = string.IsNullOrEmpty(data.chapterId) ? null : ChapterCatalog.GetChapter(data.chapterId);
            if (cfg == null)
            {
                report.AppendLine("FAIL " + fileName + ": chapterId " + data.chapterId +
                    " has no ChapterConfig registered in ChapterCatalog (stage checks skipped).");
                ok = false;
            }

            var seenSegmentIds = new HashSet<string>(StringComparer.Ordinal);
            if (data.segments != null)
            {
                foreach (Duzhe.Dialogue.DialogueSegment seg in data.segments)
                {
                    if (string.IsNullOrEmpty(seg.segmentId))
                    {
                        report.AppendLine("FAIL " + fileName + ": segment with empty segmentId.");
                        ok = false;
                        continue;
                    }
                    if (!seenSegmentIds.Add(seg.segmentId))
                    {
                        report.AppendLine("FAIL " + fileName + ": duplicate segmentId " + seg.segmentId + " in same file.");
                        ok = false;
                        continue;
                    }
                    if (string.IsNullOrEmpty(seg.stageId))
                    {
                        report.AppendLine("FAIL " + fileName + " segment " + seg.segmentId + ": empty stageId.");
                        ok = false;
                    }
                    else if (cfg != null && cfg.GetStage(seg.stageId) == null)
                    {
                        report.AppendLine("FAIL " + fileName + " segment " + seg.segmentId +
                            ": stageId " + seg.stageId + " is not defined in ChapterConfig for " + data.chapterId + ".");
                        ok = false;
                    }
                    if (seg.lines == null || seg.lines.Count == 0)
                    {
                        report.AppendLine("FAIL " + fileName + " segment " + seg.segmentId + ": lines array is empty.");
                        ok = false;
                        continue;
                    }
                    foreach (Duzhe.Dialogue.DialogueLine line in seg.lines)
                    {
                        string where = fileName + " segment " + seg.segmentId + " line " + (line == null ? "?" : line.lineId);
                        if (string.IsNullOrEmpty(line.lineId))
                        {
                            report.AppendLine("FAIL " + where + ": missing field 'lineId'.");
                            ok = false;
                        }
                        else if (!seenLineIds.Add(line.lineId))
                        {
                            report.AppendLine("FAIL " + where + ": lineId " + line.lineId + " is not globally unique.");
                            ok = false;
                        }
                        if (string.IsNullOrEmpty(line.chapterId) || line.chapterId != data.chapterId)
                        {
                            report.AppendLine("FAIL " + where + ": chapterId must be \"" + data.chapterId + "\", got \"" + line.chapterId + "\".");
                            ok = false;
                        }
                        if (string.IsNullOrEmpty(line.stageId))
                        {
                            report.AppendLine("FAIL " + where + ": missing field 'stageId'.");
                            ok = false;
                        }
                        if (string.IsNullOrEmpty(line.speakerId))
                        {
                            report.AppendLine("FAIL " + where + ": missing field 'speakerId'.");
                            ok = false;
                        }
                        else if (legalActors.Count > 0 && !legalActors.Contains(line.speakerId))
                        {
                            report.AppendLine("FAIL " + where + ": speakerId " + line.speakerId +
                                " is not a legal actor id (no Narrator; ids come from the spec actor list).");
                            ok = false;
                        }
                        if (string.IsNullOrEmpty(line.text))
                        {
                            report.AppendLine("FAIL " + where + ": missing field 'text'.");
                            ok = false;
                        }
                    }
                }
            }

            if (data.objectives != null)
            {
                foreach (Duzhe.Dialogue.ObjectiveEntry o in data.objectives)
                {
                    if (string.IsNullOrEmpty(o.stageId) || string.IsNullOrEmpty(o.text))
                    {
                        report.AppendLine("FAIL " + fileName + ": objective entry with empty stageId/text (stageId=" + o.stageId + ").");
                        ok = false;
                    }
                    else if (cfg != null && cfg.GetStage(o.stageId) == null)
                    {
                        report.AppendLine("FAIL " + fileName + ": objective for unknown stageId " + o.stageId + ".");
                        ok = false;
                    }
                }
            }

            if (data.items != null)
            {
                foreach (Duzhe.Dialogue.ItemEntry it in data.items)
                {
                    if (string.IsNullOrEmpty(it.itemId) || string.IsNullOrEmpty(it.displayName))
                    {
                        report.AppendLine("FAIL " + fileName + ": item entry with empty itemId/displayName (itemId=" + it.itemId + ").");
                        ok = false;
                    }
                }
            }

            if (ok) report.AppendLine("OK   " + fileName + " (" + data.chapterId + ")");
            return ok;
        }

        private static bool ValidateUiFile(string fileName, string json, HashSet<string> seenUiKeys, StringBuilder report)
        {
            Duzhe.Dialogue.UiStringsFile data;
            try
            {
                data = JsonUtility.FromJson<Duzhe.Dialogue.UiStringsFile>(json);
            }
            catch (Exception e)
            {
                report.AppendLine("FAIL " + fileName + ": JSON parse error: " + e.Message);
                return false;
            }
            if (data == null)
            {
                report.AppendLine("FAIL " + fileName + ": root object could not be parsed.");
                return false;
            }
            bool ok = true;
            if (data.schemaVersion != "1.0")
            {
                report.AppendLine("FAIL " + fileName + ": schemaVersion must be \"1.0\", got \"" + data.schemaVersion + "\".");
                ok = false;
            }
            if (data.strings == null || data.strings.Count == 0)
            {
                report.AppendLine("FAIL " + fileName + ": strings array is empty.");
                ok = false;
            }
            else
            {
                foreach (Duzhe.Dialogue.UiStringEntry e in data.strings)
                {
                    if (string.IsNullOrEmpty(e.key))
                    {
                        report.AppendLine("FAIL " + fileName + ": strings entry with empty key.");
                        ok = false;
                    }
                    else if (!seenUiKeys.Add(e.key))
                    {
                        report.AppendLine("FAIL " + fileName + ": duplicate ui key " + e.key + ".");
                        ok = false;
                    }
                }
            }
            if (ok) report.AppendLine("OK   " + fileName + " (" + data.strings.Count + " keys)");
            return ok;
        }
    }
}
