using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Duzhe.Core;
using Duzhe.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Duzhe.EditorTools
{
    /// Repeatable project setup: tags/layers, project settings, placeholder
    /// assets, the seven scene skeletons, Build Settings registration and a
    /// full validation report. Safe to run any number of times - existing
    /// objects are found by name and completed, never duplicated; existing
    /// user content is never overwritten. Batch entry point exits 0 on PASS.
    public static class ProjectSetupUtility
    {
        private const string ScenesDir = "Assets/_Project/Scenes";
        private const string ArtRoot = "Assets/_Project/Resources/Art";
        private const string ReportPath = "Logs/duzhe_validation.txt";

        // sceneId -> (fileName, hasNpcAndItem)
        private static readonly (string, bool)[] ScenePlan = new (string, bool)[]
        {
            (DuzheIds.SceneMainMenu, false),
            (DuzheIds.SceneUnderworld, true),
            (DuzheIds.SceneRiver, false),
            (DuzheIds.SceneFarmCourtyard, false),
            (DuzheIds.ScenePark, false),
            (DuzheIds.SceneRecordingStudio, false),
            (DuzheIds.SceneClassroom, false)
        };

        /// Per-scene default spawn (SP_Main) in world units. Underworld keeps
        /// the accepted dev position; FarmCourtyard uses the R04 FC_Spawn
        /// point (288,576)px -> (-11,-6.75)u (worldX=(px-640)/32, worldY=(360-py)/32).
        private static readonly (string sceneId, Vector3 spawn)[] SpawnPlan =
        {
            (DuzheIds.SceneUnderworld, new Vector3(0f, -6f, 0f)),
            (DuzheIds.SceneRiver, new Vector3(0f, -3f, 0f)),
            (DuzheIds.SceneFarmCourtyard, new Vector3(-11f, -6.75f, 0f)),
            (DuzheIds.ScenePark, new Vector3(0f, -6f, 0f)),
            // CH03: R09 BG05 door threshold (224,576)px -> (-13,-6.75)u.
            (DuzheIds.SceneRecordingStudio, new Vector3(-13f, -6.75f, 0f)),
            // CH04: the lesson starts in the middle of the room so the
            // student, the teacher at the board and the blackboard are all in
            // frame on arrival (the R04 CL_Spawn corner left an empty room on
            // screen and put every actor outside the camera view).
            (DuzheIds.SceneClassroom, new Vector3(-3f, -1f, 0f))
        };

        /// Scenes whose delivered formal background replaces the procedural
        /// placeholder. Everything else keeps "BG_<sceneId>_Placeholder".
        /// The MainMenu entry is a RESERVED slot: the menu art has not been
        /// delivered yet, so the lookup falls back to the placeholder until
        /// `BG00_MainMenu.png` is dropped into Resources/Art/Backgrounds.
        private static readonly (string sceneId, string resourcePath)[] FormalBackgrounds =
        {
            (DuzheIds.SceneMainMenu, "Art/Backgrounds/BG00_MainMenu"),
            (DuzheIds.SceneUnderworld, "Art/Backgrounds/BG01_Underworld"),
            (DuzheIds.SceneRiver, "Art/Backgrounds/BG02_River"),
            (DuzheIds.SceneFarmCourtyard, "Art/Backgrounds/BG03_FarmCourtyard"),
            (DuzheIds.ScenePark, "Art/Backgrounds/BG04_Park"),
            (DuzheIds.SceneRecordingStudio, "Art/Backgrounds/BG05_RecordingStudio"),
            (DuzheIds.SceneClassroom, "Art/Backgrounds/BG06_Classroom")
        };

        private static string BackgroundPathFor(string sceneId)
        {
            foreach ((string id, string path) in FormalBackgrounds)
            {
                if (id == sceneId) return path;
            }
            return "Art/Backgrounds/BG_" + sceneId + "_Placeholder";
        }

        [MenuItem("渡者/工程装配与校验")]
        public static void RunInteractive()
        {
            var report = new StringBuilder();
            bool ok = RunAll(report);
            string text = report.ToString();
            Debug.Log(text);
            WriteReport(text, ok);
            if (ok) Debug.Log("DZ_VALIDATION=PASS");
            else Debug.LogError("DZ_VALIDATION=FAIL (see " + ReportPath + ")");
        }

        public static void RunFromCommandLine()
        {
            var report = new StringBuilder();
            bool ok = RunAll(report);
            string text = report.ToString();
            Debug.Log(text);
            WriteReport(text, ok);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool RunAll(StringBuilder report)
        {
            report.AppendLine("== Duzhe project setup & validation ==");
            report.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            bool ok = true;

            ok &= Step("Tags & layers", report, EnsureTagsAndLayers);
            ok &= Step("Project settings", report, EnsureProjectSettings);
            ok &= Step("Placeholder assets", report, EnsurePlaceholderAssets);
            ok &= Step("Formal art imports", report, EnsureFormalArtImports);
            ok &= Step("Dialogue imports", report, EnsureDialogueImports);
            ok &= Step("Minigame imports", report, EnsureMinigameImports);
            ok &= Step("Audio imports", report, EnsureAudioImports);
            ok &= Step("Scenes", report, EnsureScenes);
            ok &= Step("Build Settings", report, EnsureBuildSettings);
            ok &= Step("Dialogue JSON", report, DialogueJsonValidator.ValidateAll);

            AssetDatabase.SaveAssets();
            report.AppendLine(ok ? "DZ_VALIDATION=PASS" : "DZ_VALIDATION=FAIL");
            return ok;
        }

        private delegate bool StepFn(StringBuilder report);

        private static bool Step(string title, StringBuilder report, StepFn fn)
        {
            int len = report.Length;
            bool ok;
            try
            {
                ok = fn(report);
            }
            catch (Exception e)
            {
                report.AppendLine("FAIL [" + title + "] exception: " + e.Message + "\n" + e.StackTrace);
                return false;
            }
            if (report.Length == len) report.AppendLine((ok ? "OK   " : "FAIL ") + title);
            else report.Insert(len, "-- " + title + " --\n");
            if (!ok)
            {
                // ensure a FAIL marker exists for the step
                if (!report.ToString().Contains("FAIL")) report.AppendLine("FAIL " + title);
            }
            return ok;
        }

        private static void WriteReport(string text, bool ok)
        {
            try
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText(ReportPath, text, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogError("[Duzhe] Could not write validation report: " + e.Message);
            }
        }

        // ------------------------------------------------------------------
        // Formal art imports (R11 characters / R12 main walk sheet / R14 CGs).
        // Source batches live under SourceAssets/<batch>; formal copies land
        // in Assets/_Project/Resources/Art with the delivery contract's
        // importer settings (32 PPU, Point, no mips, uncompressed, feet
        // pivot (0.5,0); the walk sheet is multiple-sliced 3x4).
        // ------------------------------------------------------------------
        private static readonly string[] R11CharacterIds =
        {
            "CHR02_Grandfather", "CHR03_Grandmother", "CHR05_LoverSpirit",
            "CHR07_DrummerSpirit", "CHR09_BandmateA", "CHR10_BandmateB", "CHR11_TeacherSpirit"
        };

        private static readonly string[] R14IllustrationIds =
        {
            "CG01_FamilyFarewell", "CG02_LoversFarewell", "CG03_BandFarewell",
            "CG04_TeacherFarewell", "CG05_SelfCrossing"
        };

        public static bool EnsureFormalArtImports(StringBuilder report)
        {
            bool ok = true;
            string root = Path.GetDirectoryName(Path.GetFullPath("Assets"));

            foreach (string id in new[] { "CG06_LoversMemory01", "CG07_LoversMemory02", "CG08_LoversMemory03" })
            {
                string src = Path.Combine(root, "SourceAssets/Codex_Optional/Illustrations/" + id + ".png");
                string dst = "Assets/_Project/Resources/Art/Illustrations/" + id + ".png";
                if (CopyIfNeeded(src, dst, report)) ok &= ConfigureIllustrationImporter(dst, report);
                else ok = false;
            }
            foreach (string id in new[] { "PRP08_StoneLantern", "CHR13_Dog" })
            {
                string folder = id.StartsWith("CHR") ? "Characters" : "Props";
                string src = Path.Combine(root, "SourceAssets/Codex_Optional/" + folder + "/" + id + ".png");
                string dst = "Assets/_Project/Resources/Art/" + folder + "/" + id + ".png";
                if (CopyIfNeeded(src, dst, report)) ok &= ConfigureStandImporter(dst, report);
                else ok = false;
            }
            foreach (string id in new[] { "CG09_FamilyMemory01", "CG10_FamilyMemory02", "CG11_FamilyMemory03", "CG12_BandMemory01", "CG13_BandMemory02", "CG14_BandMemory03", "CG15_TeacherMemory01", "CG16_TeacherMemory02", "CG17_TeacherMemory03" })
            {
                string src = Path.Combine(root, "SourceAssets/Codex_ThreeMemories/Illustrations/" + id + ".png");
                string dst = "Assets/_Project/Resources/Art/Illustrations/" + id + ".png";
                if (CopyIfNeeded(src, dst, report)) ok &= ConfigureIllustrationImporter(dst, report); else ok = false;
            }
            foreach (string id in new[] { "CG18_FamilyRecollection", "CG19_LoverRecollection", "CG20_FriendsRecollection", "CG21_TeacherRecollection" })
            {
                string src = Path.Combine(root, "SourceAssets/Codex_StoryRevision_20261102/Illustrations/" + id + ".png");
                string dst = "Assets/_Project/Resources/Art/Illustrations/" + id + ".png";
                if (CopyIfNeeded(src, dst, report))
                {
                    ok &= ConfigureIllustrationImporter(dst, report);
                    TextureImporter importer = AssetImporter.GetAtPath(dst) as TextureImporter;
                    if (importer != null) { importer.maxTextureSize = 2048; importer.SaveAndReimport(); }
                }
                else ok = false;
            }
            string menuSource = Path.Combine(root, "SourceAssets/Codex_C12/Backgrounds/BG00_MainMenu.png");
            const string menuDestination = "Assets/_Project/Resources/Art/Backgrounds/BG00_MainMenu.png";
            if (CopyIfNeeded(menuSource, menuDestination, report)) ok &= ConfigureBackgroundImporter(menuDestination, report);
            else ok = false;
            // R11: seven formal standing sprites (32x48, feet pivot).
            foreach (string id in R11CharacterIds)
            {
                string src = Path.Combine(root, "SourceAssets/R11_Characters_KimiArtB/DuzheResources/Characters/" + id + ".png");
                string dst = "Assets/_Project/Resources/Art/Characters/" + id + ".png";
                if (CopyIfNeeded(src, dst, report)) ok &= ConfigureStandImporter(dst, report);
                else ok = false;
            }

            // R12: the Ferryman walk sheet (multiple-sliced 3x4, 32x48 cells).
            string sheetSrc = Path.Combine(root, "SourceAssets/R12_Ferryman_v1.0/DuzheResources/Characters/Animations/CHR01_Ferryman.png");
            const string sheetDst = "Assets/_Project/Resources/Art/Animations/CHR01_Ferryman.png";
            if (CopyIfNeeded(sheetSrc, sheetDst, report)) ok &= ConfigureSheetImporter(sheetDst, report);
            else ok = false;

            // R14: five farewell illustrations (640x360) + the layers manifest.
            foreach (string id in R14IllustrationIds)
            {
                string src = Path.Combine(root, "SourceAssets/R14_FarewellIllustrations_v1.0/DuzheResources/Illustrations/" + id + ".png");
                string dst = "Assets/_Project/Resources/Art/Illustrations/" + id + ".png";
                if (CopyIfNeeded(src, dst, report)) ok &= ConfigureIllustrationImporter(dst, report);
                else ok = false;
            }
            string layersSrc = Path.Combine(root, "SourceAssets/R14_FarewellIllustrations_v1.0/DuzheResources/Delivery/R14/PresentationLayers.json");
            const string layersDst = "Assets/_Project/Resources/Data/Presentations/PresentationLayers.json";
            if (CopyIfNeeded(layersSrc, layersDst, report))
            {
                report.AppendLine("OK   formal data: PresentationLayers.json");
            }
            else ok = false;

            // C12 (R05/R13): the formal Underworld background plus the ferry,
            // whirlpool and the two CH01 quest items. All four used to stay on
            // their painted placeholders, which is why the underworld read as a
            // flat colour and the boat as a grey block.
            string uwBgSrc = Path.Combine(root, "SourceAssets/R05_Underworld_v1.0/Backgrounds/BG01_Underworld.png");
            const string uwBgDst = "Assets/_Project/Resources/Art/Backgrounds/BG01_Underworld.png";
            if (CopyIfNeeded(uwBgSrc, uwBgDst, report)) ok &= ConfigureBackgroundImporter(uwBgDst, report);
            else ok = false;

            // Boat / whirlpool keep the CENTRE pivot (ConfigureBackgroundImporter):
            // the river rules place the boat by its centre line, so a feet
            // pivot would shift the whole crossing by one unit.
            string boatSrc = Path.Combine(root, "SourceAssets/R13_Props_v1.0/DuzheResources/Props/PRP01_Boat.png");
            const string boatDst = "Assets/_Project/Resources/Art/Props/PRP01_Boat.png";
            if (CopyIfNeeded(boatSrc, boatDst, report)) ok &= ConfigureBackgroundImporter(boatDst, report);
            else ok = false;

            string verticalBoatSrc = Path.Combine(root, "SourceAssets/Codex_River/PRP01_BoatVertical.png");
            const string verticalBoatDst = "Assets/_Project/Resources/Art/Props/PRP01_BoatVertical.png";
            if (CopyIfNeeded(verticalBoatSrc, verticalBoatDst, report)) ok &= ConfigureBackgroundImporter(verticalBoatDst, report);
            else ok = false;

            string whirlSrc = Path.Combine(root, "SourceAssets/R13_Props_v1.0/DuzheResources/Props/PRP02_Whirlpool.png");
            const string whirlDst = "Assets/_Project/Resources/Art/Props/PRP02_Whirlpool.png";
            if (CopyIfNeeded(whirlSrc, whirlDst, report)) ok &= ConfigureBackgroundImporter(whirlDst, report);
            else ok = false;

            foreach (string id in new[] { "IT01_Glasses", "IT02_Cane" })
            {
                string src = Path.Combine(root, "SourceAssets/R13_Props_v1.0/DuzheResources/Props/" + id + ".png");
                string dst = "Assets/_Project/Resources/Art/Props/" + id + ".png";
                if (CopyIfNeeded(src, dst, report)) ok &= ConfigureStandImporter(dst, report);
                else ok = false;
            }

            // R07: the courtyard painting includes the complete house door.
            string farmSrc = Path.Combine(root, "SourceAssets/R07_R10_Backgrounds_v1.0/DuzheResources/Backgrounds/BG03_FarmCourtyard.png");
            const string farmDst = "Assets/_Project/Resources/Art/Backgrounds/BG03_FarmCourtyard.png";
            if (CopyIfNeeded(farmSrc, farmDst, report)) ok &= ConfigureBackgroundImporter(farmDst, report);
            else ok = false;

            // C05 (R07-R10): Park background BG04.
            string parkSrc = Path.Combine(root, "SourceAssets/Codex_C12/Backgrounds/BG04_Park.png");
            const string parkDst = "Assets/_Project/Resources/Art/Backgrounds/BG04_Park.png";
            if (CopyIfNeeded(parkSrc, parkDst, report)) ok &= ConfigureBackgroundImporter(parkDst, report);
            else ok = false;

            // C05 (R13): the necklace item.
            string necklaceSrc = Path.Combine(root, "SourceAssets/R13_Props_v1.0/DuzheResources/Props/IT03_Necklace.png");
            const string necklaceDst = "Assets/_Project/Resources/Art/Props/IT03_Necklace.png";
            if (CopyIfNeeded(necklaceSrc, necklaceDst, report)) ok &= ConfigureStandImporter(necklaceDst, report);
            else ok = false;

            // C06 (R09): the recording studio background BG05.
            string studioSrc = Path.Combine(root, "SourceAssets/R07_R10_Backgrounds_v1.0/DuzheResources/Backgrounds/BG05_RecordingStudio.png");
            const string studioDst = "Assets/_Project/Resources/Art/Backgrounds/BG05_RecordingStudio.png";
            if (CopyIfNeeded(studioSrc, studioDst, report)) ok &= ConfigureBackgroundImporter(studioDst, report);
            else ok = false;

            // C06 (R13): the drumsticks quest item.
            string drumsticksSrc = Path.Combine(root, "SourceAssets/R13_Props_v1.0/DuzheResources/Props/IT04_Drumsticks.png");
            const string drumsticksDst = "Assets/_Project/Resources/Art/Props/IT04_Drumsticks.png";
            if (CopyIfNeeded(drumsticksSrc, drumsticksDst, report)) ok &= ConfigureStandImporter(drumsticksDst, report);
            else ok = false;

            // R13 Panels v1.0: formal MG06/MG07 panel art (640x360).
            foreach (string id in new[] { "PRP06_PhotoFrame", "PRP07_BlackboardCloseup" })
            {
                string src = Path.Combine(root, (id == "PRP06_PhotoFrame" ? "SourceAssets/Codex_PhotoFix/Props/" : "SourceAssets/R13_Panels_v1.0/DuzheResources/Props/") + id + ".png");
                string dst = "Assets/_Project/Resources/Art/Props/" + id + ".png";
                if (CopyIfNeeded(src, dst, report)) ok &= ConfigureBackgroundImporter(dst, report);
                else ok = false;
            }

            // R11 main characters: the grown grandson (CH01) and the boy (CH02).
            string grandsonSrc = Path.Combine(root, "SourceAssets/R11_MainCharacters_v1.0/Characters/CHR04_PastSelfFamily.png");
            const string grandsonDst = "Assets/_Project/Resources/Art/Characters/CHR04_PastSelfFamily.png";
            if (CopyIfNeeded(grandsonSrc, grandsonDst, report)) ok &= ConfigureStandImporter(grandsonDst, report);
            else ok = false;

            string boySrc = Path.Combine(root, "SourceAssets/R11_MainCharacters_v1.0/Characters/CHR06_PastSelfRomance.png");
            const string boyDst = "Assets/_Project/Resources/Art/Characters/CHR06_PastSelfRomance.png";
            if (CopyIfNeeded(boySrc, boyDst, report)) ok &= ConfigureStandImporter(boyDst, report);
            else ok = false;

            // C06: the band's past self (CH03 fourth band member).
            string bandSrc = Path.Combine(root, "SourceAssets/R11_MainCharacters_v1.0/Characters/CHR08_PastSelfBand.png");
            const string bandDst = "Assets/_Project/Resources/Art/Characters/CHR08_PastSelfBand.png";
            if (CopyIfNeeded(bandSrc, bandDst, report)) ok &= ConfigureStandImporter(bandDst, report);
            else ok = false;

            // C07 (R10): the classroom background BG06.
            string classSrc = Path.Combine(root, "SourceAssets/R07_R10_Backgrounds_v1.0/DuzheResources/Backgrounds/BG06_Classroom.png");
            const string classDst = "Assets/_Project/Resources/Art/Backgrounds/BG06_Classroom.png";
            if (CopyIfNeeded(classSrc, classDst, report)) ok &= ConfigureBackgroundImporter(classDst, report);
            else ok = false;

            // C07 (R13): the lesson-plan quest item.
            string planSrc = Path.Combine(root, "SourceAssets/R13_Props_v1.0/DuzheResources/Props/IT05_LessonPlan.png");
            const string planDst = "Assets/_Project/Resources/Art/Props/IT05_LessonPlan.png";
            if (CopyIfNeeded(planSrc, planDst, report)) ok &= ConfigureStandImporter(planDst, report);
            else ok = false;

            // C07 (R11 main characters): the school student.
            string studentSrc = Path.Combine(root, "SourceAssets/R11_MainCharacters_v1.0/Characters/CHR12_PastSelfSchool.png");
            const string studentDst = "Assets/_Project/Resources/Art/Characters/CHR12_PastSelfSchool.png";
            if (CopyIfNeeded(studentSrc, studentDst, report)) ok &= ConfigureStandImporter(studentDst, report);
            else ok = false;

            // C10 (R13): the door close-up board and the stool/table props.
            string doorSrc = Path.Combine(root, "SourceAssets/R13_Panels_v1.0/DuzheResources/Props/PRP03_DoorCloseup.png");
            const string doorDst = "Assets/_Project/Resources/Art/Props/PRP03_DoorCloseup.png";
            if (CopyIfNeeded(doorSrc, doorDst, report)) ok &= ConfigureBackgroundImporter(doorDst, report);
            else ok = false;
            string stoolSrc = Path.Combine(root, "SourceAssets/R13_Props_v1.0/DuzheResources/Props/PRP04_Stool.png");
            const string stoolDst = "Assets/_Project/Resources/Art/Props/PRP04_Stool.png";
            if (CopyIfNeeded(stoolSrc, stoolDst, report)) ok &= ConfigurePropImporter(stoolDst, 128, report);
            else ok = false;
            string tableSrc = Path.Combine(root, "SourceAssets/R13_Props_v1.0/DuzheResources/Props/PRP05_DiningTable.png");
            const string tableDst = "Assets/_Project/Resources/Art/Props/PRP05_DiningTable.png";
            if (CopyIfNeeded(tableSrc, tableDst, report)) ok &= ConfigurePropImporter(tableDst, 128, report);
            else ok = false;

            // C10 (R06): the formal river background (clean 1280x720; no boat
            // or whirlpools baked in).
            string riverSrc = Path.Combine(root, "SourceAssets/R06_River_v1.0/Backgrounds/BG02_River.png");
            const string riverDst = "Assets/_Project/Resources/Art/Backgrounds/BG02_River.png";
            if (CopyIfNeeded(riverSrc, riverDst, report)) ok &= ConfigureBackgroundImporter(riverDst, report);
            else ok = false;

            // C10 (R12): supporting-character walk sheets (5 characters,
            // 3x4 grid, rows Down/Left/Right/Up, cols WalkA/Idle/WalkB).
            foreach (string id in new[]
                     { "CHR02_Grandfather", "CHR03_Grandmother", "CHR05_LoverSpirit",
                       "CHR07_DrummerSpirit", "CHR11_TeacherSpirit" })
            {
                string walkSrc = Path.Combine(root,
                    "SourceAssets/R12_SupportingWalking/DuzheResources/Characters/Animations/" + id + ".png");
                string walkDst = "Assets/_Project/Resources/Art/Animations/" + id + ".png";
                if (CopyIfNeeded(walkSrc, walkDst, report))
                {
                    ok &= ConfigureSheetImporter(walkDst, report, id);
                }
                else ok = false;
            }

            // C10 (R03): UI skins, icons and the bundled CJK font.
            ok &= EnsureR03UiImports(root, report);

            return ok;
        }

        // ------------------------------------------------------------------
        // C10 (R03): UI skins (nine-slice), HUD icons and the Noto Sans CJK
        // font. The font is imported for the delivery contract; the runtime
        // keeps the already-accepted simhei font (see GameBootstrap) unless
        // it is missing.
        // ------------------------------------------------------------------
        private static bool EnsureR03UiImports(string root, StringBuilder report)
        {
            bool ok = true;
            string uiRoot = Path.Combine(root, "SourceAssets/R03_UI_v1.0/UI");

            var skins = new (string file, int l, int b, int r, int t)[]
            {
                ("UI01_Button.png", 8, 8, 8, 8),
                ("UI01_DialoguePanel.png", 12, 12, 12, 12),
                ("UI01_GenericPanel.png", 12, 12, 12, 12)
            };
            foreach ((string file, int l, int b, int r, int t) in skins)
            {
                string src = Path.Combine(uiRoot, "Skins", file);
                string dst = "Assets/_Project/Resources/Art/UI/" + file;
                if (!CopyIfNeeded(src, dst, report)) { ok = false; continue; }
                if (!ConfigureUiSkinImporter(dst, l, b, r, t, report)) ok = false;
            }

            foreach (string file in new[]
                     { "UI02_Continue.png", "UI02_Interact.png", "UI02_Item.png",
                       "UI02_Pause.png", "UI02_Volume.png" })
            {
                string src = Path.Combine(uiRoot, "Icons", file);
                string dst = "Assets/_Project/Resources/Art/UI/" + file;
                if (!CopyIfNeeded(src, dst, report)) { ok = false; continue; }
                if (!ConfigureUiIconImporter(dst, report)) ok = false;
            }

            string fontSrc = Path.Combine(uiRoot, "Fonts/NotoSansCJKsc-Regular.otf");
            const string fontDst = "Assets/_Project/Resources/Fonts/NotoSansCJKsc-Regular.otf";
            if (CopyIfNeeded(fontSrc, fontDst, report))
            {
                TrueTypeFontImporter fontImporter = AssetImporter.GetAtPath(fontDst) as TrueTypeFontImporter;
                if (fontImporter != null)
                {
                    fontImporter.fontTextureCase = FontTextureCase.Dynamic;
                    fontImporter.SaveAndReimport();
                }
            }
            else ok = false;

            return ok;
        }

        private static bool ConfigureBackgroundImporter(string path, StringBuilder report)
        {            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { report.AppendLine("FAIL " + path + ": importer unavailable."); return false; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = GameBootstrap.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            return true;
        }

        private static bool CopyIfNeeded(string src, string dst, StringBuilder report)
        {
            try
            {
                if (!File.Exists(src))
                {
                    report.AppendLine("FAIL formal art: source missing: " + src);
                    return false;
                }
                bool needs = !File.Exists(dst);
                if (!needs)
                {
                    needs = File.GetLastWriteTimeUtc(src) > File.GetLastWriteTimeUtc(dst) ||
                            !FilesEqual(src, dst);
                }
                if (needs)
                {
                    string dir = Path.GetDirectoryName(dst);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.Copy(src, dst, true);
                    AssetDatabase.ImportAsset(dst);
                }
                else
                {
                    AssetDatabase.ImportAsset(dst);
                }
                return true;
            }
            catch (Exception e)
            {
                report.AppendLine("FAIL formal art copy " + src + ": " + e.Message);
                return false;
            }
        }

        private static bool ConfigureStandImporter(string path, StringBuilder report)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { report.AppendLine("FAIL " + path + ": importer unavailable."); return false; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = GameBootstrap.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 64;
            // Feet pivot (0.5,0) per the R11 contract - via SerializedObject:
            // this Tuanjie build has no public spriteAlignment/spritePivot API.
            SerializedObject so = new SerializedObject(importer);
            so.Update();
            so.FindProperty("m_Alignment").intValue = (int)SpriteAlignment.Custom;
            so.FindProperty("m_SpritePivot").vector2Value = new Vector2(0.5f, 0f);
            so.ApplyModifiedProperties();
            importer.SaveAndReimport();
            return true;
        }

        /// Props (stool / table / door board): bottom-centre pivot like the
        /// character stands, but with a larger max size so a 96x64 sprite is
        /// never downscaled.
        private static bool ConfigurePropImporter(string path, int maxSize, StringBuilder report)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { report.AppendLine("FAIL " + path + ": importer unavailable."); return false; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = GameBootstrap.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxSize;
            SerializedObject so = new SerializedObject(importer);
            so.Update();
            so.FindProperty("m_Alignment").intValue = (int)SpriteAlignment.Custom;
            so.FindProperty("m_SpritePivot").vector2Value = new Vector2(0.5f, 0f);
            so.ApplyModifiedProperties();
            importer.SaveAndReimport();
            return true;
        }

        /// R03 UI skin: centre pivot + nine-slice border (L/B/R/T).
        private static bool ConfigureUiSkinImporter(string path, int left, int bottom, int right, int top,
            StringBuilder report)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { report.AppendLine("FAIL " + path + ": importer unavailable."); return false; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            // UI sprites use the CANVAS reference PPU (100), NOT the 32 PPU
            // world scale: at 32 the nine-slice borders render ~3x too large
            // and the card no longer lines up with its text.
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;
            SerializedObject so = new SerializedObject(importer);
            so.Update();
            so.FindProperty("m_Alignment").intValue = (int)SpriteAlignment.Center;
            so.FindProperty("m_SpriteBorder").vector4Value = new Vector4(left, bottom, right, top);
            so.ApplyModifiedProperties();
            importer.SaveAndReimport();
            return true;
        }

        /// R03 UI icons: centre pivot, point filter, small max size (UI PPU).
        private static bool ConfigureUiIconImporter(string path, StringBuilder report)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { report.AppendLine("FAIL " + path + ": importer unavailable."); return false; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f; // canvas reference PPU, not world 32
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 128;
            SerializedObject so = new SerializedObject(importer);
            so.Update();
            so.FindProperty("m_Alignment").intValue = (int)SpriteAlignment.Center;
            so.ApplyModifiedProperties();
            importer.SaveAndReimport();
            return true;
        }

        private static bool ConfigureSheetImporter(string path, StringBuilder report,
            string namePrefix = "CHR01")
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { report.AppendLine("FAIL " + path + ": importer unavailable."); return false; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = GameBootstrap.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 256;

            // 3x4 grid, 32x48 cells: rows Down/Left/Right/Up, cols
            // WalkA/Idle/WalkB, pivot (0.5,0) per the R12 delivery contract.
            string[] rows = { "Down", "Left", "Right", "Up" };
            string[] cols = { "WalkA", "Idle", "WalkB" };
            SerializedObject so = new SerializedObject(importer);
            so.Update();
            SerializedProperty sprites = so.FindProperty("m_SpriteSheet.m_Sprites");
            sprites.arraySize = 12;
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    SerializedProperty e = sprites.GetArrayElementAtIndex(row * 3 + col);
                    e.FindPropertyRelative("m_Name").stringValue = namePrefix + "_" + rows[row] + "_" + cols[col];
                    e.FindPropertyRelative("m_Rect").rectValue = new Rect(col * 32f, 192f - (row + 1) * 48f, 32f, 48f);
                    e.FindPropertyRelative("m_Alignment").intValue = (int)SpriteAlignment.Custom;
                    e.FindPropertyRelative("m_Pivot").vector2Value = new Vector2(0.5f, 0f);
                    e.FindPropertyRelative("m_Border").vector4Value = Vector4.zero;
                }
            }
            so.ApplyModifiedProperties();
            importer.SaveAndReimport();

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            int spriteCount = 0;
            foreach (UnityEngine.Object a in assets)
            {
                if (a is Sprite) spriteCount++;
            }
            if (spriteCount != 12)
            {
                report.AppendLine("FAIL " + path + ": expected 12 sliced sprites, got " + spriteCount);
                return false;
            }
            return true;
        }

        private static bool ConfigureIllustrationImporter(string path, StringBuilder report)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { report.AppendLine("FAIL " + path + ": importer unavailable."); return false; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = GameBootstrap.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
            return true;
        }

        // ------------------------------------------------------------------
        // C04: formal delivery JSON import (Data/Dialogue -> Resources).
        // The delivered files live in Assets/_Project/Data/Dialogue as the
        // working copies; the runtime DialogueLibrary reads Resources, so the
        // import copies them (byte-identical) and (re)imports the asset.
        // ------------------------------------------------------------------
        private static readonly (string source, string target)[] DialogueImports =
        {
            ("Assets/_Project/Data/Dialogue/TXT_CH01.json", "Assets/_Project/Resources/Data/Dialogue/TXT_CH01.json"),
            ("Assets/_Project/Data/Dialogue/TXT_CH02.json", "Assets/_Project/Resources/Data/Dialogue/TXT_CH02.json"),
            ("Assets/_Project/Data/Dialogue/TXT_CH03.json", "Assets/_Project/Resources/Data/Dialogue/TXT_CH03.json"),
            ("Assets/_Project/Data/Dialogue/TXT_CH04.json", "Assets/_Project/Resources/Data/Dialogue/TXT_CH04.json"),
            ("Assets/_Project/Data/Dialogue/TXT_CH05.json", "Assets/_Project/Resources/Data/Dialogue/TXT_CH05.json"),
            ("Assets/_Project/Data/Dialogue/TXT_UI.json", "Assets/_Project/Resources/Data/Dialogue/TXT_UI.json"),
            ("Assets/_Project/Data/Dialogue/PresentationMap.json", "Assets/_Project/Resources/Data/Dialogue/PresentationMap.json"),
            ("Assets/_Project/Data/Dialogue/CharacterDisplayNames.json", "Assets/_Project/Resources/Data/Dialogue/CharacterDisplayNames.json"),
            ("Assets/_Project/Data/Dialogue/StoryVisualCues.json", "Assets/_Project/Resources/Data/Dialogue/StoryVisualCues.json")
        };

        /// Formal minigame data (R15 CFG_Notes.json). The runtime loader reads
        /// Resources/Data/Minigames; the working copy lives in Data/Minigames.
        private static readonly (string source, string target)[] MinigameImports =
        {
            ("Assets/_Project/Data/Minigames/CFG_Notes.json", "Assets/_Project/Resources/Data/Minigames/CFG_Notes.json")
        };

        public static bool EnsureMinigameImports(StringBuilder report)
        {            bool ok = true;
            foreach ((string source, string target) in MinigameImports)
            {
                try
                {
                    if (!File.Exists(source))
                    {
                        report.AppendLine("FAIL minigame import: source missing: " + source);
                        ok = false;
                        continue;
                    }
                    if (!File.Exists(target) || !FilesEqual(source, target))
                    {
                        string dir = Path.GetDirectoryName(target);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        File.Copy(source, target, true);
                        AssetDatabase.ImportAsset(target);
                        report.AppendLine("OK   imported " + Path.GetFileName(target));
                    }
                    else
                    {
                        AssetDatabase.ImportAsset(target);
                        report.AppendLine("OK   minigame import up to date: " + Path.GetFileName(target));
                    }
                }
                catch (Exception e)
                {
                    report.AppendLine("FAIL minigame import " + source + ": " + e.Message);
                    ok = false;
                }
            }
            return ok;
        }

        // ------------------------------------------------------------------
        // C10: R15/R16/R17/R19 audio. Source batches keep their nested
        // layout, targets are the fixed runtime keys:
        //   Audio/Notes/N01_Do..N07_Si + MEL01_BandPhrase
        //   Audio/SFX/SFX01_UIConfirm..SFX12_PuzzleSuccess (+ SFX13_DogHappy)
        //   Audio/Music/BGM01_RiverMist..BGM03_SelfCrossing
        // CopyIfNeeded overwrites in place and keeps the target .meta, so
        // every existing reference stays intact.
        // ------------------------------------------------------------------
        private const string AudioBatch = "SourceAssets/R15_R19_Audio_20261027/Duzhe_Audio_20261027";

        private static readonly (string sourceSub, string file, string targetSub)[] AudioImports =
        {
            ("R15_Notes", "N01_Do.wav", "Audio/Notes"),
            ("R15_Notes", "N02_Re.wav", "Audio/Notes"),
            ("R15_Notes", "N03_Mi.wav", "Audio/Notes"),
            ("R15_Notes", "N04_Fa.wav", "Audio/Notes"),
            ("R15_Notes", "N05_Sol.wav", "Audio/Notes"),
            ("R15_Notes", "N06_La.wav", "Audio/Notes"),
            ("R15_Notes", "N07_Si.wav", "Audio/Notes"),
            ("R15_Notes", "MEL01_BandPhrase.wav", "Audio/Notes"),
            ("R16_SFX", "SFX01_UIConfirm.wav", "Audio/SFX"),
            ("R16_SFX", "SFX02_Footstep.wav", "Audio/SFX"),
            ("R16_SFX", "SFX03_Pickup.wav", "Audio/SFX"),
            ("R16_SFX", "SFX04_ReturnItem.wav", "Audio/SFX"),
            ("R16_SFX", "SFX05_Ritual.wav", "Audio/SFX"),
            ("R16_SFX", "SFX06_Rowing.wav", "Audio/SFX"),
            ("R16_SFX", "SFX07_Whirlpool.wav", "Audio/SFX"),
            ("R16_SFX", "SFX08_Shutter.wav", "Audio/SFX"),
            ("R16_SFX", "SFX09_Chalk.wav", "Audio/SFX"),
            ("R16_SFX", "SFX10_SchoolBell.wav", "Audio/SFX"),
            ("R16_SFX", "SFX11_Knock.wav", "Audio/SFX"),
            ("R16_SFX", "SFX12_PuzzleSuccess.wav", "Audio/SFX"),
            ("R19_Optional", "SFX13_DogHappy.wav", "Audio/SFX"),
            ("R17_Music", "BGM01_RiverMist.wav", "Audio/Music"),
            ("R17_Music", "BGM02_Reunion.wav", "Audio/Music"),
            ("R17_Music", "BGM03_SelfCrossing.wav", "Audio/Music")
        };

        public static bool EnsureAudioImports(StringBuilder report)
        {
            bool ok = true;
            string root = Path.GetDirectoryName(Path.GetFullPath("Assets"));
            foreach ((string sourceSub, string file, string targetSub) in AudioImports)
            {
                string src = Path.Combine(root, AudioBatch, sourceSub, file);
                string dst = "Assets/_Project/Resources/" + targetSub + "/" + file;
                if (!CopyIfNeeded(src, dst, report)) { ok = false; continue; }
                if (!ConfigureAudioImporter(dst, report)) ok = false;
            }
            return ok;
        }

        private static bool ConfigureAudioImporter(string path, StringBuilder report)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                report.AppendLine("FAIL " + path + ": audio importer unavailable.");
                return false;
            }
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
            return true;
        }

        public static bool EnsureDialogueImports(StringBuilder report)
        {
            bool ok = true;
            foreach ((string source, string target) in DialogueImports)
            {
                try
                {
                    if (!File.Exists(source))
                    {
                        report.AppendLine("FAIL dialogue import: source missing: " + source);
                        ok = false;
                        continue;
                    }
                    bool needsCopy = !File.Exists(target) ||
                        !FilesEqual(source, target);
                    if (needsCopy)
                    {
                        string dir = Path.GetDirectoryName(target);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        File.Copy(source, target, true);
                        AssetDatabase.ImportAsset(target);
                        report.AppendLine("OK   imported " + Path.GetFileName(target));
                    }
                    else
                    {
                        AssetDatabase.ImportAsset(target);
                        report.AppendLine("OK   dialogue import up to date: " + Path.GetFileName(target));
                    }
                }
                catch (Exception e)
                {
                    report.AppendLine("FAIL dialogue import " + source + ": " + e.Message);
                    ok = false;
                }
            }
            return ok;
        }

        private static bool FilesEqual(string a, string b)
        {
            FileInfo fa = new FileInfo(a);
            FileInfo fb = new FileInfo(b);
            if (fa.Length != fb.Length) return false;
            using (FileStream sa = fa.OpenRead())
            using (FileStream sb = fb.OpenRead())
            {
                byte[] ba = new byte[4096];
                byte[] bb = new byte[4096];
                int ra;
                while ((ra = sa.Read(ba, 0, ba.Length)) > 0)
                {
                    if (sb.Read(bb, 0, bb.Length) != ra) return false;
                    for (int i = 0; i < ra; i++)
                    {
                        if (ba[i] != bb[i]) return false;
                    }
                }
                return sb.Read(bb, 0, 1) == 0;
            }
        }

        // ------------------------------------------------------------------
        // Tags & layers
        // ------------------------------------------------------------------
        public static bool EnsureTagsAndLayers(StringBuilder report)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                report.AppendLine("FAIL TagManager.asset not found.");
                return false;
            }
            SerializedObject tagManager = new SerializedObject(assets[0]);

            // Physics layers 8..13
            SerializedProperty layers = tagManager.FindProperty("layers");
            string[] wanted = { "Player", "WorldSolid", "Interactable", "Trigger", "Boat", "RiverHazard" };
            bool changed = false;
            for (int i = 0; i < wanted.Length; i++)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(DuzheLayers.Player + i);
                if (slot.stringValue != wanted[i])
                {
                    slot.stringValue = wanted[i];
                    changed = true;
                }
            }
            report.AppendLine("OK   physics layers 8-13 = " + string.Join(",", wanted));

            // Sorting layers
            SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");
            string[] wantedSorting = { DuzheSortingLayers.Background, DuzheSortingLayers.GroundProps,
                DuzheSortingLayers.Actors, DuzheSortingLayers.Foreground, DuzheSortingLayers.Effects };
            HashSet<string> existing = new HashSet<string>();
            for (int i = 0; i < sortingLayers.arraySize; i++)
            {
                existing.Add(sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
            }
            for (int i = 0; i < wantedSorting.Length; i++)
            {
                if (existing.Contains(wantedSorting[i])) continue;
                int idx = sortingLayers.arraySize;
                sortingLayers.arraySize = idx + 1;
                SerializedProperty entry = sortingLayers.GetArrayElementAtIndex(idx);
                entry.FindPropertyRelative("name").stringValue = wantedSorting[i];
                entry.FindPropertyRelative("uniqueID").intValue = UnityEngine.Random.Range(1000, int.MaxValue / 2);
                changed = true;
            }
            report.AppendLine("OK   sorting layers = " + string.Join(",", wantedSorting));

            if (changed) tagManager.ApplyModifiedProperties();
            return true;
        }

        // ------------------------------------------------------------------
        // Project settings
        // ------------------------------------------------------------------
        public static bool EnsureProjectSettings(StringBuilder report)
        {
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.runInBackground = false; // focus loss must pause
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; // visible 1280x720 window
            // C12: the player may resize the window; the runtime applies the
            // stored windowed/fullscreen + resolution choice on top of this.
            PlayerSettings.resizableWindow = true;
            report.AppendLine("OK   default window = 1280x720 windowed, resizable (runtime switch in Settings)");
            return true;
        }

        // ------------------------------------------------------------------
        // Placeholder assets (stable ids, real alpha PNG)
        // ------------------------------------------------------------------
        public static bool EnsurePlaceholderAssets(StringBuilder report)
        {
            bool ok = true;
            ok &= EnsureSprite(ArtRoot + "/Characters/Duke_Placeholder.png", 24, 32,
                tex => PaintCharacter(tex, new Color32(56, 78, 132, 255), new Color32(214, 186, 150, 255)), report);
            ok &= EnsureSprite(ArtRoot + "/Characters/Npc_Placeholder.png", 24, 32,
                tex => PaintCharacter(tex, new Color32(126, 106, 88, 255), new Color32(222, 198, 168, 255)), report);
            ok &= EnsureSprite(ArtRoot + "/Props/Item_Placeholder.png", 16, 16,
                tex => PaintGlasses(tex), report);
            ok &= EnsureSprite(ArtRoot + "/Props/Boat_Placeholder.png", 96, 64,
                tex => PaintBoat(tex), report);
            ok &= EnsureSprite(ArtRoot + "/Props/Whirlpool_Placeholder.png", 32, 32,
                tex => PaintWhirlpool(tex), report);
            ok &= EnsureSprite(ArtRoot + "/Props/RitualAnchor_Placeholder.png", 16, 24,
                tex => PaintStoneLantern(tex), report);
            ok &= EnsureSprite(ArtRoot + "/Props/Stool_Placeholder.png", 32, 32,
                tex => PaintStool(tex), report);
            ok &= EnsureSprite(ArtRoot + "/Props/Cane_Placeholder.png", 16, 32,
                tex => PaintCane(tex), report);
            ok &= EnsureSprite(ArtRoot + "/Props/Door_Placeholder.png", 48, 96,
                tex => PaintDoor(tex), report);
            ok &= EnsureSprite(ArtRoot + "/Props/DiningTable_Placeholder.png", 96, 64,
                tex => PaintDiningTable(tex), report);
            ok &= EnsureSprite(ArtRoot + "/Backgrounds/BG_Underworld_Placeholder.png", 1280, 720,
                tex => PaintBackground(tex, new Color32(52, 36, 72, 255), new Color32(22, 14, 34, 255), new Color32(40, 28, 52, 255)), report);
            ok &= EnsureSprite(ArtRoot + "/Backgrounds/BG_River_Placeholder.png", 1280, 720,
                tex => PaintBackground(tex, new Color32(26, 46, 74, 255), new Color32(10, 18, 34, 255), new Color32(20, 44, 80, 255)), report);
            ok &= EnsureSprite(ArtRoot + "/Backgrounds/BG_FarmCourtyard_Placeholder.png", 1280, 720,
                tex => PaintBackground(tex, new Color32(104, 86, 60, 255), new Color32(50, 42, 34, 255), new Color32(74, 62, 42, 255)), report);
            ok &= EnsureSprite(ArtRoot + "/Backgrounds/BG_Park_Placeholder.png", 1280, 720,
                tex => PaintBackground(tex, new Color32(50, 92, 50, 255), new Color32(24, 48, 28, 255), new Color32(62, 86, 46, 255)), report);
            ok &= EnsureSprite(ArtRoot + "/Backgrounds/BG_RecordingStudio_Placeholder.png", 1280, 720,
                tex => PaintBackground(tex, new Color32(66, 58, 82, 255), new Color32(32, 28, 40, 255), new Color32(50, 44, 58, 255)), report);
            ok &= EnsureSprite(ArtRoot + "/Backgrounds/BG_Classroom_Placeholder.png", 1280, 720,
                tex => PaintBackground(tex, new Color32(74, 82, 98, 255), new Color32(40, 44, 56, 255), new Color32(98, 90, 74, 255)), report);
            ok &= EnsureSprite(ArtRoot + "/Backgrounds/BG_MainMenu_Placeholder.png", 1280, 720,
                tex => PaintBackground(tex, new Color32(88, 76, 120, 255), new Color32(40, 34, 58, 255), new Color32(58, 50, 78, 255)), report);
            return ok;
        }

        private static bool EnsureSprite(string path, int w, int h, Action<Texture2D> paint, StringBuilder report)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                if (!File.Exists(path))
                {
                    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    paint(tex);
                    tex.Apply();
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(tex);
                    AssetDatabase.ImportAsset(path);
                    report.AppendLine("OK   created placeholder " + Path.GetFileName(path));
                }
                else
                {
                    AssetDatabase.ImportAsset(path);
                }
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    report.AppendLine("FAIL " + path + ": importer unavailable.");
                    return false;
                }
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (Mathf.Abs(importer.spritePixelsPerUnit - GameBootstrap.PixelsPerUnit) > 0.01f)
                {
                    importer.spritePixelsPerUnit = GameBootstrap.PixelsPerUnit; dirty = true;
                }
                if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
                if (w > 2048 || h > 2048) { if (importer.maxTextureSize != 4096) { importer.maxTextureSize = 4096; dirty = true; } }
                if (dirty)
                {
                    importer.SaveAndReimport();
                }
                return true;
            }
            catch (Exception e)
            {
                report.AppendLine("FAIL " + path + ": " + e.Message);
                return false;
            }
        }

        // --- pixel painters ------------------------------------------------

        private static void PaintCharacter(Texture2D tex, Color body, Color head)
        {
            int w = tex.width, h = tex.height;
            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            // body: x 5..18, y 4..23 ; head: x 7..16, y 24..31 (y from bottom)
            Fill(px, w, 5, 4, 14, 20, body);
            Fill(px, w, 7, 24, 10, 8, head);
            // hood edge / shadow
            Fill(px, w, 5, 4, 14, 2, new Color(body.r * 0.6f, body.g * 0.6f, body.b * 0.6f, 1f));
            tex.SetPixels(px);
        }

        private static void PaintGlasses(Texture2D tex)
        {
            int w = tex.width, h = tex.height;
            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            Color gold = new Color32(212, 178, 74, 255);
            // two lenses + bridge (y from bottom)
            Frame(px, w, 2, 5, 5, 6, gold);
            Frame(px, w, 9, 5, 5, 6, gold);
            Fill(px, w, 6, 7, 4, 2, gold);
            tex.SetPixels(px);
        }

        private static void PaintBoat(Texture2D tex)
        {
            // Top-down 2D ferry, 96x64 px (= 3.0x2.0 world units at 32 PPU),
            // matching the formal PRP01_Boat 96x64 spec so real art drops in
            // at the same scale. The old 40x24 stub was too small to read as
            // a boat once passengers stood on it.
            int w = tex.width, h = tex.height;
            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            Color hull = new Color32(120, 92, 62, 255);
            Color hullDark = new Color32(86, 64, 42, 255);
            Color rim = new Color32(160, 132, 96, 255);
            Color deck = new Color32(139, 108, 74, 255);
            Color lantern = new Color32(220, 190, 110, 255);

            // Hull: rounded block stern (bottom), tapered bow (top) - the
            // boat points up, the direction of travel.
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                float halfW;
                if (t < 0.25f) halfW = Mathf.Lerp(30f, 34f, t / 0.25f);
                else if (t < 0.75f) halfW = Mathf.Lerp(34f, 26f, (t - 0.25f) / 0.5f);
                else halfW = Mathf.Lerp(26f, 8f, (t - 0.75f) / 0.25f);
                for (int x = 48 - (int)halfW; x < 48 + (int)halfW; x++)
                {
                    bool edge = x < 48 - (int)halfW + 2 || x >= 48 + (int)halfW - 2 || y < 2 || y > h - 3;
                    px[y * w + x] = edge ? rim : hull;
                }
            }
            // Inner deck
            for (int y = 5; y < h - 6; y++)
            {
                float t = y / (float)(h - 1);
                float halfW = t < 0.25f ? 26f : (t < 0.75f ? Mathf.Lerp(30f, 22f, (t - 0.25f) / 0.5f) : Mathf.Lerp(22f, 5f, (t - 0.75f) / 0.25f));
                for (int x = 48 - (int)halfW; x < 48 + (int)halfW; x++)
                {
                    px[y * w + x] = deck;
                }
            }
            // Bench planks at the stern (seats for the passengers)
            for (int y = 8; y < 14; y++) for (int x = 24; x < 72; x++) px[y * w + x] = hullDark;
            for (int y = 17; y < 21; y++) for (int x = 26; x < 70; x++) px[y * w + x] = hullDark;
            // Lantern glow at the bow
            for (int y = 52; y < 60; y++) for (int x = 45; x < 51; x++) px[y * w + x] = lantern;
            tex.SetPixels(px);
        }

        private static void PaintWhirlpool(Texture2D tex)
        {
            int w = tex.width, h = tex.height;
            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            Color ring = new Color32(96, 128, 168, 255);
            Color ringDark = new Color32(56, 78, 108, 255);
            int cx = w / 2, cy = h / 2;
            // Spiral rings; visual silhouette slightly larger than the
            // collision box the River rules assume.
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx + 0.5f, dy = y - cy + 0.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Atan2(dy, dx);
                    if (r <= 14.5f)
                    {
                        float spiral = Mathf.Sin((a + r * 0.8f) * 2f);
                        if (r > 12.5f) px[y * w + x] = ring;
                        else if (spiral > 0.55f && r > 3f) px[y * w + x] = ring;
                        else if (r <= 3f) px[y * w + x] = ringDark;
                    }
                }
            }
            tex.SetPixels(px);
        }

        private static void PaintStool(Texture2D tex)
        {
            int w = tex.width, h = tex.height;
            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            Color wood = new Color32(139, 108, 74, 255);
            Color woodDark = new Color32(96, 72, 48, 255);
            // Stool: round-ish seat on four legs (y from bottom).
            Fill(px, w, 4, 18, 24, 7, wood);
            Fill(px, w, 4, 18, 24, 2, woodDark);
            Fill(px, w, 6, 11, 5, 8, woodDark);
            Fill(px, w, 21, 11, 5, 8, woodDark);
            tex.SetPixels(px);
        }

        private static void PaintCane(Texture2D tex)
        {
            int w = tex.width, h = tex.height;
            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            Color wood = new Color32(150, 112, 70, 255);
            Color woodDark = new Color32(104, 76, 46, 255);
            // Vertical shaft (y from bottom), then a hooked handle curving
            // right and down - the silhouette the MG01 trace produces.
            for (int y = 0; y <= 22; y++) px[y * w + 7] = wood;
            for (int y = 0; y <= 22; y++) px[y * w + 8] = woodDark;
            // Hook: right arm + down curve.
            for (int x = 9; x <= 12; x++) px[24 * w + x] = wood;
            px[23 * w + 12] = wood;
            px[22 * w + 12] = woodDark;
            px[21 * w + 12] = woodDark;
            px[21 * w + 11] = wood;
            tex.SetPixels(px);
        }

        private static void PaintDoor(Texture2D tex)
        {
            int w = tex.width, h = tex.height;
            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            Color frame = new Color32(70, 52, 36, 255);
            Color leaf = new Color32(104, 78, 52, 255);
            Color leafInner = new Color32(120, 92, 62, 255);
            Color knocker = new Color32(200, 170, 110, 255);
            // Frame + leaf + inner panel (y from bottom), knocker dot on the
            // right side matching the MG02 close-up's knocker placement.
            Fill(px, w, 0, 0, 48, 96, frame);
            Fill(px, w, 4, 4, 40, 90, leaf);
            Fill(px, w, 8, 10, 32, 74, leafInner);
            Fill(px, w, 34, 52, 5, 5, knocker);
            // Threshold plank.
            Fill(px, w, 2, 0, 44, 3, new Color32(58, 42, 30, 255));
            tex.SetPixels(px);
        }

        private static void PaintDiningTable(Texture2D tex)
        {
            int w = tex.width, h = tex.height;
            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            Color cloth = new Color32(139, 108, 74, 255);
            Color clothDark = new Color32(96, 72, 48, 255);
            Color dish = new Color32(212, 198, 168, 255);
            // Tabletop (3/4 view) with dishes (y from bottom).
            Fill(px, w, 4, 22, 88, 34, cloth);
            Fill(px, w, 4, 22, 88, 4, clothDark);
            Fill(px, w, 0, 26, 96, 22, cloth);   // near edge (wider)
            Fill(px, w, 0, 26, 96, 3, clothDark);
            // Three dishes.
            Fill(px, w, 18, 40, 16, 8, dish);
            Fill(px, w, 42, 44, 16, 8, dish);
            Fill(px, w, 64, 40, 16, 8, dish);
            tex.SetPixels(px);
        }

        private static void PaintStoneLantern(Texture2D tex)
        {
            int w = tex.width, h = tex.height;
            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            Color stone = new Color32(150, 150, 160, 255);
            Color stoneDark = new Color32(104, 104, 116, 255);
            Color glow = new Color32(235, 210, 140, 255);
            // Stone lantern: cap, body, base (y from bottom).
            Fill(px, w, 2, 0, 12, 3, stoneDark);        // base
            Fill(px, w, 4, 2, 8, 14, stone);            // pillar
            Fill(px, w, 5, 16, 6, 4, glow);             // light window
            Fill(px, w, 3, 20, 10, 3, stone);           // roof
            Fill(px, w, 5, 23, 6, 1, stoneDark);        // top knob
            tex.SetPixels(px);
        }

        private static void PaintBackground(Texture2D tex, Color top, Color bottom, Color ground)
        {
            int w = tex.width, h = tex.height;
            Color[] px = new Color[w * h];
            int groundHeight = 96;
            for (int y = 0; y < h; y++)
            {
                Color c;
                if (y < groundHeight) c = ground;
                else
                {
                    float t = (float)(y - groundHeight) / (h - groundHeight);
                    c = Color.Lerp(bottom, top, t);
                }
                for (int x = 0; x < w; x++)
                {
                    bool border = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                    px[y * w + x] = border ? new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 1f) : c;
                }
            }
            // ground line
            for (int x = 0; x < w; x++)
            {
                int y = groundHeight;
                px[y * w + x] = new Color(ground.r * 1.4f, ground.g * 1.4f, ground.b * 1.4f, 1f);
            }
            tex.SetPixels(px);
        }

        private static void Fill(Color[] px, int w, int x0, int y0, int rw, int rh, Color c)
        {
            for (int y = y0; y < y0 + rh; y++)
            {
                for (int x = x0; x < x0 + rw; x++)
                {
                    px[y * w + x] = c;
                }
            }
        }

        private static void Frame(Color[] px, int w, int x0, int y0, int rw, int rh, Color c)
        {
            for (int x = x0; x < x0 + rw; x++)
            {
                px[y0 * w + x] = c;
                px[(y0 + rh - 1) * w + x] = c;
            }
            for (int y = y0; y < y0 + rh; y++)
            {
                px[y * w + x0] = c;
                px[y * w + x0 + rw - 1] = c;
            }
        }

        // ------------------------------------------------------------------
        // Scenes
        // ------------------------------------------------------------------
        public static bool EnsureScenes(StringBuilder report)
        {
            bool allOk = true;
            if (!Directory.Exists(ScenesDir)) Directory.CreateDirectory(ScenesDir);
            foreach ((string sceneId, bool withNpcAndItem) in ScenePlan)
            {
                bool ok = EnsureOneScene(sceneId, withNpcAndItem, report);
                if (!ok) allOk = false;
            }
            return allOk;
        }

        private static bool EnsureOneScene(string sceneId, bool withNpcAndItem, StringBuilder report)
        {
            string path = ScenesDir + "/" + sceneId + ".unity";
            bool created = false;
            Scene scene;
            if (File.Exists(path))
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                created = true;
            }

            bool changed = created;
            bool ok = true;

            GameObject root = FindOrCreate("SceneRoot", ref changed);
            SceneIdentity identity = root.GetComponent<SceneIdentity>();
            if (identity == null) { identity = root.AddComponent<SceneIdentity>(); changed = true; }
            if (identity.sceneId != sceneId) { identity.sceneId = sceneId; changed = true; }

            SceneContent content = root.GetComponent<SceneContent>();
            if (content == null) { content = root.AddComponent<SceneContent>(); changed = true; }

            // Background
            Sprite bgSprite = LoadSprite(BackgroundPathFor(sceneId));
            if (bgSprite == null) bgSprite = LoadSprite("Art/Backgrounds/BG_" + sceneId + "_Placeholder");
            GameObject bgGo = FindOrCreate("Background", ref changed);
            SpriteRenderer bgSr = bgGo.GetComponent<SpriteRenderer>();
            if (bgSr == null) { bgSr = bgGo.AddComponent<SpriteRenderer>(); changed = true; }
            if (bgSr.sprite != bgSprite) { bgSr.sprite = bgSprite; changed = true; }
            if (bgSr.sortingLayerName != DuzheSortingLayers.Background) { bgSr.sortingLayerName = DuzheSortingLayers.Background; changed = true; }
            if (bgSr.transform.position != Vector3.zero) { bgSr.transform.position = Vector3.zero; changed = true; }
            if (content.backgroundRenderer != bgSr) { content.backgroundRenderer = bgSr; changed = true; }

            // World walls (game scenes only; the menu has no player)
            if (sceneId != DuzheIds.SceneMainMenu)
            {
                ok &= EnsureWalls(root.transform, bgSr, ref changed, report);
            }

            // Spawn point
            if (sceneId != DuzheIds.SceneMainMenu)
            {
                Vector3 spawnPos = new Vector3(0f, -6f, 0f);
                foreach ((string planScene, Vector3 planPos) in SpawnPlan)
                {
                    if (planScene == sceneId) { spawnPos = planPos; break; }
                }
                Transform spawns = FindOrCreate("SpawnPoints", ref changed).transform;
                Transform spawn = spawns.Find("SP_Main");
                if (spawn == null)
                {
                    GameObject sp = new GameObject("SP_Main");
                    sp.transform.SetParent(spawns, false);
                    spawn = sp.transform;
                    changed = true;
                }
                if (spawn.transform.position != spawnPos) { spawn.transform.position = spawnPos; changed = true; }
                List<Transform> spawnList = new List<Transform>(spawn.GetComponentsInChildren<Transform>(true));
                spawnList.Remove(spawn);
                spawnList.Insert(0, spawn);
                if (content.spawnPoints == null || content.spawnPoints.Length != spawnList.Count)
                {
                    content.spawnPoints = spawnList.ToArray();
                    changed = true;
                }
                else
                {
                    for (int i = 0; i < spawnList.Count; i++)
                    {
                        if (content.spawnPoints[i] != spawnList[i]) { content.spawnPoints = spawnList.ToArray(); changed = true; break; }
                    }
                }
            }

            // Underworld dev-test NPC + item
            if (withNpcAndItem)
            {
                ok &= EnsureUnderworldNpcs(root.transform, content, ref changed, report);
                ok &= EnsureUnderworldRitualArea(root.transform, ref changed, report);
                ok &= EnsureUnderworldCh01(root.transform, ref changed, report);
                ok &= EnsureUnderworldCh02(root.transform, ref changed, report);
                ok &= EnsureUnderworldCh03(root.transform, ref changed, report);
                ok &= EnsureUnderworldCh04(root.transform, ref changed, report);
                ok &= EnsureUnderworldCh05(root.transform, ref changed, report);
            }

            // River crossing assembly (boat + controllers)
            if (sceneId == DuzheIds.SceneRiver)
            {
                ok &= EnsureRiverCrossing(root.transform, ref changed, report);
                ok &= EnsureRiverCh05(root.transform, ref changed, report);
            }

            // FarmCourtyard CH01 assembly (R04 layout).
            if (sceneId == DuzheIds.SceneFarmCourtyard)
            {
                ok &= EnsureFarmCourtyardCh01(root.transform, ref changed, report);
            }

            // Park CH02 assembly (R04 layout).
            if (sceneId == DuzheIds.ScenePark)
            {
                ok &= EnsureParkCh02(root.transform, ref changed, report);
            }

            // RecordingStudio CH03 assembly (R09 BG05 anchors).
            if (sceneId == DuzheIds.SceneRecordingStudio)
            {
                ok &= EnsureRecordingStudioCh03(root.transform, ref changed, report);
            }

            // Classroom CH04 assembly (R04 Classroom layout).
            if (sceneId == DuzheIds.SceneClassroom)
            {
                ok &= EnsureClassroomCh04(root.transform, ref changed, report);
            }

            // Refresh interactable list reference
            Interactable[] interactables = root.GetComponentsInChildren<Interactable>(true);
            if (content.interactables == null || content.interactables.Length != interactables.Length)
            {
                content.interactables = interactables;
                changed = true;
            }
            else
            {
                for (int i = 0; i < interactables.Length; i++)
                {
                    if (content.interactables[i] != interactables[i]) { content.interactables = interactables; changed = true; break; }
                }
            }

            // World-space guidance markers on EVERY interactable: a gold "!"
            // bobbing above each actionable point, visible from anywhere in
            // the camera view (the old E-prompt only showed within 1.2u).
            foreach (Interactable it in interactables)
            {
                if (it.GetComponent<Interaction.InteractableHintMarker>() == null)
                {
                    it.gameObject.AddComponent<Interaction.InteractableHintMarker>();
                    changed = true;
                }
            }

            if (changed)
            {
                if (!EditorSceneManager.SaveScene(scene, path))
                {
                    report.AppendLine("FAIL scene " + sceneId + ": could not save to " + path);
                    return false;
                }
                report.AppendLine("OK   " + (created ? "created" : "updated") + " scene " + sceneId);
            }
            else
            {
                report.AppendLine("OK   scene " + sceneId + " already complete");
            }
            return ok;
        }

        private static bool EnsureWalls(Transform parent, SpriteRenderer bg, ref bool changed, StringBuilder report)
        {
            // Background is 1280x720 px at 32 PPU = 40 x 22.5 units centred on
            // the world origin. Walls sit just outside so the player can never
            // leave the art bounds.
            Bounds b = bg != null && bg.sprite != null
                ? bg.bounds
                : new Bounds(Vector3.zero, new Vector3(40f, 22.5f, 1f));
            bool ok = true;
            ok &= EnsureWall(parent, "Wall_Top", new Vector2(0f, b.max.y + 1f), new Vector2(b.size.x + 4f, 2f), ref changed, report);
            ok &= EnsureWall(parent, "Wall_Bottom", new Vector2(0f, b.min.y - 1f), new Vector2(b.size.x + 4f, 2f), ref changed, report);
            ok &= EnsureWall(parent, "Wall_Left", new Vector2(b.min.x - 1f, 0f), new Vector2(2f, b.size.y + 4f), ref changed, report);
            ok &= EnsureWall(parent, "Wall_Right", new Vector2(b.max.x + 1f, 0f), new Vector2(2f, b.size.y + 4f), ref changed, report);
            return ok;
        }

        private static bool EnsureWall(Transform parent, string name, Vector2 pos, Vector2 size, ref bool changed, StringBuilder report)
        {
            GameObject go = FindOrCreate(name, ref changed, parent);
            BoxCollider2D col = go.GetComponent<BoxCollider2D>();
            if (col == null) { col = go.AddComponent<BoxCollider2D>(); changed = true; }
            if (go.layer != DuzheLayers.WorldSolid) { go.layer = DuzheLayers.WorldSolid; changed = true; }
            if (col.isTrigger) { col.isTrigger = false; changed = true; }
            if (col.size != size) { col.size = size; changed = true; }
            Vector3 want = new Vector3(pos.x, pos.y, 0f);
            if (go.transform.position != want) { go.transform.position = want; changed = true; }
            return true;
        }

        private static bool EnsureUnderworldNpcs(Transform parent, SceneContent content, ref bool changed, StringBuilder report)
        {
            bool ok = true;
            // NPC: grandpa placeholder. Active in the intro stage (auto-talk)
            // and in the return stage (hand over the glasses).
            Sprite npcSprite = LoadSprite("Art/Characters/Npc_Placeholder");
            GameObject npc = FindOrCreate("NPC_DEV_GRANDPA", ref changed, parent);
            Interactable npcInt = npc.GetComponent<Interactable>();
            if (npcInt == null) { npcInt = npc.AddComponent<Interactable>(); changed = true; }
            if (npcInt.kind != InteractableKind.Npc) { npcInt.kind = InteractableKind.Npc; changed = true; }
            if (npcInt.interactableId != "DEV_GRANDPA") { npcInt.interactableId = "DEV_GRANDPA"; changed = true; }
            if (npcInt.displayName != "老人（Placeholder）") { npcInt.displayName = "老人（Placeholder）"; changed = true; }
            if (npcInt.autoTalkSegmentId != "ST01_INTRO") { npcInt.autoTalkSegmentId = "ST01_INTRO"; changed = true; }
            if (npcInt.returnItemId != "DEV_GLASSES") { npcInt.returnItemId = "DEV_GLASSES"; changed = true; }
            if (npcInt.returnSegmentId != "ST03_THANKS") { npcInt.returnSegmentId = "ST03_THANKS"; changed = true; }
            if (!StringArrayEqual(npcInt.activeStageIds, new[] { "ST01_INTRO", "ST03_RETURN" }))
            { npcInt.activeStageIds = new[] { "ST01_INTRO", "ST03_RETURN" }; changed = true; }

            SpriteRenderer npcSr = npc.GetComponent<SpriteRenderer>();
            if (npcSr == null) { npcSr = npc.AddComponent<SpriteRenderer>(); changed = true; }
            if (npcSr.sprite != npcSprite) { npcSr.sprite = npcSprite; changed = true; }
            if (npcSr.sortingLayerName != DuzheSortingLayers.Actors) { npcSr.sortingLayerName = DuzheSortingLayers.Actors; changed = true; }
            Vector3 npcPos = new Vector3(2f, -2f, 0f);
            if (npc.transform.position != npcPos) { npc.transform.position = npcPos; changed = true; }
            if (npc.layer != DuzheLayers.Trigger) { npc.layer = DuzheLayers.Trigger; changed = true; }
            BoxCollider2D npcCol = npc.GetComponent<BoxCollider2D>();
            if (npcCol == null) { npcCol = npc.AddComponent<BoxCollider2D>(); changed = true; }
            if (!npcCol.isTrigger) { npcCol.isTrigger = true; changed = true; }
            if (npcCol.size != new Vector2(0.75f, 1f)) { npcCol.size = new Vector2(0.75f, 1f); changed = true; }
            if (npcCol.offset != new Vector2(0f, 0.5f)) { npcCol.offset = new Vector2(0f, 0.5f); changed = true; }

            // Item: glasses placeholder, pickup only in ST02_FIND.
            Sprite itemSprite = LoadSprite("Art/Props/Item_Placeholder");
            GameObject item = FindOrCreate("Item_DEV_GLASSES", ref changed, parent);
            Interactable itemInt = item.GetComponent<Interactable>();
            if (itemInt == null) { itemInt = item.AddComponent<Interactable>(); changed = true; }
            if (itemInt.kind != InteractableKind.Item) { itemInt.kind = InteractableKind.Item; changed = true; }
            if (itemInt.interactableId != "DEV_GLASSES") { itemInt.interactableId = "DEV_GLASSES"; changed = true; }
            if (itemInt.displayName != "老人的眼镜（Placeholder）") { itemInt.displayName = "老人的眼镜（Placeholder）"; changed = true; }
            if (itemInt.chapterId != "CH01_DEV") { itemInt.chapterId = "CH01_DEV"; changed = true; }
            if (!StringArrayEqual(itemInt.activeStageIds, new[] { "ST02_FIND" }))
            { itemInt.activeStageIds = new[] { "ST02_FIND" }; changed = true; }

            SpriteRenderer itemSr = item.GetComponent<SpriteRenderer>();
            if (itemSr == null) { itemSr = item.AddComponent<SpriteRenderer>(); changed = true; }
            if (itemSr.sprite != itemSprite) { itemSr.sprite = itemSprite; changed = true; }
            if (itemSr.sortingLayerName != DuzheSortingLayers.Actors) { itemSr.sortingLayerName = DuzheSortingLayers.Actors; changed = true; }
            Vector3 itemPos = new Vector3(-4f, 1f, 0f);
            if (item.transform.position != itemPos) { item.transform.position = itemPos; changed = true; }
            if (item.layer != DuzheLayers.Trigger) { item.layer = DuzheLayers.Trigger; changed = true; }
            BoxCollider2D itemCol = item.GetComponent<BoxCollider2D>();
            if (itemCol == null) { itemCol = item.AddComponent<BoxCollider2D>(); changed = true; }
            if (!itemCol.isTrigger) { itemCol.isTrigger = true; changed = true; }
            if (itemCol.size != new Vector2(0.5f, 0.5f)) { itemCol.size = new Vector2(0.5f, 0.5f); changed = true; }
            if (itemCol.offset != Vector2.zero) { itemCol.offset = Vector2.zero; changed = true; }
            // DEV route objects are hidden while a formal chapter is active
            // (they share the layout points with the CH01 objects).
            EnsureRouteGate(npc, "CH01_DEV", ref changed);
            EnsureRouteGate(item, "CH01_DEV", ref changed);
            return ok;
        }

        // ------------------------------------------------------------------
        // C02: ritual area (Underworld) + river crossing (River scene)
        // ------------------------------------------------------------------

        private static bool EnsureUnderworldRitualArea(Transform parent, ref bool changed, StringBuilder report)
        {
            bool ok = true;

            // Ritual controller on the scene root (idempotent).
            Ritual.RitualController rc = parent.GetComponent<Ritual.RitualController>();
            if (rc == null) { rc = parent.gameObject.AddComponent<Ritual.RitualController>(); changed = true; }

            // Ritual anchor: stone lantern prop, E starts the farewell ritual.
            Sprite lantern = LoadSprite("Art/Props/RitualAnchor_Placeholder");
            GameObject anchor = FindOrCreateChild(parent, "RitualAnchor_DEV", ref changed);
            Interactable anchorInt = anchor.GetComponent<Interactable>();
            if (anchorInt == null) { anchorInt = anchor.AddComponent<Interactable>(); changed = true; }
            if (anchorInt.kind != InteractableKind.Prop) { anchorInt.kind = InteractableKind.Prop; changed = true; }
            if (anchorInt.interactableId != "DEV_RITUAL_ANCHOR") { anchorInt.interactableId = "DEV_RITUAL_ANCHOR"; changed = true; }
            if (anchorInt.displayName != "送别仪式（Placeholder）") { anchorInt.displayName = "送别仪式（Placeholder）"; changed = true; }
            if (!StringArrayEqual(anchorInt.activeStageIds, new[] { "ST04_RITUAL" }))
            { anchorInt.activeStageIds = new[] { "ST04_RITUAL" }; changed = true; }
            if (anchor.GetComponent<Ritual.RitualTrigger>() == null) { anchor.AddComponent<Ritual.RitualTrigger>(); changed = true; }
            Vector3 anchorPos = new Vector3(-2f, -5f, 0f);
            if (anchor.transform.position != anchorPos) { anchor.transform.position = anchorPos; changed = true; }
            if (anchor.layer != DuzheLayers.Trigger) { anchor.layer = DuzheLayers.Trigger; changed = true; }
            BoxCollider2D anchorCol = anchor.GetComponent<BoxCollider2D>();
            if (anchorCol == null) { anchorCol = anchor.AddComponent<BoxCollider2D>(); changed = true; }
            if (!anchorCol.isTrigger) { anchorCol.isTrigger = true; changed = true; }
            if (anchorCol.size != new Vector2(0.7f, 0.9f)) { anchorCol.size = new Vector2(0.7f, 0.9f); changed = true; }
            if (anchorCol.offset != new Vector2(0f, 0.45f)) { anchorCol.offset = new Vector2(0f, 0.45f); changed = true; }
            SpriteRenderer anchorSr = anchor.GetComponent<SpriteRenderer>();
            if (anchorSr == null) { anchorSr = anchor.AddComponent<SpriteRenderer>(); changed = true; }
            if (anchorSr.sprite != lantern) { anchorSr.sprite = lantern; changed = true; }
            if (anchorSr.sortingLayerName != DuzheSortingLayers.Actors) { anchorSr.sortingLayerName = DuzheSortingLayers.Actors; changed = true; }

            // Boarding spot: the docked boat, E boards it (stage gated).
            Sprite boatSprite = LoadFormalOrPlaceholder("Art/Props/PRP01_Boat", "Art/Props/Boat_Placeholder");
            GameObject dock = FindOrCreateChild(parent, "BoatDock_DEV", ref changed);
            Interactable dockInt = dock.GetComponent<Interactable>();
            if (dockInt == null) { dockInt = dock.AddComponent<Interactable>(); changed = true; }
            if (dockInt.kind != InteractableKind.Prop) { dockInt.kind = InteractableKind.Prop; changed = true; }
            if (dockInt.interactableId != "DEV_BOAT") { dockInt.interactableId = "DEV_BOAT"; changed = true; }
            if (dockInt.displayName != "渡船（Placeholder）") { dockInt.displayName = "渡船（Placeholder）"; changed = true; }
            if (!StringArrayEqual(dockInt.activeStageIds, new[] { "ST05_BOARD" }))
            { dockInt.activeStageIds = new[] { "ST05_BOARD" }; changed = true; }
            if (dock.GetComponent<Ritual.BoardingSpot>() == null) { dock.AddComponent<Ritual.BoardingSpot>(); changed = true; }
            Vector3 dockPos = new Vector3(2f, -6.2f, 0f);
            if (dock.transform.position != dockPos) { dock.transform.position = dockPos; changed = true; }
            if (dock.layer != DuzheLayers.Trigger) { dock.layer = DuzheLayers.Trigger; changed = true; }
            BoxCollider2D dockCol = dock.GetComponent<BoxCollider2D>();
            if (dockCol == null) { dockCol = dock.AddComponent<BoxCollider2D>(); changed = true; }
            if (!dockCol.isTrigger) { dockCol.isTrigger = true; changed = true; }
            if (dockCol.size != new Vector2(1.6f, 0.8f)) { dockCol.size = new Vector2(1.6f, 0.8f); changed = true; }
            SpriteRenderer dockSr = dock.GetComponent<SpriteRenderer>();
            if (dockSr == null) { dockSr = dock.AddComponent<SpriteRenderer>(); changed = true; }
            if (dockSr.sprite != boatSprite) { dockSr.sprite = boatSprite; changed = true; }
            if (dockSr.sortingLayerName != DuzheSortingLayers.Actors) { dockSr.sortingLayerName = DuzheSortingLayers.Actors; changed = true; }

            // Spirit spots + two ghost passengers (hidden until the ritual).
            Transform spots = FindOrCreateChild(parent, "SpiritSpots", ref changed).transform;
            Transform spotA = spots.Find("SpiritSpotA");
            if (spotA == null)
            {
                GameObject s = new GameObject("SpiritSpotA");
                s.transform.SetParent(spots, false);
                s.transform.position = new Vector3(-1.2f, -4.6f, 0f);
                spotA = s.transform;
                changed = true;
            }
            Transform spotB = spots.Find("SpiritSpotB");
            if (spotB == null)
            {
                GameObject s = new GameObject("SpiritSpotB");
                s.transform.SetParent(spots, false);
                s.transform.position = new Vector3(-0.4f, -5.2f, 0f);
                spotB = s.transform;
                changed = true;
            }

            Sprite npcSprite = LoadSprite("Art/Characters/Npc_Placeholder");
            Ritual.SpiritEscort[] spirits = new Ritual.SpiritEscort[2];
            string[] spiritNames = { "SpiritA_DEV", "SpiritB_DEV" };
            Vector3[] spiritPos = { new Vector3(-1.2f, -4.6f, 0f), new Vector3(-0.4f, -5.2f, 0f) };
            for (int i = 0; i < 2; i++)
            {
                GameObject sp = FindOrCreateChild(parent, spiritNames[i], ref changed);
                Vector3 pos = spiritPos[i];
                if (sp.transform.position != pos) { sp.transform.position = pos; changed = true; }
                SpriteRenderer spSr = sp.GetComponent<SpriteRenderer>();
                if (spSr == null) { spSr = sp.AddComponent<SpriteRenderer>(); changed = true; }
                if (spSr.sprite != npcSprite) { spSr.sprite = npcSprite; changed = true; }
                if (spSr.sortingLayerName != DuzheSortingLayers.Actors) { spSr.sortingLayerName = DuzheSortingLayers.Actors; changed = true; }
                if (spSr.sortingOrder != 1) { spSr.sortingOrder = 1; changed = true; }
                spirits[i] = sp.GetComponent<Ritual.SpiritEscort>();
                if (spirits[i] == null) { spirits[i] = sp.AddComponent<Ritual.SpiritEscort>(); changed = true; }
                spirits[i].followOffset = new Vector2(i == 0 ? -0.65f : 0.65f, -0.2f);
                if (sp.activeSelf) { sp.SetActive(false); changed = true; } // hidden until the ritual
            }

            // Wire the ritual controller references.
            if (rc.ritualAnchorId != "DEV_RITUAL_ANCHOR") { rc.ritualAnchorId = "DEV_RITUAL_ANCHOR"; changed = true; }
            if (rc.spirits == null || rc.spirits.Length != 2 || rc.spirits[0] != spirits[0] || rc.spirits[1] != spirits[1])
            { rc.spirits = spirits; changed = true; }
            Transform[] spotArr = { spotA, spotB };
            if (rc.spiritSpots == null || rc.spiritSpots.Length != 2 || rc.spiritSpots[0] != spotA || rc.spiritSpots[1] != spotB)
            { rc.spiritSpots = spotArr; changed = true; }

            // The departed's standing body (the NPC the player talked to)
            // must vanish when the ritual starts; wire it into hideOnRitual.
            Transform npcT = parent.Find("NPC_DEV_GRANDPA");
            Interactable npcBody = npcT != null ? npcT.GetComponent<Interactable>() : null;
            if (npcBody != null)
            {
                if (rc.hideOnRitual == null || rc.hideOnRitual.Length != 1 || rc.hideOnRitual[0] != npcBody)
                { rc.hideOnRitual = new Interactable[] { npcBody }; changed = true; }
            }
            else
            {
                report.AppendLine("WARN Underworld: NPC_DEV_GRANDPA not found; ritual hide list empty.");
            }

            // DEV ritual objects also hide while a formal chapter is active.
            EnsureRouteGate(anchor, "CH01_DEV", ref changed);
            EnsureRouteGate(dock, "CH01_DEV", ref changed);
            EnsureRouteGate(parent.Find("SpiritA_DEV").gameObject, "CH01_DEV", ref changed);
            EnsureRouteGate(parent.Find("SpiritB_DEV").gameObject, "CH01_DEV", ref changed);
            report.AppendLine("OK   Underworld ritual area (anchor/boat/spirits) ensured");
            return ok;
        }

        // ------------------------------------------------------------------
        // C04: formal CH01 assembly in the Underworld. Positions are the R04
        // Underworld.layout.json points (which intentionally match the
        // accepted dev assembly) plus the new cane-drawing station:
        //   UW_Departed (704,424)px -> (2,-2)u  grandfather
        //   UW_Companion (748,424)px -> (3.375,-2)u  grandmother
        //   UW_Item (512,328)px -> (-4,1)u  glasses
        //   UW_CaneDrawing (480,400)px -> (-5,-1.25)u  cane station
        //   UW_Ritual (576,520)px -> (-2,-5)u  ritual anchor
        //   UW_Dock (704,558)px -> (2,-6.19)u ~ dev dock (2,-6.2)
        // ------------------------------------------------------------------
        private static bool EnsureUnderworldCh01(Transform parent, ref bool changed, StringBuilder report)
        {
            bool ok = true;
            Sprite npcSprite = LoadSprite("Art/Characters/Npc_Placeholder");
            Sprite itemSprite = LoadFormalOrPlaceholder("Art/Props/IT01_Glasses", "Art/Props/Item_Placeholder");
            Sprite caneSprite = LoadFormalOrPlaceholder("Art/Props/IT02_Cane", "Art/Props/Cane_Placeholder");
            Sprite lanternSprite = LoadFormalOrPlaceholder("Art/Props/PRP08_StoneLantern", "Art/Props/RitualAnchor_Placeholder");
            Sprite boatSprite = LoadFormalOrPlaceholder("Art/Props/PRP01_Boat", "Art/Props/Boat_Placeholder");
            // Formal R11 standing sprites (feet pivot), placeholder fallback.
            Sprite grandpaSprite = LoadSprite("Art/Characters/CHR02_Grandfather");
            if (grandpaSprite == null) grandpaSprite = npcSprite;
            Sprite grandmaSprite = LoadSprite("Art/Characters/CHR03_Grandmother");
            if (grandmaSprite == null) grandmaSprite = npcSprite;

            // Grandfather: auto-talk in the opening, hand-over point later.
            GameObject grandfather = FindOrCreateChild(parent, "NPC_CH01_GRANDFATHER", ref changed);
            Interactable gInt = grandfather.GetComponent<Interactable>();
            if (gInt == null) { gInt = grandfather.AddComponent<Interactable>(); changed = true; }
            if (gInt.kind != InteractableKind.Npc) { gInt.kind = InteractableKind.Npc; changed = true; }
            if (gInt.interactableId != "CH01_GRANDFATHER") { gInt.interactableId = "CH01_GRANDFATHER"; changed = true; }
            if (gInt.displayName != "爷爷") { gInt.displayName = "爷爷"; changed = true; }
            if (gInt.autoTalkSegmentId != "CH01_Opening") { gInt.autoTalkSegmentId = "CH01_Opening"; changed = true; }
            if (gInt.returnSegmentId != "") { gInt.returnSegmentId = ""; changed = true; }
            if (!StringArrayEqual(gInt.returnItemIds, new[] { "IT01_Glasses", "IT02_Cane" }))
            { gInt.returnItemIds = new[] { "IT01_Glasses", "IT02_Cane" }; changed = true; }
            if (!StringArrayEqual(gInt.activeStageIds, new[] { "CH01_Opening", "CH01_ReturnItems" }))
            { gInt.activeStageIds = new[] { "CH01_Opening", "CH01_ReturnItems" }; changed = true; }
            if (gInt.promptKey != "hint.return_items") { gInt.promptKey = "hint.return_items"; changed = true; }
            EnsureNpcBody(grandfather, grandpaSprite, new Vector3(2f, -2f, 0f), new Color(0.94f, 0.90f, 0.84f, 1f), ref changed);

            // Grandmother: scenery body next to the grandfather (also hidden
            // by the ritual via hideOnRitual, so she carries an Interactable).
            GameObject grandmother = FindOrCreateChild(parent, "NPC_CH01_GRANDMOTHER", ref changed);
            Interactable mInt = grandmother.GetComponent<Interactable>();
            if (mInt == null) { mInt = grandmother.AddComponent<Interactable>(); changed = true; }
            if (mInt.kind != InteractableKind.Npc) { mInt.kind = InteractableKind.Npc; changed = true; }
            if (mInt.interactableId != "CH01_GRANDMOTHER") { mInt.interactableId = "CH01_GRANDMOTHER"; changed = true; }
            if (mInt.displayName != "奶奶") { mInt.displayName = "奶奶"; changed = true; }
            if (!StringArrayEqual(mInt.returnItemIds, new string[0])) { mInt.returnItemIds = new string[0]; changed = true; }
            if (mInt.autoTalkSegmentId != "") { mInt.autoTalkSegmentId = ""; changed = true; }
            if (!StringArrayEqual(mInt.activeStageIds, new string[0])) { mInt.activeStageIds = new string[0]; changed = true; }
            EnsureNpcBody(grandmother, grandmaSprite, new Vector3(3.375f, -2f, 0f), new Color(1f, 0.95f, 0.9f, 1f), ref changed);

            // Glasses pickup (CH01_Prepare only).
            GameObject glasses = FindOrCreateChild(parent, "Item_CH01_GLASSES", ref changed);
            Interactable glInt = glasses.GetComponent<Interactable>();
            if (glInt == null) { glInt = glasses.AddComponent<Interactable>(); changed = true; }
            if (glInt.kind != InteractableKind.Item) { glInt.kind = InteractableKind.Item; changed = true; }
            if (glInt.interactableId != "IT01_Glasses") { glInt.interactableId = "IT01_Glasses"; changed = true; }
            if (glInt.displayName != "老花镜") { glInt.displayName = "老花镜"; changed = true; }
            if (glInt.chapterId != "CH01") { glInt.chapterId = "CH01"; changed = true; }
            if (glInt.promptKey != "hint.pickup") { glInt.promptKey = "hint.pickup"; changed = true; }
            if (!StringArrayEqual(glInt.activeStageIds, new[] { "CH01_Prepare" }))
            { glInt.activeStageIds = new[] { "CH01_Prepare" }; changed = true; }
            EnsureNpcBody(glasses, itemSprite, new Vector3(-4f, 1f, 0f), Color.white, ref changed, itemSize: true);

            // Cane drawing station: E opens MG01_Cane.
            Interactable caneInt = EnsureCh01Prop(parent, "Station_CH01_CANE", "CH01_CANE_STATION",
                "hint.cane_station", new[] { "CH01_Prepare" },
                new Vector3(-5f, -1.25f, 0f), new Vector2(1.2f, 1.2f), ref changed);
            if (caneInt.GetComponent<Minigames.MinigameLauncher>() == null)
            { caneInt.gameObject.AddComponent<Minigames.MinigameLauncher>(); changed = true; }
            Minigames.MinigameLauncher caneLauncher = caneInt.GetComponent<Minigames.MinigameLauncher>();
            if (caneLauncher != null && caneLauncher.minigameId != "MG01_Cane") { caneLauncher.minigameId = "MG01_Cane"; changed = true; }
            EnsurePropVisual(parent, "Visual_CH01_CANE", caneSprite, new Vector3(-5f, -1.05f, 0f),
                DuzheSortingLayers.GroundProps, 0.85f, ref changed);

            // Ritual anchor (E starts the CH01 farewell ritual).
            Interactable ritualInt = EnsureCh01Prop(parent, "RitualAnchor_CH01", "CH01_RITUAL",
                "hint.ritual", new[] { "CH01_Ritual" },
                new Vector3(-2f, -5f, 0f), new Vector2(0.7f, 0.9f), ref changed);
            if (ritualInt.GetComponent<Ritual.RitualTrigger>() == null)
            { ritualInt.gameObject.AddComponent<Ritual.RitualTrigger>(); changed = true; }
            EnsurePropVisual(parent, "Visual_CH01_RITUAL", lanternSprite, new Vector3(-2f, -4.78f, 0f),
                DuzheSortingLayers.Actors, 1f, ref changed);

            // Dock: E boards the boat.
            Interactable dockInt = EnsureCh01Prop(parent, "BoatDock_CH01", "CH01_BOAT",
                "hint.board", new[] { "CH01_Board" },
                new Vector3(2f, -6.2f, 0f), new Vector2(1.6f, 0.8f), ref changed);
            if (dockInt.GetComponent<Ritual.BoardingSpot>() == null)
            { dockInt.gameObject.AddComponent<Ritual.BoardingSpot>(); changed = true; }
            EnsurePropVisual(parent, "Visual_CH01_BOAT", boatSprite, new Vector3(2f, -6.2f, 0f),
                DuzheSortingLayers.Actors, 1f, ref changed);

            // CH01 ritual controller + two elder ghost escorts.
            GameObject ritualRoot = FindOrCreateChild(parent, "RitualRoot_CH01", ref changed);
            Ritual.RitualController rc = ritualRoot.GetComponent<Ritual.RitualController>();
            if (rc == null) { rc = ritualRoot.AddComponent<Ritual.RitualController>(); changed = true; }
            if (rc.ritualAnchorId != "CH01_RITUAL") { rc.ritualAnchorId = "CH01_RITUAL"; changed = true; }

            Transform spots = FindOrCreateChild(parent, "SpiritSpots_CH01", ref changed).transform;
            Transform spotA = spots.Find("SpiritSpot_CH01_A");
            if (spotA == null)
            {
                GameObject s = new GameObject("SpiritSpot_CH01_A");
                s.transform.SetParent(spots, false);
                spotA = s.transform;
                changed = true;
            }
            if (spotA.position != new Vector3(-2.6f, -4.4f, 0f)) { spotA.position = new Vector3(-2.6f, -4.4f, 0f); changed = true; }
            Transform spotB = spots.Find("SpiritSpot_CH01_B");
            if (spotB == null)
            {
                GameObject s = new GameObject("SpiritSpot_CH01_B");
                s.transform.SetParent(spots, false);
                spotB = s.transform;
                changed = true;
            }
            if (spotB.position != new Vector3(-1.5f, -5.3f, 0f)) { spotB.position = new Vector3(-1.5f, -5.3f, 0f); changed = true; }

            Ritual.SpiritEscort[] spirits = new Ritual.SpiritEscort[2];
            string[] ghostNames = { "Ghost_CH01_A", "Ghost_CH01_B" };
            Vector3[] ghostPos = { new Vector3(-2.6f, -4.4f, 0f), new Vector3(-1.5f, -5.3f, 0f) };
            Color[] ghostTint = { new Color(0.94f, 0.90f, 0.84f, 1f), new Color(1f, 0.95f, 0.9f, 1f) };
            Sprite[] ghostSprite = { grandpaSprite, grandmaSprite };
            for (int i = 0; i < 2; i++)
            {
                GameObject sp = FindOrCreateChild(parent, ghostNames[i], ref changed);
                if (sp.transform.position != ghostPos[i]) { sp.transform.position = ghostPos[i]; changed = true; }
                EnsureFeetVisual(sp, ghostSprite[i], ghostTint[i], 1, ref changed);
                EnsureNpcWalk(sp, i == 0 ? "CHR02_Grandfather" : "CHR03_Grandmother", ref changed);
                spirits[i] = sp.GetComponent<Ritual.SpiritEscort>();
                if (spirits[i] == null) { spirits[i] = sp.AddComponent<Ritual.SpiritEscort>(); changed = true; }
                spirits[i].followOffset = new Vector2(i == 0 ? -0.65f : 0.65f, -0.2f);
                if (sp.activeSelf) { sp.SetActive(false); changed = true; }
            }

            if (rc.spirits == null || rc.spirits.Length != 2 || rc.spirits[0] != spirits[0] || rc.spirits[1] != spirits[1])
            { rc.spirits = spirits; changed = true; }
            Transform[] spotArr = { spotA, spotB };
            if (rc.spiritSpots == null || rc.spiritSpots.Length != 2 || rc.spiritSpots[0] != spotA || rc.spiritSpots[1] != spotB)
            { rc.spiritSpots = spotArr; changed = true; }
            if (rc.hideOnRitual == null || rc.hideOnRitual.Length != 2 ||
                rc.hideOnRitual[0] != gInt || rc.hideOnRitual[1] != mInt)
            { rc.hideOnRitual = new Interactable[] { gInt, mInt }; changed = true; }

            // Route gates: these CH01 objects are visible only during CH01
            // (never while the DEV route is active). The cane station and its
            // visual also disappear once MG01_Cane has been completed.
            EnsureRouteGate(grandfather, "CH01", ref changed);
            EnsureRouteGate(grandmother, "CH01", ref changed);
            EnsureRouteGate(glasses, "CH01", ref changed);
            EnsureRouteGate(caneInt.gameObject, "CH01", ref changed);
            EnsureHideOnMinigame(caneInt.gameObject, "MG01_Cane", "CH01", ref changed);
            Transform caneVisualT = parent.Find("Visual_CH01_CANE");
            if (caneVisualT != null)
            {
                EnsureRouteGate(caneVisualT.gameObject, "CH01", ref changed);
                EnsureHideOnMinigame(caneVisualT.gameObject, "MG01_Cane", "CH01", ref changed);
            }
            EnsureRouteGate(ritualInt.gameObject, "CH01", ref changed);
            Transform ritualVisualT = parent.Find("Visual_CH01_RITUAL");
            if (ritualVisualT != null) EnsureRouteGate(ritualVisualT.gameObject, "CH01", ref changed);
            EnsureRouteGate(dockInt.gameObject, "CH01", ref changed);
            Transform dockVisualT = parent.Find("Visual_CH01_BOAT");
            if (dockVisualT != null) EnsureRouteGate(dockVisualT.gameObject, "CH01", ref changed);
            Transform ghostAT = parent.Find("Ghost_CH01_A");
            if (ghostAT != null) EnsureRouteGate(ghostAT.gameObject, "CH01", ref changed);
            Transform ghostBT = parent.Find("Ghost_CH01_B");
            if (ghostBT != null) EnsureRouteGate(ghostBT.gameObject, "CH01", ref changed);

            report.AppendLine("OK   Underworld CH01 assembly (elders/glasses/cane station/ritual/dock/ghosts) ensured");
            return ok;
        }

        // ------------------------------------------------------------------
        // C05: formal CH02 assembly in the Underworld. Positions are the R04
        // Underworld.layout.json points (shared with CH01):
        //   UW_Departed (704,424)px -> (2,-2)u  the girl (LoverSpirit)
        //   UW_Item (512,328)px -> (-4,1)u  the necklace
        //   UW_Ritual (576,520)px -> (-2,-5)u  ritual anchor
        //   UW_Dock (704,558)px -> (2,-6.2)u  dock
        // Route-visibility gates keep CH01 and CH02 objects mutually exclusive.
        // ------------------------------------------------------------------
        private static bool EnsureUnderworldCh02(Transform parent, ref bool changed, StringBuilder report)
        {
            bool ok = true;
            Sprite loverSprite = LoadSprite("Art/Characters/CHR05_LoverSpirit");
            if (loverSprite == null) loverSprite = LoadSprite("Art/Characters/Npc_Placeholder");
            Sprite necklaceSprite = LoadSprite("Art/Props/IT03_Necklace");
            if (necklaceSprite == null) necklaceSprite = LoadSprite("Art/Props/Item_Placeholder");
            Sprite lanternSprite = LoadFormalOrPlaceholder("Art/Props/PRP08_StoneLantern", "Art/Props/RitualAnchor_Placeholder");
            Sprite boatSprite = LoadFormalOrPlaceholder("Art/Props/PRP01_Boat", "Art/Props/Boat_Placeholder");

            // The girl: auto-talk in the opening, hand-over point later.
            GameObject lover = FindOrCreateChild(parent, "NPC_CH02_LOVER", ref changed);
            Interactable lInt = lover.GetComponent<Interactable>();
            if (lInt == null) { lInt = lover.AddComponent<Interactable>(); changed = true; }
            if (lInt.kind != InteractableKind.Npc) { lInt.kind = InteractableKind.Npc; changed = true; }
            if (lInt.interactableId != "CH02_LoverSpirit") { lInt.interactableId = "CH02_LoverSpirit"; changed = true; }
            if (lInt.displayName != "女孩") { lInt.displayName = "女孩"; changed = true; }
            if (lInt.autoTalkSegmentId != "CH02_Opening") { lInt.autoTalkSegmentId = "CH02_Opening"; changed = true; }
            if (lInt.returnSegmentId != "") { lInt.returnSegmentId = ""; changed = true; }
            if (!StringArrayEqual(lInt.returnItemIds, new[] { "IT03_Necklace" }))
            { lInt.returnItemIds = new[] { "IT03_Necklace" }; changed = true; }
            if (!StringArrayEqual(lInt.activeStageIds, new[] { "CH02_Opening", "CH02_ReturnNecklace" }))
            { lInt.activeStageIds = new[] { "CH02_Opening", "CH02_ReturnNecklace" }; changed = true; }
            if (lInt.promptKey != "hint.return_items") { lInt.promptKey = "hint.return_items"; changed = true; }
            EnsureNpcBody(lover, loverSprite, new Vector3(2f, -2f, 0f), new Color(1f, 0.94f, 0.96f, 1f), ref changed);

            // Necklace pickup (CH02_FindNecklace only).
            GameObject necklace = FindOrCreateChild(parent, "Item_CH02_NECKLACE", ref changed);
            Interactable nInt = necklace.GetComponent<Interactable>();
            if (nInt == null) { nInt = necklace.AddComponent<Interactable>(); changed = true; }
            if (nInt.kind != InteractableKind.Item) { nInt.kind = InteractableKind.Item; changed = true; }
            if (nInt.interactableId != "IT03_Necklace") { nInt.interactableId = "IT03_Necklace"; changed = true; }
            if (nInt.displayName != "情侣项链") { nInt.displayName = "情侣项链"; changed = true; }
            if (nInt.chapterId != "CH02") { nInt.chapterId = "CH02"; changed = true; }
            if (nInt.promptKey != "hint.pickup") { nInt.promptKey = "hint.pickup"; changed = true; }
            if (!StringArrayEqual(nInt.activeStageIds, new[] { "CH02_FindNecklace" }))
            { nInt.activeStageIds = new[] { "CH02_FindNecklace" }; changed = true; }
            EnsureNpcBody(necklace, necklaceSprite, new Vector3(-4f, 1f, 0f), Color.white, ref changed, itemSize: true);

            // Ritual anchor (E starts the CH02 farewell ritual).
            Interactable ritualInt = EnsureCh01Prop(parent, "RitualAnchor_CH02", "CH02_RITUAL",
                "hint.ritual", new[] { "CH02_Ritual" },
                new Vector3(-2f, -5f, 0f), new Vector2(0.7f, 0.9f), ref changed);
            if (ritualInt.GetComponent<Ritual.RitualTrigger>() == null)
            { ritualInt.gameObject.AddComponent<Ritual.RitualTrigger>(); changed = true; }
            EnsurePropVisual(parent, "Visual_CH02_RITUAL", lanternSprite, new Vector3(-2f, -4.78f, 0f),
                DuzheSortingLayers.Actors, 1f, ref changed);

            // Dock: E boards the boat.
            Interactable dockInt = EnsureCh01Prop(parent, "BoatDock_CH02", "CH02_BOAT",
                "hint.board", new[] { "CH02_Board" },
                new Vector3(2f, -6.2f, 0f), new Vector2(1.6f, 0.8f), ref changed);
            if (dockInt.GetComponent<Ritual.BoardingSpot>() == null)
            { dockInt.gameObject.AddComponent<Ritual.BoardingSpot>(); changed = true; }
            EnsurePropVisual(parent, "Visual_CH02_BOAT", boatSprite, new Vector3(2f, -6.2f, 0f),
                DuzheSortingLayers.Actors, 1f, ref changed);

            // CH02 ritual controller + one ghost escort.
            GameObject ritualRoot = FindOrCreateChild(parent, "RitualRoot_CH02", ref changed);
            Ritual.RitualController rc = ritualRoot.GetComponent<Ritual.RitualController>();
            if (rc == null) { rc = ritualRoot.AddComponent<Ritual.RitualController>(); changed = true; }
            if (rc.ritualAnchorId != "CH02_RITUAL") { rc.ritualAnchorId = "CH02_RITUAL"; changed = true; }

            Transform spots = FindOrCreateChild(parent, "SpiritSpots_CH02", ref changed).transform;
            Transform spotA = spots.Find("SpiritSpot_CH02_A");
            if (spotA == null)
            {
                GameObject s = new GameObject("SpiritSpot_CH02_A");
                s.transform.SetParent(spots, false);
                spotA = s.transform;
                changed = true;
            }
            if (spotA.position != new Vector3(-2.6f, -4.4f, 0f)) { spotA.position = new Vector3(-2.6f, -4.4f, 0f); changed = true; }

            GameObject ghost = FindOrCreateChild(parent, "Ghost_CH02_A", ref changed);
            if (ghost.transform.position != new Vector3(-2.6f, -4.4f, 0f)) { ghost.transform.position = new Vector3(-2.6f, -4.4f, 0f); changed = true; }
            EnsureFeetVisual(ghost, loverSprite, new Color(1f, 0.94f, 0.96f, 1f), 1, ref changed);
            EnsureNpcWalk(ghost, "CHR05_LoverSpirit", ref changed);
            Ritual.SpiritEscort escort = ghost.GetComponent<Ritual.SpiritEscort>();
            if (escort == null) { escort = ghost.AddComponent<Ritual.SpiritEscort>(); changed = true; }
            if (escort.followOffset != new Vector2(-0.65f, -0.2f)) { escort.followOffset = new Vector2(-0.65f, -0.2f); changed = true; }
            if (ghost.activeSelf) { ghost.SetActive(false); changed = true; }

            if (rc.spirits == null || rc.spirits.Length != 1 || rc.spirits[0] != escort)
            { rc.spirits = new Ritual.SpiritEscort[] { escort }; changed = true; }
            Transform[] spotArr = { spotA };
            if (rc.spiritSpots == null || rc.spiritSpots.Length != 1 || rc.spiritSpots[0] != spotA)
            { rc.spiritSpots = spotArr; changed = true; }
            if (rc.hideOnRitual == null || rc.hideOnRitual.Length != 1 || rc.hideOnRitual[0] != lInt)
            { rc.hideOnRitual = new Interactable[] { lInt }; changed = true; }

            // Route gates: these CH02 objects are visible only during CH02.
            EnsureRouteGate(lover, "CH02", ref changed);
            EnsureRouteGate(necklace, "CH02", ref changed);
            EnsureRouteGate(ritualInt.gameObject, "CH02", ref changed);
            Transform ritualVisualT = parent.Find("Visual_CH02_RITUAL");
            if (ritualVisualT != null) EnsureRouteGate(ritualVisualT.gameObject, "CH02", ref changed);
            EnsureRouteGate(dockInt.gameObject, "CH02", ref changed);
            Transform dockVisualT = parent.Find("Visual_CH02_BOAT");
            if (dockVisualT != null) EnsureRouteGate(dockVisualT.gameObject, "CH02", ref changed);
            EnsureRouteGate(ghost, "CH02", ref changed);

            report.AppendLine("OK   Underworld CH02 assembly (lover/necklace/ritual/dock/ghost) ensured");
            return ok;
        }

        // ------------------------------------------------------------------
        // C06: formal CH03 assembly in the Underworld. The drumsticks quest,
        // the drummer NPC, the ritual anchor and the dock reuse the accepted
        // CH01/CH02 layout points; ChapterObjectVisibility keeps exactly one
        // route's objects visible at a time.
        // ------------------------------------------------------------------
        private static bool EnsureUnderworldCh03(Transform parent, ref bool changed, StringBuilder report)
        {
            bool ok = true;
            Sprite drummerSprite = LoadSprite("Art/Characters/CHR07_DrummerSpirit");
            if (drummerSprite == null) drummerSprite = LoadSprite("Art/Characters/Npc_Placeholder");
            Sprite drumsticksSprite = LoadSprite("Art/Props/IT04_Drumsticks");
            if (drumsticksSprite == null) drumsticksSprite = LoadSprite("Art/Props/Item_Placeholder");
            Sprite lanternSprite = LoadFormalOrPlaceholder("Art/Props/PRP08_StoneLantern", "Art/Props/RitualAnchor_Placeholder");
            Sprite boatSprite = LoadFormalOrPlaceholder("Art/Props/PRP01_Boat", "Art/Props/Boat_Placeholder");

            // The drummer: auto-talk in the opening, hand-over point later.
            // No returnSegmentId: the Director auto-plays CH03_RitualTalk when
            // the hand-over advances the stage (never twice).
            GameObject drummer = FindOrCreateChild(parent, "NPC_CH03_DRUMMER", ref changed);
            Interactable dInt = drummer.GetComponent<Interactable>();
            if (dInt == null) { dInt = drummer.AddComponent<Interactable>(); changed = true; }
            if (dInt.kind != InteractableKind.Npc) { dInt.kind = InteractableKind.Npc; changed = true; }
            if (dInt.interactableId != "CH03_DrummerSpirit") { dInt.interactableId = "CH03_DrummerSpirit"; changed = true; }
            if (dInt.displayName != "鼓手") { dInt.displayName = "鼓手"; changed = true; }
            if (dInt.autoTalkSegmentId != "CH03_Opening") { dInt.autoTalkSegmentId = "CH03_Opening"; changed = true; }
            if (dInt.returnSegmentId != "") { dInt.returnSegmentId = ""; changed = true; }
            if (!StringArrayEqual(dInt.returnItemIds, new[] { "IT04_Drumsticks" }))
            { dInt.returnItemIds = new[] { "IT04_Drumsticks" }; changed = true; }
            if (!StringArrayEqual(dInt.activeStageIds, new[] { "CH03_Opening", "CH03_ReturnDrumsticks" }))
            { dInt.activeStageIds = new[] { "CH03_Opening", "CH03_ReturnDrumsticks" }; changed = true; }
            if (dInt.promptKey != "hint.return_items") { dInt.promptKey = "hint.return_items"; changed = true; }
            EnsureNpcBody(drummer, drummerSprite, new Vector3(2f, -2f, 0f),
                new Color(0.93f, 0.92f, 0.98f, 1f), ref changed);

            // Drumsticks pickup (CH03_FindDrumsticks only). One quest item:
            // the pair is never split into two pickups.
            GameObject sticks = FindOrCreateChild(parent, "Item_CH03_DRUMSTICKS", ref changed);
            Interactable sInt = sticks.GetComponent<Interactable>();
            if (sInt == null) { sInt = sticks.AddComponent<Interactable>(); changed = true; }
            if (sInt.kind != InteractableKind.Item) { sInt.kind = InteractableKind.Item; changed = true; }
            if (sInt.interactableId != "IT04_Drumsticks") { sInt.interactableId = "IT04_Drumsticks"; changed = true; }
            if (sInt.displayName != "鼓槌") { sInt.displayName = "鼓槌"; changed = true; }
            if (sInt.chapterId != "CH03") { sInt.chapterId = "CH03"; changed = true; }
            if (sInt.promptKey != "hint.pickup") { sInt.promptKey = "hint.pickup"; changed = true; }
            if (!StringArrayEqual(sInt.activeStageIds, new[] { "CH03_FindDrumsticks" }))
            { sInt.activeStageIds = new[] { "CH03_FindDrumsticks" }; changed = true; }
            EnsureNpcBody(sticks, drumsticksSprite, new Vector3(-4f, 1f, 0f), Color.white, ref changed, itemSize: true);

            // Ritual anchor (E starts the CH03 farewell ritual).
            Interactable ritualInt = EnsureCh01Prop(parent, "RitualAnchor_CH03", "CH03_RITUAL",
                "hint.ritual", new[] { "CH03_Ritual" },
                new Vector3(-2f, -5f, 0f), new Vector2(0.7f, 0.9f), ref changed);
            if (ritualInt.GetComponent<Ritual.RitualTrigger>() == null)
            { ritualInt.gameObject.AddComponent<Ritual.RitualTrigger>(); changed = true; }
            EnsurePropVisual(parent, "Visual_CH03_RITUAL", lanternSprite, new Vector3(-2f, -4.78f, 0f),
                DuzheSortingLayers.Actors, 1f, ref changed);

            // Dock: E boards the boat.
            Interactable dockInt = EnsureCh01Prop(parent, "BoatDock_CH03", "CH03_BOAT",
                "hint.board", new[] { "CH03_Board" },
                new Vector3(2f, -6.2f, 0f), new Vector2(1.6f, 0.8f), ref changed);
            if (dockInt.GetComponent<Ritual.BoardingSpot>() == null)
            { dockInt.gameObject.AddComponent<Ritual.BoardingSpot>(); changed = true; }
            EnsurePropVisual(parent, "Visual_CH03_BOAT", boatSprite, new Vector3(2f, -6.2f, 0f),
                DuzheSortingLayers.Actors, 1f, ref changed);

            // CH03 ritual controller + one ghost escort (the drummer).
            GameObject ritualRoot = FindOrCreateChild(parent, "RitualRoot_CH03", ref changed);
            Ritual.RitualController rc = ritualRoot.GetComponent<Ritual.RitualController>();
            if (rc == null) { rc = ritualRoot.AddComponent<Ritual.RitualController>(); changed = true; }
            if (rc.ritualAnchorId != "CH03_RITUAL") { rc.ritualAnchorId = "CH03_RITUAL"; changed = true; }

            Transform spots = FindOrCreateChild(parent, "SpiritSpots_CH03", ref changed).transform;
            Transform spotA = spots.Find("SpiritSpot_CH03_A");
            if (spotA == null)
            {
                GameObject s = new GameObject("SpiritSpot_CH03_A");
                s.transform.SetParent(spots, false);
                spotA = s.transform;
                changed = true;
            }
            if (spotA.position != new Vector3(-2.6f, -4.4f, 0f)) { spotA.position = new Vector3(-2.6f, -4.4f, 0f); changed = true; }

            GameObject ghost = FindOrCreateChild(parent, "Ghost_CH03_A", ref changed);
            if (ghost.transform.position != new Vector3(-2.6f, -4.4f, 0f)) { ghost.transform.position = new Vector3(-2.6f, -4.4f, 0f); changed = true; }
            EnsureFeetVisual(ghost, drummerSprite, new Color(0.93f, 0.92f, 0.98f, 1f), 1, ref changed);
            EnsureNpcWalk(ghost, "CHR07_DrummerSpirit", ref changed);
            Ritual.SpiritEscort escort = ghost.GetComponent<Ritual.SpiritEscort>();
            if (escort == null) { escort = ghost.AddComponent<Ritual.SpiritEscort>(); changed = true; }
            if (escort.followOffset != new Vector2(-0.65f, -0.2f)) { escort.followOffset = new Vector2(-0.65f, -0.2f); changed = true; }
            if (ghost.activeSelf) { ghost.SetActive(false); changed = true; }

            if (rc.spirits == null || rc.spirits.Length != 1 || rc.spirits[0] != escort)
            { rc.spirits = new Ritual.SpiritEscort[] { escort }; changed = true; }
            Transform[] spotArr = { spotA };
            if (rc.spiritSpots == null || rc.spiritSpots.Length != 1 || rc.spiritSpots[0] != spotA)
            { rc.spiritSpots = spotArr; changed = true; }
            if (rc.hideOnRitual == null || rc.hideOnRitual.Length != 1 || rc.hideOnRitual[0] != dInt)
            { rc.hideOnRitual = new Interactable[] { dInt }; changed = true; }

            // Route gates: these CH03 objects are visible only during CH03.
            EnsureRouteGate(drummer, "CH03", ref changed);
            EnsureRouteGate(sticks, "CH03", ref changed);
            EnsureRouteGate(ritualInt.gameObject, "CH03", ref changed);
            Transform ritualVisualT = parent.Find("Visual_CH03_RITUAL");
            if (ritualVisualT != null) EnsureRouteGate(ritualVisualT.gameObject, "CH03", ref changed);
            EnsureRouteGate(dockInt.gameObject, "CH03", ref changed);
            Transform dockVisualT = parent.Find("Visual_CH03_BOAT");
            if (dockVisualT != null) EnsureRouteGate(dockVisualT.gameObject, "CH03", ref changed);
            EnsureRouteGate(ghost, "CH03", ref changed);

            int ch03Objects = 0;
            foreach (Transform child in parent)
            {
                if (child.name.Contains("_CH03_")) ch03Objects++;
            }
            report.AppendLine("OK   Underworld CH03 assembly (drummer/drumsticks/ritual/dock/ghost) ensured [objects=" +
                ch03Objects + "]");
            return ok;
        }

        // ------------------------------------------------------------------
        // C06: formal CH03 assembly in the RecordingStudio (R09 BG05 anchors
        // from BackgroundQA.json, worldX=(px-640)/32, worldY=(360-py)/32):
        //   door threshold (224,576) -> (-13,-6.75) = SP_Main
        //   band spots x=704/768/832/896 @ y=384 -> (2/4/6/8,-0.75)
        //   RS_Performance (768,320) -> (4,1.25)
        //   RS_PhotoApproach (960,448) -> (10,-2.75)
        // The drum kit / amps / guitar rack are baked into BG05 and are NOT
        // interactive; only the performance and photo hotspots are added.
        // ------------------------------------------------------------------
        private static bool EnsureRecordingStudioCh03(Transform parent, ref bool changed, StringBuilder report)
        {
            bool ok = true;
            Sprite bandSprite = LoadSprite("Art/Characters/CHR08_PastSelfBand");
            if (bandSprite == null) bandSprite = LoadSprite("Art/Characters/Npc_Placeholder");
            Sprite bassSprite = LoadSprite("Art/Characters/CHR09_BandmateA");
            if (bassSprite == null) bassSprite = bandSprite;
            Sprite singerSprite = LoadSprite("Art/Characters/CHR10_BandmateB");
            if (singerSprite == null) singerSprite = bandSprite;
            Sprite drummerSprite = LoadSprite("Art/Characters/CHR07_DrummerSpirit");
            if (drummerSprite == null) drummerSprite = LoadSprite("Art/Characters/Npc_Placeholder");

            // PastSelfBand: the only auto-talk NPC here; it speaks in
            // CH03_ReunionTalk, so the reunion cannot fire earlier.
            GameObject band = FindOrCreateChild(parent, "NPC_CH03_PASTSELF", ref changed);
            Interactable bInt = band.GetComponent<Interactable>();
            if (bInt == null) { bInt = band.AddComponent<Interactable>(); changed = true; }
            if (bInt.kind != InteractableKind.Npc) { bInt.kind = InteractableKind.Npc; changed = true; }
            if (bInt.interactableId != "CH03_PastSelfBand") { bInt.interactableId = "CH03_PastSelfBand"; changed = true; }
            if (bInt.displayName != "队友") { bInt.displayName = "队友"; changed = true; }
            if (bInt.autoTalkSegmentId != "CH03_ReunionTalk") { bInt.autoTalkSegmentId = "CH03_ReunionTalk"; changed = true; }
            if (bInt.returnSegmentId != "") { bInt.returnSegmentId = ""; changed = true; }
            if (!StringArrayEqual(bInt.activeStageIds, new[] { "CH03_ReunionTalk" }))
            { bInt.activeStageIds = new[] { "CH03_ReunionTalk" }; changed = true; }
            EnsureNpcBody(band, bandSprite, new Vector3(2f, -0.75f, 0f), Color.white, ref changed);

            // The two other living bandmates are scenery bodies (no hand-over,
            // no auto-talk): they stand at their spots for the whole chapter.
            GameObject bass = FindOrCreateChild(parent, "NPC_CH03_BANDMATE_A", ref changed);
            Interactable bassInt = bass.GetComponent<Interactable>();
            if (bassInt == null) { bassInt = bass.AddComponent<Interactable>(); changed = true; }
            if (bassInt.kind != InteractableKind.Npc) { bassInt.kind = InteractableKind.Npc; changed = true; }
            if (bassInt.interactableId != "CH03_BandmateA") { bassInt.interactableId = "CH03_BandmateA"; changed = true; }
            if (bassInt.displayName != "贝斯手") { bassInt.displayName = "贝斯手"; changed = true; }
            if (bassInt.autoTalkSegmentId != "") { bassInt.autoTalkSegmentId = ""; changed = true; }
            if (!StringArrayEqual(bassInt.returnItemIds, new string[0])) { bassInt.returnItemIds = new string[0]; changed = true; }
            if (!StringArrayEqual(bassInt.activeStageIds, new string[0])) { bassInt.activeStageIds = new string[0]; changed = true; }
            EnsureNpcBody(bass, bassSprite, new Vector3(4f, -0.75f, 0f), Color.white, ref changed);

            GameObject singer = FindOrCreateChild(parent, "NPC_CH03_BANDMATE_B", ref changed);
            Interactable singerInt = singer.GetComponent<Interactable>();
            if (singerInt == null) { singerInt = singer.AddComponent<Interactable>(); changed = true; }
            if (singerInt.kind != InteractableKind.Npc) { singerInt.kind = InteractableKind.Npc; changed = true; }
            if (singerInt.interactableId != "CH03_BandmateB") { singerInt.interactableId = "CH03_BandmateB"; changed = true; }
            if (singerInt.displayName != "主唱") { singerInt.displayName = "主唱"; changed = true; }
            if (singerInt.autoTalkSegmentId != "") { singerInt.autoTalkSegmentId = ""; changed = true; }
            if (!StringArrayEqual(singerInt.returnItemIds, new string[0])) { singerInt.returnItemIds = new string[0]; changed = true; }
            if (!StringArrayEqual(singerInt.activeStageIds, new string[0])) { singerInt.activeStageIds = new string[0]; changed = true; }
            EnsureNpcBody(singer, singerSprite, new Vector3(6f, -0.75f, 0f), Color.white, ref changed);

            // The drummer's empty spot (band spot 4) - his place for the photo.
            Transform studioSpots = FindOrCreateChild(parent, "SeatPoints_CH03", ref changed).transform;
            Transform drummerSpot = studioSpots.Find("Seat_CH03_Drummer");
            if (drummerSpot == null)
            {
                GameObject seat = new GameObject("Seat_CH03_Drummer");
                seat.transform.SetParent(studioSpots, false);
                drummerSpot = seat.transform;
                changed = true;
            }
            if (drummerSpot.position != new Vector3(8f, -0.75f, 0f))
            { drummerSpot.position = new Vector3(8f, -0.75f, 0f); changed = true; }

            // The drummer's ghost (rode the boat across): hidden until the
            // scene cast activates him beside the door.
            GameObject ghost = FindOrCreateChild(parent, "Ghost_CH03_Studio", ref changed);
            EnsureFeetVisual(ghost, drummerSprite, new Color(0.93f, 0.92f, 0.98f, 1f), 1, ref changed);
            EnsureNpcWalk(ghost, "CHR07_DrummerSpirit", ref changed);
            Ritual.SpiritEscort escort = ghost.GetComponent<Ritual.SpiritEscort>();
            if (escort == null) { escort = ghost.AddComponent<Ritual.SpiritEscort>(); changed = true; }
            if (escort.followOffset != new Vector2(-0.65f, -0.2f)) { escort.followOffset = new Vector2(-0.65f, -0.2f); changed = true; }
            if (ghost.activeSelf) { ghost.SetActive(false); changed = true; }

            // Performance hotspot: E opens MG05_Notes (CH03_Perform only).
            Interactable performInt = EnsureCh01Prop(parent, "Zone_CH03_PERFORM", "CH03_PERFORM",
                "", new[] { "CH03_Perform" },
                new Vector3(4f, 1.25f, 0f), new Vector2(2.4f, 1.8f), ref changed);
            if (performInt.GetComponent<Minigames.MinigameLauncher>() == null)
            { performInt.gameObject.AddComponent<Minigames.MinigameLauncher>(); changed = true; }
            Minigames.MinigameLauncher performLauncher = performInt.GetComponent<Minigames.MinigameLauncher>();
            if (performLauncher != null && performLauncher.minigameId != "MG05_Notes")
            { performLauncher.minigameId = "MG05_Notes"; changed = true; }
            if (performInt.displayName != "一起演奏") { performInt.displayName = "一起演奏"; changed = true; }

            // Photo hotspot: E opens MG06_Photo (CH03_Photo only). The shutter
            // itself fires once on the successful drop (RecordingStudioCast).
            Interactable photoInt = EnsureCh01Prop(parent, "Zone_CH03_PHOTO", "CH03_PHOTO",
                "", new[] { "CH03_Photo" },
                new Vector3(10f, -2.75f, 0f), new Vector2(2.4f, 2.2f), ref changed);
            if (photoInt.GetComponent<Minigames.MinigameLauncher>() == null)
            { photoInt.gameObject.AddComponent<Minigames.MinigameLauncher>(); changed = true; }
            Minigames.MinigameLauncher photoLauncher = photoInt.GetComponent<Minigames.MinigameLauncher>();
            if (photoLauncher != null && photoLauncher.minigameId != "MG06_Photo")
            { photoLauncher.minigameId = "MG06_Photo"; changed = true; }
            if (photoInt.displayName != "拍合照") { photoInt.displayName = "拍合照"; changed = true; }

            // Scene cast wiring (ghost activation + one-shot shutter).
            Scenes.RecordingStudioCast cast = EnsureSingleExternalComponent<Scenes.RecordingStudioCast>(parent.gameObject, ref changed);
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(cast))))
            {
                report.AppendLine("FAIL RecordingStudio: RecordingStudioCast script binding is not asset-backed");
                ok = false;
            }
            if (cast.drummerGhost != escort) { cast.drummerGhost = escort; changed = true; }
            if (cast.drummerSpot != drummerSpot) { cast.drummerSpot = drummerSpot; changed = true; }
            if (cast.bandmates == null || cast.bandmates.Length != 3 ||
                cast.bandmates[0] != band || cast.bandmates[1] != bass || cast.bandmates[2] != singer)
            { cast.bandmates = new[] { band, bass, singer }; changed = true; }

            Scenes.CH03FarewellCast farewell = EnsureSingleExternalComponent<Scenes.CH03FarewellCast>(parent.gameObject, ref changed);
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(farewell))))
            {
                report.AppendLine("FAIL RecordingStudio: CH03FarewellCast script binding is not asset-backed");
                ok = false;
            }
            if (farewell.presentationId != "PR_CH03_Farewell") { farewell.presentationId = "PR_CH03_Farewell"; changed = true; }
            if (farewell.studio != cast) { farewell.studio = cast; changed = true; }

            // Route gates: the studio is CH03-only for now.
            EnsureRouteGate(band, "CH03", ref changed);
            EnsureRouteGate(bass, "CH03", ref changed);
            EnsureRouteGate(singer, "CH03", ref changed);
            EnsureRouteGate(performInt.gameObject, "CH03", ref changed);
            EnsureRouteGate(photoInt.gameObject, "CH03", ref changed);
            EnsureRouteGate(ghost, "CH03", ref changed);

            // Idempotency evidence: a second assembly must not grow the object
            // or component counts.
            int ch03Objects = 0;
            foreach (Transform child in parent)
            {
                if (child.name.Contains("_CH03_")) ch03Objects++;
            }
            Scenes.ChapterMemoryCast montage03 = EnsureSingleExternalComponent<Scenes.ChapterMemoryCast>(parent.gameObject, ref changed);
            if (montage03.presentationId != "PR_CH03_Memory") { montage03.presentationId = "PR_CH03_Memory"; changed = true; }
            report.AppendLine("OK   RecordingStudio CH03 assembly (band/drummer ghost/perform/photo/casts) ensured" +
                " [objects=" + ch03Objects +
                " casts=" + parent.GetComponents<Scenes.RecordingStudioCast>().Length +
                "/" + parent.GetComponents<Scenes.CH03FarewellCast>().Length + "]");
            return ok;
        }

        // ------------------------------------------------------------------
        // C07: formal CH04 assembly in the Underworld. Same accepted layout
        // points as CH01-CH03 (route gates keep exactly one chapter visible).
        // ------------------------------------------------------------------
        private static bool EnsureUnderworldCh04(Transform parent, ref bool changed, StringBuilder report)
        {
            bool ok = true;
            Sprite teacherSprite = LoadSprite("Art/Characters/CHR11_TeacherSpirit");
            if (teacherSprite == null) teacherSprite = LoadSprite("Art/Characters/Npc_Placeholder");
            Sprite planSprite = LoadSprite("Art/Props/IT05_LessonPlan");
            if (planSprite == null) planSprite = LoadSprite("Art/Props/Item_Placeholder");
            Sprite lanternSprite = LoadFormalOrPlaceholder("Art/Props/PRP08_StoneLantern", "Art/Props/RitualAnchor_Placeholder");
            Sprite boatSprite = LoadFormalOrPlaceholder("Art/Props/PRP01_Boat", "Art/Props/Boat_Placeholder");

            // The teacher: auto-talk in the opening, hand-over point later.
            GameObject teacher = FindOrCreateChild(parent, "NPC_CH04_TEACHER", ref changed);
            Interactable tInt = teacher.GetComponent<Interactable>();
            if (tInt == null) { tInt = teacher.AddComponent<Interactable>(); changed = true; }
            if (tInt.kind != InteractableKind.Npc) { tInt.kind = InteractableKind.Npc; changed = true; }
            if (tInt.interactableId != "CH04_TeacherSpirit") { tInt.interactableId = "CH04_TeacherSpirit"; changed = true; }
            if (tInt.displayName != "老师") { tInt.displayName = "老师"; changed = true; }
            if (tInt.autoTalkSegmentId != "CH04_Opening") { tInt.autoTalkSegmentId = "CH04_Opening"; changed = true; }
            if (tInt.returnSegmentId != "") { tInt.returnSegmentId = ""; changed = true; }
            if (!StringArrayEqual(tInt.returnItemIds, new[] { "IT05_LessonPlan" }))
            { tInt.returnItemIds = new[] { "IT05_LessonPlan" }; changed = true; }
            if (!StringArrayEqual(tInt.activeStageIds, new[] { "CH04_Opening", "CH04_ReturnLessonPlan" }))
            { tInt.activeStageIds = new[] { "CH04_Opening", "CH04_ReturnLessonPlan" }; changed = true; }
            if (tInt.promptKey != "hint.return_items") { tInt.promptKey = "hint.return_items"; changed = true; }
            EnsureNpcBody(teacher, teacherSprite, new Vector3(2f, -2f, 0f),
                new Color(0.94f, 0.94f, 1f, 1f), ref changed);

            // Lesson plan pickup (CH04_FindLessonPlan only).
            GameObject plan = FindOrCreateChild(parent, "Item_CH04_LESSONPLAN", ref changed);
            Interactable pInt = plan.GetComponent<Interactable>();
            if (pInt == null) { pInt = plan.AddComponent<Interactable>(); changed = true; }
            if (pInt.kind != InteractableKind.Item) { pInt.kind = InteractableKind.Item; changed = true; }
            if (pInt.interactableId != "IT05_LessonPlan") { pInt.interactableId = "IT05_LessonPlan"; changed = true; }
            if (pInt.displayName != "教案") { pInt.displayName = "教案"; changed = true; }
            if (pInt.chapterId != "CH04") { pInt.chapterId = "CH04"; changed = true; }
            if (pInt.promptKey != "hint.pickup") { pInt.promptKey = "hint.pickup"; changed = true; }
            if (!StringArrayEqual(pInt.activeStageIds, new[] { "CH04_FindLessonPlan" }))
            { pInt.activeStageIds = new[] { "CH04_FindLessonPlan" }; changed = true; }
            EnsureNpcBody(plan, planSprite, new Vector3(-4f, 1f, 0f), Color.white, ref changed, itemSize: true);

            // Ritual anchor (E starts the CH04 farewell ritual).
            Interactable ritualInt = EnsureCh01Prop(parent, "RitualAnchor_CH04", "CH04_RITUAL",
                "hint.ritual", new[] { "CH04_Ritual" },
                new Vector3(-2f, -5f, 0f), new Vector2(0.7f, 0.9f), ref changed);
            if (ritualInt.GetComponent<Ritual.RitualTrigger>() == null)
            { ritualInt.gameObject.AddComponent<Ritual.RitualTrigger>(); changed = true; }
            EnsurePropVisual(parent, "Visual_CH04_RITUAL", lanternSprite, new Vector3(-2f, -4.78f, 0f),
                DuzheSortingLayers.Actors, 1f, ref changed);

            // Dock: E boards the boat.
            Interactable dockInt = EnsureCh01Prop(parent, "BoatDock_CH04", "CH04_BOAT",
                "hint.board", new[] { "CH04_Board" },
                new Vector3(2f, -6.2f, 0f), new Vector2(1.6f, 0.8f), ref changed);
            if (dockInt.GetComponent<Ritual.BoardingSpot>() == null)
            { dockInt.gameObject.AddComponent<Ritual.BoardingSpot>(); changed = true; }
            EnsurePropVisual(parent, "Visual_CH04_BOAT", boatSprite, new Vector3(2f, -6.2f, 0f),
                DuzheSortingLayers.Actors, 1f, ref changed);

            // CH04 ritual controller + exactly ONE ghost escort (the teacher).
            GameObject ritualRoot = FindOrCreateChild(parent, "RitualRoot_CH04", ref changed);
            Ritual.RitualController rc = ritualRoot.GetComponent<Ritual.RitualController>();
            if (rc == null) { rc = ritualRoot.AddComponent<Ritual.RitualController>(); changed = true; }
            if (rc.ritualAnchorId != "CH04_RITUAL") { rc.ritualAnchorId = "CH04_RITUAL"; changed = true; }

            Transform spots = FindOrCreateChild(parent, "SpiritSpots_CH04", ref changed).transform;
            Transform spotA = spots.Find("SpiritSpot_CH04_A");
            if (spotA == null)
            {
                GameObject s = new GameObject("SpiritSpot_CH04_A");
                s.transform.SetParent(spots, false);
                spotA = s.transform;
                changed = true;
            }
            if (spotA.position != new Vector3(-2.6f, -4.4f, 0f)) { spotA.position = new Vector3(-2.6f, -4.4f, 0f); changed = true; }

            GameObject ghost = FindOrCreateChild(parent, "Ghost_CH04_A", ref changed);
            if (ghost.transform.position != new Vector3(-2.6f, -4.4f, 0f)) { ghost.transform.position = new Vector3(-2.6f, -4.4f, 0f); changed = true; }
            EnsureFeetVisual(ghost, teacherSprite, new Color(0.94f, 0.94f, 1f, 1f), 1, ref changed);
            EnsureNpcWalk(ghost, "CHR11_TeacherSpirit", ref changed);
            Ritual.SpiritEscort escort = ghost.GetComponent<Ritual.SpiritEscort>();
            if (escort == null) { escort = ghost.AddComponent<Ritual.SpiritEscort>(); changed = true; }
            if (escort.followOffset != new Vector2(-0.65f, -0.2f)) { escort.followOffset = new Vector2(-0.65f, -0.2f); changed = true; }
            if (ghost.activeSelf) { ghost.SetActive(false); changed = true; }

            if (rc.spirits == null || rc.spirits.Length != 1 || rc.spirits[0] != escort)
            { rc.spirits = new Ritual.SpiritEscort[] { escort }; changed = true; }
            Transform[] spotArr = { spotA };
            if (rc.spiritSpots == null || rc.spiritSpots.Length != 1 || rc.spiritSpots[0] != spotA)
            { rc.spiritSpots = spotArr; changed = true; }
            if (rc.hideOnRitual == null || rc.hideOnRitual.Length != 1 || rc.hideOnRitual[0] != tInt)
            { rc.hideOnRitual = new Interactable[] { tInt }; changed = true; }

            // Route gates: these CH04 objects are visible only during CH04.
            EnsureRouteGate(teacher, "CH04", ref changed);
            EnsureRouteGate(plan, "CH04", ref changed);
            EnsureRouteGate(ritualInt.gameObject, "CH04", ref changed);
            Transform ritualVisualT = parent.Find("Visual_CH04_RITUAL");
            if (ritualVisualT != null) EnsureRouteGate(ritualVisualT.gameObject, "CH04", ref changed);
            EnsureRouteGate(dockInt.gameObject, "CH04", ref changed);
            Transform dockVisualT = parent.Find("Visual_CH04_BOAT");
            if (dockVisualT != null) EnsureRouteGate(dockVisualT.gameObject, "CH04", ref changed);
            EnsureRouteGate(ghost, "CH04", ref changed);

            int ch04Objects = 0;
            foreach (Transform child in parent)
            {
                if (child.name.Contains("_CH04_")) ch04Objects++;
            }
            report.AppendLine("OK   Underworld CH04 assembly (teacher/lesson plan/ritual/dock/ghost) ensured [objects=" +
                ch04Objects + "]");
            return ok;
        }

        // ------------------------------------------------------------------
        // C07: formal CH04 assembly in the Classroom (R04 Classroom layout,
        // worldX=(px-640)/32, worldY=(360-py)/32):
        //   CL_Spawn (224,600) -> (-13,-7.5); C12 moved SP_Main to (-3,-1) so
        //   the student, the teacher and the board are on screen on arrival.
        //   CL_Student (608,416) -> (-1,-1.75)
        //   CL_Teacher (768,192) -> (4,5.25)
        //   CL_Blackboard (768,96) -> (4,8.25)  (baked into BG06; the E
        //   hotspot sits at the reachable front approach (4,4.0))
        // Solids: the R04 desk boxes plus the top wall band. The blackboard
        // writing panel is a screen-space UI, never read off the world board.
        // ------------------------------------------------------------------
        private static bool EnsureClassroomCh04(Transform parent, ref bool changed, StringBuilder report)
        {
            bool ok = true;
            Sprite studentSprite = LoadSprite("Art/Characters/CHR12_PastSelfSchool");
            if (studentSprite == null) studentSprite = LoadSprite("Art/Characters/Npc_Placeholder");
            Sprite teacherSprite = LoadSprite("Art/Characters/CHR11_TeacherSpirit");
            if (teacherSprite == null) teacherSprite = LoadSprite("Art/Characters/Npc_Placeholder");

            // Static collision straight from R04 Classroom.layout.json.
            ok &= EnsureWall(parent, "Solid_CL_TopWall", new Vector2(0f, 8.5f), new Vector2(37f, 2.5f), ref changed, report);
            ok &= EnsureWall(parent, "Solid_CL_Desk1", new Vector2(-7.75f, 0.5f), new Vector2(1.9f, 0.9f), ref changed, report);
            ok &= EnsureWall(parent, "Solid_CL_Desk2", new Vector2(-2.75f, 0.5f), new Vector2(1.9f, 0.9f), ref changed, report);
            ok &= EnsureWall(parent, "Solid_CL_Desk3", new Vector2(-7.75f, -4f), new Vector2(1.9f, 0.9f), ref changed, report);
            ok &= EnsureWall(parent, "Solid_CL_Desk4", new Vector2(-2.75f, -4f), new Vector2(1.9f, 0.9f), ref changed, report);
            ok &= EnsureWall(parent, "Solid_CL_Desk5", new Vector2(2.25f, -4f), new Vector2(1.9f, 0.9f), ref changed, report);
            ok &= EnsureWall(parent, "Solid_CL_Desk6", new Vector2(7.25f, -4f), new Vector2(1.9f, 0.9f), ref changed, report);
            ok &= EnsureWall(parent, "Solid_CL_Desk7", new Vector2(14.25f, 0.5f), new Vector2(1.9f, 0.9f), ref changed, report);

            // The student: fixed at CL_Student. No auto-talk here - the stage
            // itself auto-starts CH04_ReunionTalk, so a second start is
            // impossible.
            GameObject student = FindOrCreateChild(parent, "NPC_CH04_STUDENT", ref changed);
            Interactable sInt = student.GetComponent<Interactable>();
            if (sInt == null) { sInt = student.AddComponent<Interactable>(); changed = true; }
            if (sInt.kind != InteractableKind.Npc) { sInt.kind = InteractableKind.Npc; changed = true; }
            if (sInt.interactableId != "CH04_PastSelfSchool") { sInt.interactableId = "CH04_PastSelfSchool"; changed = true; }
            if (sInt.displayName != "学生") { sInt.displayName = "学生"; changed = true; }
            if (sInt.autoTalkSegmentId != "") { sInt.autoTalkSegmentId = ""; changed = true; }
            if (!StringArrayEqual(sInt.returnItemIds, new string[0])) { sInt.returnItemIds = new string[0]; changed = true; }
            if (!StringArrayEqual(sInt.activeStageIds, new string[0])) { sInt.activeStageIds = new string[0]; changed = true; }
            EnsureNpcBody(student, studentSprite, new Vector3(-1f, -1.75f, 0f), Color.white, ref changed);

            // The teacher's spirit stands at the podium for the whole lesson.
            GameObject teacher = FindOrCreateChild(parent, "NPC_CH04_CLASS_TEACHER", ref changed);
            Interactable tInt = teacher.GetComponent<Interactable>();
            if (tInt == null) { tInt = teacher.AddComponent<Interactable>(); changed = true; }
            if (tInt.kind != InteractableKind.Npc) { tInt.kind = InteractableKind.Npc; changed = true; }
            if (tInt.interactableId != "CH04_TeacherSpiritClass") { tInt.interactableId = "CH04_TeacherSpiritClass"; changed = true; }
            if (tInt.displayName != "老师") { tInt.displayName = "老师"; changed = true; }
            if (tInt.autoTalkSegmentId != "") { tInt.autoTalkSegmentId = ""; changed = true; }
            if (!StringArrayEqual(tInt.returnItemIds, new string[0])) { tInt.returnItemIds = new string[0]; changed = true; }
            if (!StringArrayEqual(tInt.activeStageIds, new string[0])) { tInt.activeStageIds = new string[0]; changed = true; }
            // C12: the teacher stands in front of the board but low enough to
            // stay inside the arrival framing (he used to be at y=5.25, above
            // the camera's top edge). The blackboard hotspot stays where the
            // art's board is.
            EnsureNpcBody(teacher, teacherSprite, new Vector3(3f, 2.6f, 0f),
                new Color(0.94f, 0.94f, 1f, 1f), ref changed);

            // Blackboard hotspot: the writing stage's only entry. It carries
            // CH04GoodbyeGate (auto-open once + E to reopen), never a
            // MinigameLauncher (one IPropInteractable per prop).
            Interactable boardInt = EnsureCh01Prop(parent, "Prop_CH04_BLACKBOARD", "CH04_BLACKBOARD",
                "", new[] { "CH04_WriteGoodbye" },
                new Vector3(4f, 4f, 0f), new Vector2(3f, 1.2f), ref changed);
            if (boardInt.displayName != "写再见") { boardInt.displayName = "写再见"; changed = true; }
            Scenes.CH04GoodbyeGate gate = boardInt.GetComponent<Scenes.CH04GoodbyeGate>();
            if (gate == null) { boardInt.gameObject.AddComponent<Scenes.CH04GoodbyeGate>(); changed = true; }

            // Scene cast wiring (fixed positions + farewell references).
            Scenes.ClassroomCast cast = EnsureSingleExternalComponent<Scenes.ClassroomCast>(parent.gameObject, ref changed);
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(cast))))
            {
                report.AppendLine("FAIL Classroom: ClassroomCast script binding is not asset-backed");
                ok = false;
            }
            if (cast.student != student) { cast.student = student; changed = true; }
            if (cast.teacher != teacher) { cast.teacher = teacher; changed = true; }
            if (cast.blackboardProp != boardInt.gameObject) { cast.blackboardProp = boardInt.gameObject; changed = true; }

            Scenes.CH04FarewellCast farewell = EnsureSingleExternalComponent<Scenes.CH04FarewellCast>(parent.gameObject, ref changed);
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(farewell))))
            {
                report.AppendLine("FAIL Classroom: CH04FarewellCast script binding is not asset-backed");
                ok = false;
            }
            if (farewell.presentationId != "PR_CH04_Farewell") { farewell.presentationId = "PR_CH04_Farewell"; changed = true; }
            if (farewell.classroom != cast) { farewell.classroom = cast; changed = true; }

            // Route gates: the classroom is CH04-only for now.
            EnsureRouteGate(student, "CH04", ref changed);
            EnsureRouteGate(teacher, "CH04", ref changed);
            EnsureRouteGate(boardInt.gameObject, "CH04", ref changed);

            int ch04Objects = 0;
            foreach (Transform child in parent)
            {
                if (child.name.Contains("_CH04_")) ch04Objects++;
            }
            Scenes.ChapterMemoryCast montage04 = EnsureSingleExternalComponent<Scenes.ChapterMemoryCast>(parent.gameObject, ref changed);
            if (montage04.presentationId != "PR_CH04_Memory") { montage04.presentationId = "PR_CH04_Memory"; changed = true; }
            report.AppendLine("OK   Classroom CH04 assembly (desks/student/teacher/blackboard/casts) ensured" +
                " [objects=" + ch04Objects +
                " casts=" + parent.GetComponents<Scenes.ClassroomCast>().Length +
                "/" + parent.GetComponents<Scenes.CH04FarewellCast>().Length + "]");
            return ok;
        }

        // ------------------------------------------------------------------
        // C08: formal CH05 assembly. The Underworld shows ONLY the ferryman:
        // every CH01-CH04 / DEV object is already route-gated, so nothing has
        // to be hidden here. CH05 needs one boat (no ritual, no NPC, no item)
        // and the recall presentation cast.
        // ------------------------------------------------------------------
        private static bool EnsureUnderworldCh05(Transform parent, ref bool changed, StringBuilder report)
        {
            bool ok = true;
            Sprite boatSprite = LoadFormalOrPlaceholder("Art/Props/PRP01_Boat", "Art/Props/Boat_Placeholder");

            Interactable dockInt = EnsureCh01Prop(parent, "BoatDock_CH05", "CH05_BOAT",
                "hint.board", new[] { "CH05_Board" },
                new Vector3(2f, -6.2f, 0f), new Vector2(1.6f, 0.8f), ref changed);
            if (dockInt.GetComponent<Ritual.BoardingSpot>() == null)
            { dockInt.gameObject.AddComponent<Ritual.BoardingSpot>(); changed = true; }
            EnsurePropVisual(parent, "Visual_CH05_BOAT", boatSprite, new Vector3(2f, -6.2f, 0f),
                DuzheSortingLayers.Actors, 1f, ref changed);

            EnsureRouteGate(dockInt.gameObject, "CH05", ref changed);
            Transform dockVisualT = parent.Find("Visual_CH05_BOAT");
            if (dockVisualT != null) EnsureRouteGate(dockVisualT.gameObject, "CH05", ref changed);

            // Recall presentation cast (image follows the dialogue lines).
            Scenes.SelfCrossingCast cast = EnsureSingleExternalComponent<Scenes.SelfCrossingCast>(parent.gameObject, ref changed);
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(cast))))
            {
                report.AppendLine("FAIL Underworld: SelfCrossingCast script binding is not asset-backed");
                ok = false;
            }
            if (cast.presentationId != "PR_CH05_Recall") { cast.presentationId = "PR_CH05_Recall"; changed = true; }

            int ch05Objects = 0;
            foreach (Transform child in parent)
            {
                if (child.name.Contains("_CH05_")) ch05Objects++;
            }
            report.AppendLine("OK   Underworld CH05 assembly (boat/recall cast) ensured [objects=" +
                ch05Objects + " casts=" + parent.GetComponents<Scenes.SelfCrossingCast>().Length + "]");
            return ok;
        }

        /// River side of CH05: the reincarnation cast lives in the River scene
        /// (CG05 -> real boat drifts away into a warm fade).
        private static bool EnsureRiverCh05(Transform parent, ref bool changed, StringBuilder report)
        {
            bool ok = true;
            Scenes.SelfCrossingCast cast = EnsureSingleExternalComponent<Scenes.SelfCrossingCast>(parent.gameObject, ref changed);
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(cast))))
            {
                report.AppendLine("FAIL River: SelfCrossingCast script binding is not asset-backed");
                ok = false;
            }
            if (cast.presentationId != "PR_CH05_Reincarnation")
            { cast.presentationId = "PR_CH05_Reincarnation"; changed = true; }

            report.AppendLine("OK   River CH05 assembly (reincarnation cast) ensured [casts=" +
                parent.GetComponents<Scenes.SelfCrossingCast>().Length + "]");
            return ok;
        }

        /// Shared NPC/item body: trigger collider sized like the dev
        /// assemblies (feet-area trigger so tall art never blocks). NPC
        /// bodies carry a feet-pivot CHILD visual ("Visual" at (0,-0.5)) so
        /// formal (0.5,0)-pivot sprites keep the established ground line
        /// (the old centre-pivot placeholder's bottom edge); item pickups
        /// keep the root renderer (small centred icons).
        private static void EnsureNpcBody(GameObject go, Sprite sprite, Vector3 pos, Color tint,
            ref bool changed, bool itemSize = false)
        {
            if (go.transform.position != pos) { go.transform.position = pos; changed = true; }
            if (go.layer != DuzheLayers.Trigger) { go.layer = DuzheLayers.Trigger; changed = true; }
            BoxCollider2D col = go.GetComponent<BoxCollider2D>();
            if (col == null) { col = go.AddComponent<BoxCollider2D>(); changed = true; }
            if (!col.isTrigger) { col.isTrigger = true; changed = true; }
            if (itemSize)
            {
                if (col.size != new Vector2(0.5f, 0.5f)) { col.size = new Vector2(0.5f, 0.5f); changed = true; }
                if (col.offset != Vector2.zero) { col.offset = Vector2.zero; changed = true; }
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr == null) { sr = go.AddComponent<SpriteRenderer>(); changed = true; }
                if (sr.sprite != sprite) { sr.sprite = sprite; changed = true; }
                if (sr.sortingLayerName != DuzheSortingLayers.Actors) { sr.sortingLayerName = DuzheSortingLayers.Actors; changed = true; }
                if (sr.color != tint) { sr.color = tint; changed = true; }
            }
            else
            {
                if (col.size != new Vector2(0.75f, 1f)) { col.size = new Vector2(0.75f, 1f); changed = true; }
                if (col.offset != new Vector2(0f, 0.5f)) { col.offset = new Vector2(0f, 0.5f); changed = true; }
                EnsureFeetVisual(go, sprite, tint, 0, ref changed);
            }
        }

        /// Feet-pivot child visual for NPC/ghost bodies: the child sits 0.5u
        /// below the transform so formal (0.5,0)-pivot sprites keep the
        /// established ground line. A legacy root renderer is removed.
        private static void EnsureFeetVisual(GameObject go, Sprite sprite, Color tint, int sortingOrder, ref bool changed)
        {
            Transform visualT = go.transform.Find("Visual");
            GameObject visual;
            if (visualT == null)
            {
                visual = new GameObject("Visual");
                visual.transform.SetParent(go.transform, false);
                changed = true;
            }
            else
            {
                visual = visualT.gameObject;
            }
            if (visual.transform.localPosition != new Vector3(0f, -0.5f, 0f))
            { visual.transform.localPosition = new Vector3(0f, -0.5f, 0f); changed = true; }
            SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
            if (sr == null) { sr = visual.AddComponent<SpriteRenderer>(); changed = true; }
            if (sr.sprite != sprite) { sr.sprite = sprite; changed = true; }
            if (sr.sortingLayerName != DuzheSortingLayers.Actors) { sr.sortingLayerName = DuzheSortingLayers.Actors; changed = true; }
            if (sr.sortingOrder != sortingOrder) { sr.sortingOrder = sortingOrder; changed = true; }
            if (sr.color != tint) { sr.color = tint; changed = true; }
            SpriteRenderer rootSr = go.GetComponent<SpriteRenderer>();
            if (rootSr != null) { UnityEngine.Object.DestroyImmediate(rootSr); changed = true; }
        }

        private static bool EnsureRiverCrossing(Transform parent, ref bool changed, StringBuilder report)

        {

            bool ok = true;


            GameObject riverRoot = FindOrCreateChild(parent, "RiverRoot", ref changed);

            River.RiverController rc = riverRoot.GetComponent<River.RiverController>();

            if (rc == null) { rc = riverRoot.AddComponent<River.RiverController>(); changed = true; }

            River.WhirlpoolSpawner spawner = riverRoot.GetComponent<River.WhirlpoolSpawner>();

            if (spawner == null) { spawner = riverRoot.AddComponent<River.WhirlpoolSpawner>(); changed = true; }


            Sprite boatSprite = LoadFormalOrPlaceholder("Art/Props/PRP01_Boat", "Art/Props/Boat_Placeholder");
            Sprite verticalBoat = LoadSprite("Art/Props/PRP01_BoatVertical");
            if (verticalBoat != null) boatSprite = verticalBoat;
            GameObject boat = FindOrCreateChild(parent, "Boat", ref changed);
            Vector3 boatPos = new Vector3(0f, -3f, 0f);
            if (boat.transform.position != boatPos) { boat.transform.position = boatPos; changed = true; }
            if (boat.transform.localScale != Vector3.one * 0.72f) { boat.transform.localScale = Vector3.one * 0.72f; changed = true; }
            SpriteRenderer sr = boat.GetComponent<SpriteRenderer>();
            if (sr == null) { sr = boat.AddComponent<SpriteRenderer>(); changed = true; }
            if (sr.sprite != boatSprite) { sr.sprite = boatSprite; changed = true; }
            if (sr.sortingLayerName != DuzheSortingLayers.Actors) { sr.sortingLayerName = DuzheSortingLayers.Actors; changed = true; }
            // order -2: the boat shares the actors' band but must render BELOW
            // the passengers (2) and the ferryman (0) standing on it.
            if (sr.sortingOrder != -2) { sr.sortingOrder = -2; changed = true; }
            River.BoatController bc = boat.GetComponent<River.BoatController>();
            if (bc == null) { bc = boat.AddComponent<River.BoatController>(); changed = true; }
            if (bc.boatLineY != -3f) { bc.boatLineY = -3f; changed = true; }

            if (rc.boat != bc) { rc.boat = bc; changed = true; }
            if (rc.spawner != spawner) { rc.spawner = spawner; changed = true; }

            report.AppendLine("OK   River crossing assembly (RiverRoot/Boat) ensured");
            return ok;
        }

        // ------------------------------------------------------------------
        // C04: formal CH01 assembly in the FarmCourtyard, all coordinates from
        // R04 FarmCourtyard.layout.json via worldX=(px-640)/32, worldY=(360-py)/32:
        //   FC_Spawn (288,576) -> (-11,-6.75)   FC_Door (640,152) -> (0,6.5)
        //   FC_DoorInteract rect (608,184,64,64) -> centre (0,4.5)
        //   FC_Table (736,400) -> (3,-1.25)     FC_TableInteract (736,472) -> (3,-3.5)
        //   FC_StoolLeft (672,448) -> (1,-2.75) FC_StoolRight (800,448) -> (5,-2.75)
        //   FC_StoolFront (736,496) -> (3,-4.25)
        //   Reunion override: door 6.5 > grandson 4.25 > player 2.25, with
        //   the grandson emerging at 5.1 just below the door.
        //   solidPolygons -> three house-wall boxes + the dining table box.
        // ------------------------------------------------------------------
        private static bool EnsureFarmCourtyardCh01(Transform parent, ref bool changed, StringBuilder report)
        {
            bool ok = true;
            // Use the door painted into BG03; a separate placeholder would
            // cover its threshold and create two overlapping doors.
            Sprite doorSprite = LoadSprite("Art/Backgrounds/BG03_FarmCourtyard") != null
                ? null : LoadSprite("Art/Props/Door_Placeholder");
            // C10: delivered stool / dining-table art (R13) with placeholder fallback.
            Sprite tableSprite = LoadSprite("Art/Props/PRP05_DiningTable");
            if (tableSprite == null) tableSprite = LoadSprite("Art/Props/DiningTable_Placeholder");
            Sprite stoolSprite = LoadSprite("Art/Props/PRP04_Stool");
            if (stoolSprite == null) stoolSprite = LoadSprite("Art/Props/Stool_Placeholder");
            Sprite npcSprite = LoadSprite("Art/Characters/Npc_Placeholder");
            Sprite grandsonSprite = LoadSprite("Art/Characters/CHR04_PastSelfFamily");
            if (grandsonSprite == null) grandsonSprite = npcSprite;

            // Static collision: the house band across the top (door gap at
            // x -3.5..3.5) and the dining table box.
            ok &= EnsureWall(parent, "Wall_FC_TopBand", new Vector2(0f, 8.5f), new Vector2(37f, 2.5f), ref changed, report);
            ok &= EnsureWall(parent, "Wall_FC_HouseLeft", new Vector2(-11f, 6.25f), new Vector2(15f, 2f), ref changed, report);
            ok &= EnsureWall(parent, "Wall_FC_HouseRight", new Vector2(11f, 6.25f), new Vector2(15f, 2f), ref changed, report);
            ok &= EnsureWall(parent, "Solid_FC_Table", new Vector2(3f, -1.25f), new Vector2(4f, 2f), ref changed, report);

            // Door visual on the house face + the E interaction zone below it.
            EnsurePropVisual(parent, "Visual_CH01_DOOR", doorSprite, new Vector3(0f, 6.26f, 0f),
                DuzheSortingLayers.GroundProps, 1f, ref changed);
            Interactable doorInt = EnsureCh01Prop(parent, "Zone_CH01_DOOR", "CH01_DOOR",
                "hint.door", new[] { "CH01_Door" },
                new Vector3(0f, 4.5f, 0f), new Vector2(2f, 2f), ref changed);
            if (doorInt.GetComponent<Minigames.MinigameLauncher>() == null)
            { doorInt.gameObject.AddComponent<Minigames.MinigameLauncher>(); changed = true; }
            Minigames.MinigameLauncher doorLauncher = doorInt.GetComponent<Minigames.MinigameLauncher>();
            if (doorLauncher != null && doorLauncher.minigameId != "MG02_Door") { doorLauncher.minigameId = "MG02_Door"; changed = true; }

            // Dining table visual + the E interaction zone on the approach side.
            EnsurePropVisual(parent, "Visual_CH01_TABLE", tableSprite, new Vector3(3f, -1.05f, 0f),
                DuzheSortingLayers.GroundProps, 1f, ref changed);
            Interactable tableInt = EnsureCh01Prop(parent, "Zone_CH01_TABLE", "CH01_TABLE",
                "hint.table", new[] { "CH01_Stools" },
                new Vector3(3f, -3.5f, 0f), new Vector2(2f, 2f), ref changed);
            if (tableInt.GetComponent<Minigames.MinigameLauncher>() == null)
            { tableInt.gameObject.AddComponent<Minigames.MinigameLauncher>(); changed = true; }
            Minigames.MinigameLauncher tableLauncher = tableInt.GetComponent<Minigames.MinigameLauncher>();
            if (tableLauncher != null && tableLauncher.minigameId != "MG03_Stools") { tableLauncher.minigameId = "MG03_Stools"; changed = true; }

            // Three stool target positions: faint markers visible from the
            // start ("三个凳子目标位可见"), the real stools appear locked-in
            // when MG03_Stools completes (FarmCourtyardCast swaps them).
            Vector3[] stoolPoints =
            {
                new Vector3(1f, -2.55f, 0f),   // FC_StoolLeft
                new Vector3(5f, -2.55f, 0f),   // FC_StoolRight
                new Vector3(3f, -4.05f, 0f)    // FC_StoolFront
            };
            string[] stoolNames = { "Stool_CH01_Left", "Stool_CH01_Right", "Stool_CH01_Front" };
            string[] markerNames = { "StoolMarker_CH01_Left", "StoolMarker_CH01_Right", "StoolMarker_CH01_Front" };
            GameObject[] stools = new GameObject[3];
            GameObject[] markers = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject stool = FindOrCreateChild(parent, stoolNames[i], ref changed);
                SpriteRenderer stoolSr = stool.GetComponent<SpriteRenderer>();
                if (stoolSr == null) { stoolSr = stool.AddComponent<SpriteRenderer>(); changed = true; }
                if (stoolSr.sprite != stoolSprite) { stoolSr.sprite = stoolSprite; changed = true; }
                if (stoolSr.sortingLayerName != DuzheSortingLayers.GroundProps) { stoolSr.sortingLayerName = DuzheSortingLayers.GroundProps; changed = true; }
                if (stoolSr.color != Color.white) { stoolSr.color = Color.white; changed = true; }
                if (stool.transform.position != stoolPoints[i]) { stool.transform.position = stoolPoints[i]; changed = true; }
                if (stool.activeSelf) { stool.SetActive(false); changed = true; } // hidden until MG03 completes
                stools[i] = stool;

                GameObject marker = FindOrCreateChild(parent, markerNames[i], ref changed);
                SpriteRenderer mSr = marker.GetComponent<SpriteRenderer>();
                if (mSr == null) { mSr = marker.AddComponent<SpriteRenderer>(); changed = true; }
                if (mSr.sprite != stoolSprite) { mSr.sprite = stoolSprite; changed = true; }
                if (mSr.sortingLayerName != DuzheSortingLayers.GroundProps) { mSr.sortingLayerName = DuzheSortingLayers.GroundProps; changed = true; }
                Color markerTint = new Color(1f, 1f, 1f, 0.25f);
                if (mSr.color != markerTint) { mSr.color = markerTint; changed = true; }
                if (marker.transform.position != stoolPoints[i]) { marker.transform.position = stoolPoints[i]; changed = true; }
                markers[i] = marker;
            }

            // Grandson: appears at the door when MG02_Door completes, steps
            // into the gap between the door and the player; the farewell
            // presentation seats him at the front stool. Hidden until the knock.
            GameObject grandson = FindOrCreateChild(parent, "NPC_CH01_GRANDSON", ref changed);
            EnsureFeetVisual(grandson, grandsonSprite, Color.white, 1, ref changed);
            if (grandson.transform.position != new Vector3(0f, 5.1f, 0f))
            { grandson.transform.position = new Vector3(0f, 5.1f, 0f); changed = true; }
            if (grandson.activeSelf) { grandson.SetActive(false); changed = true; }

            // Two elder ghost escorts (rode the boat across); hidden until the
            // cast activates them beside the spawn point. Formal R11 sprites.
            Sprite elderSpriteA = LoadSprite("Art/Characters/CHR02_Grandfather");
            if (elderSpriteA == null) elderSpriteA = npcSprite;
            Sprite elderSpriteB = LoadSprite("Art/Characters/CHR03_Grandmother");
            if (elderSpriteB == null) elderSpriteB = npcSprite;
            Ritual.SpiritEscort elderA = EnsureElderGhost(parent, "ElderGhost_CH01_A", elderSpriteA,
                new Color(0.94f, 0.90f, 0.84f, 1f), new Vector2(-0.65f, -0.2f), ref changed);
            EnsureNpcWalk(elderA.gameObject, "CHR02_Grandfather", ref changed);
            Ritual.SpiritEscort elderB = EnsureElderGhost(parent, "ElderGhost_CH01_B", elderSpriteB,
                new Color(1f, 0.95f, 0.9f, 1f), new Vector2(0.65f, -0.2f), ref changed);
            EnsureNpcWalk(elderB.gameObject, "CHR03_Grandmother", ref changed);

            // Scene cast wiring (stool swap + grandson walk + elder activation).
            Scenes.FarmCourtyardCast cast = parent.GetComponent<Scenes.FarmCourtyardCast>();
            if (cast == null) { cast = parent.gameObject.AddComponent<Scenes.FarmCourtyardCast>(); changed = true; }
            if (cast.doorPoint != new Vector2(0f, 6.5f)) { cast.doorPoint = new Vector2(0f, 6.5f); changed = true; }
            if (cast.grandsonSpawnPoint != new Vector2(0f, 5.1f)) { cast.grandsonSpawnPoint = new Vector2(0f, 5.1f); changed = true; }
            if (cast.grandsonReunionPoint != new Vector2(0f, 4.25f)) { cast.grandsonReunionPoint = new Vector2(0f, 4.25f); changed = true; }
            if (cast.playerReunionPoint != new Vector2(0f, 2.25f)) { cast.playerReunionPoint = new Vector2(0f, 2.25f); changed = true; }
            if (cast.stoolMarkers == null || cast.stoolMarkers.Length != 3 || cast.stoolMarkers[0] != markers[0])
            { cast.stoolMarkers = markers; changed = true; }
            if (cast.stools == null || cast.stools.Length != 3 || cast.stools[0] != stools[0])
            { cast.stools = stools; changed = true; }
            if (cast.grandson != grandson) { cast.grandson = grandson; changed = true; }
            if (cast.elderA != elderA) { cast.elderA = elderA; changed = true; }
            if (cast.elderB != elderB) { cast.elderB = elderB; changed = true; }

            // Farewell presentation cast: seat points + the same courtyard link.
            Scenes.CH01FarewellCast farewell = EnsureSingleExternalComponent<Scenes.CH01FarewellCast>(parent.gameObject, ref changed);
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(farewell))))
            {
                report.AppendLine("FAIL FarmCourtyard: CH01FarewellCast script binding is not asset-backed");
                ok = false;
            }
            if (farewell.presentationId != "PR_CH01_Farewell") { farewell.presentationId = "PR_CH01_Farewell"; changed = true; }
            if (farewell.courtyard != cast) { farewell.courtyard = cast; changed = true; }
            if (farewell.elderSeatA == null || farewell.elderSeatA.name != "Seat_CH01_Left")
            { farewell.elderSeatA = EnsureSeatPoint(parent, "Seat_CH01_Left", new Vector3(1f, -2.75f, 0f), ref changed); }
            if (farewell.elderSeatB == null || farewell.elderSeatB.name != "Seat_CH01_Right")
            { farewell.elderSeatB = EnsureSeatPoint(parent, "Seat_CH01_Right", new Vector3(5f, -2.75f, 0f), ref changed); }
            if (farewell.grandsonSeat == null || farewell.grandsonSeat.name != "Seat_CH01_Front")
            { farewell.grandsonSeat = EnsureSeatPoint(parent, "Seat_CH01_Front", new Vector3(3f, -4.25f, 0f), ref changed); }

            EnsurePropVisual(parent, "Dog_CH01", LoadSprite("Art/Characters/CHR13_Dog"), new Vector3(-2.6f, 3.5f, 0f), DuzheSortingLayers.Actors, 1f, ref changed);
            GameObject dog = parent.Find("Dog_CH01").gameObject;
            EnsureRouteGate(dog, "CH01", ref changed, true);
            EnsureSingleExternalComponent<Scenes.CourtyardDog>(dog, ref changed);
            Scenes.ChapterMemoryCast montage01 = EnsureSingleExternalComponent<Scenes.ChapterMemoryCast>(parent.gameObject, ref changed);
            if (montage01.presentationId != "PR_CH01_Memory") { montage01.presentationId = "PR_CH01_Memory"; changed = true; }
            report.AppendLine("OK   FarmCourtyard CH01 assembly (door/table/stools/grandson/ghosts/casts) ensured");
            return ok;
        }

        /// C10 (R12): gives a moving NPC/spirit its 4-direction walk sheet.
        /// Missing sheets keep the static stand (never a broken sprite).
        private static void EnsureNpcWalk(GameObject go, string assetId, ref bool changed)
        {
            Sprite[] frames = Core.FormalArt.LoadWalkFrames(assetId);
            Player.NpcSpriteAnimator anim = go.GetComponent<Player.NpcSpriteAnimator>();
            if (frames == null)
            {
                if (anim != null) { UnityEngine.Object.DestroyImmediate(anim, true); changed = true; }
                return;
            }
            SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null) return;
            if (anim == null) { anim = go.AddComponent<Player.NpcSpriteAnimator>(); changed = true; }
            anim.Bind(frames, sr);
        }

        private static Ritual.SpiritEscort EnsureElderGhost(Transform parent, string name, Sprite sprite,
            Color tint, Vector2 followOffset, ref bool changed)
        {
            GameObject go = FindOrCreateChild(parent, name, ref changed);
            EnsureFeetVisual(go, sprite, tint, 1, ref changed);
            Ritual.SpiritEscort escort = go.GetComponent<Ritual.SpiritEscort>();
            if (escort == null) { escort = go.AddComponent<Ritual.SpiritEscort>(); changed = true; }
            // Two elders must never converge to the SAME follow point - an
            // identical offset stacks them into one visible sprite.
            if (escort.followOffset != followOffset) { escort.followOffset = followOffset; changed = true; }
            if (go.activeSelf) { go.SetActive(false); changed = true; } // cast activates beside the spawn
            return escort;
        }

        private static Transform EnsureSeatPoint(Transform parent, string name, Vector3 pos, ref bool changed)
        {
            Transform spots = FindOrCreateChild(parent, "SeatPoints_CH01", ref changed).transform;
            Transform t = spots.Find(name);
            if (t == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(spots, false);
                t = go.transform;
                changed = true;
            }
            if (t.position != pos) { t.position = pos; changed = true; }
            return t;
        }

        // ------------------------------------------------------------------
        // C05: formal CH02 assembly in the Park, all coordinates from R04
        // Park.layout.json via worldX=(px-640)/32, worldY=(360-py)/32:
        //   PK_Spawn (192,576) -> (-14,-6.75)   PK_Bench (864,256) -> (7,3.25)
        //   PK_Reunion (832,384) -> (6,-0.75)   PK_PastSelf (896,344) -> (8,0.5)
        //   bench solid [800,224,128,64] -> centre (7,3.5) size (4,1.5)
        // ------------------------------------------------------------------
        private static bool EnsureParkCh02(Transform parent, ref bool changed, StringBuilder report)
        {
            bool ok = true;
            Sprite boySprite = LoadSprite("Art/Characters/CHR06_PastSelfRomance");
            if (boySprite == null) boySprite = LoadSprite("Art/Characters/Npc_Placeholder");
            Sprite loverSprite = LoadSprite("Art/Characters/CHR05_LoverSpirit");
            if (loverSprite == null) loverSprite = LoadSprite("Art/Characters/Npc_Placeholder");

            // Static collision: the bench box (the bench itself is baked into
            // the BG04 background; only the collider is added here). C12: the
            // box now matches the painted bench exactly (art spans x 4.8..9.2,
            // y 2.3..4.06), so no invisible edge sticks out past the art.
            ok &= EnsureWall(parent, "Solid_PK_Bench", new Vector2(7f, 3.18f), new Vector2(4.4f, 1.76f), ref changed, report);

            // The boy (PastSelfRomance): waits at PK_PastSelf, auto-talks when
            // the player approaches during CH02_ReunionTalk.
            GameObject boy = FindOrCreateChild(parent, "NPC_CH02_BOY", ref changed);
            Interactable bInt = boy.GetComponent<Interactable>();
            if (bInt == null) { bInt = boy.AddComponent<Interactable>(); changed = true; }
            if (bInt.kind != InteractableKind.Npc) { bInt.kind = InteractableKind.Npc; changed = true; }
            if (bInt.interactableId != "CH02_PastSelfRomance") { bInt.interactableId = "CH02_PastSelfRomance"; changed = true; }
            if (bInt.displayName != "男孩") { bInt.displayName = "男孩"; changed = true; }
            if (bInt.autoTalkSegmentId != "CH02_ReunionTalk") { bInt.autoTalkSegmentId = "CH02_ReunionTalk"; changed = true; }
            if (bInt.returnSegmentId != "") { bInt.returnSegmentId = ""; changed = true; }
            if (!StringArrayEqual(bInt.activeStageIds, new[] { "CH02_ReunionTalk" }))
            { bInt.activeStageIds = new[] { "CH02_ReunionTalk" }; changed = true; }
            EnsureNpcBody(boy, boySprite, new Vector3(8f, 0.5f, 0f), Color.white, ref changed);

            // Reunion hotspot: E opens MG04_Heart (only after the reunion
            // dialogue has finished - the stage gate enforces that).
            Interactable heartInt = EnsureCh01Prop(parent, "Zone_CH02_HEART", "CH02_HEART",
                "", new[] { "CH02_Heart" },
                new Vector3(6f, -0.75f, 0f), new Vector2(2f, 2f), ref changed);
            if (heartInt.GetComponent<Minigames.MinigameLauncher>() == null)
            { heartInt.gameObject.AddComponent<Minigames.MinigameLauncher>(); changed = true; }
            Minigames.MinigameLauncher heartLauncher = heartInt.GetComponent<Minigames.MinigameLauncher>();
            if (heartLauncher != null && heartLauncher.minigameId != "MG04_Heart") { heartLauncher.minigameId = "MG04_Heart"; changed = true; }
            if (heartInt.displayName != "画心") { heartInt.displayName = "画心"; changed = true; }

            // The girl's ghost escort (rode the boat across); hidden until the
            // scene cast activates it beside the spawn point.
            Ritual.SpiritEscort ghost = EnsureElderGhost(parent, "Ghost_CH02_Park", loverSprite,
                new Color(1f, 0.94f, 0.96f, 1f), new Vector2(-0.65f, -0.2f), ref changed);
            EnsureNpcWalk(ghost.gameObject, "CHR05_LoverSpirit", ref changed);

            // Scene cast wiring (ghost activation + farewell presentation cast).
            Scenes.ParkCast cast = parent.GetComponent<Scenes.ParkCast>();
            if (cast == null) { cast = parent.gameObject.AddComponent<Scenes.ParkCast>(); changed = true; }
            if (cast.loverGhost != ghost) { cast.loverGhost = ghost; changed = true; }
            if (cast.boy != boy) { cast.boy = boy; changed = true; }

            Scenes.CH02FarewellCast farewell = EnsureSingleExternalComponent<Scenes.CH02FarewellCast>(parent.gameObject, ref changed);
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(farewell))))
            {
                report.AppendLine("FAIL Park: CH02FarewellCast script binding is not asset-backed");
                ok = false;
            }
            if (farewell.presentationId != "PR_CH02_Farewell") { farewell.presentationId = "PR_CH02_Farewell"; changed = true; }
            if (farewell.park != cast) { farewell.park = cast; changed = true; }
            if (farewell.embraceSpot == null || farewell.embraceSpot.name != "Seat_CH02_Embrace")
            { farewell.embraceSpot = EnsureSeatPoint(parent, "Seat_CH02_Embrace", new Vector3(6f, -0.75f, 0f), ref changed); }

            Scenes.CH02MemoryCast memory = EnsureSingleExternalComponent<Scenes.CH02MemoryCast>(parent.gameObject, ref changed);
            if (memory.presentationId != "PR_CH02_Memory") { memory.presentationId = "PR_CH02_Memory"; changed = true; }
            report.AppendLine("OK   Park CH02 assembly (bench/boy/reunion/ghost/casts) ensured");
            return ok;
        }

        // Scene components must resolve to a script ASSET, not a scene-local
        // MonoScript. Secondary MonoBehaviours sharing another class's file
        // previously produced local bindings, duplicated on every reassembly
        // and corrupted the player's serialized scene data.
        private static T EnsureSingleExternalComponent<T>(GameObject go, ref bool changed) where T : MonoBehaviour
        {
            if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go) > 0) changed = true;
            foreach (MonoBehaviour behaviour in go.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null) continue;
                MonoScript script = MonoScript.FromMonoBehaviour(behaviour);
                if (script != null && script.name == typeof(T).Name &&
                    string.IsNullOrEmpty(AssetDatabase.GetAssetPath(script)))
                {
                    UnityEngine.Object.DestroyImmediate(behaviour, true);
                    changed = true;
                }
            }

            T[] components = go.GetComponents<T>();
            for (int i = 1; i < components.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(components[i], true);
                changed = true;
            }
            T result = go.GetComponent<T>();
            if (result == null)
            {
                result = go.AddComponent<T>();
                changed = true;
            }
            return result;
        }

        private static bool StringArrayEqual(string[] a, string[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------
        // Shared CH01 assembly helpers
        // ------------------------------------------------------------------

        /// Ensures a Prop Interactable with a prompt at a world position.
        /// Returns the Interactable for further wiring (launcher/trigger are
        /// added by the caller afterwards - ref params cannot cross lambdas).
        private static Interactable EnsureCh01Prop(Transform parent, string goName, string interactableId,
            string promptKey, string[] activeStages, Vector3 pos, Vector2 colliderSize, ref bool changed)
        {
            GameObject go = FindOrCreateChild(parent, goName, ref changed);
            Interactable it = go.GetComponent<Interactable>();
            if (it == null) { it = go.AddComponent<Interactable>(); changed = true; }
            if (it.kind != InteractableKind.Prop) { it.kind = InteractableKind.Prop; changed = true; }
            if (it.interactableId != interactableId) { it.interactableId = interactableId; changed = true; }
            if (it.promptKey != promptKey) { it.promptKey = promptKey; changed = true; }
            if (!StringArrayEqual(it.activeStageIds, activeStages)) { it.activeStageIds = activeStages; changed = true; }
            if (go.transform.position != pos) { go.transform.position = pos; changed = true; }
            if (go.layer != DuzheLayers.Trigger) { go.layer = DuzheLayers.Trigger; changed = true; }
            BoxCollider2D col = go.GetComponent<BoxCollider2D>();
            if (col == null) { col = go.AddComponent<BoxCollider2D>(); changed = true; }
            if (!col.isTrigger) { col.isTrigger = true; changed = true; }
            if (col.size != colliderSize) { col.size = colliderSize; changed = true; }
            if (col.offset != Vector2.zero) { col.offset = Vector2.zero; changed = true; }
            return it;
        }

        /// Adds a SpriteRenderer child visual (no collider) for a prop point.
        private static void EnsurePropVisual(Transform parent, string goName, Sprite sprite, Vector3 pos,
            string sortingLayer, float alpha, ref bool changed)
        {
            GameObject go = FindOrCreateChild(parent, goName, ref changed);
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) { sr = go.AddComponent<SpriteRenderer>(); changed = true; }
            if (sr.sprite != sprite) { sr.sprite = sprite; changed = true; }
            if (sr.sortingLayerName != sortingLayer) { sr.sortingLayerName = sortingLayer; changed = true; }
            Color want = new Color(1f, 1f, 1f, alpha);
            if (sr.color != want) { sr.color = want; changed = true; }
            if (go.transform.position != pos) { go.transform.position = pos; changed = true; }
        }

        /// Route-visibility gate: DEV and CH01 objects share the same layout
        /// active state unless the caller passes an explicit value (ghosts and
        /// other later-appearing actors pass false).
        private static void EnsureRouteGate(GameObject go, string chapterId, ref bool changed,
            bool? visibleOnChapterEntry = null)
        {
            Chapters.ChapterObjectVisibility gate = go.GetComponent<Chapters.ChapterObjectVisibility>();
            if (gate == null) { gate = go.AddComponent<Chapters.ChapterObjectVisibility>(); changed = true; }
            if (gate.chapterId != chapterId) { gate.chapterId = chapterId; changed = true; }
            bool want = visibleOnChapterEntry ?? go.activeSelf;
            if (gate.visibleOnChapterEntry != want) { gate.visibleOnChapterEntry = want; changed = true; }
        }

        /// Hides an object once the given minigame completes (cane station).
        private static void EnsureHideOnMinigame(GameObject go, string minigameId, string chapterId, ref bool changed)
        {
            Chapters.HideOnChapterEvent h = go.GetComponent<Chapters.HideOnChapterEvent>();
            if (h == null) { h = go.AddComponent<Chapters.HideOnChapterEvent>(); changed = true; }
            if (h.eventType != Core.GameEventType.MinigameCompleted) { h.eventType = Core.GameEventType.MinigameCompleted; changed = true; }
            if (h.sourceId != minigameId) { h.sourceId = minigameId; changed = true; }
            if (h.chapterId != chapterId) { h.chapterId = chapterId; changed = true; }
        }

        /// Finds a child BY NAME under a specific parent (finds inactive
        /// children too, unlike GameObject.Find) or creates it.
        private static GameObject FindOrCreateChild(Transform parent, string name, ref bool changed)
        {
            Transform t = parent.Find(name);
            if (t != null) return t.gameObject;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            changed = true;
            return go;
        }

        private static GameObject FindOrCreate(string name, ref bool changed, Transform parent = null)
        {
            GameObject found = GameObject.Find(name);
            if (found == null)
            {
                found = new GameObject(name);
                if (parent != null) found.transform.SetParent(parent, false);
                changed = true;
            }
            return found;
        }

        private static Sprite LoadSprite(string artRelativePath)
        {
            // artRelativePath is relative to Assets/_Project/Resources/Art
            // (calls pass "Art/<subdir>/<name>"; strip the redundant "Art/").
            string p = artRelativePath.StartsWith("Art/", StringComparison.OrdinalIgnoreCase)
                ? artRelativePath.Substring(4)
                : artRelativePath;
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/" + p + ".png");
        }

        /// Delivered art first, the development placeholder only as fallback -
        /// so dropping the formal PNG in never requires touching the assembly.
        private static Sprite LoadFormalOrPlaceholder(string formalPath, string placeholderPath)
        {
            Sprite formal = LoadSprite(formalPath);
            return formal != null ? formal : LoadSprite(placeholderPath);
        }

        // ------------------------------------------------------------------
        // Build Settings
        // ------------------------------------------------------------------
        public static bool EnsureBuildSettings(StringBuilder report)
        {
            string[] wanted = GetScenePaths();
            List<EditorBuildSettingsScene> next = new List<EditorBuildSettingsScene>();
            foreach (string p in wanted)
            {
                next.Add(new EditorBuildSettingsScene(p, true));
            }
            EditorBuildSettings.scenes = next.ToArray();
            report.AppendLine("OK   Build Settings: " + wanted.Length + " scenes registered");
            return true;
        }

        public static string[] GetScenePaths()
        {
            List<string> paths = new List<string>();
            foreach ((string sceneId, bool _) in ScenePlan)
            {
                paths.Add(ScenesDir + "/" + sceneId + ".unity");
            }
            return paths.ToArray();
        }
    }
}
