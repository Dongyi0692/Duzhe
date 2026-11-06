# StageCoverage_CH04.md — CH04 阶段覆盖表（C07，2026-10-29）

> 对照《07 第四关规范 v1.0》阶段表逐行核对。实现 = C07 交付的代码/装配；验证 = 自动验证与人工验收的对应关系；剩余问题如实列出。
> 坐标换算：R04 `Classroom.layout.json` 的 TopLeft 像素 → 世界坐标 `worldX=(px-640)/32`、`worldY=(360-py)/32`。

## 一、阶段覆盖（12/12）

| # | stageId | 进入条件/场景 | 当前实现 | 完成事件与输出 | 验证方式 | 剩余问题 |
|---|---|---|---|---|---|---|
| 1 | CH04_Opening | 主线 CH03 完成或回放；Underworld | 老师 NPC（(704,424)px→(2,−2)，CHR11_TeacherSpirit）`autoTalkSegmentId=CH04_Opening`，靠近 1.4u 自动对话（本运行只一次，9 句） | DialogueFinished(source=CH04_Opening) → FindLessonPlan | DZ_SMOKE（段存在/目标断言）；人工走近触发 | 无 |
| 2 | CH04_FindLessonPlan | 开场结束 | 教案可拾取（(512,328)px→(−4,1)，仅本阶段激活；IT05_LessonPlan 正式图 + HUD 图标） | ItemCollected(IT05_LessonPlan) → ReturnLessonPlan | DZ_SMOKE（source/图标断言）；人工拾取 | 无 |
| 3 | CH04_ReturnLessonPlan | 已拾取 | 老师 NPC `returnItemIds=[IT05_LessonPlan]`（缺件不可交还）；交还后收起任务物图标；NPC 不重复播对白 | ItemsReturned(source=CH04_TeacherSpirit) → RitualTalk | DZ_SMOKE（requiredItemIds+source 断言） | 无 |
| 4 | CH04_RitualTalk | 已交还 | 阶段进入自动播 `CH04_RitualTalk`（7 句，手动逐句） | DialogueFinished(source=CH04_RitualTalk) → Ritual | DZ_SMOKE（autoStart 段存在断言） | 无 |
| 5 | CH04_Ritual | 引导完成，仍在 Underworld | 仪式锚点 CH04_RITUAL（(576,520)px→(−2,−5)）E 触发；老师本体隐藏→灵魂淡入走位→跟随；四章锚点按 id 精确匹配 | RitualCompleted(source=CH04_RITUAL) → Board | 装配断言（1 灵魂/1 站位/hideOnRitual）；人工试玩 | 无 |
| 6 | CH04_Board | 仪式完成 | 渡口 CH04_BOAT（(704,558)px→(2,−6.2)）E 上船，仅本阶段激活 | Boarded(source=CH04_BOAT) → 转 River | 装配断言（activeStageIds 仅 CH04_Board） | 无 |
| 7 | CH04_River | 已登船 | C02 渡河全复用：10 秒航程、4 波、封 2 道（CH02–04 规则组，乘客 1 人）；失败只重置 RiverSession，教案/对白进度保留 | RiverSucceeded(source=River) → 转 Classroom | DZ_RIVER_SEEDS=PASS（64 种子）；人工故意失败 | 无 |
| 8 | CH04_ReunionTalk | 抵达教室 | 教室（R04）：学生 CHR12 固定在 CL_Student (608,416)→(−1,−1.75)，老师灵魂固定在 CL_Teacher (768,192)→(4,5.25)，**位置在装配时固定，无走位**；阶段进入自动播 `CH04_ReunionTalk`（8 句） | DialogueFinished(source=CH04_ReunionTalk) → Lesson | DZ_SMOKE（autoStart 段存在）；装配断言 | 无 |
| 9 | CH04_Lesson | 相认对白结束 | 阶段进入自动播 `CH04_Lesson`（**正式 21 句**，末句 `CH04_Lesson_021`「这就是，最后一课了。」）；只有整段结束才置 `ChapterRuntimeState.lessonFinished=true` | DialogueFinished(source=CH04_Lesson) → WriteGoodbye | DZ_SMOKE（21 行 + 第 21 行 lineId/文本断言）；DZ_CH04_PLAY（回看路径 21 行） | 无 |
| 10 | CH04_WriteGoodbye | 课堂整段结束 | 黑板热点 (4,4.0) 挂 `CH04GoodbyeGate`（`activeStageIds` 仅本阶段）：课堂结束自动开一次；关闭后可按 E 重开，不每帧弹回；缺 `lessonFinished` 时 Director 拒绝进入本阶段、Launcher/Gate 拒绝开面板 | MinigameCompleted(source=MG07_Goodbye) → Farewell | DZ_CH04_PLAY（面板几何 + 真实描完 14 笔「再见」+ 恰好 1 次完成；无章节时 E 不开面板）；DZ_MINIGAMES（Goodbye 模板判定矩阵） | 无 |
| 11 | CH04_Farewell | 写完「再见」 | **演出 PR_CH04_Farewell**：主线/章回放只播 `CH04_Farewell`（课堂已在第 9 步上过），CG04 定格 + 板上已完成「再见」→ 8 句 → 先隐藏 CG 与字层 → 只淡老师 1.2s，学生与教室保留；回忆馆回放播完整 `[CH04_Lesson, CH04_Farewell]`，两段之间显示字迹并响铃 | PresentationFinished(source=PR_CH04_Farewell) → Complete | DZ_CH04_PLAY（主线 8 句、回看 29 句、字迹出现时机、只淡老师、各恰好 1 次事件）；DZ_SMOKE（演出顺序断言） | 铃声/音效未接线（C10） |
| 12 | CH04_Complete | 告别演出完成 | 短淡出 1.4s → ChapterCompleted 写盘 → 回主菜单（C09 做连续转场） | ChapterCompleted（仅 ChapterDirector 标记） | DZ_SMOKE（autoAdvance/末阶段断言） | 主线到 CH05 由 C08 注册后自动衔接 |

## 二、验收对照表（任务要求逐条，已测/未测如实标注）

| 验收项 | 状态 | 说明 |
|---|---|---|
| 叙旧→21 行上课→写字顺序 | **结构+自动验证** | 阶段链 + `lessonFinished` 守卫 + Gate/Launcher 门禁；实机全流程待用户试玩 |
| 上课时按 E/点黑板不开机关 | **DZ_CH04_PLAY PASS（无章节路径）** | 无 CH04 写字阶段时 `CH04GoodbyeGate.OnPropInteract` 返回 false 且面板未开；阶段内「课堂未结束」由同一 `Allowed()` 的 `LessonFinished` 合取条件保证（Director 也会拒绝进入该阶段） |
| 只写一个字无法告别 | **代码路径** | 阶段只在 `MinigameCompleted(MG07_Goodbye)` 推进；描画机关只在全部笔画通过后发完成事件（DZ_CH04_PLAY 的 14 笔全过才完成） |
| 书写暂停/恢复、关闭/重开、退出再进入 | **部分自动验证** | 暂停清未完成笔、保留已完成笔（`OnGamePaused`，C03 机制未改）；关闭后 E 重开（Gate 设计，未在 Play 中模拟暂停）；**未测待用户** |
| 主线与章回放需描字，演出回看不描字且存档不变 | **DZ_CH04_PLAY PASS（播放逻辑）** | 回看模式 `PresentationReplay` 播放完整两段且不触发任何机关；正式回忆馆入口由 C09 提供 |
| 黑板文字实机可见 | **DZ_CH04_PLAY PASS（字层显示）+ 坐标修正** | `BlackboardTextLayer` 锚点由 `+120`（屏外）改为 `-120` 并关闭 raycast；Play 中确认字层显示/隐藏 |
| 老师淡出学生留下 | **DZ_CH04_PLAY PASS** | 老师 inactive、学生 active |
| 河段故意失败后不重做教案 | 代码路径（C02 已验证） | 失败只重置 RiverSession；ChapterRuntimeState 不清 |
| 两次装配对象数不增长 | **装配报告 PASS** | 连续两次 `DZ_VALIDATION`：Underworld CH04 objects=5、Classroom objects=3、casts=1/1，第二轮全部 "already complete" |

## 三、资源核对（如实）

| 项 | 状态 |
|---|---|
| TXT_CH04.json | **已接入运行时**（DialogueJsonValidator PASS：12 目标/1 物品/5 段/3 演员） |
| PresentationMap.json | PR_CH04_Farewell 保留 `[CH04_Lesson, CH04_Farewell]`（回看顺序）；主线由 `PresentationPlayer.ResolveSegmentIds` 裁为 `[CH04_Farewell]` |
| PresentationLayers.json | 已含 CG04 规则（黑板字由程序渲染） |
| BG06_Classroom | **已导入并装配**（R10，1280×720；黑板已局部校正至 (544,40,448,112)） |
| IT05_LessonPlan | **已导入**（R13，拾取物 + HUD 图标） |
| CHR11_TeacherSpirit / CHR12_PastSelfSchool | 已导入（R11 配角组 / R11_MainCharacters） |
| CG04_TeacherFarewell | 已导入（R14，640×360） |
| PRP07_BlackboardCloseup | 已导入（R13 Panels v1.0，640×360） |
| 老师/学生四向行走表 | 未交付（R12 未含 CHR11/CHR12）；教室为固定站位，无需行走 |
| 音频（SFX10_SchoolBell、BGM04） | R15–R17 已收货未接线（C10）；当前调用留日志、占位静音 |

## 四、用户试玩步骤（约 10 分钟）

候选构建：`Builds/Candidates/20261029_232735_ca1b20af/Duzhe.exe`（121,011,740 字节）

> 当前存档已记录 CH01–CH04 全部完成（`completedChapterIds` 含 CH01/CH02/CH03/CH04）。因为第 5 章要到 C08 才接入，「开始故事」会按规范重玩第 1 章；**进入本章请用：进游戏后按 ESC →「DEV:选择章节」→ CH04**（DEV 入口，C11 清理）。

1. 主菜单「开始」→（ESC →「DEV:选择章节」→ CH04）→ 阴间：走近老师听完叙旧 → 左侧捡起教案（右上角出现教案图标）→ 回老师按 E 交还 → 听完引导 → 渡口 E 仪式 → 上船；渡河**故意失败一次**，确认教案与已播对白仍在。
2. 教室：学生与老师已就位，相认对白自动开始 → 接着自动进入**完整 21 句课堂**，请逐句点完（最后一课那句是第 21 句）。**课堂途中按 E 或点黑板都不应打开写字面板。**
3. 课堂结束后黑板自动打开写字面板：沿亮起的笔画把「再见」写完（14 笔）；只写一笔时面板不会完成。关闭面板后可在黑板按 E 重开，进度（已完成的笔）保留。
4. 告别：CG04 定格，黑板上显示已写的「再见」→ 8 句对白 → 结束后切回干净教室，**只有老师淡出，学生与教室保留**。
5. 回归：暂停/恢复一次、返回主菜单再进，确认没有残留面板或角色；CH04 通关后主菜单应显示 CH04 已完成。
