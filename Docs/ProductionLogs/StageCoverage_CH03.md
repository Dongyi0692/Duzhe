# StageCoverage_CH03.md — CH03 阶段覆盖表（C06，2026-10-29）

> 对照《06 第三关规范 v1.0》阶段表逐行核对。实现 = C06 交付的代码/装配；验证 = 自动验证与人工验收的对应关系；剩余问题如实列出。
> 坐标换算：R09 `BackgroundQA.json` 的 TopLeft 像素 → 世界坐标 `worldX=(px-640)/32`、`worldY=(360-py)/32`。

## 一、阶段覆盖（12/12）

| # | stageId | 进入条件/场景 | 当前实现 | 完成事件与输出 | 验证方式 | 剩余问题 |
|---|---|---|---|---|---|---|
| 1 | CH03_Opening | 主线 CH02 完成或回放；Underworld | 鼓手 NPC（(704,424)px→(2,−2)，CHR07_DrummerSpirit）`autoTalkSegmentId=CH03_Opening`，靠近 1.4u 自动对话（本运行只一次，11 句） | DialogueFinished(source=CH03_Opening) → FindDrumsticks | DZ_SMOKE（段存在/stageId 匹配/目标断言）；人工走近触发 | 无 |
| 2 | CH03_FindDrumsticks | 开场结束 | 鼓槌可拾取（(512,328)px→(−4,1)，仅本阶段激活；IT04_Drumsticks 正式图已导入；HUD 图标映射到正式鼓槌） | ItemCollected(IT04_Drumsticks) → ReturnDrumsticks | DZ_SMOKE（ItemCollected source + 图标断言）；人工拾取 | 无 |
| 3 | CH03_ReturnDrumsticks | 已拾取 | 鼓手 NPC `returnItemIds=[IT04_Drumsticks]`（缺件不可交还）；交还后 HUD 任务物图标收起；NPC 不重复播对白（returnSegmentId 为空） | ItemsReturned(source=CH03_DrummerSpirit) → RitualTalk | DZ_SMOKE（requiredItemIds+source 断言）；人工验证缺件不可交还 | 无 |
| 4 | CH03_RitualTalk | 已交还 | 阶段进入自动播 TXT_CH03 `CH03_RitualTalk`（7 句，手动逐句推进） | DialogueFinished(source=CH03_RitualTalk) → Ritual | DZ_SMOKE（autoStart 段存在断言）；人工试玩 | 无 |
| 5 | CH03_Ritual | 引导完成，仍在 Underworld | 仪式锚点 CH03_RITUAL（(576,520)px→(−2,−5)）E 触发；RitualController_CH03：鼓手本体隐藏→灵魂淡入走位→跟随；锚点按 id 精确匹配（CH01/CH02/CH03 三锚点共存不串扰） | RitualCompleted(source=CH03_RITUAL) → Board | 装配断言（1 灵魂/1 站位/hideOnRitual=鼓手本体）；人工试玩 | 无 |
| 6 | CH03_Board | 仪式完成 | 渡口 CH03_BOAT（(704,558)px→(2,−6.2)）E 上船，仅本阶段激活（仪式前不可登船） | Boarded(source=CH03_BOAT) → 转 River 场景 | 装配断言（activeStageIds 仅 CH03_Board） | 无 |
| 7 | CH03_River | 已登船 | C02 渡河全复用：说明面板 → 10 秒航程、4 波、**封 2 道**（CH02–04 规则组，乘客 1 人）；失败只重置 RiverSession，鼓槌/对白进度保留 | RiverSucceeded(source=River) → 转 RecordingStudio | DZ_RIVER_SEEDS=PASS（64 种子，含 CH02–04 规则组）；人工故意失败 1 次+成功 | 无 |
| 8 | CH03_ReunionTalk | 抵达录音室并靠近队友 | 录音室（R09 BG05）：PastSelfBand NPC（(704,384)px→(2,−0.75)）`autoTalkSegmentId=CH03_ReunionTalk`，仅本阶段激活；贝斯手 (4,−0.75)、主唱 (6,−0.75) 为常驻陈设；鼓手灵魂跟随玩家（(896,384)px→(8,−0.75) 为他空着的站位） | DialogueFinished(source=CH03_ReunionTalk) → Perform | DZ_SMOKE（段存在）；人工走近触发 | 无 |
| 9 | CH03_Perform | 相认对白结束 | RS_Performance (768,320)px→(4,1.25) E 打开 MG05_Notes；**正式 CFG_Notes（78 BPM、16 音、noteBeats 16 项）** 经运行时校验后使用；错音发声但不推进、不清进度、无节拍失败 | MinigameCompleted(source=MG05_Notes) → Photo | DZ_MINIGAMES（正式谱校验+16 音完整+错音不推进）；DZ_CH03_PLAY（错音后仍在第 1 音、16 音完成、恰好 1 次事件） | 7 个音 WAV 未接线（C10）；当前为程序正弦占位音 |
| 10 | CH03_Photo | 演奏完成 | RS_PhotoApproach (960,448)px→(10,−2.75) E 打开 MG06_Photo；PRP06 底图承载三位固定成员，只有鼓手可拖，唯一缺口 [384,198,48,64]；正确松手一次即完成 | MinigameCompleted(source=MG06_Photo) → Farewell | DZ_CH03_PLAY（仅 1 可拖/1 缺口、无重影、单次落位完成、恰好 1 次事件）；DZ_MINIGAMES（MG03 仍需 3 凳） | 无 |
| 11 | CH03_Farewell | 合照完成 | **演出 PR_CH03_Farewell**：先就位（鼓手走到 (8,−0.75)）→ 显示 CG03 定格 → `CH03_Farewell` 段 19 句手动推进 → 末句后**切回干净场景只淡鼓手 1.2s**，三位生者与录音室保持 → PresentationFinished；回看解锁写盘（仅 MainRun）；快门只在 MG06 成功落位时响一次 | PresentationFinished(source=PR_CH03_Farewell) → Complete | DZ_CH03_PLAY（CG 显示→19 句→CG 先隐藏→只淡鼓手→三位队友仍在→恰好 1 次 PresentationFinished）；快门重复通知仍只响 1 次 | 快门音效资源未导入（R16 SFX08_Shutter 待 C10），当前为占位静音并留日志 |
| 12 | CH03_Complete | 告别演出已完成 | 短淡出 1.4s（FadeOverlay，暂停冻结同步）→ ChapterCompleted 写盘 → 回主菜单（C09 才做连续转场） | ChapterCompleted（仅 ChapterDirector 可标记） | DZ_SMOKE（autoAdvanceSeconds/末阶段断言） | 主线到 CH04 由 C07 注册后自动衔接 |

## 二、验收对照表（任务要求逐条，已测/未测如实标注）

| 验收项 | 状态 | 说明 |
|---|---|---|
| 本关从开头到结尾完整走一次 | **未测（待用户试玩）** | 自动验证覆盖结构/事件链/机关/演出；实机全流程需人工（见 §四 试玩步骤） |
| 缺鼓槌不能交还 | **代码路径复用 + 结构断言** | `InteractionController.CanManuallyInteract` 要求 `returnItemIds` 全部已收集（与 CH01/CH02 同一路径）；本次未在 Play 中单独复验缺件路径 |
| 缺鼓槌不能仪式 | 结构断言 | 仪式锚点 `activeStageIds` 仅 `CH03_Ritual`，而该阶段位于 ReturnDrumsticks 之后；未到阶段则锚点完全不响应 |
| 渡河失败保留解谜与已播对白 | 代码路径（C02 已验证） | 失败只重置 RiverSession；ChapterRuntimeState（含 playedSegmentIds/collectedItemIds）不清 |
| 错音不清进度、不前进、不失败 | **DZ_CH03_PLAY PASS** | 错音后进度点仍停在第 1 音；无任何超时/节拍失败路径 |
| 最后一音只触发一次 | **DZ_CH03_PLAY PASS** | 16 音完成后 `MinigameCompleted(MG05_Notes)` 恰好 1 次 |
| 合照固定人物不可拖、照片四人 | **DZ_CH03_PLAY PASS** | 仅 `Drummer` 可拖，`DrummerGap` 单一缺口；PRP06 已烘焙三位成员，程序不再画 |
| 松手成功一次才快门 | **DZ_CH03_PLAY PASS** | 重复投递完成通知后 `ShutterFireCount` 仍为 1 |
| CG 后只有鼓手淡出 | **DZ_CH03_PLAY PASS** | CG03 先隐藏；鼓手失活；三位生者 active=3 |
| 暂停恢复、返回菜单再进不残留 | **未测（待用户）** | 暂停冻结 timeScale/输入路由（C01–C05 机制不变）；PresentationPlayer.ForceAbort 隐藏 CG，PauseMenu 离开调用 AbandonCurrentRun；本次未改这些管理器 |
| 两次装配对象数不增长 | **装配报告 PASS** | 连续两次 `DZ_VALIDATION`：Underworld CH03 objects=5、RecordingStudio objects=6、casts=1/1，第二次全部 "already complete" |

## 三、资源核对（如实）

| 项 | 状态 |
|---|---|
| TXT_CH03.json | **已接入运行时**（Resources/Data/Dialogue；DialogueJsonValidator PASS：12 阶段目标/1 物品/4 段对白） |
| PresentationMap.json | 已含 PR_CH03_Farewell（orderedSegmentIds=[CH03_Farewell]） |
| PresentationLayers.json | 已含 CG03 规则（keepCompleteFourPersonPhoto=true） |
| CFG_Notes.json | **已导入**（Data/Minigames 工作副本 + Resources/Data/Minigames；78 BPM、16 音、noteBeats 16 项；运行时严格校验） |
| BG05_RecordingStudio | **已导入并装配**（R09，1280×720；场景背景已从占位图切换为正式图） |
| IT04_Drumsticks | **已导入**（R13，拾取物 + HUD 图标） |
| CHR07_DrummerSpirit / CHR09_BandmateA / CHR10_BandmateB | 已导入（R11 配角组） |
| CHR08_PastSelfBand | **本次导入**（R11_MainCharacters） |
| CG03_BandFarewell | 已导入（R14，640×360） |
| PRP06_PhotoFrame | 已导入（R13 Panels v1.0，640×360） |
| BG04_Park（顺带修正） | **本次才真正应用到 Park 场景**：C05 只导入了文件，`EnsureOneScene` 仍赋占位图；本次改为正式背景表驱动 |
| 鼓手/队友四向行走表 | 未交付（R12 仅 CHR02/03/05/07/11）；鼓手灵魂用站姿跟随 |
| 音频（BGM03、7 音 WAV、SFX08_Shutter） | R15–R17 已收货未接线（C10）；本章只调用接口并留日志 |

## 四、用户试玩步骤（约 10–12 分钟）

候选构建：`Builds/Candidates/20261029_183952_a6123452/Duzhe.exe`（118,196,320 字节；构建版已用临时探针确认 Underworld / Park / FarmCourtyard / RecordingStudio 四个场景均能正常加载，探针随后删除并重建）

1. **快速到达 CH03**：主菜单「开始」进入 CH01 → ESC 暂停 → 「DEV:完成当前章」回主菜单 → 再「开始」进 CH02 → 同样 DEV 完成 → 第三次「开始」即 CH03。
   ⚠ DEV 完成会写入真实存档（duzhe_save.json）；如需保留原进度，先备份 `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Duzhe\duzhe_save.json`。
2. 阴间：走近鼓手听完开场 → 到左侧 (−4,1) 捡起鼓槌（右上角出现鼓槌图标）→ 回鼓手按 E 交还 → 听完引导 → 到渡口按 E 仪式 → 上船渡河（故意失败一次确认鼓槌仍在）。
3. 录音室：进门后鼓手灵魂跟随；走近队友触发相认对白 → 到中央演奏区按 E，按亮起的音完成 16 音（可故意按错，进度不应清空）→ 到右侧合照点按 E，把鼓手拖进空位松手。
4. 告别：确认合照界面**只有一位可拖的鼓手**、照片里共四人 → 演出时 CG03 定格 → 对白结束后画面切回干净场景、**只有鼓手淡出**，三位队友与录音室保留。
5. 回归：暂停/恢复一次、返回主菜单再进，确认没有残留面板或角色；CH03 通关后主菜单应显示 CH03 已完成。
