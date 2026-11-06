using System.Collections.Generic;

namespace Duzhe.Chapters
{
    /// Registry of all chapters. Formal CH01-CH05 are added here by tasks
    /// C04-C08; C01 ships only the placeholder dev chapter that exercises
    /// the whole pipeline (auto-talk once, item pickup, hand-over, dialogue
    /// completion, save). All display strings in this chapter are marked
    /// Placeholder and are NOT the final script.
    public static class ChapterCatalog
    {
        private static readonly List<ChapterConfig> all = new List<ChapterConfig>();
        private static readonly List<string> mainRunOrder = new List<string>();

        static ChapterCatalog()
        {
            Register(BuildDevChapter());
            Register(BuildChapter01());
            Register(BuildChapter02());
            Register(BuildChapter03());
            Register(BuildChapter04());
            Register(BuildChapter05());
        }

        public static void Register(ChapterConfig cfg)
        {
            if (cfg.chapterId == "CH01" || cfg.chapterId == "CH02" || cfg.chapterId == "CH03" || cfg.chapterId == "CH04")
            {
                cfg.riverSettings.forwardSpeed = 6f;
                cfg.riverSettings.steerSpeed = 5.5f;
                cfg.riverSettings.cameraYOffset = 3.5f;
                cfg.riverSettings.whirlHalfWidth = 0.6f;
                cfg.riverSettings.whirlHalfHeight = 0.6f;
                cfg.riverSettings.whirlDriftSpeed = 0.28f;
                cfg.riverSettings.waveArrivalTimes = new[] { 3f, 4.2f, 5.4f, 6.6f, 7.8f, 9f };
            }
            all.Add(cfg);
            mainRunOrder.Add(cfg.chapterId);
        }

        public static ChapterConfig GetChapter(string chapterId)
        {
            foreach (ChapterConfig c in all)
            {
                if (c.chapterId == chapterId) return c;
            }
            return null;
        }

        public static IReadOnlyList<string> MainRunOrder { get { return mainRunOrder; } }

        /// The five FORMAL chapter ids. Anything else (e.g. the CH01_DEV
        /// development route) never counts towards formal progress, menus or
        /// completion displays.
        public static readonly string[] FormalChapterIds = { "CH01", "CH02", "CH03", "CH04", "CH05" };

        public static bool IsFormalChapter(string id)
        {
            foreach (string formal in FormalChapterIds)
            {
                if (formal == id) return true;
            }
            return false;
        }

        /// How many formal chapters exist in this build (CH05 is added by
        /// C08, so this is 4 until then). The menu uses it to explain why
        /// "start" replays CH01 once every registered chapter is finished.
        public static int RegisteredFormalChapterCount()
        {
            int count = 0;
            foreach (string id in FormalChapterIds)
            {
                if (GetChapter(id) != null) count++;
            }
            return count;
        }

        /// The formal chapter after the given one, skipping chapters this
        /// build does not register. Empty when the chain ends (after CH05 or
        /// for an unknown id) - the main run then returns to the start menu.
        public static string GetNextFormalChapterId(string chapterId)
        {
            int index = System.Array.IndexOf(FormalChapterIds, chapterId);
            if (index < 0) return "";
            for (int i = index + 1; i < FormalChapterIds.Length; i++)
            {
                if (GetChapter(FormalChapterIds[i]) != null) return FormalChapterIds[i];
            }
            return "";
        }

        /// First unfinished FORMAL chapter of the chain (CH01-CH05). Chapters
        /// that are not registered yet (later tasks C05-C08) are skipped, so
        /// the menu always starts the newest available formal chapter. When
        /// every registered formal chapter is done, CH01 re-runs replay-style
        /// keeping all unlocks (spec section 9). The CH01_DEV route is a
        /// development aid and is never chosen here.
        public static string GetFirstUnfinishedChapterId(Core.SaveService save)
        {
            foreach (string id in FormalChapterIds)
            {
                if (GetChapter(id) == null) continue; // registered by C05+
                if (save == null || !save.HasCompletedChapter(id)) return id;
            }
            return "CH01";
        }

        /// Speaker display name lookup across all chapters (dialogue view).
        /// Falls back to the delivered CharacterDisplayNames.json (imported by
        /// C04), then to the caller's fallback.
        public static string GetActorDisplayName(string actorId, string fallback)
        {
            foreach (ChapterConfig c in all)
            {
                string name = c.GetActorDisplayName(actorId);
                if (!string.IsNullOrEmpty(name)) return name;
            }
            Dialogue.DialogueLibrary lib = Dialogue.DialogueLibrary.Instance;
            string global = lib != null ? lib.GetGlobalActorDisplayName(actorId) : "";
            return !string.IsNullOrEmpty(global) ? global : fallback;
        }

        // ------------------------------------------------------------------
        // C01 development test chapter. Every string is a Placeholder.
        // Formal chapters replace nothing here; they are added separately.
        // ------------------------------------------------------------------
        private static ChapterConfig BuildDevChapter()
        {
            ChapterConfig cfg = new ChapterConfig
            {
                chapterId = "CH01_DEV",
                displayName = "开发测试章（Placeholder）",
                startSceneId = Core.DuzheIds.SceneUnderworld,
                spawnPointId = "SP_Main",
                dialogueChapterId = "CH01_DEV",
                stages = new StageDefinition[]
                {
                    new StageDefinition
                    {
                        stageId = "ST01_INTRO",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "走近河岸的老人（Placeholder）",
                        completionEvent = Core.GameEventType.DialogueFinished,
                        expectedSourceId = "ST01_INTRO",
                        nextStageId = "ST02_FIND"
                    },
                    new StageDefinition
                    {
                        stageId = "ST02_FIND",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "找到老人的眼镜（Placeholder）",
                        completionEvent = Core.GameEventType.ItemCollected,
                        expectedSourceId = "DEV_GLASSES",
                        nextStageId = "ST03_RETURN"
                    },
                    new StageDefinition
                    {
                        stageId = "ST03_RETURN",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "把眼镜还给老人（Placeholder）",
                        completionEvent = Core.GameEventType.DialogueFinished,
                        expectedSourceId = "ST03_THANKS",
                        nextStageId = "ST04_RITUAL"
                    },
                    new StageDefinition
                    {
                        stageId = "ST04_RITUAL",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "在渡口按 E 举行送别仪式（Placeholder）",
                        completionEvent = Core.GameEventType.RitualCompleted,
                        expectedSourceId = "DEV_RITUAL_ANCHOR",
                        nextStageId = "ST05_BOARD"
                    },
                    new StageDefinition
                    {
                        stageId = "ST05_BOARD",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "登船（Placeholder）",
                        completionEvent = Core.GameEventType.Boarded,
                        expectedSourceId = "DEV_BOAT",
                        nextStageId = "ST06_RIVER"
                    },
                    new StageDefinition
                    {
                        stageId = "ST06_RIVER",
                        sceneId = Core.DuzheIds.SceneRiver,
                        inputMode = Core.InputMode.River,
                        objectiveText = "渡过冥河，避开漩涡（Placeholder）",
                        completionEvent = Core.GameEventType.RiverSucceeded,
                        expectedSourceId = "River",
                        nextStageId = ""
                    }
                }
            };
            // DEV chapter mimics CH01 rules: two passengers, one banned lane
            // per wave.
            cfg.riverSettings.passengerCount = 2;
            cfg.riverSettings.maxBannedLanesPerWave = 1;
            cfg.actorDisplayNames["DUKE"] = "渡客（Placeholder）";
            cfg.actorDisplayNames["DEV_GRANDPA"] = "老人（Placeholder）";
            cfg.itemTargetActors["DEV_GLASSES"] = "DEV_GRANDPA";
            return cfg;
        }

        // ------------------------------------------------------------------
        // C04: formal CH01 「别让等待成为遗憾」. Twelve stages from the delivery
        // stage table (04 第一关规范 v1.0). All display text comes from the
        // delivered TXT_CH01.json / TXT_UI.json; this config only wires ids,
        // conditions and scene routing. No Placeholder text below.
        // ------------------------------------------------------------------
        private static ChapterConfig BuildChapter01()
        {
            ChapterConfig cfg = new ChapterConfig
            {
                chapterId = "CH01",
                displayName = "别让等待成为遗憾",
                startSceneId = Core.DuzheIds.SceneUnderworld,
                spawnPointId = "SP_Main", // per-scene spawn: Underworld (0,-6), FarmCourtyard (-11,-6.75)
                dialogueChapterId = "CH01",
                presentationIds = new[] { "PR_CH01_Memory", "PR_CH01_Farewell" },
                stages = new StageDefinition[]
                {
                    // 1. Approach the elderly couple -> auto dialogue once.
                    new StageDefinition
                    {
                        stageId = "CH01_Opening",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "走近两位老人，听听他们需要什么。",
                        entryHintKey = "hint.move",
                        completionEvent = Core.GameEventType.DialogueFinished,
                        expectedSourceId = "CH01_Opening",
                        nextStageId = "CH01_Prepare"
                    },
                    // 2. Glasses pickup AND traced cane, in any order.
                    new StageDefinition
                    {
                        stageId = "CH01_Prepare",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "找回老花镜，并按轮廓画出拐杖。",
                        requireAll = new StageCondition[]
                        {
                            new StageCondition { type = Core.GameEventType.ItemCollected, sourceId = "IT01_Glasses" },
                            new StageCondition { type = Core.GameEventType.MinigameCompleted, sourceId = "MG01_Cane", grantsItemId = "IT02_Cane" }
                        },
                        nextStageId = "CH01_ReturnItems"
                    },
                    // 3. Both items back in ONE hand-over at the elderly.
                    new StageDefinition
                    {
                        stageId = "CH01_ReturnItems",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "回到老人身边，按 E 交还眼镜和拐杖。",
                        requiredItemIds = new[] { "IT01_Glasses", "IT02_Cane" },
                        completionEvent = Core.GameEventType.ItemsReturned,
                        expectedSourceId = "CH01_GRANDFATHER",
                        nextStageId = "CH01_RitualTalk"
                    },
                    // 4. Manual ritual-guidance dialogue (auto-started).
                    new StageDefinition
                    {
                        stageId = "CH01_RitualTalk",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "听完两位老人的话。",
                        autoStartSegmentId = "CH01_RitualTalk",
                        completionEvent = Core.GameEventType.DialogueFinished,
                        expectedSourceId = "CH01_RitualTalk",
                        nextStageId = "CH01_Ritual"
                    },
                    // 5. E ritual at the anchor; opens the dock.
                    new StageDefinition
                    {
                        stageId = "CH01_Ritual",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "靠近石灯，按 E 开始仪式。",
                        completionEvent = Core.GameEventType.RitualCompleted,
                        expectedSourceId = "CH01_RITUAL",
                        nextStageId = "CH01_Board"
                    },
                    // 6. Board the boat at the dock.
                    new StageDefinition
                    {
                        stageId = "CH01_Board",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "带两位老人前往渡口，按 E 上船。",
                        completionEvent = Core.GameEventType.Boarded,
                        expectedSourceId = "CH01_BOAT",
                        nextStageId = "CH01_River"
                    },
                    // 7. The crossing; a crash retries locally with all flags kept.
                    new StageDefinition
                    {
                        stageId = "CH01_River",
                        sceneId = Core.DuzheIds.SceneRiver,
                        inputMode = Core.InputMode.River,
                        objectiveText = "左右移动船只，避开漩涡。",
                        completionEvent = Core.GameEventType.RiverSucceeded,
                        expectedSourceId = "River",
                        nextStageId = "CH01_Door"
                    },
                    // 8. Door close-up: two isolated clicks (door, then knocker).
                    new StageDefinition
                    {
                        stageId = "CH01_Door",
                        sceneId = Core.DuzheIds.SceneFarmCourtyard,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "靠近院门按 E 开门，再点击放大画面中的门环。",
                        entryHintKey = "hint.door",
                        completionEvent = Core.GameEventType.MinigameCompleted,
                        expectedSourceId = "MG02_Door",
                        nextStageId = "CH01_ReunionTalk"
                    },
                    // 9. Reunion dialogue (auto-started after the knock).
                    new StageDefinition
                    {
                        stageId = "CH01_ReunionTalk",
                        sceneId = Core.DuzheIds.SceneFarmCourtyard,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "陪他们说说话。",
                        autoStartSegmentId = "CH01_ReunionTalk",
                        completionEvent = Core.GameEventType.DialogueFinished,
                        expectedSourceId = "CH01_ReunionTalk",
                        nextStageId = "CH01_Stools"
                    },
                    // 10. Drag three stools into the three seats.
                    new StageDefinition
                    {
                        stageId = "CH01_Stools",
                        sceneId = Core.DuzheIds.SceneFarmCourtyard,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "靠近餐桌按 E，将三把凳子放进空位。",
                        completionEvent = Core.GameEventType.MinigameCompleted,
                        expectedSourceId = "MG03_Stools",
                        nextStageId = "CH01_Memory"
                    },
                    // 11. The farewell presentation: seated meal dialogue, then
                    //     the two elders fade out; PresentationFinished unlocks
                    //     the gallery record. This - not the river - completes
                    //     the chapter.
                    new StageDefinition
                    {
                        stageId = "CH01_Memory", sceneId = Core.DuzheIds.SceneFarmCourtyard,
                        inputMode = Core.InputMode.Exploring, objectiveText = "想起一家人的日子。",
                        autoStartPresentationId = "PR_CH01_Memory", completionEvent = Core.GameEventType.PresentationFinished,
                        expectedSourceId = "PR_CH01_Memory", nextStageId = "CH01_Farewell"
                    },
                    new StageDefinition
                    {
                        stageId = "CH01_Farewell",
                        sceneId = Core.DuzheIds.SceneFarmCourtyard,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "坐下来，吃完这顿团圆饭。",
                        autoStartPresentationId = "PR_CH01_Farewell",
                        completionEvent = Core.GameEventType.PresentationFinished,
                        expectedSourceId = "PR_CH01_Farewell",
                        nextStageId = "CH01_Complete"
                    },
                    // 12. Short fade-out, then ChapterCompleted (main line to
                    //     CH02; replays return to the chapter list in C09).
                    new StageDefinition
                    {
                        stageId = "CH01_Complete",
                        sceneId = Core.DuzheIds.SceneFarmCourtyard,
                        inputMode = Core.InputMode.Ritual, // locked while fading
                        objectiveText = "这一程，已经完成。",
                        autoAdvanceSeconds = 1.4f,
                        nextStageId = ""
                    }
                }
            };
            // CH01 rule set: two passengers, one banned lane per wave.
            cfg.riverSettings.passengerCount = 2;
            cfg.riverSettings.maxBannedLanesPerWave = 1;
            // Speaker names live in the delivered CharacterDisplayNames.json;
            // these config entries are the fallback if that file is missing.
            cfg.actorDisplayNames["Ferryman"] = "渡客";
            cfg.actorDisplayNames["Grandfather"] = "爷爷";
            cfg.actorDisplayNames["Grandmother"] = "奶奶";
            cfg.actorDisplayNames["PastSelfFamily"] = "孙子";
            cfg.itemTargetActors["IT01_Glasses"] = "Grandfather";
            cfg.itemTargetActors["IT02_Cane"] = "Grandfather";
            return cfg;
        }

        // ------------------------------------------------------------------
        // C05: formal CH02 「真心为你」. Eleven stages from the delivery stage
        // table (05 第二关规范 v1.0). All display text comes from the delivered
        // TXT_CH02.json / TXT_UI.json; this config only wires ids, conditions
        // and scene routing. No Placeholder text below.
        // ------------------------------------------------------------------
        private static ChapterConfig BuildChapter02()
        {
            ChapterConfig cfg = new ChapterConfig
            {
                chapterId = "CH02",
                displayName = "真心为你",
                startSceneId = Core.DuzheIds.SceneUnderworld,
                spawnPointId = "SP_Main",
                dialogueChapterId = "CH02",
                presentationIds = new[] { "PR_CH02_Memory", "PR_CH02_Farewell" },
                stages = new StageDefinition[]
                {
                    // 1. Approach the girl -> auto dialogue once.
                    new StageDefinition
                    {
                        stageId = "CH02_Opening",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "走近女孩，听她说说项链的事。",
                        completionEvent = Core.GameEventType.DialogueFinished,
                        expectedSourceId = "CH02_Opening",
                        nextStageId = "CH02_FindNecklace"
                    },
                    // 2. Pick up the necklace.
                    new StageDefinition
                    {
                        stageId = "CH02_FindNecklace",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "在附近找到女孩丢失的项链。",
                        completionEvent = Core.GameEventType.ItemCollected,
                        expectedSourceId = "IT03_Necklace",
                        nextStageId = "CH02_ReturnNecklace"
                    },
                    // 3. Return it to the girl.
                    new StageDefinition
                    {
                        stageId = "CH02_ReturnNecklace",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "回到女孩身边，按 E 交还项链。",
                        requiredItemIds = new[] { "IT03_Necklace" },
                        completionEvent = Core.GameEventType.ItemsReturned,
                        expectedSourceId = "CH02_LoverSpirit",
                        nextStageId = "CH02_RitualTalk"
                    },
                    // 4. Manual ritual-guidance dialogue (auto-started).
                    new StageDefinition
                    {
                        stageId = "CH02_RitualTalk",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "听完女孩的话。",
                        autoStartSegmentId = "CH02_RitualTalk",
                        completionEvent = Core.GameEventType.DialogueFinished,
                        expectedSourceId = "CH02_RitualTalk",
                        nextStageId = "CH02_Ritual"
                    },
                    // 5. E ritual at the anchor; opens the dock.
                    new StageDefinition
                    {
                        stageId = "CH02_Ritual",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "靠近石灯，按 E 开始仪式。",
                        completionEvent = Core.GameEventType.RitualCompleted,
                        expectedSourceId = "CH02_RITUAL",
                        nextStageId = "CH02_Board"
                    },
                    // 6. Board the boat at the dock.
                    new StageDefinition
                    {
                        stageId = "CH02_Board",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "带女孩前往渡口，按 E 上船。",
                        completionEvent = Core.GameEventType.Boarded,
                        expectedSourceId = "CH02_BOAT",
                        nextStageId = "CH02_River"
                    },
                    // 7. The crossing; a crash retries locally with all flags kept.
                    new StageDefinition
                    {
                        stageId = "CH02_River",
                        sceneId = Core.DuzheIds.SceneRiver,
                        inputMode = Core.InputMode.River,
                        objectiveText = "左右移动船只，避开漩涡。",
                        completionEvent = Core.GameEventType.RiverSucceeded,
                        expectedSourceId = "River",
                        nextStageId = "CH02_ReunionTalk"
                    },
                    // 8. Reunion dialogue (auto-started when approaching the boy).
                    new StageDefinition
                    {
                        stageId = "CH02_ReunionTalk",
                        sceneId = Core.DuzheIds.ScenePark,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "走近长椅旁的男孩。",
                        completionEvent = Core.GameEventType.DialogueFinished,
                        expectedSourceId = "CH02_ReunionTalk",
                        nextStageId = "CH02_Heart"
                    },
                    // 9. Draw the heart at the reunion point.
                    new StageDefinition
                    {
                        stageId = "CH02_Heart",
                        sceneId = Core.DuzheIds.ScenePark,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "按住鼠标，沿引导画出完整的爱心。",
                        completionEvent = Core.GameEventType.MinigameCompleted,
                        expectedSourceId = "MG04_Heart",
                        nextStageId = "CH02_Memory"
                    },
                    new StageDefinition
                    {
                        stageId = "CH02_Memory",
                        sceneId = Core.DuzheIds.ScenePark,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "听他们说起从前。",
                        autoStartPresentationId = "PR_CH02_Memory",
                        completionEvent = Core.GameEventType.PresentationFinished,
                        expectedSourceId = "PR_CH02_Memory",
                        nextStageId = "CH02_Farewell"
                    },
                    // 10. The farewell presentation: embrace dialogue, then the
                    //     girl fades out; PresentationFinished unlocks the
                    //     gallery record. This - not the river - completes the chapter.
                    new StageDefinition
                    {
                        stageId = "CH02_Farewell",
                        sceneId = Core.DuzheIds.ScenePark,
                        inputMode = Core.InputMode.Exploring,
                        objectiveText = "陪他们完成这次告别。",
                        autoStartPresentationId = "PR_CH02_Farewell",
                        completionEvent = Core.GameEventType.PresentationFinished,
                        expectedSourceId = "PR_CH02_Farewell",
                        nextStageId = "CH02_Complete"
                    },
                    // 11. Short fade-out, then ChapterCompleted (main line to
                    //     CH03; replays return to the chapter list in C09).
                    new StageDefinition
                    {
                        stageId = "CH02_Complete",
                        sceneId = Core.DuzheIds.ScenePark,
                        inputMode = Core.InputMode.Ritual, // locked while fading
                        objectiveText = "这一程，已经完成。",
                        autoAdvanceSeconds = 1.4f,
                        nextStageId = ""
                    }
                }
            };
            // CH02 rule set: one passenger, two banned lanes per wave.
            cfg.riverSettings.passengerCount = 1;
            cfg.riverSettings.maxBannedLanesPerWave = 2;
            // Speaker names live in the delivered CharacterDisplayNames.json;
            // these config entries are the fallback if that file is missing.
            cfg.actorDisplayNames["Ferryman"] = "渡客";
            cfg.actorDisplayNames["LoverSpirit"] = "女孩";
            cfg.actorDisplayNames["PastSelfRomance"] = "男孩";
            cfg.itemTargetActors["IT03_Necklace"] = "LoverSpirit";
            return cfg;
        }

        // ------------------------------------------------------------------
        // C06: formal CH03 「组一辈子乐队」. Twelve stages from the delivery
        // stage table (06 第三关规范 v1.0). Objectives and every spoken line
        // come from TXT_CH03.json; this config wires ids only (objectiveText
        // stays empty on purpose so a missing objective is a validation
        // failure instead of a silently shipped fallback).
        // ------------------------------------------------------------------
        private static StageDefinition Ch03Stage(string id, string scene,
            Core.GameEventType evt, string source, string next)
        {
            return new StageDefinition
            {
                stageId = id,
                sceneId = scene,
                inputMode = Core.InputMode.Exploring,
                completionEvent = evt,
                expectedSourceId = source,
                nextStageId = next,
                objectiveText = ""
            };
        }

        private static ChapterConfig BuildChapter03()
        {
            StageDefinition opening = Ch03Stage("CH03_Opening", Core.DuzheIds.SceneUnderworld,
                Core.GameEventType.DialogueFinished, "CH03_Opening", "CH03_FindDrumsticks");
            opening.entryHintKey = "hint.move";

            StageDefinition find = Ch03Stage("CH03_FindDrumsticks", Core.DuzheIds.SceneUnderworld,
                Core.GameEventType.ItemCollected, "IT04_Drumsticks", "CH03_ReturnDrumsticks");

            // One quest item (a pair of drumsticks), handed over in one E.
            StageDefinition give = Ch03Stage("CH03_ReturnDrumsticks", Core.DuzheIds.SceneUnderworld,
                Core.GameEventType.ItemsReturned, "CH03_DrummerSpirit", "CH03_RitualTalk");
            give.requiredItemIds = new[] { "IT04_Drumsticks" };

            StageDefinition ritualTalk = Ch03Stage("CH03_RitualTalk", Core.DuzheIds.SceneUnderworld,
                Core.GameEventType.DialogueFinished, "CH03_RitualTalk", "CH03_Ritual");
            ritualTalk.autoStartSegmentId = "CH03_RitualTalk";

            StageDefinition ritual = Ch03Stage("CH03_Ritual", Core.DuzheIds.SceneUnderworld,
                Core.GameEventType.RitualCompleted, "CH03_RITUAL", "CH03_Board");

            StageDefinition board = Ch03Stage("CH03_Board", Core.DuzheIds.SceneUnderworld,
                Core.GameEventType.Boarded, "CH03_BOAT", "CH03_River");

            StageDefinition river = Ch03Stage("CH03_River", Core.DuzheIds.SceneRiver,
                Core.GameEventType.RiverSucceeded, "River", "CH03_ReunionTalk");
            river.inputMode = Core.InputMode.River;

            // The three living bandmates wait in the studio; the drummer's
            // spirit auto-talks when the player walks up to them.
            StageDefinition reunion = Ch03Stage("CH03_ReunionTalk", Core.DuzheIds.SceneRecordingStudio,
                Core.GameEventType.DialogueFinished, "CH03_ReunionTalk", "CH03_Perform");

            StageDefinition perform = Ch03Stage("CH03_Perform", Core.DuzheIds.SceneRecordingStudio,
                Core.GameEventType.MinigameCompleted, "MG05_Notes", "CH03_PhotoTalk");

            StageDefinition photoTalk = Ch03Stage("CH03_PhotoTalk", Core.DuzheIds.SceneRecordingStudio,
                Core.GameEventType.DialogueFinished, "CH03_PhotoTalk", "CH03_Photo");
            photoTalk.autoStartSegmentId = "CH03_PhotoTalk";

            StageDefinition photo = Ch03Stage("CH03_Photo", Core.DuzheIds.SceneRecordingStudio,
                Core.GameEventType.MinigameCompleted, "MG06_Photo", "CH03_Memory");

            StageDefinition memory = Ch03Stage("CH03_Memory", Core.DuzheIds.SceneRecordingStudio,
                Core.GameEventType.PresentationFinished, "PR_CH03_Memory", "CH03_Farewell");
            memory.autoStartPresentationId = "PR_CH03_Memory";

            StageDefinition farewell = Ch03Stage("CH03_Farewell", Core.DuzheIds.SceneRecordingStudio,
                Core.GameEventType.PresentationFinished, "PR_CH03_Farewell", "CH03_Complete");
            farewell.autoStartPresentationId = "PR_CH03_Farewell";

            StageDefinition complete = Ch03Stage("CH03_Complete", Core.DuzheIds.SceneRecordingStudio,
                Core.GameEventType.DialogueFinished, "", "");
            complete.inputMode = Core.InputMode.Ritual; // locked while fading
            complete.autoAdvanceSeconds = 1.4f;

            ChapterConfig cfg = new ChapterConfig
            {
                chapterId = "CH03",
                displayName = "组一辈子乐队",
                startSceneId = Core.DuzheIds.SceneUnderworld,
                spawnPointId = "SP_Main",
                dialogueChapterId = "CH03",
                presentationIds = new[] { "PR_CH03_Memory", "PR_CH03_Farewell" },
                stages = new[]
                {
                    opening, find, give, ritualTalk, ritual, board,
                    river, reunion, perform, photoTalk, photo, memory, farewell, complete
                }
            };
            // CH03 rule set: one passenger, two banned lanes per wave.
            cfg.riverSettings.passengerCount = 1;
            cfg.riverSettings.maxBannedLanesPerWave = 2;
            cfg.enablePerformance = true;
            // Speaker names live in the delivered CharacterDisplayNames.json;
            // these entries are the fallback if that file is missing.
            cfg.actorDisplayNames["Ferryman"] = "渡客";
            cfg.actorDisplayNames["DrummerSpirit"] = "鼓手";
            cfg.actorDisplayNames["PastSelfBand"] = "队友";
            cfg.actorDisplayNames["BandmateA"] = "蓝衣队友";
            cfg.actorDisplayNames["BandmateB"] = "黄衣队友";
            cfg.itemTargetActors["IT04_Drumsticks"] = "DrummerSpirit";
            return cfg;
        }

        // ------------------------------------------------------------------
        // C07: formal CH04 「最后一课」. Twelve stages from the delivery stage
        // table (07 第四关规范 v1.0). Objectives and every line come from
        // TXT_CH04.json; the classroom lesson and the goodbye writing are two
        // separate stages with a real gate between them.
        // ------------------------------------------------------------------
        private static StageDefinition Ch04Stage(string id, string scene,
            Core.GameEventType evt, string source, string next)
        {
            return new StageDefinition
            {
                stageId = id,
                sceneId = scene,
                inputMode = Core.InputMode.Exploring,
                completionEvent = evt,
                expectedSourceId = source,
                nextStageId = next,
                objectiveText = ""
            };
        }

        private static ChapterConfig BuildChapter04()
        {
            StageDefinition opening = Ch04Stage("CH04_Opening", Core.DuzheIds.SceneUnderworld,
                Core.GameEventType.DialogueFinished, "CH04_Opening", "CH04_FindLessonPlan");
            opening.entryHintKey = "hint.move";

            StageDefinition find = Ch04Stage("CH04_FindLessonPlan", Core.DuzheIds.SceneUnderworld,
                Core.GameEventType.ItemCollected, "IT05_LessonPlan", "CH04_ReturnLessonPlan");

            StageDefinition give = Ch04Stage("CH04_ReturnLessonPlan", Core.DuzheIds.SceneUnderworld,
                Core.GameEventType.ItemsReturned, "CH04_TeacherSpirit", "CH04_RitualTalk");
            give.requiredItemIds = new[] { "IT05_LessonPlan" };

            StageDefinition ritualTalk = Ch04Stage("CH04_RitualTalk", Core.DuzheIds.SceneUnderworld,
                Core.GameEventType.DialogueFinished, "CH04_RitualTalk", "CH04_Ritual");
            ritualTalk.autoStartSegmentId = "CH04_RitualTalk";

            StageDefinition ritual = Ch04Stage("CH04_Ritual", Core.DuzheIds.SceneUnderworld,
                Core.GameEventType.RitualCompleted, "CH04_RITUAL", "CH04_Board");

            StageDefinition board = Ch04Stage("CH04_Board", Core.DuzheIds.SceneUnderworld,
                Core.GameEventType.Boarded, "CH04_BOAT", "CH04_River");

            StageDefinition river = Ch04Stage("CH04_River", Core.DuzheIds.SceneRiver,
                Core.GameEventType.RiverSucceeded, "River", "CH04_ReunionTalk");
            river.inputMode = Core.InputMode.River;

            // The reunion and the lesson are auto-started; the classroom cast
            // is assembled at FIXED positions, so no walk can race the
            // dialogue start (see EnsureClassroomCh04).
            StageDefinition reunion = Ch04Stage("CH04_ReunionTalk", Core.DuzheIds.SceneClassroom,
                Core.GameEventType.DialogueFinished, "CH04_ReunionTalk", "CH04_Lesson");
            reunion.autoStartSegmentId = "CH04_ReunionTalk";

            StageDefinition lesson = Ch04Stage("CH04_Lesson", Core.DuzheIds.SceneClassroom,
                Core.GameEventType.DialogueFinished, "CH04_Lesson", "CH04_WriteGoodbye");
            lesson.autoStartSegmentId = "CH04_Lesson";

            // Gated by ChapterRuntimeState.lessonFinished (ChapterDirector).
            StageDefinition write = Ch04Stage("CH04_WriteGoodbye", Core.DuzheIds.SceneClassroom,
                Core.GameEventType.MinigameCompleted, "MG07_Goodbye", "CH04_Memory");

            StageDefinition memory = Ch04Stage("CH04_Memory", Core.DuzheIds.SceneClassroom,
                Core.GameEventType.PresentationFinished, "PR_CH04_Memory", "CH04_Farewell");
            memory.autoStartPresentationId = "PR_CH04_Memory";

            StageDefinition farewell = Ch04Stage("CH04_Farewell", Core.DuzheIds.SceneClassroom,
                Core.GameEventType.PresentationFinished, "PR_CH04_Farewell", "CH04_Complete");
            farewell.autoStartPresentationId = "PR_CH04_Farewell";

            StageDefinition complete = Ch04Stage("CH04_Complete", Core.DuzheIds.SceneClassroom,
                Core.GameEventType.DialogueFinished, "", "");
            complete.inputMode = Core.InputMode.Ritual; // locked while fading
            complete.autoAdvanceSeconds = 1.4f;

            ChapterConfig cfg = new ChapterConfig
            {
                chapterId = "CH04",
                displayName = "最后一课",
                startSceneId = Core.DuzheIds.SceneUnderworld,
                spawnPointId = "SP_Main",
                dialogueChapterId = "CH04",
                presentationIds = new[] { "PR_CH04_Memory", "PR_CH04_Farewell" },
                stages = new[]
                {
                    opening, find, give, ritualTalk, ritual, board,
                    river, reunion, lesson, write, memory, farewell, complete
                }
            };
            // CH04 rule set: one passenger, two banned lanes per wave.
            cfg.riverSettings.passengerCount = 1;
            cfg.riverSettings.maxBannedLanesPerWave = 2;
            // Speaker names live in the delivered CharacterDisplayNames.json;
            // these entries are the fallback if that file is missing.
            cfg.actorDisplayNames["Ferryman"] = "渡客";
            cfg.actorDisplayNames["TeacherSpirit"] = "老师";
            cfg.actorDisplayNames["PastSelfSchool"] = "学生";
            cfg.itemTargetActors["IT05_LessonPlan"] = "TeacherSpirit";
            return cfg;
        }

        // ------------------------------------------------------------------
        // C08: formal CH05 「自渡」. Six stages from the delivery stage table
        // (08 第五关规范 v1.0). No NPC, no quest item, no ritual: the ferryman
        // remembers, lets go, boards alone and crosses into the cycle. The
        // credits confirm event is the only way the chapter completes.
        // ------------------------------------------------------------------
        private static ChapterConfig BuildChapter05()
        {
            ChapterConfig cfg = new ChapterConfig
            {
                chapterId = "CH05",
                displayName = "自渡",
                startSceneId = Core.DuzheIds.SceneUnderworld,
                spawnPointId = "SP_Main",
                dialogueChapterId = "CH05",
                presentationIds = new[] { "PR_CH05_Recall", "PR_CH05_Reincarnation" },
                stages = new[]
                {
                    new StageDefinition
                    {
                        stageId = "CH05_Recall",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Ritual,
                        autoStartPresentationId = "PR_CH05_Recall",
                        completionEvent = Core.GameEventType.PresentationFinished,
                        expectedSourceId = "PR_CH05_Recall",
                        nextStageId = "CH05_LetGo",
                        objectiveText = ""
                    },
                    new StageDefinition
                    {
                        stageId = "CH05_LetGo",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        autoStartSegmentId = "CH05_LetGo",
                        completionEvent = Core.GameEventType.DialogueFinished,
                        expectedSourceId = "CH05_LetGo",
                        nextStageId = "CH05_Board",
                        objectiveText = ""
                    },
                    new StageDefinition
                    {
                        stageId = "CH05_Board",
                        sceneId = Core.DuzheIds.SceneUnderworld,
                        inputMode = Core.InputMode.Exploring,
                        completionEvent = Core.GameEventType.Boarded,
                        expectedSourceId = "CH05_BOAT",
                        nextStageId = "CH05_River",
                        objectiveText = ""
                    },
                    new StageDefinition
                    {
                        stageId = "CH05_River",
                        sceneId = Core.DuzheIds.SceneRiver,
                        inputMode = Core.InputMode.River,
                        completionEvent = Core.GameEventType.RiverSucceeded,
                        expectedSourceId = "River",
                        nextStageId = "CH05_Reincarnation",
                        objectiveText = ""
                    },
                    new StageDefinition
                    {
                        stageId = "CH05_Reincarnation",
                        sceneId = Core.DuzheIds.SceneRiver,
                        inputMode = Core.InputMode.Ritual,
                        autoStartPresentationId = "PR_CH05_Reincarnation",
                        completionEvent = Core.GameEventType.PresentationFinished,
                        expectedSourceId = "PR_CH05_Reincarnation",
                        nextStageId = "CH05_Complete",
                        objectiveText = ""
                    },
                    new StageDefinition
                    {
                        stageId = "CH05_Complete",
                        sceneId = Core.DuzheIds.SceneRiver,
                        inputMode = Core.InputMode.Ritual,
                        completionEvent = Core.GameEventType.CreditsConfirmed,
                        expectedSourceId = "CH05_CREDITS",
                        nextStageId = "",
                        objectiveText = ""
                    }
                }
            };
            // CH05 rule set: nobody rides along, two banned lanes per wave.
            cfg.riverSettings.passengerCount = 0;
            cfg.riverSettings.maxBannedLanesPerWave = 2;
            cfg.actorDisplayNames["Ferryman"] = "渡客";
            return cfg;
        }
    }
}
