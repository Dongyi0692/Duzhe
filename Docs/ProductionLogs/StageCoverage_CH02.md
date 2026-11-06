# StageCoverage_CH02.md — CH02 阶段覆盖表（C05，2026-10-29）

> 对照《05 第二关规范 v1.0（2026-10-23）》阶段表逐行核对。实现 = C05 交付的代码/装配；验证 = 自动验证与人工验收的对应关系；剩余问题如实列出，不粉饰。
> 换算约定：R04 布局 TopLeft 像素 → 世界坐标 `worldX=(px-640)/32`、`worldY=(360-py)/32`。

## 一、阶段覆盖（11/11）

| # | stageId | 进入条件/场景 | 当前实现 | 完成事件与输出 | 验证方式 | 剩余问题 |
|---|---|---|---|---|---|---|
| 1 | CH02_Opening | 主线 CH01 完成或回放；Underworld | 女孩 NPC（(704,424)px→(2,−2)）`autoTalkSegmentId=CH02_Opening`，靠近 1.4u 自动对话（本运行只一次） | DialogueFinished(source=CH02_Opening) → FindNecklace | DZ_SMOKE（opening 段存在/stageId 匹配断言）；人工走近触发 | 无 |
| 2 | CH02_FindNecklace | 开场结束 | 项链可拾取（(512,328)px→(−4,1)，仅本阶段激活；IT03_Necklace 正式图已导入） | ItemCollected(IT03_Necklace) → ReturnNecklace | DZ_SMOKE（ItemCollected source 断言）；人工拾取 | 无 |
| 3 | CH02_ReturnNecklace | 已拾取 | 女孩 NPC 交还点 `returnItemIds=[IT03_Necklace]`（缺一不出提示/不可交还）；交还后 HUD 任务物图标收起 | ItemsReturned(source=CH02_LoverSpirit) → RitualTalk | DZ_SMOKE（requiredItemIds+source 断言）；人工验证缺件不可交还 | 无 |
| 4 | CH02_RitualTalk | 已交还 | 阶段进入自动播 TXT_CH02 `CH02_RitualTalk` 段（7 句，手动逐句推进） | DialogueFinished(source=CH02_RitualTalk) → Ritual | DZ_SMOKE（autoStart 段存在断言）；人工试玩 | 无 |
| 5 | CH02_Ritual | 引导完成，仍在 Underworld | 仪式锚点（(576,520)px→(−2,−5)）E 触发；RitualController_CH02：女孩本体隐藏→灵魂淡入走位→跟随；锚点按 id 精确匹配控制器（与 CH01 互不串扰） | RitualCompleted(source=CH02_RITUAL) → Board | 人工试玩（CH01/CH02 双锚点共存无串扰）；装配断言 | 无 |
| 6 | CH02_Board | 仪式完成 | 渡口（(704,558)px→(2,−6.2)）E 上船（仅本阶段激活，仪式前不可登船） | Boarded(source=CH02_BOAT) → 转 River 场景 | 人工试玩（仪式未完成时锚点不激活=前置校验） | 无 |
| 7 | CH02_River | 已登船 | C02 渡河全复用：说明面板（正式文案 river.instructions）→ 10 秒航程、4 波、**封 2 道**（CH02–04 规则组，乘客 1 人）；失败只重置 RiverSession | RiverSucceeded(source=River) → 转 Park 场景 | DZ_RIVER_SEEDS=PASS（64 种子含 CH02 规则组）；人工故意失败 1 次+成功 | 无 |
| 8 | CH02_ReunionTalk | 抵达公园并靠近男孩 | 男孩 NPC（PK_PastSelf (896,344)px→(8,0.5)）`autoTalkSegmentId=CH02_ReunionTalk`，靠近自动开始相认对白（13 句，手动逐句推进） | DialogueFinished(source=CH02_ReunionTalk) → Heart | DZ_SMOKE（段存在断言）；人工走近触发 | 无 |
| 9 | CH02_Heart | 相认对白结束 | 相会点 PK_Reunion (832,384)px→(6,−0.75) E 打开 MG04_Heart 描画（Heart 模板单闭合心形）；描画未完成保持基础画面，画错可重画 | MinigameCompleted(source=MG04_Heart) → Farewell | DZ_MINIGAMES（Heart 判定矩阵）；人工描画 | 无 |
| 10 | CH02_Farewell | 心形完成 | **演出 PR_CH02_Farewell**（PresentationMap.json 驱动）：拥抱姿态 → `CH02_Farewell` 段 16 句手动推进 → 末句后**切回干净场景只淡女孩 1.2s**、男孩与公园保持 → PresentationFinished；回看解锁写盘（仅 MainRun） | PresentationFinished(source=PR_CH02_Farewell) → Complete | DZ_SMOKE（演出映射+段存在断言）；人工完整观看 | CG02 已导入并按 R14 规则执行（不整图降透明） |
| 11 | CH02_Complete | 告别演出已完成 | 短淡出 1.4s（FadeOverlay，暂停冻结同步）→ ChapterCompleted 写盘 → 回主菜单 | ChapterCompleted（仅 ChapterDirector 可标记） | DZ_SMOKE（autoAdvanceSeconds 断言）；人工观看淡出 | “主线到 CH03”：CH03 由 C06 注册后自动衔接（菜单继续=首个未完成正式章，本任务已把选章逻辑改为正式章优先） |

## 二、验收对照表（任务要求逐条，已测/未测如实标注）

| 验收项 | 状态 | 说明 |
|---|---|---|
| 本关从开头到结尾完整走一次 | **未测（待用户试玩）** | 自动验证覆盖结构/事件链，实机全流程需人工 |
| 河段至少故意失败一次 | **未测（待用户）** | 机制同 C02 已验收（失败只清航程、保留全部前置 flag） |
| 每个机关未完成时尝试推进 | **部分自动验证** | 阶段激活门禁（activeStageIds）自动断言链+人工可复验；仪式前不能登船=Board 锚点仅 CH02_Board 激活（装配断言）；未归还项链不能仪式=ReturnNecklace requiredItemIds 校验（代码路径） |
| 对话/机关中暂停再恢复 | **未测（待用户）** | 暂停冻结 timeScale/输入路由（C01-C04 机制不变）；机关面板 OnGamePaused 恢复规则不变 |
| 事件重复与上一章演员残留 | **自动验证部分** | 事件五重过滤（runId/chapterId/stageId/type/source）不变；CH01/CH02 对象按路线门禁互斥（DEV 与 CH01 亦互斥）；**实测待用户** |
| 女孩是亡者、男孩是生者 | 剧情文本明确（TXT_CH02：LoverSpirit=亡者女孩、PastSelfRomance=生者男孩）；仪式上船仅女孩 1 灵魂（乘客 1 人） |
| 先归还项链再仪式 | 阶段链保证（ReturnNecklace → RitualTalk → Ritual）+ 锚点仅 CH02_Ritual 激活 |
| 渡河失败不丢项链任务进度 | RiverSession 只重置航程（C02 已验证机制，CH02 复用同一代码路径） |
| 相认对白之后才开放画心 | 阶段链保证（ReunionTalk → Heart），MG04 挂接点仅 CH02_Heart 激活 |
| 没画完心形不能告别 | 阶段链保证（Heart → Farewell），未完成 MG04 不推进 |
| 终章前不透露男孩身份 | 文本无剧透（StoryBible：前四关不直接剧透）；CH02 无身份揭示 |
| 淡出完成后通关 | 演出协程顺序保证：淡出+停留完成才发 PresentationFinished（代码路径） |
| 回忆馆解锁对应演出 | MainRun 演出完整结束 → Save.UnlockPresentation（DZ_SMOKE 覆盖幂等写盘；回忆馆界面属 C09） |
| 可选回忆蒙太奇 PR_CH02_Memory | **未制作**（按规范"默认先不制作，保证主线"）——无空槽位、无未实现按钮 |

## 三、资源核对（如实）

| 项 | 状态 |
|---|---|
| TXT_CH02.json | **已接入运行时**（Resources/Data/Dialogue，DialogueJsonValidator PASS：11 阶段目标/1 物品/4 段对白） |
| PresentationMap.json | 已含 PR_CH02_Farewell（R01 交付，沿用） |
| PresentationLayers.json | 已含 CG02 规则（fadeOnly=CHR05_LoverSpirit，persistent=CHR06_PastSelfRomance） |
| BG04_Park | **已导入**（R07-R10 交付，1280×720，长椅已局部校正至 R04 区域） |
| IT03_Necklace | **已导入**（R13 交付，32×32 RGBA） |
| CHR05_LoverSpirit（女孩站姿） | **已导入**（R11 交付） |
| CHR06_PastSelfRomance（男孩站姿） | **已导入**（R11_MainCharacters 交付） |
| CG02_LoversFarewell | **已导入**（R14 交付，640×360） |
| CHR05/06 四向行走表 | **未交付**（R12 配角行走表仅 CHR02/03/05/07/11 五位；CHR06 男孩无行走表——用站姿滑动即可，幽灵跟随不需要跨步动画） |
| 音频（BGM02_Reunion / SFX） | R15-R17 已收货未接线（C10 音频接入任务） |
| 长椅碰撞与画面一致性 | 已按 R04 solidPolygons 放置 Solid_PK_Bench (4,1.5)u；R07-R10 交接已局部校正长椅到 R04 区域（接入时按用户指令核对过） |

## 四、交付后待办（非 C05 范围）

1. C06 注册 CH03 后，CH02_Complete 的"主线到 CH03"自动生效（菜单"开始"=首个未完成正式章）。
2. C09 回忆馆/章节目录界面消费 unlockedPresentationIds 与回放入口。
3. C10 音频接入（BGM02_Reunion、SFX 触发映射）。
4. C11 清理：DEV 机关测试按钮、DEV 双路线对象（CH01_DEV 全套）、探针日志、ClickProbe。
