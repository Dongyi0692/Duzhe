# StageCoverage.md — CH01 阶段覆盖表（C04，2026-10-28）

> **五章覆盖表索引**：CH01 本文件；CH02 `StageCoverage_CH02.md`；CH03 `StageCoverage_CH03.md`；CH04 `StageCoverage_CH04.md`；CH05 `StageCoverage_CH05.md`（C08，2026-10-30）。各章进度与自动验证记录见 `Progress.md`。
>
> 对照《04 第一关规范 v1.0（2026-10-23）》阶段表逐行核对。实现 = C04 交付的代码/装配；验证 = 自动验证与人工验收的对应关系；剩余问题如实列出，不粉饰。
> 换算约定：R04 布局 TopLeft 像素 → 世界坐标 `worldX=(px-640)/32`、`worldY=(360-py)/32`。

## 一、阶段覆盖（12/12）

| # | stageId | 进入条件/场景 | 当前实现 | 完成事件与输出 | 验证方式 | 剩余问题 |
|---|---|---|---|---|---|---|
| 1 | CH01_Opening | 初次进章，Underworld | 爷爷 NPC（(704,424)px→(2,−2)）`autoTalkSegmentId=CH01_Opening`，靠近 1.4u 自动对话（本运行只一次）；移动提示 `hint.move` 入场 Toast | DialogueFinished(source=CH01_Opening) → Prepare | DZ_SMOKE 结构断言（段落存在/stageId 匹配）；人工走近触发 | 无 |
| 2 | CH01_Prepare | 开场结束，Underworld | 眼镜可拾取（(512,328)px→(−4,1)，仅本阶段激活）；拐杖台（(480,400)px→(−5,−1.25)）E 打开 MG01_Cane 描画（Cane 模板 2 笔）。**requireAll 多条件阶段**：两条件任意先后齐备才推进；MG01 完成即授予 IT02_Cane（HUD 出现任务物图标） | ItemCollected(IT01_Glasses) + MinigameCompleted(MG01_Cane) 全齐 → ReturnItems | DZ_MINIGAMES（Cane 判定矩阵）；DZ_SMOKE（requireAll 2 条件+cane grants IT02_Cane 断言）；人工任意顺序走通 | 无 |
| 3 | CH01_ReturnItems | 两物件齐备 | 爷爷 NPC 交还点 `returnItemIds=[IT01_Glasses,IT02_Cane]`（**一次 E 交还两件**，缺一不出提示/不可交还）；交还后 HUD 任务物图标收起 | ItemsReturned(source=CH01_GRANDFATHER) → RitualTalk | DZ_SMOKE（requiredItemIds+source 断言）；人工验证缺件不可交还 | 无 |
| 4 | CH01_RitualTalk | 物件交还 | 阶段进入自动播 TXT_CH01 `CH01_RitualTalk` 段（9 句，手动逐句推进） | DialogueFinished(source=CH01_RitualTalk) → Ritual | DZ_SMOKE（autoStart 段存在断言）；人工试玩 | 无 |
| 5 | CH01_Ritual | 引导结束，仍在 Underworld | 仪式锚点（(576,520)px→(−2,−5)）E 触发；RitualController_CH01：两老人本体隐藏→双灵魂淡入走位→跟随；锚点按 id 精确匹配控制器（与 DEV 路线互不串扰） | RitualCompleted(source=CH01_RITUAL) → Board | 人工试玩（DEV/CH01 双锚点共存无串扰）；编译+装配断言 | 无 |
| 6 | CH01_Board | 仪式完成 | 渡口（(704,558)px→(2,−6.2)）E 上船（仅本阶段激活，仪式前不可登船） | Boarded(source=CH01_BOAT) → 转 River 场景 | 人工试玩（仪式未完成时锚点不激活=前置校验） | 无 |
| 7 | CH01_River | 已登船 | C02 渡河全复用：说明面板（正式文案 river.instructions）→ 10 秒航程、4 波、封 1 道（CH01 规则组）；失败只重置 RiverSession | RiverSucceeded(source=River) → 转 FarmCourtyard | DZ_RIVER_SEEDS=PASS（64 种子含 CH01 规则组）；人工故意失败 1 次+成功 | 无 |
| 8 | CH01_Door | 已抵达院门，FarmCourtyard | 出生点 FC_Spawn(288,576)px→(−11,−6.75)；门可视（(640,152)px→(0,6.5)）+ 交互区（(640,216)px 中心→(0,4.5)）E 打开 MG02_Door 特写；**两次点击隔离**（门环点击才完成，可关闭重开）；完成后孙子在门口出现并走到 (0,2.75) | MinigameCompleted(source=MG02_Door) → ReunionTalk | DZ_SMOKE（source 断言）；人工验证点击穿透隔离+重开 | 无（门环位置与 R04 交接第 4 条一致：(380,200)，48×48=(356,176)） |
| 9 | CH01_ReunionTalk | 敲门完成 | 阶段进入自动播 `CH01_ReunionTalk` 段（10 句） | DialogueFinished(source=CH01_ReunionTalk) → Stools | DZ_SMOKE；人工试玩 | 无 |
| 10 | CH01_Stools | 相认对白结束 | 餐桌（(736,400)px→(3,−1.25)，独立桌碰撞 (4,2)u）+ 交互区（(736,472)px→(3,−3.5)）E 打开 MG03_Stools 拖放（3 凳任意空位）；三个目标位（FC_StoolLeft/Right/Front px→(1,−2.75)/(5,−2.75)/(3,−4.25)）先以淡影可见，完成后锁定实凳显示 | MinigameCompleted(source=MG03_Stools) → Farewell | DZ_MINIGAMES（拖放逻辑）；人工拖放+座位锁定 | 面板标题/说明/状态行/关闭按钮本轮已修正为可见（C03 遗留正 y 坐标修复） |
| 11 | CH01_Farewell | 凳子全摆好 | **演出 PR_CH01_Farewell**（PresentationMap.json 驱动）：座位演出（两老人+孙子入座三凳）→ `CH01_Farewell` 段 26 句手动推进 → 末句后两老人 1.2s 淡出、孙子留座 0.8s → PresentationFinished；回看解锁写盘（仅 MainRun） | PresentationFinished(source=PR_CH01_Farewell) → Complete | DZ_SMOKE（演出映射+段存在断言）；人工完整观看 | CG01_Farewell 插画未交付——占位为场景演员层表现（符合“静态图+演员位置/透明度”降级规则），交付后替换 |
| 12 | CH01_Complete | 告别演出已完成 | 短淡出 1.4s（FadeOverlay，暂停冻结同步）→ ChapterCompleted 写盘 → 回主菜单 | ChapterCompleted（仅 ChapterDirector 可标记） | DZ_SMOKE（autoAdvanceSeconds 断言）；人工观看淡出 | “主线到 CH02”：CH02 由 C05 注册后自动衔接（菜单继续=首个未完成正式章）；回放回目录属 C09 菜单 |

## 二、验收对照表（任务要求逐条，已测/未测如实标注）

| 验收项 | 状态 | 说明 |
|---|---|---|
| 本关从开头到结尾完整走一次 | **未测（待用户试玩）** | 自动验证覆盖结构/事件链，实机全流程需人工 |
| 河段至少故意失败一次 | **未测（待用户）** | 机制同 C02 已验收（失败只清航程、保留全部前置 flag） |
| 每个机关未完成时尝试推进 | **部分自动验证** | 阶段激活门禁（activeStageIds）自动断言链+人工可复验；仪式前不能登船=Board 锚点仅 CH01_Board 激活（装配断言）；两物未齐不能交还=CanManuallyInteract 全件校验（代码路径） |
| 对话/机关中暂停再恢复 | **未测（待用户）** | 暂停冻结 timeScale/输入路由（C01-C03 机制不变）；机关面板 OnGamePaused 恢复规则不变 |
| 事件重复与上一章演员残留 | **自动验证部分** | 事件五重过滤（runId/chapterId/stageId/type/source）不变；DEV 与 CH01 对象按 stageId 门禁互斥；新增：暂停-离开时强制关闭机关面板/对话/演出（防泄漏到菜单）；**实测待用户** |
| 两件物未齐不能仪式 | 装配断言（Ritual 锚点仅 CH01_Ritual 激活，Ritual 晚于 ReturnItems）+ 人工可复验 |
| 仪式前不能登船 | 同上（Board 锚点仅 CH01_Board 激活） |
| 渡河失败保留两物交还与仪式 | RiverSession 只重置航程（C02 已验证机制，CH01 复用同一代码路径） |
| 关闭门特写后可重开 | 关闭按钮本轮修复为可见+可点（C03 该按钮不可见），待人工复验 |
| 两把凳子不触发吃饭 | MG03 满 3 件才 Complete（代码路径，DZ_MINIGAMES 覆盖拖放计数） |
| 老人淡出结束后才通关 | 演出协程顺序保证：淡出+停留完成才发 PresentationFinished（代码路径） |
| 回忆馆解锁对应演出 | MainRun 演出完整结束 → Save.UnlockPresentation（DZ_SMOKE 覆盖幂等写盘；回忆馆界面属 C09） |
| 首关教学只教共用及本关操作 | 入场提示 hint.move/hint.door + 各交互点正式文案提示（无空格跳关路径） |

## 三、资源核对（如实）

| 项 | 状态 |
|---|---|
| TXT_CH01.json | **已接入运行时**（Resources/Data/Dialogue，DialogueJsonValidator PASS：53 阶段目标/2 物品/4 段对白） |
| TXT_UI.json | **已接入运行时**（201 key 正式版替换开发子集；全部调用点迁移到正式点分 key） |
| PresentationMap.json | **已接入运行时**（6 条演出，CH01 用 PR_CH01_Farewell） |
| CharacterDisplayNames.json | **已接入运行时**（12 演员显示名，验证器合法演员表合并） |
| R04 六场景布局 | **CH01 相关三景已装配**（Underworld 增拐杖点；FarmCourtyard 全量；River 沿用 C02 实装=布局一致）；Park/RecordingStudio/Classroom 数据已归档待 C05-C07 |
| BG01_Underworld / BG02_River / BG03_FarmCourtyard 正式背景 | **未交付**——当前为占位渐变图（稳定文件名，替换不动代码） |
| IT01_Glasses / IT02_Cane / PRP03_DoorCloseup / PRP04_Stool / PRP05_DiningTable / CG01_FamilyFarewell | **未交付**——占位图/程序绘制门特写/模板描画；MG02 门环判定位按 R04 交接第 4 条与代码一致 (380,200) |
| 音频（12 SFX / BGM / CFG_Notes） | **未交付**——Trace/Notes 程序音占位（既有机制） |

## 四、交付后待办（非 C04 范围）

1. CG01_FamilyFarewell 插画与 PRP03_DoorCloseup 正式图交付后，分别替换演出叠加层与门特写底图（文件名稳定，代码不动）。
2. C05 注册 CH02 后，CH01_Complete 的“主线到 CH02”自动生效（菜单“开始”=首个未完成正式章，本任务已把选章逻辑改为正式章优先）。
3. C09 回忆馆/章节目录界面消费 unlockedPresentationIds 与回放入口。
4. C11 清理：DEV 机关测试按钮、DEV 双路线对象（CH01_DEV 全套）、探针日志、ClickProbe。
