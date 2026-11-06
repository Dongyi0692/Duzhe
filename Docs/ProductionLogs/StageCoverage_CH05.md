# StageCoverage_CH05.md — CH05 阶段覆盖表（C08，2026-10-30）

> 对照《08 第五关规范 v1.0》阶段表逐行核对。实现 = C08 交付的代码/装配；验证 = 自动验证与人工验收的对应关系；剩余问题如实列出。
> 本章无 NPC、无任务物、无仪式；唯一可控角色是渡客，四个生者只作为"过去的身份"在回忆图里出现。

## 一、阶段覆盖（6/6）

| # | stageId | 进入条件/场景 | 当前实现 | 完成事件与输出 | 验证方式 | 剩余问题 |
|---|---|---|---|---|---|---|
| 1 | CH05_Recall | 主线 CH04 完成或回放；Underworld | 自动播 `PR_CH05_Recall`（`CH05_Recall` 16 句，手动逐句）：第 004/005/006/007 句分别叠出 CG01/CG02/CG03/CG04（CG04 同时显示引擎字层「再见」），第 009–012 句再轮一遍，第 013 句清空画面；图片只随行号切换，不按计时 | PresentationFinished(source=PR_CH05_Recall) → LetGo | DZ_CH05_PLAY（16 句、四次切图与行号一一对应、第 013 句清屏、恰好 1 次事件）；DZ_SMOKE（段/PR 存在） | 四个身份到渡客的"短叠化"用现有插画默认实现，未做额外图层叠化 |
| 2 | CH05_LetGo | 回忆演出结束 | 阶段进入自动播 `CH05_LetGo`（11 句，手动逐句）；只有整段结束才置 `letGoFinished` | DialogueFinished(source=CH05_LetGo) → Board | DZ_CH05_PLAY（11 句、旗标置位）；DZ_SMOKE（autoStart 断言） | 无 |
| 3 | CH05_Board | 放下完成 | 渡口 `CH05_BOAT`（(2,−6.2)）`BoardingSpot`，`activeStageIds` 仅 `CH05_Board`；回忆/放下期间热点不响应（进入前 Director 还校验 memoryRestored && letGoFinished，缺一即报配置错误） | Boarded(source=CH05_BOAT) → 转 River | DZ_CH05_PLAY（阶段门禁：前两阶段热点 inactive、Board 阶段可用；点击后进入 River） | 无 |
| 4 | CH05_River | 已登船 | C02 渡河全复用：**乘客 0 人**（不生成任何虚拟亡魂）、封 2 道；失败只重置 RiverSession，`memoryRestored`/`letGoFinished`/种子不变 | RiverSucceeded(source=River) → 留在 River 进入 Reincarnation | DZ_RIVER_SEEDS（64 种子，含 CH02–04 规则组）；DZ_CH05_PLAY（`RiverPassenger_0` 不存在） | 渡河成功后的 10 秒航程本身由种子验证器覆盖，Play 测试直接投递成功事件以验证结尾 |
| 5 | CH05_Reincarnation | 渡河成功 | 同一 River 场景：`PR_CH05_Reincarnation` 显示 CG05 → `CH05_Reincarnation` 4 句手动推进 → 末句后**先收起 CG**，再由 cast 让**船与渡客一起**缓缓远去（3.5s、Time.deltaTime、暂停冻结）并叠加暖色渐隐（可点穿，不挡名单按钮） | PresentationFinished(source=PR_CH05_Reincarnation) → Complete | DZ_CH05_PLAY（CG05 显示、4 句、CG 先隐藏、恰好 1 次事件）；DZ_SMOKE（PR 映射） | 远去为位置平移+暖色叠加，未做逐帧美术序列 |
| 6 | CH05_Complete | 轮回演出结束 | 显示 `UI/CreditsPanel`（制作名单，真实信息：程序与关卡集成 Codely / 美术音频文本 R01–R19 交付批次 / 团结引擎 1.10.4）；**必须点「返回开始界面」**才发 `CreditsConfirmed`（autoAdvanceSeconds=0）；重复点击只发一次；离开章节只隐藏不发事件 | CreditsConfirmed(source=CH05_CREDITS) → ChapterCompleted（仅 MainRun 写盘）→ 回主菜单 | DZ_CH05_PLAY（名单显示、双击只发 1 次、ChapterCompleted 恰好 1 次、ChapterReplay 不写档） | 回看后的最终目录/章节路由属 C09；本版完成后回主菜单 |

## 二、验收对照表（任务要求逐条）

| 验收项 | 状态 | 说明 |
|---|---|---|
| 六阶段各有入口/真实事件/退出 | **自动验证 + 待实机** | DZ_CH05_PLAY 从 StartChapter 一路走到 CreditsConfirmed；实机手感待用户 |
| 四个生者是主角过去的身份、另一空间、独自渡河入轮回 | 文本与演出保证 | TXT_CH05 与 CG01–CG05 表达；无新增 NPC/引路/现实复活 |
| 河段失败 3 次不重播回忆、旗标与种子不变、零乘客 | **结构 + 种子验证** | 失败只清 RiverSession（C02 机制）；旗标在 ChapterRuntimeState；`passengerCount=0` |
| 实际加载场景只含 Underworld/River（完成后 MainMenu） | **DZ_CH05_PLAY 路径证据** | 回忆在 Underworld，轮回在同一 River 场景（不换场景），完成后回 MainMenu |
| 回忆/轮回/名单分别暂停恢复、退出无残留 | **部分** | 字层/CG 在演出结束与 ForceAbort 时隐藏；暖色层随场景卸载销毁；名单 HideWithoutEvent 由暂停离开调用；**暂停中途退出未逐一实机复验** |
| 快连点确认不重复通关 | **DZ_CH05_PLAY PASS** | 双击只发 1 次 CreditsConfirmed、ChapterCompleted 1 次 |
| 仅完整轮回后解锁 PR_CH05_Reincarnation | 代码路径 | PresentationPlayer 在 MainRun 完成时解锁（C04 机制） |
| 不把其他四章一起标记完成 | **DZ_CH05_PLAY PASS** | 只标记 CH05；其余章节来自各自真实流程 |
| 两次装配对象数不增长 | **装配报告 PASS** | Underworld CH05 objects=1/casts=1、River casts=1，第二轮全部 already complete |

## 三、资源核对（如实）

| 项 | 状态 |
|---|---|
| TXT_CH05.json | **已接入运行时**（3 段：Recall 16 / LetGo 11 / Reincarnation 4；6 目标；演员仅 Ferryman） |
| PresentationMap.json | PR_CH05_Recall（Underworld）与 PR_CH05_Reincarnation（River）已存在且各映射 1 段 |
| PresentationLayers.json | CG05 允许整幅淡出（fullScreenFadeAllowed） |
| CG01–CG05 | 已导入（R14，640×360） |
| CH05 场景 | Underworld 只显示渡客（CH01–04/DEV 物件均按章隐藏）；River 复用既有渡船 |
| 音频（BGM05 等） | R15–R19 未接线（C10）；本章无新增音效调用 |

## 四、用户试玩步骤（约 6–8 分钟）

候选构建：`Builds/Candidates/20261030_012825_fb762981/Duzhe.exe`（121,029,996 字节）

> 当前存档记录 CH01–CH04 完成，主菜单「开始故事」会**直接进入 CH05**（第 5 章尚未通关）。想先看别的章可用 ESC →「DEV:选择章节」。

1. 进入 CH05：渡客独自站在阴间，回忆对白自动开始。逐句点下去，注意 **CG01/CG02/CG03/CG04 分别在认出爷爷/女孩/乐队/老师的句子出现**，CG04 上应能看到「再见」，到"这些都过去了"那句画面清空。
2. 回忆结束后自动进入"放下"对白（11 句），逐句点完。**在此之前渡口上船没有反应**；结束后到渡口按 E 上船。
3. 渡河：船上**没有乘客**；避开漩涡，失败一次也确认不会重播回忆/放下。
4. 到岸后 CG05 出现，4 句对白结束后画面切回河面，**船与渡客一起缓缓远去、画面转暖**。
5. 制作名单出现：点「返回开始界面」返回主菜单，确认 CH05 已记录通关；连续点两下按钮不应重复通关。
