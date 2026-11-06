using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Duzhe.EditorTools
{
    /// Batch-runnable smoke test for the parts of C01 that can be verified
    /// without play mode: save-file round trip (write -> reload -> idempotent
    /// completion -> backup behaviour) and dialogue library parsing/lookup.
    /// Gameplay feel (movement, prompts, dialogue pacing) is a manual play-
    /// acceptance item and is explicitly NOT claimed here.
    public static class DevSmokeTest
    {
        [MenuItem("渡者/开发冒烟测试(存档与对白)")]
        public static void RunInteractive()
        {
            var report = new StringBuilder();
            bool ok = Run(report);
            Debug.Log(report.ToString());
            File.WriteAllText("Logs/duzhe_smoke.txt", report.ToString(), Encoding.UTF8);
            if (ok) Debug.Log("DZ_SMOKE=PASS");
            else Debug.LogError("DZ_SMOKE=FAIL");
        }

        public static void RunFromCommandLine()
        {
            var report = new StringBuilder();
            bool ok = Run(report);
            Debug.Log(report.ToString());
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/duzhe_smoke.txt", report.ToString(), Encoding.UTF8);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Run(StringBuilder report)
        {
            // The save round-trip test corrupts and clears files by design.
            // Run it in a throwaway directory so the player's real
            // duzhe_save.json / .bak is never read or written.
            string previous = Core.SaveService.TestSaveDirectoryOverride;
            string dir = Path.Combine(Path.GetTempPath(), "DuzheSmoke", Guid.NewGuid().ToString("N"));
            bool ok;
            try
            {
                Directory.CreateDirectory(dir);
                Core.SaveService.TestSaveDirectoryOverride = dir;
                report.AppendLine("Save isolation: " + dir);
                ok = RunIsolatedBody(report);
            }
            finally
            {
                Core.SaveService.TestSaveDirectoryOverride = previous;
                try
                {
                    if (Directory.Exists(dir)) Directory.Delete(dir, true);
                }
                catch (Exception e)
                {
                    report.AppendLine("WARN: could not delete isolated save dir: " + e.Message);
                }
            }
            return ok;
        }

        private static bool RunIsolatedBody(StringBuilder report)
        {
            report.AppendLine("== Duzhe dev smoke test ==");
            report.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            bool ok = true;
            try
            {
                ok &= TestSaveRoundTrip(report);
                ok &= TestDialogueLibrary(report);
            }
            catch (Exception e)
            {
                report.AppendLine("FAIL exception: " + e.Message + "\n" + e.StackTrace);
                ok = false;
            }
            report.AppendLine(ok ? "DZ_SMOKE=PASS" : "DZ_SMOKE=FAIL");
            return ok;
        }

        private static bool TestSaveRoundTrip(StringBuilder report)
        {
            bool ok = true;

            // Isolated host object so Awake/Load runs like at runtime.
            GameObject host = new GameObject("DZ_SmokeSaveHost");
            Core.SaveService save = host.AddComponent<Core.SaveService>();
            try
            {
                save.ClearAll();

                save.MarkChapterCompleted("CH01_DEV");
                save.MarkChapterCompleted("CH01_DEV"); // idempotent second call
                save.UnlockPresentation("PRES_TEST");
                save.SetVolumes(0.8f, 0.6f, 0.9f);
                save.SaveVolumes();

                // Reload through a second instance (fresh load path).
                Core.SaveService.DuzheSaveData d = save.Data;
                if (d == null || d.completedChapterIds == null)
                {
                    report.AppendLine("FAIL save: data container missing.");
                    return false;
                }
                if (d.completedChapterIds.Count != 1 || d.completedChapterIds[0] != "CH01_DEV")
                {
                    report.AppendLine("FAIL save: completion idempotency broken, entries=" + d.completedChapterIds.Count);
                    ok = false;
                }
                if (Math.Abs(d.masterVolume - 0.8f) > 0.001f || Math.Abs(d.musicVolume - 0.6f) > 0.001f)
                {
                    report.AppendLine("FAIL save: volumes not persisted.");
                    ok = false;
                }
                if (!save.HasUnlockedPresentation("PRES_TEST"))
                {
                    report.AppendLine("FAIL save: presentation unlock lost.");
                    ok = false;
                }

                // Fresh load must read the same file back.
                Core.SaveService second = host.AddComponent<Core.SaveService>();
                second.Load();
                if (!second.HasCompletedChapter("CH01_DEV"))
                {
                    report.AppendLine("FAIL save: reload lost CH01_DEV.");
                    ok = false;
                }

                // Corrupt main file must fall back to .bak with a warning.
                string main = Path.Combine(Core.SaveService.SaveDirectoryPath, "duzhe_save.json");
                File.WriteAllText(main, "{ this is not json");
                Core.SaveService third = host.AddComponent<Core.SaveService>();
                third.Load();
                if (!third.HasCompletedChapter("CH01_DEV"))
                {
                    report.AppendLine("FAIL save: corrupt-file backup recovery lost progress.");
                    ok = false;
                }
                else
                {
                    report.AppendLine("OK   save: corrupt main file recovered from backup.");
                }

                // Clean up so the manual play acceptance starts fresh.
                save.ClearAll();
                report.AppendLine("OK   save: round trip + idempotency + backup recovery (cleared after test)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
            return ok;
        }

        private static bool TestDialogueLibrary(StringBuilder report)
        {
            bool ok = true;
            Dialogue.DialogueLibrary.Load();
            Dialogue.DialogueLibrary lib = Dialogue.DialogueLibrary.Instance;
            if (lib == null)
            {
                report.AppendLine("FAIL dialogue: library failed to load.");
                return false;
            }

            Dialogue.DialogueSegment seg;
            if (!lib.TryGetSegment("CH01_DEV", "ST01_INTRO", out seg))
            {
                report.AppendLine("FAIL dialogue: segment ST01_INTRO not found.");
                ok = false;
            }
            else
            {
                if (seg.stageId != "ST01_INTRO")
                {
                    report.AppendLine("FAIL dialogue: ST01_INTRO stageId mismatch: " + seg.stageId);
                    ok = false;
                }
                if (seg.lines.Count != 2)
                {
                    report.AppendLine("FAIL dialogue: ST01_INTRO expected 2 lines, got " + seg.lines.Count);
                    ok = false;
                }
                if (seg.lines.Count > 0 && seg.lines[0].speakerId != "DEV_GRANDPA")
                {
                    report.AppendLine("FAIL dialogue: ST01_INTRO first line speakerId " + seg.lines[0].speakerId);
                    ok = false;
                }
                if (seg.lines.Count > 0 && string.IsNullOrEmpty(seg.lines[0].text))
                {
                    report.AppendLine("FAIL dialogue: ST01_INTRO first line empty text.");
                    ok = false;
                }
            }

            Dialogue.DialogueSegment thanks;
            if (!lib.TryGetSegment("CH01_DEV", "ST03_THANKS", out thanks))
            {
                report.AppendLine("FAIL dialogue: segment ST03_THANKS not found.");
                ok = false;
            }

            if (lib.GetUiString("game.title", "") != "渡者")
            {
                report.AppendLine("FAIL dialogue: TXT_UI game.title wrong: '" + lib.GetUiString("game.title", "") + "'");
                ok = false;
            }
            if (lib.GetObjectiveText("CH01_DEV", "ST02_FIND", "") == "")
            {
                report.AppendLine("FAIL dialogue: objective for ST02_FIND missing.");
                ok = false;
            }
            if (lib.GetItemDisplayName("DEV_GLASSES", "") == "")
            {
                report.AppendLine("FAIL dialogue: item DEV_GLASSES display name missing.");
                ok = false;
            }

            // Chapter catalogue lookup.
            Chapters.ChapterConfig devCfg = Chapters.ChapterCatalog.GetChapter("CH01_DEV");
            if (devCfg == null)
            {
                report.AppendLine("FAIL catalogue: CH01_DEV missing.");
                ok = false;
            }
            else
            {
                if (devCfg.stages.Length != 6)
                {
                    report.AppendLine("FAIL catalogue: CH01_DEV expected 6 stages (C02 extended), got " + devCfg.stages.Length);
                    ok = false;
                }
                if (Chapters.ChapterCatalog.GetActorDisplayName("DEV_GRANDPA", "") == "")
                {
                    report.AppendLine("FAIL catalogue: actor DEV_GRANDPA display name missing.");
                    ok = false;
                }
            }

            ok &= TestChapter01Structure(lib, report);
            ok &= TestChapter02Structure(lib, report);
            ok &= TestChapter03Structure(lib, report);
            ok &= TestChapter04Structure(lib, report);
            ok &= TestChapter05Structure(lib, report);
            ok &= TestPresentationMap(lib, report);
            ok &= TestUiStrings(lib, report);
            ok &= TestFormalArt(report);
            ok &= TestAudioResources(report);
            ok &= TestNpcWalkSheets(report);

            if (ok) report.AppendLine("OK   dialogue: library parse, segment lookup, UI strings, objectives, items");
            return ok;
        }

        /// C05: the formal CH02 stage table (11 stages, chain + dialogue +
        /// presentation wiring + river rules + menu selection after CH01).
        private static bool TestChapter02Structure(Dialogue.DialogueLibrary lib, StringBuilder report)
        {
            bool ok = true;
            Chapters.ChapterConfig cfg = Chapters.ChapterCatalog.GetChapter("CH02");
            if (cfg == null)
            {
                report.AppendLine("FAIL CH02: formal chapter config missing.");
                return false;
            }

            string[] expected =
            {
                "CH02_Opening", "CH02_FindNecklace", "CH02_ReturnNecklace", "CH02_RitualTalk",
                "CH02_Ritual", "CH02_Board", "CH02_River", "CH02_ReunionTalk",
                "CH02_Heart", "CH02_Memory", "CH02_Farewell", "CH02_Complete"
            };
            if (cfg.stages.Length != expected.Length)
            {
                report.AppendLine("FAIL CH02: expected " + expected.Length + " stages, got " + cfg.stages.Length);
                ok = false;
            }
            var ids = new HashSet<string>();
            foreach (Chapters.StageDefinition s in cfg.stages) ids.Add(s.stageId);
            foreach (string id in expected)
            {
                if (!ids.Contains(id))
                {
                    report.AppendLine("FAIL CH02: stage missing: " + id);
                    ok = false;
                }
            }
            // Chain closure: every stage's nextStageId exists (or is empty).
            foreach (Chapters.StageDefinition s in cfg.stages)
            {
                if (string.IsNullOrEmpty(s.nextStageId)) continue;
                if (!ids.Contains(s.nextStageId))
                {
                    report.AppendLine("FAIL CH02: stage " + s.stageId + " points at unknown next " + s.nextStageId);
                    ok = false;
                }
            }

            // Necklace item + return source.
            Chapters.StageDefinition find = cfg.GetStage("CH02_FindNecklace");
            if (find == null || find.completionEvent != Core.GameEventType.ItemCollected ||
                find.expectedSourceId != "IT03_Necklace")
            {
                report.AppendLine("FAIL CH02: FindNecklace must complete on ItemCollected(IT03_Necklace).");
                ok = false;
            }
            Chapters.StageDefinition ret = cfg.GetStage("CH02_ReturnNecklace");
            if (ret == null || ret.requiredItemIds == null || ret.requiredItemIds.Length != 1 ||
                ret.requiredItemIds[0] != "IT03_Necklace" || ret.expectedSourceId != "CH02_LoverSpirit")
            {
                report.AppendLine("FAIL CH02: ReturnNecklace must require IT03_Necklace and expect source CH02_LoverSpirit.");
                ok = false;
            }

            // Auto-start dialogue segments must exist in TXT_CH02.json.
            // CH02_RitualTalk is auto-started by the stage; CH02_ReunionTalk is
            // auto-talked by the boy NPC on approach (no stage autoStartSegmentId).
            Chapters.StageDefinition ritualSt = cfg.GetStage("CH02_RitualTalk");
            Dialogue.DialogueSegment ritualSeg;
            if (ritualSt == null || ritualSt.autoStartSegmentId != "CH02_RitualTalk" ||
                !lib.TryGetSegment("CH02", "CH02_RitualTalk", out ritualSeg))
            {
                report.AppendLine("FAIL CH02: CH02_RitualTalk auto-start segment missing.");
                ok = false;
            }
            Dialogue.DialogueSegment reunionSeg;
            if (!lib.TryGetSegment("CH02", "CH02_ReunionTalk", out reunionSeg))
            {
                report.AppendLine("FAIL CH02: CH02_ReunionTalk segment missing in TXT_CH02.json (auto-talked by the boy NPC on approach).");
                ok = false;
            }
            Dialogue.DialogueSegment openingSeg;
            if (!lib.TryGetSegment("CH02", "CH02_Opening", out openingSeg) || openingSeg.stageId != "CH02_Opening")
            {
                report.AppendLine("FAIL CH02: opening segment missing/stage mismatch.");
                ok = false;
            }

            // Heart minigame + farewell presentation.
            Chapters.StageDefinition heart = cfg.GetStage("CH02_Heart");
            if (heart == null || heart.completionEvent != Core.GameEventType.MinigameCompleted ||
                heart.expectedSourceId != "MG04_Heart")
            {
                report.AppendLine("FAIL CH02: Heart stage must complete on MinigameCompleted(MG04_Heart).");
                ok = false;
            }
            Chapters.StageDefinition farewell = cfg.GetStage("CH02_Farewell");
            if (farewell == null || farewell.autoStartPresentationId != "PR_CH02_Farewell" ||
                farewell.completionEvent != Core.GameEventType.PresentationFinished)
            {
                report.AppendLine("FAIL CH02: Farewell stage must auto-start PR_CH02_Farewell and complete on PresentationFinished.");
                ok = false;
            }
            Chapters.StageDefinition complete = cfg.GetStage("CH02_Complete");
            if (complete == null || complete.autoAdvanceSeconds <= 0f)
            {
                report.AppendLine("FAIL CH02: Complete stage must auto-advance after the closing fade.");
                ok = false;
            }

            // River rules: one passenger, two banned lanes per wave.
            if (cfg.riverSettings.passengerCount != 1 || cfg.riverSettings.maxBannedLanesPerWave != 2)
            {
                report.AppendLine("FAIL CH02: riverSettings must be passengerCount=1 / maxBannedLanesPerWave=2.");
                ok = false;
            }

            // After CH01 is completed, the menu must select CH02.
            Core.SaveService saveProbe = new GameObject("DZ_SmokeSaveProbe").AddComponent<Core.SaveService>();
            try
            {
                saveProbe.MarkChapterCompleted("CH01");
                string next = Chapters.ChapterCatalog.GetFirstUnfinishedChapterId(saveProbe);
                if (next != "CH02")
                {
                    report.AppendLine("FAIL CH02: after CH01 is done the menu should select CH02 (got " + next + ").");
                    ok = false;
                }
                saveProbe.ClearAll();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(saveProbe.gameObject);
            }

            if (ok) report.AppendLine("OK   CH02: 11-stage table, necklace/heart/presentation wiring, river rules, menu selection");
            return ok;
        }

        /// C06: the formal CH03 stage table (12 stages), its objective/dialogue
        /// references and the delivered CFG_Notes score.
        private static bool TestChapter03Structure(Dialogue.DialogueLibrary lib, StringBuilder report)
        {
            bool ok = true;
            Chapters.ChapterConfig cfg = Chapters.ChapterCatalog.GetChapter("CH03");
            if (cfg == null)
            {
                report.AppendLine("FAIL CH03: formal chapter config missing.");
                return false;
            }

            string[] expected =
            {
                "CH03_Opening", "CH03_FindDrumsticks", "CH03_ReturnDrumsticks", "CH03_RitualTalk",
                "CH03_Ritual", "CH03_Board", "CH03_River", "CH03_ReunionTalk",
                "CH03_Perform", "CH03_PhotoTalk", "CH03_Photo", "CH03_Memory", "CH03_Farewell", "CH03_Complete"
            };
            if (cfg.stages.Length != expected.Length)
            {
                report.AppendLine("FAIL CH03: expected " + expected.Length + " stages, got " + cfg.stages.Length);
                ok = false;
            }
            var ids = new HashSet<string>();
            foreach (Chapters.StageDefinition s in cfg.stages) ids.Add(s.stageId);
            foreach (string id in expected)
            {
                if (!ids.Contains(id))
                {
                    report.AppendLine("FAIL CH03: stage missing: " + id);
                    ok = false;
                }
            }
            foreach (Chapters.StageDefinition s in cfg.stages)
            {
                if (string.IsNullOrEmpty(s.nextStageId)) continue;
                if (!ids.Contains(s.nextStageId))
                {
                    report.AppendLine("FAIL CH03: stage " + s.stageId + " points at unknown next " + s.nextStageId);
                    ok = false;
                }
            }

            // Stage table wiring: quest item, hand-over, ritual, boat, river.
            Chapters.StageDefinition find = cfg.GetStage("CH03_FindDrumsticks");
            if (find == null || find.completionEvent != Core.GameEventType.ItemCollected ||
                find.expectedSourceId != "IT04_Drumsticks")
            {
                report.AppendLine("FAIL CH03: FindDrumsticks must complete on ItemCollected(IT04_Drumsticks).");
                ok = false;
            }
            Chapters.StageDefinition give = cfg.GetStage("CH03_ReturnDrumsticks");
            if (give == null || give.requiredItemIds == null || give.requiredItemIds.Length != 1 ||
                give.requiredItemIds[0] != "IT04_Drumsticks" || give.expectedSourceId != "CH03_DrummerSpirit")
            {
                report.AppendLine("FAIL CH03: ReturnDrumsticks must require IT04_Drumsticks and expect source CH03_DrummerSpirit.");
                ok = false;
            }
            if (cfg.GetStage("CH03_Ritual")?.expectedSourceId != "CH03_RITUAL" ||
                cfg.GetStage("CH03_Board")?.expectedSourceId != "CH03_BOAT" ||
                cfg.GetStage("CH03_River")?.expectedSourceId != "River")
            {
                report.AppendLine("FAIL CH03: ritual/boat/river expectedSourceId wiring wrong.");
                ok = false;
            }
            Chapters.StageDefinition riverStage = cfg.GetStage("CH03_River");
            if (riverStage == null || riverStage.sceneId != Core.DuzheIds.SceneRiver ||
                riverStage.inputMode != Core.InputMode.River)
            {
                report.AppendLine("FAIL CH03: River stage must run in the River scene with River input.");
                ok = false;
            }
            if (cfg.GetStage("CH03_Perform")?.expectedSourceId != "MG05_Notes" ||
                cfg.GetStage("CH03_Photo")?.expectedSourceId != "MG06_Photo")
            {
                report.AppendLine("FAIL CH03: perform/photo minigame wiring wrong.");
                ok = false;
            }

            // Auto-start dialogue/presentation.
            Chapters.StageDefinition ritualTalk = cfg.GetStage("CH03_RitualTalk");
            Dialogue.DialogueSegment ritualSeg;
            if (ritualTalk == null || ritualTalk.autoStartSegmentId != "CH03_RitualTalk" ||
                !lib.TryGetSegment("CH03", "CH03_RitualTalk", out ritualSeg))
            {
                report.AppendLine("FAIL CH03: CH03_RitualTalk auto-start segment missing.");
                ok = false;
            }
            foreach (string segId in new[] { "CH03_Opening", "CH03_ReunionTalk", "CH03_Farewell" })
            {
                Dialogue.DialogueSegment seg;
                if (!lib.TryGetSegment("CH03", segId, out seg))
                {
                    report.AppendLine("FAIL CH03: segment missing in TXT_CH03.json: " + segId);
                    ok = false;
                }
            }
            Chapters.StageDefinition farewell = cfg.GetStage("CH03_Farewell");
            if (farewell == null || farewell.autoStartPresentationId != "PR_CH03_Farewell" ||
                farewell.completionEvent != Core.GameEventType.PresentationFinished)
            {
                report.AppendLine("FAIL CH03: Farewell must auto-start PR_CH03_Farewell and complete on PresentationFinished.");
                ok = false;
            }
            Chapters.StageDefinition complete = cfg.GetStage("CH03_Complete");
            if (complete == null || complete.autoAdvanceSeconds <= 0f || !string.IsNullOrEmpty(complete.nextStageId))
            {
                report.AppendLine("FAIL CH03: Complete must auto-advance and end the chapter.");
                ok = false;
            }

            // Objectives come from TXT_CH03 only: an empty one is a failure,
            // never a silent fallback.
            foreach (Chapters.StageDefinition s in cfg.stages)
            {
                string objective = lib.GetObjectiveText("CH03", s.stageId, "");
                if (string.IsNullOrEmpty(objective))
                {
                    report.AppendLine("FAIL CH03: objective missing in TXT_CH03.json for stage " + s.stageId);
                    ok = false;
                }
            }
            if (lib.GetItemDisplayName("IT04_Drumsticks", "") == "")
            {
                report.AppendLine("FAIL CH03: item IT04_Drumsticks display name missing.");
                ok = false;
            }

            // Presentation map entry.
            Dialogue.PresentationEntry pr;
            if (!lib.TryGetPresentation("PR_CH03_Farewell", out pr) ||
                pr.orderedSegmentIds == null || pr.orderedSegmentIds.Length != 1 ||
                pr.orderedSegmentIds[0] != "CH03_Farewell")
            {
                report.AppendLine("FAIL CH03: PR_CH03_Farewell orderedSegmentIds must be [CH03_Farewell].");
                ok = false;
            }

            // River rules: one passenger, two banned lanes per wave.
            if (cfg.riverSettings.passengerCount != 1 || cfg.riverSettings.maxBannedLanesPerWave != 2)
            {
                report.AppendLine("FAIL CH03: riverSettings must be passengerCount=1 / maxBannedLanesPerWave=2.");
                ok = false;
            }
            if (!cfg.enablePerformance)
            {
                report.AppendLine("FAIL CH03: enablePerformance must stay true (the seven-note performance is not optional).");
                ok = false;
            }

            // Delivered CFG_Notes.json must be valid, not a placeholder.
            string notesPath = "Assets/_Project/Resources/Data/Minigames/CFG_Notes.json";
            if (!File.Exists(notesPath))
            {
                report.AppendLine("FAIL CH03: " + notesPath + " missing.");
                ok = false;
            }
            else
            {
                string reason;
                string json = File.ReadAllText(notesPath, Encoding.UTF8);
                if (!Minigames.NoteSequenceMinigame.ValidateNotesConfigText(json, out reason))
                {
                    report.AppendLine("FAIL CH03: CFG_Notes.json rejected: " + reason);
                    ok = false;
                }
                else if (!json.Contains("78") || !json.Contains("MEL01_BandPhrase"))
                {
                    report.AppendLine("FAIL CH03: CFG_Notes.json does not look like the delivered 78 BPM score.");
                    ok = false;
                }
            }

            // Menu selection after CH01+CH02.
            Core.SaveService saveProbe = new GameObject("DZ_SmokeSaveProbe03").AddComponent<Core.SaveService>();
            try
            {
                saveProbe.MarkChapterCompleted("CH01");
                saveProbe.MarkChapterCompleted("CH02");
                string next = Chapters.ChapterCatalog.GetFirstUnfinishedChapterId(saveProbe);
                if (next != "CH03")
                {
                    report.AppendLine("FAIL CH03: after CH01+CH02 the menu should select CH03 (got " + next + ").");
                    ok = false;
                }
                saveProbe.ClearAll();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(saveProbe.gameObject);
            }

            if (ok) report.AppendLine("OK   CH03: 12-stage table, objectives/segments, presentation, river rules, CFG_Notes, menu selection");
            return ok;
        }

        /// C07: the formal CH04 stage table (12 stages), the lesson→writing
        /// gate and the split presentation list.
        private static bool TestChapter04Structure(Dialogue.DialogueLibrary lib, StringBuilder report)
        {
            bool ok = true;
            Chapters.ChapterConfig cfg = Chapters.ChapterCatalog.GetChapter("CH04");
            if (cfg == null)
            {
                report.AppendLine("FAIL CH04: formal chapter config missing.");
                return false;
            }

            string[] expected =
            {
                "CH04_Opening", "CH04_FindLessonPlan", "CH04_ReturnLessonPlan", "CH04_RitualTalk",
                "CH04_Ritual", "CH04_Board", "CH04_River", "CH04_ReunionTalk",
                "CH04_Lesson", "CH04_WriteGoodbye", "CH04_Memory", "CH04_Farewell", "CH04_Complete"
            };
            if (cfg.stages.Length != expected.Length)
            {
                report.AppendLine("FAIL CH04: expected " + expected.Length + " stages, got " + cfg.stages.Length);
                ok = false;
            }
            var ids = new HashSet<string>();
            foreach (Chapters.StageDefinition s in cfg.stages) ids.Add(s.stageId);
            foreach (string id in expected)
            {
                if (!ids.Contains(id))
                {
                    report.AppendLine("FAIL CH04: stage missing: " + id);
                    ok = false;
                }
            }
            foreach (Chapters.StageDefinition s in cfg.stages)
            {
                if (string.IsNullOrEmpty(s.nextStageId)) continue;
                if (!ids.Contains(s.nextStageId))
                {
                    report.AppendLine("FAIL CH04: stage " + s.stageId + " points at unknown next " + s.nextStageId);
                    ok = false;
                }
            }

            // Quest item / hand-over / ritual / boat / river wiring.
            Chapters.StageDefinition find = cfg.GetStage("CH04_FindLessonPlan");
            if (find == null || find.completionEvent != Core.GameEventType.ItemCollected ||
                find.expectedSourceId != "IT05_LessonPlan")
            {
                report.AppendLine("FAIL CH04: FindLessonPlan must complete on ItemCollected(IT05_LessonPlan).");
                ok = false;
            }
            Chapters.StageDefinition give = cfg.GetStage("CH04_ReturnLessonPlan");
            if (give == null || give.requiredItemIds == null || give.requiredItemIds.Length != 1 ||
                give.requiredItemIds[0] != "IT05_LessonPlan" || give.expectedSourceId != "CH04_TeacherSpirit")
            {
                report.AppendLine("FAIL CH04: ReturnLessonPlan must require IT05_LessonPlan and expect source CH04_TeacherSpirit.");
                ok = false;
            }
            if (cfg.GetStage("CH04_Ritual")?.expectedSourceId != "CH04_RITUAL" ||
                cfg.GetStage("CH04_Board")?.expectedSourceId != "CH04_BOAT" ||
                cfg.GetStage("CH04_River")?.expectedSourceId != "River")
            {
                report.AppendLine("FAIL CH04: ritual/boat/river expectedSourceId wiring wrong.");
                ok = false;
            }
            Chapters.StageDefinition riverStage = cfg.GetStage("CH04_River");
            if (riverStage == null || riverStage.sceneId != Core.DuzheIds.SceneRiver ||
                riverStage.inputMode != Core.InputMode.River)
            {
                report.AppendLine("FAIL CH04: River stage must run in the River scene with River input.");
                ok = false;
            }

            // Auto-started segments: reunion, lesson, ritual talk.
            foreach (string segId in new[] { "CH04_ReunionTalk", "CH04_Lesson", "CH04_RitualTalk" })
            {
                Chapters.StageDefinition st = cfg.GetStage(segId);
                Dialogue.DialogueSegment seg;
                if (st == null || st.autoStartSegmentId != segId || !lib.TryGetSegment("CH04", segId, out seg))
                {
                    report.AppendLine("FAIL CH04: " + segId + " must auto-start its own segment and the segment must exist.");
                    ok = false;
                }
            }
            Dialogue.DialogueSegment lesson;
            if (lib.TryGetSegment("CH04", "CH04_Lesson", out lesson))
            {
                bool hasFarewell = false;
                if (lesson.lines != null)
                    foreach (Dialogue.DialogueLine line in lesson.lines)
                        if (line.text == "这就是，最后一课了。") hasFarewell = true;
                if (!hasFarewell || lesson.lines.Count < 21 || lesson.lines[lesson.lines.Count - 1].text != "下课。")
                {
                    report.AppendLine("FAIL CH04: expanded lesson must retain the final-lesson line and end with dismissal.");
                    ok = false;
                }
            }

            // Writing stage is a minigame; farewell is presentation-driven.
            Chapters.StageDefinition write = cfg.GetStage("CH04_WriteGoodbye");
            if (write == null || write.completionEvent != Core.GameEventType.MinigameCompleted ||
                write.expectedSourceId != "MG07_Goodbye")
            {
                report.AppendLine("FAIL CH04: WriteGoodbye must complete on MinigameCompleted(MG07_Goodbye).");
                ok = false;
            }
            Chapters.StageDefinition farewell = cfg.GetStage("CH04_Farewell");
            if (farewell == null || farewell.autoStartPresentationId != "PR_CH04_Farewell" ||
                farewell.completionEvent != Core.GameEventType.PresentationFinished)
            {
                report.AppendLine("FAIL CH04: Farewell must auto-start PR_CH04_Farewell and complete on PresentationFinished.");
                ok = false;
            }
            Chapters.StageDefinition complete = cfg.GetStage("CH04_Complete");
            if (complete == null || complete.autoAdvanceSeconds <= 0f || !string.IsNullOrEmpty(complete.nextStageId))
            {
                report.AppendLine("FAIL CH04: Complete must auto-advance and end the chapter.");
                ok = false;
            }

            // Objectives come from TXT_CH04 only.
            foreach (Chapters.StageDefinition s in cfg.stages)
            {
                if (string.IsNullOrEmpty(lib.GetObjectiveText("CH04", s.stageId, "")))
                {
                    report.AppendLine("FAIL CH04: objective missing in TXT_CH04.json for stage " + s.stageId);
                    ok = false;
                }
            }
            if (lib.GetItemDisplayName("IT05_LessonPlan", "") == "")
            {
                report.AppendLine("FAIL CH04: item IT05_LessonPlan display name missing.");
                ok = false;
            }

            // Gallery order keeps the lesson, the main run plays only the
            // farewell (the class already happened in its own stage).
            Dialogue.PresentationEntry pr;
            if (!lib.TryGetPresentation("PR_CH04_Farewell", out pr) ||
                pr.orderedSegmentIds == null || pr.orderedSegmentIds.Length != 5 ||
                pr.orderedSegmentIds[0] != "CH04_Lesson" || pr.orderedSegmentIds[1] != "CH04_Memory01" ||
                pr.orderedSegmentIds[2] != "CH04_Memory02" || pr.orderedSegmentIds[3] != "CH04_Memory03" ||
                pr.orderedSegmentIds[4] != "CH04_Farewell")
            {
                report.AppendLine("FAIL CH04: gallery order must be lesson, three memories, farewell.");
                ok = false;
            }

            if (cfg.riverSettings.passengerCount != 1 || cfg.riverSettings.maxBannedLanesPerWave != 2)
            {
                report.AppendLine("FAIL CH04: riverSettings must be passengerCount=1 / maxBannedLanesPerWave=2.");
                ok = false;
            }

            // The lesson gate starts closed every run.
            if (new Chapters.ChapterRuntimeState().lessonFinished)
            {
                report.AppendLine("FAIL CH04: lessonFinished must default to false.");
                ok = false;
            }

            // Menu selection after CH01+CH02+CH03.
            Core.SaveService saveProbe = new GameObject("DZ_SmokeSaveProbe04").AddComponent<Core.SaveService>();
            try
            {
                saveProbe.MarkChapterCompleted("CH01");
                saveProbe.MarkChapterCompleted("CH02");
                saveProbe.MarkChapterCompleted("CH03");
                string next = Chapters.ChapterCatalog.GetFirstUnfinishedChapterId(saveProbe);
                if (next != "CH04")
                {
                    report.AppendLine("FAIL CH04: after CH01-CH03 the menu should select CH04 (got " + next + ").");
                    ok = false;
                }
                saveProbe.ClearAll();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(saveProbe.gameObject);
            }

            if (ok) report.AppendLine("OK   CH04: 12-stage table, lesson gate default, 21-line lesson, presentation split, river rules, menu selection");
            return ok;
        }

        /// C08: the formal CH05 stage table (6 stages), the two presentations,
        /// the credits gate and the zero-passenger crossing.
        private static bool TestChapter05Structure(Dialogue.DialogueLibrary lib, StringBuilder report)
        {
            bool ok = true;
            Chapters.ChapterConfig cfg = Chapters.ChapterCatalog.GetChapter("CH05");
            if (cfg == null)
            {
                report.AppendLine("FAIL CH05: formal chapter config missing.");
                return false;
            }

            string[] expected =
            {
                "CH05_Recall", "CH05_LetGo", "CH05_Board",
                "CH05_River", "CH05_Reincarnation", "CH05_Complete"
            };
            if (cfg.stages.Length != expected.Length)
            {
                report.AppendLine("FAIL CH05: expected " + expected.Length + " stages, got " + cfg.stages.Length);
                ok = false;
            }
            var ids = new HashSet<string>();
            foreach (Chapters.StageDefinition s in cfg.stages) ids.Add(s.stageId);
            foreach (string id in expected)
            {
                if (!ids.Contains(id))
                {
                    report.AppendLine("FAIL CH05: stage missing: " + id);
                    ok = false;
                }
            }
            foreach (Chapters.StageDefinition s in cfg.stages)
            {
                if (string.IsNullOrEmpty(s.nextStageId)) continue;
                if (!ids.Contains(s.nextStageId))
                {
                    report.AppendLine("FAIL CH05: stage " + s.stageId + " points at unknown next " + s.nextStageId);
                    ok = false;
                }
            }

            // Recall: presentation-driven; LetGo: dialogue-driven.
            Chapters.StageDefinition recall = cfg.GetStage("CH05_Recall");
            if (recall == null || recall.autoStartPresentationId != "PR_CH05_Recall" ||
                recall.completionEvent != Core.GameEventType.PresentationFinished ||
                recall.expectedSourceId != "PR_CH05_Recall")
            {
                report.AppendLine("FAIL CH05: Recall must auto-start PR_CH05_Recall and complete on PresentationFinished.");
                ok = false;
            }
            Chapters.StageDefinition letGo = cfg.GetStage("CH05_LetGo");
            if (letGo == null || letGo.autoStartSegmentId != "CH05_LetGo" ||
                letGo.completionEvent != Core.GameEventType.DialogueFinished ||
                letGo.expectedSourceId != "CH05_LetGo")
            {
                report.AppendLine("FAIL CH05: LetGo must auto-start its segment and complete on DialogueFinished.");
                ok = false;
            }
            if (cfg.GetStage("CH05_Board")?.expectedSourceId != "CH05_BOAT")
            {
                report.AppendLine("FAIL CH05: Board must expect source CH05_BOAT.");
                ok = false;
            }
            Chapters.StageDefinition river = cfg.GetStage("CH05_River");
            if (river == null || river.sceneId != Core.DuzheIds.SceneRiver ||
                river.inputMode != Core.InputMode.River || river.expectedSourceId != "River")
            {
                report.AppendLine("FAIL CH05: River must run in the River scene with River input and source River.");
                ok = false;
            }
            Chapters.StageDefinition reinc = cfg.GetStage("CH05_Reincarnation");
            if (reinc == null || reinc.autoStartPresentationId != "PR_CH05_Reincarnation" ||
                reinc.completionEvent != Core.GameEventType.PresentationFinished ||
                reinc.sceneId != Core.DuzheIds.SceneRiver)
            {
                report.AppendLine("FAIL CH05: Reincarnation must auto-start PR_CH05_Reincarnation in the River scene.");
                ok = false;
            }
            Chapters.StageDefinition complete = cfg.GetStage("CH05_Complete");
            if (complete == null || complete.completionEvent != Core.GameEventType.CreditsConfirmed ||
                complete.expectedSourceId != "CH05_CREDITS" || !string.IsNullOrEmpty(complete.nextStageId))
            {
                report.AppendLine("FAIL CH05: Complete must wait for CreditsConfirmed(CH05_CREDITS) and end the chapter.");
                ok = false;
            }
            if (complete != null && complete.autoAdvanceSeconds > 0f)
            {
                report.AppendLine("FAIL CH05: Complete must NOT auto-advance - the credits need a real confirm.");
                ok = false;
            }

            // Objectives and segments come from TXT_CH05 only.
            foreach (Chapters.StageDefinition s in cfg.stages)
            {
                if (string.IsNullOrEmpty(lib.GetObjectiveText("CH05", s.stageId, "")))
                {
                    report.AppendLine("FAIL CH05: objective missing in TXT_CH05.json for stage " + s.stageId);
                    ok = false;
                }
            }
            int[] expectedLines = { 52, 11, 6, 6, 7, 7, 8 };
            string[] segIds = { "CH05_Recall", "CH05_LetGo", "CH05_Reincarnation", "CH05_RecollectionFamily", "CH05_RecollectionLover", "CH05_RecollectionFriends", "CH05_RecollectionTeacher" };
            for (int i = 0; i < segIds.Length; i++)
            {
                Dialogue.DialogueSegment seg;
                if (!lib.TryGetSegment("CH05", segIds[i], out seg))
                {
                    report.AppendLine("FAIL CH05: segment missing: " + segIds[i]);
                    ok = false;
                }
                else if (seg.lines.Count != expectedLines[i])
                {
                    report.AppendLine("FAIL CH05: " + segIds[i] + " expected " + expectedLines[i] +
                        " lines, got " + seg.lines.Count);
                    ok = false;
                }
            }

            // Both presentations exist with the delivered segment order.
            Dialogue.PresentationEntry pr;
            if (!lib.TryGetPresentation("PR_CH05_Recall", out pr) ||
                pr.orderedSegmentIds == null || pr.orderedSegmentIds.Length != 1 ||
                pr.orderedSegmentIds[0] != "CH05_Recall")
            {
                report.AppendLine("FAIL CH05: PR_CH05_Recall must map to [CH05_Recall].");
                ok = false;
            }
            if (!lib.TryGetPresentation("PR_CH05_Reincarnation", out pr) ||
                pr.orderedSegmentIds == null || string.Join(",", pr.orderedSegmentIds) !=
                "CH05_RecollectionFamily,CH05_RecollectionLover,CH05_RecollectionFriends,CH05_RecollectionTeacher,CH05_Reincarnation")
            {
                report.AppendLine("FAIL CH05: PR_CH05_Reincarnation must play all four recollections before the ending.");
                ok = false;
            }

            // Zero passengers, two banned lanes.
            if (cfg.riverSettings.passengerCount != 0 || cfg.riverSettings.maxBannedLanesPerWave != 2)
            {
                report.AppendLine("FAIL CH05: riverSettings must be passengerCount=0 / maxBannedLanesPerWave=2.");
                ok = false;
            }

            // Runtime gates start closed every run.
            Chapters.ChapterRuntimeState fresh = new Chapters.ChapterRuntimeState();
            if (fresh.memoryRestored || fresh.letGoFinished)
            {
                report.AppendLine("FAIL CH05: memoryRestored/letGoFinished must default to false.");
                ok = false;
            }

            // Menu selection after CH01-CH04.
            Core.SaveService saveProbe = new GameObject("DZ_SmokeSaveProbe05").AddComponent<Core.SaveService>();
            try
            {
                saveProbe.MarkChapterCompleted("CH01");
                saveProbe.MarkChapterCompleted("CH02");
                saveProbe.MarkChapterCompleted("CH03");
                saveProbe.MarkChapterCompleted("CH04");
                string next = Chapters.ChapterCatalog.GetFirstUnfinishedChapterId(saveProbe);
                if (next != "CH05")
                {
                    report.AppendLine("FAIL CH05: after CH01-CH04 the menu should select CH05 (got " + next + ").");
                    ok = false;
                }
                saveProbe.ClearAll();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(saveProbe.gameObject);
            }

            if (ok) report.AppendLine("OK   CH05: 6-stage table, recall/letgo flags, credits gate, 2 presentations, 0 passengers, menu selection");
            return ok;
        }

        /// R12/R14 formal art assertions: the walk sheet is sliced to 12
        /// named frames, all five CGs load at 640x360, and the delivered
        /// PresentationLayers.json matches the R14 selective-fade contract.
        private static bool TestFormalArt(StringBuilder report)
        {
            bool ok = true;

            // Walk sheet: 12 frames with the contract grid names.
            Sprite[] all = Resources.LoadAll<Sprite>("Art/Animations/CHR01_Ferryman");
            if (all == null || all.Length != 12)
            {
                report.AppendLine("FAIL formal art: CHR01 sheet expected 12 sliced sprites, got " +
                    (all == null ? 0 : all.Length));
                ok = false;
            }
            else
            {
                string[] requiredNames =
                {
                    "CHR01_Down_WalkA", "CHR01_Down_Idle", "CHR01_Down_WalkB",
                    "CHR01_Left_Idle", "CHR01_Right_Idle", "CHR01_Up_WalkB"
                };
                HashSet<string> names = new HashSet<string>();
                foreach (Sprite s in all) names.Add(s.name);
                foreach (string n in requiredNames)
                {
                    if (!names.Contains(n))
                    {
                        report.AppendLine("FAIL formal art: sheet frame missing: " + n);
                        ok = false;
                    }
                }
            }

            // Formal R11 stands used by the CH01/CH02 scenes.
            foreach (string id in new[] { "CHR02_Grandfather", "CHR03_Grandmother",
                                          "CHR04_PastSelfFamily", "CHR06_PastSelfRomance" })
            {
                if (Resources.Load<Sprite>("Art/Characters/" + id) == null)
                {
                    report.AppendLine("FAIL formal art: R11 stand missing: " + id);
                    ok = false;
                }
            }

            // CH03 band stands + drumsticks item + the R09 studio background.
            foreach (string id in new[] { "CHR07_DrummerSpirit", "CHR08_PastSelfBand",
                                          "CHR09_BandmateA", "CHR10_BandmateB" })
            {
                if (Resources.Load<Sprite>("Art/Characters/" + id) == null)
                {
                    report.AppendLine("FAIL formal art: CH03 band stand missing: " + id);
                    ok = false;
                }
            }
            Sprite drumsticks = Resources.Load<Sprite>("Art/Props/IT04_Drumsticks");
            if (drumsticks == null)
            {
                report.AppendLine("FAIL formal art: IT04_Drumsticks sprite missing.");
                ok = false;
            }
            else if (Core.PlaceholderArt.QuestIconSprite("IT04_Drumsticks") != drumsticks)
            {
                report.AppendLine("FAIL formal art: IT04_Drumsticks HUD icon does not use the formal drumsticks sprite.");
                ok = false;
            }
            Sprite studioBg = Resources.Load<Sprite>("Art/Backgrounds/BG05_RecordingStudio");
            if (studioBg == null)
            {
                report.AppendLine("FAIL formal art: BG05_RecordingStudio background missing.");
                ok = false;
            }
            else if (Mathf.RoundToInt(studioBg.rect.width) != 1280 || Mathf.RoundToInt(studioBg.rect.height) != 720)
            {
                report.AppendLine("FAIL formal art: BG05_RecordingStudio size " + studioBg.rect.width + "x" + studioBg.rect.height);
                ok = false;
            }

            // C12: the formal Underworld background + ferry/whirlpool + the two
            // CH01 quest items must actually be imported (they used to run on
            // the painted placeholders).
            Sprite underworldBg = Resources.Load<Sprite>("Art/Backgrounds/BG01_Underworld");
            if (underworldBg == null)
            {
                report.AppendLine("FAIL formal art: BG01_Underworld background missing.");
                ok = false;
            }
            else if (Mathf.RoundToInt(underworldBg.rect.width) != 1280 ||
                     Mathf.RoundToInt(underworldBg.rect.height) != 720)
            {
                report.AppendLine("FAIL formal art: BG01_Underworld size " +
                    underworldBg.rect.width + "x" + underworldBg.rect.height);
                ok = false;
            }
            Sprite formalBoat = Resources.Load<Sprite>("Art/Props/PRP01_Boat");
            if (formalBoat == null)
            {
                report.AppendLine("FAIL formal art: PRP01_Boat sprite missing.");
                ok = false;
            }
            else if (Core.FormalArt.LoadBoatSprite() != formalBoat)
            {
                report.AppendLine("FAIL formal art: the river boat does not use PRP01_Boat.");
                ok = false;
            }
            if (Resources.Load<Sprite>("Art/Props/PRP02_Whirlpool") == null)
            {
                report.AppendLine("FAIL formal art: PRP02_Whirlpool sprite missing.");
                ok = false;
            }
            foreach (string id in new[] { "IT01_Glasses", "IT02_Cane" })
            {
                if (Resources.Load<Sprite>("Art/Props/" + id) == null)
                {
                    report.AppendLine("FAIL formal art: CH01 quest item missing: " + id);
                    ok = false;
                }
            }

            // CH02 HUD must use the formal necklace, not the CH01 glasses fallback.
            Sprite necklace = Resources.Load<Sprite>("Art/Props/IT03_Necklace");
            if (necklace == null)
            {
                report.AppendLine("FAIL formal art: IT03_Necklace sprite missing.");
                ok = false;
            }
            else if (Core.PlaceholderArt.QuestIconSprite("IT03_Necklace") != necklace)
            {
                report.AppendLine("FAIL formal art: IT03_Necklace HUD icon does not use the formal necklace sprite.");
                ok = false;
            }

            // R13 Panels v1.0: formal 640x360 MG06/MG07 boards.
            foreach (string id in new[] { "PRP06_PhotoFrame", "PRP07_BlackboardCloseup" })
            {
                Sprite s = Resources.Load<Sprite>("Art/Props/" + id);
                if (s == null)
                {
                    report.AppendLine("FAIL formal art: panel missing: " + id);
                    ok = false;
                }
                else if (Mathf.RoundToInt(s.rect.width) != 640 || Mathf.RoundToInt(s.rect.height) != 360)
                {
                    report.AppendLine("FAIL formal art: " + id + " size " + s.rect.width + "x" + s.rect.height + " (expected 640x360)");
                    ok = false;
                }
            }
            if (Minigames.MinigameCatalog.Get("MG07_Goodbye").panelH != 360f)
            {
                report.AppendLine("FAIL formal art: MG07 panel height must match the 640x360 board.");
                ok = false;
            }

            // R14 CGs: 640x360 each.
            string[] cgs = { "CG01_FamilyFarewell", "CG02_LoversFarewell", "CG03_BandFarewell",
                             "CG04_TeacherFarewell", "CG05_SelfCrossing" };
            foreach (string id in cgs)
            {
                Sprite s = Resources.Load<Sprite>("Art/Illustrations/" + id);
                if (s == null)
                {
                    report.AppendLine("FAIL formal art: illustration missing: " + id);
                    ok = false;
                }
                else if (Mathf.RoundToInt(s.rect.width) != 640 || Mathf.RoundToInt(s.rect.height) != 360)
                {
                    report.AppendLine("FAIL formal art: " + id + " size " + s.rect.width + "x" + s.rect.height + " (expected 640x360)");
                    ok = false;
                }
            }

            // PresentationLayers contract: CG01 fades only the two elders and
            // forbids a whole-CG fade; CG03 keeps the complete photo; CG05 is
            // the only full-screen fade; CG04 renders text by program.
            Presentation.PresentationLayerItem cg1;
            if (!Presentation.PresentationLayerCatalog.TryGet("CG01_FamilyFarewell", out cg1))
            {
                report.AppendLine("FAIL formal art: PresentationLayers.json CG01 entry missing.");
                ok = false;
            }
            else
            {
                if (cg1.fadeOnlyActorIds == null || cg1.fadeOnlyActorIds.Length != 2 ||
                    cg1.fadeOnlyActorIds[0] != "CHR02_Grandfather" || cg1.fadeOnlyActorIds[1] != "CHR03_Grandmother")
                {
                    report.AppendLine("FAIL formal art: CG01 fadeOnlyActorIds wrong.");
                    ok = false;
                }
                if (!cg1.neverFadeEntireCGForSpiritDeparture || cg1.fullScreenFadeAllowed)
                {
                    report.AppendLine("FAIL formal art: CG01 must forbid a whole-CG fade.");
                    ok = false;
                }
            }
            Presentation.PresentationLayerItem cg3;
            if (!Presentation.PresentationLayerCatalog.TryGet("CG03_BandFarewell", out cg3) || !cg3.keepCompleteFourPersonPhoto)
            {
                report.AppendLine("FAIL formal art: CG03 must keep the complete four-person photo.");
                ok = false;
            }
            Presentation.PresentationLayerItem cg5;
            if (!Presentation.PresentationLayerCatalog.TryGet("CG05_SelfCrossing", out cg5) || !cg5.fullScreenFadeAllowed)
            {
                report.AppendLine("FAIL formal art: CG05 must allow the final full-screen fade.");
                ok = false;
            }
            Presentation.PresentationLayerItem cg4;
            if (!Presentation.PresentationLayerCatalog.TryGet("CG04_TeacherFarewell", out cg4) || !cg4.blackboardTextRenderedByProgram)
            {
                report.AppendLine("FAIL formal art: CG04 must render blackboard text by program.");
                ok = false;
            }
            if (Presentation.PresentationLayerCatalog.Count != 5)
            {
                report.AppendLine("FAIL formal art: PresentationLayers.json expected 5 items, got " +
                    Presentation.PresentationLayerCatalog.Count);
                ok = false;
            }

            if (ok) report.AppendLine("OK   formal art: CHR01 sheet 12 frames, R11 stands, IT03 HUD icon, 5 CGs 640x360, layer contract");
            return ok;
        }

        /// C04: the formal CH01 stage table (12 stages, chain + conditions +
        /// dialogue wiring) must match the delivery stage table.
        private static bool TestChapter01Structure(Dialogue.DialogueLibrary lib, StringBuilder report)
        {
            bool ok = true;
            Chapters.ChapterConfig cfg = Chapters.ChapterCatalog.GetChapter("CH01");
            if (cfg == null)
            {
                report.AppendLine("FAIL CH01: formal chapter config missing.");
                return false;
            }

            string[] expected =
            {
                "CH01_Opening", "CH01_Prepare", "CH01_ReturnItems", "CH01_RitualTalk",
                "CH01_Ritual", "CH01_Board", "CH01_River", "CH01_Door",
                "CH01_ReunionTalk", "CH01_Stools", "CH01_Memory", "CH01_Farewell", "CH01_Complete"
            };
            if (cfg.stages.Length != expected.Length)
            {
                report.AppendLine("FAIL CH01: expected " + expected.Length + " stages, got " + cfg.stages.Length);
                ok = false;
            }

            var ids = new HashSet<string>();
            foreach (Chapters.StageDefinition s in cfg.stages) ids.Add(s.stageId);
            foreach (string id in expected)
            {
                if (!ids.Contains(id))
                {
                    report.AppendLine("FAIL CH01: stage missing: " + id);
                    ok = false;
                }
            }

            // Chain closure: every stage's nextStageId exists (or is empty).
            foreach (Chapters.StageDefinition s in cfg.stages)
            {
                if (string.IsNullOrEmpty(s.nextStageId)) continue;
                if (!ids.Contains(s.nextStageId))
                {
                    report.AppendLine("FAIL CH01: stage " + s.stageId + " points at unknown next " + s.nextStageId);
                    ok = false;
                }
            }

            // Prepare = all-of (glasses + traced cane), cane condition grants the item.
            Chapters.StageDefinition prepare = cfg.GetStage("CH01_Prepare");
            if (prepare == null || prepare.requireAll == null || prepare.requireAll.Length != 2)
            {
                report.AppendLine("FAIL CH01: Prepare must require ALL of {ItemCollected(IT01_Glasses), MinigameCompleted(MG01_Cane)}.");
                ok = false;
            }
            else
            {
                bool hasGlasses = false, hasCane = false;
                foreach (Chapters.StageCondition c in prepare.requireAll)
                {
                    if (c.type == Core.GameEventType.ItemCollected && c.sourceId == "IT01_Glasses") hasGlasses = true;
                    if (c.type == Core.GameEventType.MinigameCompleted && c.sourceId == "MG01_Cane" &&
                        c.grantsItemId == "IT02_Cane") hasCane = true;
                }
                if (!hasGlasses || !hasCane)
                {
                    report.AppendLine("FAIL CH01: Prepare conditions wrong (glasses=" + hasGlasses + " caneGrant=" + hasCane + ").");
                    ok = false;
                }
            }

            // ReturnItems requires both items; the scene anchor ids must match
            // the assembly (grandfather id / CH01_RITUAL / CH01_BOAT).
            Chapters.StageDefinition ret = cfg.GetStage("CH01_ReturnItems");
            if (ret == null || ret.requiredItemIds == null || ret.requiredItemIds.Length != 2 ||
                ret.expectedSourceId != "CH01_GRANDFATHER")
            {
                report.AppendLine("FAIL CH01: ReturnItems must require both items and expect source CH01_GRANDFATHER.");
                ok = false;
            }
            if (cfg.GetStage("CH01_Ritual")?.expectedSourceId != "CH01_RITUAL" ||
                cfg.GetStage("CH01_Board")?.expectedSourceId != "CH01_BOAT" ||
                cfg.GetStage("CH01_River")?.expectedSourceId != "River" ||
                cfg.GetStage("CH01_Door")?.expectedSourceId != "MG02_Door" ||
                cfg.GetStage("CH01_Stools")?.expectedSourceId != "MG03_Stools")
            {
                report.AppendLine("FAIL CH01: stage expectedSourceId wiring wrong.");
                ok = false;
            }

            // Auto-start dialogue segments must exist in TXT_CH01.json.
            foreach ((string stageId, string segId) in new[] { ("CH01_RitualTalk", "CH01_RitualTalk"), ("CH01_ReunionTalk", "CH01_ReunionTalk") })
            {
                Chapters.StageDefinition st = cfg.GetStage(stageId);
                Dialogue.DialogueSegment segment;
                if (st == null || st.autoStartSegmentId != segId || !lib.TryGetSegment("CH01", segId, out segment))
                {
                    report.AppendLine("FAIL CH01: " + stageId + " auto-start segment missing: " + segId);
                    ok = false;
                }
            }
            Dialogue.DialogueSegment openingSeg;
            if (!lib.TryGetSegment("CH01", "CH01_Opening", out openingSeg) || openingSeg.stageId != "CH01_Opening")
            {
                report.AppendLine("FAIL CH01: opening segment missing/stage mismatch.");
                ok = false;
            }

            // Farewell: presentation-driven; Complete: auto-advance fade.
            Chapters.StageDefinition farewell = cfg.GetStage("CH01_Farewell");
            if (farewell == null || farewell.autoStartPresentationId != "PR_CH01_Farewell" ||
                farewell.completionEvent != Core.GameEventType.PresentationFinished)
            {
                report.AppendLine("FAIL CH01: Farewell stage must auto-start PR_CH01_Farewell and complete on PresentationFinished.");
                ok = false;
            }
            Chapters.StageDefinition complete = cfg.GetStage("CH01_Complete");
            if (complete == null || complete.autoAdvanceSeconds <= 0f)
            {
                report.AppendLine("FAIL CH01: Complete stage must auto-advance after the closing fade.");
                ok = false;
            }

            // River rules: two passengers, one banned lane per wave (CH01 rule set).
            if (cfg.riverSettings.passengerCount != 2 || cfg.riverSettings.maxBannedLanesPerWave != 1)
            {
                report.AppendLine("FAIL CH01: riverSettings must be passengerCount=2 / maxBannedLanesPerWave=1.");
                ok = false;
            }

            // Formal-first menu selection.
            string first = Chapters.ChapterCatalog.GetFirstUnfinishedChapterId(null);
            if (first != "CH01")
            {
                report.AppendLine("FAIL CH01: menu start should select CH01 first (got " + first + ").");
                ok = false;
            }

            if (ok) report.AppendLine("OK   CH01: 12-stage table, conditions, dialogue wiring, river rules");
            return ok;
        }

        private static bool TestPresentationMap(Dialogue.DialogueLibrary lib, StringBuilder report)
        {
            bool ok = true;
            Dialogue.PresentationEntry pr;
            if (!lib.TryGetPresentation("PR_CH01_Farewell", out pr))
            {
                report.AppendLine("FAIL CH01: PR_CH01_Farewell missing from PresentationMap.json.");
                return false;
            }
            if (pr.orderedSegmentIds.Length != 1 || pr.orderedSegmentIds[0] != "CH01_Farewell")
            {
                report.AppendLine("FAIL CH01: PR_CH01_Farewell orderedSegmentIds wrong: " + string.Join(",", pr.orderedSegmentIds));
                ok = false;
            }
            Dialogue.DialogueSegment farewellSeg;
            if (!lib.TryGetSegment("CH01", pr.orderedSegmentIds[0], out farewellSeg))
            {
                report.AppendLine("FAIL CH01: presentation segment CH01_Farewell missing in TXT_CH01.json.");
                ok = false;
            }
            if (lib.PresentationCount < 6)
            {
                report.AppendLine("FAIL CH01: expected >=6 presentations in PresentationMap.json, got " + lib.PresentationCount);
                ok = false;
            }
            if (Chapters.ChapterCatalog.GetActorDisplayName("Grandmother", "") != "奶奶" ||
                Chapters.ChapterCatalog.GetActorDisplayName("PastSelfFamily", "") != "孙子")
            {
                report.AppendLine("FAIL CH01: CharacterDisplayNames lookup wrong.");
                ok = false;
            }
            if (ok) report.AppendLine("OK   CH01: PresentationMap + actor display names");
            return ok;
        }

        /// C10: every delivered R15/R16/R17/R19 clip must be loadable at its
        /// runtime key with real audio data (durations are only a sanity
        /// check - listening is a separate, user-side acceptance item).
        private static bool TestAudioResources(StringBuilder report)
        {
            bool ok = true;
            string[] notes = { "N01_Do", "N02_Re", "N03_Mi", "N04_Fa", "N05_Sol", "N06_La", "N07_Si", "MEL01_BandPhrase" };
            foreach (string id in notes)
            {
                AudioClip clip = Resources.Load<AudioClip>("Audio/Notes/" + id);
                if (clip == null || clip.length <= 0.05f)
                {
                    report.AppendLine("FAIL audio: note clip missing/empty: " + id);
                    ok = false;
                }
            }
            string[] sfx =
            {
                "SFX01_UIConfirm", "SFX02_Footstep", "SFX03_Pickup", "SFX04_ReturnItem",
                "SFX05_Ritual", "SFX06_Rowing", "SFX07_Whirlpool", "SFX08_Shutter",
                "SFX09_Chalk", "SFX10_SchoolBell", "SFX11_Knock", "SFX12_PuzzleSuccess",
                "SFX13_DogHappy"
            };
            foreach (string id in sfx)
            {
                AudioClip clip = Resources.Load<AudioClip>("Audio/SFX/" + id);
                if (clip == null || clip.length <= 0.05f)
                {
                    report.AppendLine("FAIL audio: sfx clip missing/empty: " + id);
                    ok = false;
                }
            }
            string[] music = { "BGM01_RiverMist", "BGM02_Reunion", "BGM03_SelfCrossing" };
            foreach (string id in music)
            {
                AudioClip clip = Resources.Load<AudioClip>("Audio/Music/" + id);
                if (clip == null || clip.length < 60f)
                {
                    report.AppendLine("FAIL audio: music clip missing or too short: " + id +
                        " (" + (clip != null ? clip.length.ToString("F1") : "null") + "s)");
                    ok = false;
                }
            }
            if (ok) report.AppendLine("OK   audio: 8 notes + 13 SFX + 3 BGM load at their runtime keys");
            return ok;
        }

        /// C10 (R12): the five supporting walk sheets are imported and sliced
        /// to 12 frames each with the contract grid; R06 river background too.
        private static bool TestNpcWalkSheets(StringBuilder report)
        {
            bool ok = true;
            foreach (string id in new[] { "CHR02_Grandfather", "CHR03_Grandmother",
                                          "CHR05_LoverSpirit", "CHR07_DrummerSpirit",
                                          "CHR11_TeacherSpirit" })
            {
                Sprite[] frames = Core.FormalArt.LoadWalkFrames(id);
                if (frames == null)
                {
                    report.AppendLine("FAIL walk sheet: " + id + " is missing or not sliced to 12 frames.");
                    ok = false;
                }
            }
            Sprite riverBg = Resources.Load<Sprite>("Art/Backgrounds/BG02_River");
            if (riverBg == null || Mathf.RoundToInt(riverBg.rect.width) != 1280)
            {
                report.AppendLine("FAIL formal art: BG02_River background missing or wrong size.");
                ok = false;
            }
            if (ok) report.AppendLine("OK   R12: 5 walk sheets sliced to 12 frames; R06 river background present");
            return ok;
        }

        private static bool TestUiStrings(Dialogue.DialogueLibrary lib, StringBuilder report)
        {
            bool ok = true;
            // Keys the C04 code paths rely on (formal dot-notation keys).
            string[][] required =
            {
                new[] { "hint.move", "使用 WASD 或方向键移动。" },
                new[] { "hint.pickup", "按 E 拾取" },
                new[] { "hint.return_items", "按 E 交还物品" },
                new[] { "hint.ritual", "按 E 开始仪式" },
                new[] { "hint.board", "按 E 上船" },
                new[] { "hint.cane_station", "按 E 画出拐杖" },
                new[] { "hint.door", "按 E 敲门" },
                new[] { "hint.table", "按 E 摆放凳子" },
                new[] { "feedback.item_collected", "已找到物品。" },
                new[] { "feedback.items_returned", "物品已交还。" },
                new[] { "feedback.ritual_complete", "仪式完成，可以前往渡口。" },
                new[] { "feedback.chapter_complete", "这一程，已经完成。" },
                new[] { "feedback.gallery_unlocked", "一段回忆已收进回忆馆。" },
                new[] { "river.instructions", "船会自行前进。使用 A、D 或左右方向键，避开漩涡。" },
                new[] { "button.start_crossing", "开始渡河" },
                new[] { "button.retry_river", "重新渡河" },
                new[] { "minigame.cane.instructions", "按住鼠标左键，沿轮廓描出拐杖。可以抬笔，再接着画。" },
                new[] { "minigame.door.instructions", "点击门环，轻轻敲门。" },
                new[] { "minigame.stools.progress", "已摆好 {count} / {total}" }
            };
            foreach (string[] kv in required)
            {
                string actual = lib.GetUiString(kv[0], "");
                if (actual != kv[1])
                {
                    report.AppendLine("FAIL UI strings: " + kv[0] + " = '" + actual + "' (expected '" + kv[1] + "')");
                    ok = false;
                }
            }
            if (lib.GetObjectiveText("CH01", "CH01_Prepare", "") == "")
            {
                report.AppendLine("FAIL UI strings: CH01 objectives missing.");
                ok = false;
            }
            if (lib.GetItemDisplayName("IT01_Glasses", "") != "老花镜" ||
                lib.GetItemDisplayName("IT02_Cane", "") != "拐杖")
            {
                report.AppendLine("FAIL UI strings: CH01 item display names missing.");
                ok = false;
            }
            if (ok) report.AppendLine("OK   UI strings: formal TXT_UI keys + CH01 objectives/items");
            return ok;
        }
    }
}
