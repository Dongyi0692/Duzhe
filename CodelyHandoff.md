# CodelyHandoff.md — 《渡者》五章主线交付（C01–C08 + 试玩修复，2026-10-30）

> 《渡者》Codely 侧交付报告。请把本文件全文发回资源助手对话。
> 详细生产日志见 `Docs/ProductionLogs/Progress.md`、`ProjectEnvironment.md`；逐章阶段覆盖表见 `Docs/ProductionLogs/StageCoverage.md`（CH01）、`StageCoverage_CH02.md`、`StageCoverage_CH03.md`、`StageCoverage_CH04.md`、`StageCoverage_CH05.md`。
> **本文件按时间追加**：§0–§7 是总览（每次交付同步更新），§8A–§8S 是各次交付/修复的现场记录。为避免拿到过期路径，历史章节里的候选构建路径已统一替换为**当前候选**；各次交付当时的字节数以 §8N/§8O/§8R/§8S 的说明为准。

## 0. 任务状态（2026-10-30 02:10 更新）

| 任务 | 状态 |
|---|---|
| C01 工程盘点与可运行基础层 | ✅ 已完成并经用户全流程验收（黑屏五根因、EventSystem 跨场景销毁等已修） |
| C02 仪式、登船、10 秒渡河与局部重试 | ✅ 已完成（含审查补正：面板生命周期、正式/DEV 分离、验证器公平模型） |
| C03 七种机关共用组件与配置 | ✅ 已完成（判定矩阵自动验证；多轮试玩修复） |
| C04 第1关 CH01「再吃一顿饭」 | ✅ 代码/装配/自动验证/构建完成；**实机全流程待用户验收**（三条试玩反馈已修，§8C） |
| C05 第2关 CH02「真心为你」 | ✅ 同上（§8I） |
| C06 第3关 CH03「一辈子的乐队」 | ✅ 同上（§8N），含前置测试隔离与候选构建目录改造 |
| C07 第4关 CH04「最后一课」 | ✅ 同上（§8O），含演出主线/回看分流与写字门禁 |
| C08 第5关 CH05「自渡」 | ✅ 同上（§8R），含制作名单与零乘客河段 |
| 主线连续进章 | ✅ 已补（§8S）：CH01→…→CH05 连续进入，仅最后一章回主菜单 |
| 试玩反馈修复 | ✅ §8K（CH01 崩溃）/§8L（项链图标、孙子站位）/§8M（MG06/MG07 面板）/§8P（选章）/§8Q（切章后无人物）/§8S（连续进章） |
| C09 主菜单/章节目录/回忆馆/运行重置 | ✅ 已完成（§8T，代码与自动验证；实机待试玩） |
| C10 音频接入 | ✅ 音频已完成（§8U；听感待用户试听）· 美术/UI 缺项见 §8U.5 |
| C11 发布清理与提交包 | ✅ 已完成（§8W，代码与自动验证；实机逐关待试玩） |
| C12 试玩反馈修复（场景/角色/UI/流程） | 🟡 **代码完成，未编译/未构建**（§8Y；本机 shell EPERM，需用户跑一键脚本） |
| C12 故障修复工具 | 按需 |

**当前候选构建**：`Builds\Candidates\20261030_144654_3ab6a797\Duzhe.exe`（143,707,948 字节，Succeeded errors=0，含 R12 行走表 + R06 河面；**不含 §8Y 的 C12 改动**）。上一版 `20261030_134744_dff9fcb8` 保留可回退。

## 0A. 章节 / 场景 / 入口速查

| 章节 | 场景 | 关键实体 | 机关 | 演出 |
|---|---|---|---|---|
| CH01 再吃一顿饭 | Underworld → River → FarmCourtyard | CH01_GRANDFATHER、IT01/IT02、CH01_RITUAL、CH01_BOAT | MG01_Cane、MG02_Door、MG03_Stools | PR_CH01_Farewell（CG01，只淡双老人） |
| CH02 真心为你 | Underworld → River → Park | CH02_LoverSpirit、IT03、CH02_RITUAL、CH02_BOAT | MG04_Heart | PR_CH02_Farewell（CG02，只淡女孩） |
| CH03 一辈子的乐队 | Underworld → River → RecordingStudio | CH03_DrummerSpirit、IT04、CH03_RITUAL、CH03_BOAT | MG05_Notes（正式 16 音）、MG06_Photo | PR_CH03_Farewell（CG03，只淡鼓手） |
| CH04 最后一课 | Underworld → River → Classroom | CH04_TeacherSpirit、IT05、CH04_RITUAL、CH04_BOAT | MG07_Goodbye（课堂后才可写） | PR_CH04_Farewell（CG04，只淡老师） |
| CH05 自渡 | Underworld → River（不换场景） | CH05_BOAT（零乘客） | 无 | PR_CH05_Recall（CG01–04 按行）+ PR_CH05_Reincarnation（CG05→船远去）+ 制作名单 |

## 1. 工程实际状态（全部实读，无假设）

| 项 | 值 |
|---|---|
| 工程路径 | `C:\Users\東壹\Duzhe`（已注册进 Tuanjie Hub） |
| 引擎 | 团结 1.10.4（内部版本 2022.3.62t16，修订 ab17684acf1d）— 已冻结 |
| 模板 | cn.tuanjie.template.2d 7.0.4（2D 内置渲染管线） |
| 输入方案 | 旧版 Input Manager（activeInputHandler=0，全工程统一旧版 Input API） |
| 渲染 | 内置管线 2D 正交（相机半高 5.625 @32PPU；窗口固定 1280×720，启动时 SetResolution 复位） |
| 构建产物 | **每次构建输出到新的** `Builds\Candidates\<yyyyMMdd_HHmmss>_<id8>\Duzhe.exe`（不再删除旧可玩版本）。当前候选：`Builds\Candidates\20261030_134744_dff9fcb8\Duzhe.exe`，**140,523,400 字节（2026-10-30 C08 五章完整版 + 连续进章，Succeeded errors=0）**；旧版 `Builds\Windows_x64\Duzhe.exe`（115,382,612 字节）保留可回退；窗口固定 1280×720；桌面快捷方式「渡者-Duzhe」仍指向旧版，验证新功能请直接用上面的候选路径 |
| 最近一轮自动验证（2026-10-30 02:05） | `DZ_VALIDATION` / `DZ_SMOKE` / `DZ_MINIGAMES` / `DZ_RIVER_SEEDS`(64 种子) / `DZ_CH03_PLAY` / `DZ_CH04_PLAY` / `DZ_CH05_PLAY` / `DZ_CHAPTER_FLOW` 全 PASS；报告在 `Logs/*.txt`。构建版启动与场景加载探针（Underworld/River/Classroom/RecordingStudio/Park/FarmCourtyard/MainMenu）均 PASS |
| 存档现状（用户真实存档） | `completedChapterIds=[CH01..CH05]`、`unlockedPresentationIds=[PR_CH03_Farewell,PR_CH05_Recall,PR_CH05_Reincarnation]`；所有自动测试均把写入重定向到 `Temp/*`，不改动该文件 |
| 测试隔离 | `SaveService.TestSaveDirectoryOverride`（编辑器专属）；`DevSmokeTest.Run` 整段包裹 `Temp/DuzheSmoke/<guid>`；Play 测试以 MainMenu/ChapterReplay 运行或在测试内设置覆盖目录 |

## 2. C02 交付内容（范围：仪式、灵魂就位、登船、10 秒渡河与局部重试）

### 2.1 新增模块

- **`Assets/_Project/Scripts/Ritual/`**
  - `IPropInteractable.cs`（Interaction 命名空间下）：Prop 类交互行为接口，InteractionController 按 E 时转发
  - `RitualTrigger.cs`：仪式锚点转发器（挂 Interactable kind=Prop 旁）
  - `BoardingSpot.cs`：登船点（发 Boarded 事件；阶段门禁由 activeStageIds 保证——未完成仪式无法登船）
  - `RitualController.cs`（场景根组件）：E 触发 → 相位守卫（**连按 E 不重复**）→ 短演出（InputMode.Ritual 锁输入、无对白、暂停兼容）→ 灵魂显现站位 → RitualCompleted 事件（幂等，只发一次）
  - `SpiritEscort.cs`：灵魂淡入、走到站位、演出后简单队列跟随（无寻路）；**无碰撞体，不挡玩家、不参与河中判定**；乘客数由章节配置（DEV=2，模拟 CH01 两位老人；CH02–04 一位、CH05 零位由 C04–C08 正式章配置）
- **`Assets/_Project/Scripts/River/`**
  - `RiverRules.cs`：**种子驱动的确定性波次规划**（System.Random(seed)，失败重试同种子可学习、重玩换种子）+ 可达性几何验证（运行时与编辑器验证器共用）：安全道存在、相邻波安全道移动 ≤1 条航道、封道不含安全道、时间窗 = 横移时间+反应 0.5s ≤ 波间隔、可见窗口 =(相机偏移+半屏高)/前进速度 ≥1.5s、漩涡生成于屏幕带之外
  - `RiverSession.cs`：**航程独立状态**（相位/计时/船位/已生成波次/重试次数）——失败重置只清这些；解谜、交还、仪式、已播对白与终章记忆全部保留在 ChapterRuntimeState，永不动
  - `WhirlpoolSpawner.cs`：按计划波次生成漩涡（占位 sprite、可视为碰撞区略大）、按前进速度下移、清空；碰撞为**纯数学 AABB**（无物理时序不可控）
  - `BoatController.cs`：船横向连续移动（x∈[-4.5,4.5]，速度可调），船体固定船线、漩涡相对下移表现"船自动向前"
  - `RiverController.cs`：状态机 Briefing→Sailing→Crashed→Succeeded；**首次说明面板确认后开航且不计时、重试不重复说明**；计时用 deltaTime（**暂停（timeScale=0）与失焦（runInBackground=false）天然冻结有效时间**）；每帧顺序=转向→生成→移动→**碰撞判定（先）→10 秒成功（后）——同帧唯一仲裁**；失败显示"被漩涡卷回了渡口"+"重新渡河"（同一按钮重置本航程，同种子重来）；**成功只发一次 RiverSucceeded，不直接标记章完成**（仅 ChapterDirector 可标）；渡河期间渡客位置每帧同步船位（不挂场景物体）；相机 y 偏移进入 +2.0/离开复位；船上按乘客数自动生成占位乘客
- **`Assets/_Project/Scripts/Editor/RiverSeedValidator.cs`**：批处理种子可达性验证（菜单「渡者/河流种子可达性验证」/ `-executeMethod Duzhe.EditorTools.RiverSeedValidator.RunFromCommandLine`）。**2026-10-27 审查补正后为可见性公平模型**：模拟船只在漩涡进入可见带（到达时间 − 可见窗口 1.525s）+0.5s 反应后才开始转向（不再以屏外生成时间作弊）；碰撞按全下落轨迹的箱边扫描；**双组各 32 种子**——GroupA 封 1 道（CH01/DEV 规则）、GroupB 封 2 道（CH02–04 规则），报告 `Logs/river_seeds_report.txt`（两组参数+每种子波表+模型说明）
- **修改**：`ChapterConfig.cs`（新增 RiverSettings 全参数可调类 + ChapterRuntimeState.riverSeed）、`ChapterCatalog.cs`（CH01_DEV 扩展为 6 阶段：INTRO→FIND→RETURN→**RITUAL→BOARD→RIVER**→章完成；riverSettings=乘客2/每波封1道）、`ChapterDirector.cs`（进章生成种子、暴露 CurrentRiverSeed）、`InputRouter.cs`（River 模式读横向输入，无纵向）、`CameraFollow2D.cs`（yOffset 字段）、`InteractionController.cs`（Prop 分支转发 IPropInteractable）、`PlaceholderArt.cs`、`ProjectSetupUtility.cs`（新增占位图 Boat/Whirlpool/RitualAnchor + Underworld 仪式区与 River 渡河区幂等装配 + FindOrCreateChild 支持隐藏物体幂等）、`TXT_UI.json`（28 键，新增 river_* 5 键）、`TXT_CH01_DEV.json`（6 阶段 objectives + 修正感谢对白尾句）

### 2.3 C03 新增（2026-10-27，七种机关的共用组件与配置）

- **`Assets/_Project/Scripts/Minigames/`（9 文件）**
  - `MinigameConfig.cs`：7 份机关配置目录（MG01_Cane/MG02_Door/MG03_Stools/MG04_Heart/MG05_Notes/MG06_Photo/MG07_Goodbye，type/templateId/slotCount/sequence/panel 尺寸）——C06 起 CFG_* JSON 接入替换数据不改 ID
  - `MinigameHost.cs`：唯一开关门（防重入、InputMode.MiniGame、一次 MinigameCompleted 事件带 runId/stageId/sourceId=minigameId、暂停边沿通知面板、Close 恢复探索、无残留）+ MinigamePanelBase（面板挂持久 Canvas SetAsFirstSibling 底层=暂停置顶永远盖住）+ MinigameTesterPanel（DEV 选择面板）
  - `TraceTemplates.cs`：三套真实可玩模板（Cane 两单元竖柄+钩弧；Heart 参数方程闭合心形；**Goodbye 现为"再见"字形骨架 13 单元**（再 6 笔含左右两短竖（上端固定、长度×2）+ 见闭合方框+内横+儿双腿 2 笔）——正式 CFG_TraceGoodbye 在 C06+ 替换形状）
  - `TraceEvaluator.cs`：像素空间判定（采样覆盖率≥75%、走廊内墨迹占比≥65%、走廊半宽=绘图区短边 5%、起点容差、终点到达≥95%、防直线/乱涂）
  - `TraceMinigame.cs`：绘图区 RawImage+Texture2D 双层（模板引导线分色=完成绿/当前黄/未始灰、**实时笔迹颜色反馈（走廊内绿/外红）**、**笔画完成/拒绝程序提示音**、重画只清当前笔画、抬笔保留进度、暂停清当前笔、**起笔自由化**）
  - `DragPlacementMinigame.cs`：MG03（3 把同款凳子任意空位、满 3 完成、错放回原位、已放锁定、**吸附半径 80 UI 像素**）；MG06（仅鼓手可拖、唯一缺口、**吸附判定用屏幕空间实际位置**（GetWorldCorners 屏幕四角，与显示严格一致）；**拖拽保持鼠标-物品相对偏移**（点击不瞬移）、拖起置顶）；暂停打断未完成拖拽回原位、已完成保留
  - `DoorKnockMinigame.cs`：门环热点点击完成（其他区域无进度、可关闭重开、完成自动退出）
  - `NoteSequenceMinigame.cs`：七音 Do–Si 按钮+数字 1-7 辅助、**目标音按钮金黄高亮+明确指令**（请点击：X（数字键 N））、下一目标进度点、错音发声不推进不清进度、无节拍失败；程序生成七音正弦占位音；CFG_Notes.json 读取骨架（Resources/Data/Minigames/，缺失回退占位序列）；enablePerformance=false 关闭路径（自动完成一次事件）
- **修改**：`GameBootstrap.cs`/`GameSession.cs`（挂 MinigameHost 服务）、`PauseMenu.cs`（**DEV"机关测试"按钮移出 #if 条件编译**（构建版可用，C11 清理）→MinigameTester）、`ProjectSetupUtility.cs`（Stool 占位图）、`PlaceholderArt.cs`（Stool 路径）、`TXT_UI.json`（新增 trace_redraw/trace_done/minigame_close/minigame_stools_progress/menu_progress_dev 等 8 键）、`MinigameHost.cs`（PanelGO pivot=(0,1) 与 anchor 一致）
- **试玩反馈修复记录（2026-10-27–28，多轮）**：MG02 门面板 y 坐标出屏修正；MG03/MG06 槽位 y 坐标出屏修正；MG07 重画按钮 y 符号修正；Goodbye 形状迭代为可辨认"再见"骨架（含"再"字两竖加长至翻倍）；Notes 引导增强与进度点容器全屏修正；MG06 布局回滚到 4 槽位等距布局（3 成员+1 缺口）、**吸附判定屏幕空间化**（物品与槽位实际屏幕位置做判定，绕开 pivot/anchor 语义差）、**吸附半径 24→80**、**拖起物品置顶**、**拖拽保持鼠标-物品相对偏移（点击不瞬移）**；SNAP FAIL/TRACE down 探针用于定位（C11 清理）。

### 2.4 自动验证记录（全部真实执行，报告在 `Logs/`）

| 验证 | 结果 | 报告 |
|---|---|---|
| 装配+校验（两轮，第二轮验证幂等） | DZ_VALIDATION=PASS；Underworld/River 场景第二轮 already complete，无重复对象 | `Logs/duzhe_validation.txt` |
| **河流种子可达性验证（双组各 32 种子，共 64，可见性公平模型）** | **DZ_RIVER_SEEDS=PASS**——GroupA 封 1 道（CH01/DEV 规则）32 种子 + GroupB 封 2 道（CH02–04 规则）32 种子全部可达；每种子波表+安全道+全程箱边碰撞扫描；时间余量 0.275s（需求 0.75s） | `Logs/river_seeds_report.txt` |
| 开发冒烟回归（存档/对白库/章节目录） | DZ_SMOKE=PASS（存档写读/幂等/坏档恢复/对白解析查找/6 阶段目录） | `Logs/duzhe_smoke.txt` |
| Windows x64 构建 | Succeeded，errors=0，**107,474,912 字节（当前构建，含 C03 及试玩反馈修复）**；启动探针正常 | `Logs/duzhe_offset_build.log` |
| **机关判定矩阵（TraceEvaluator×三模板 + Notes 状态机）** | **DZ_MINIGAMES=PASS**——三模板完美轨迹全过；直线/乱涂 8 组/半程/错序全被防住；Notes 正确序列完成、错音不推进 | `Logs/minigames_report.txt` |

验证器实战三例：① 渡河可见窗口公式方向写反（0.98s<1.5s）；② 模拟船以屏外生成时间提前反应（作弊）——两处渡河问题；③ TraceEvaluator 坐标空间混用（归一化模板 vs 像素墨迹）与验证器抖动空间——判 0% 覆盖。全部由自动验证抓出并修正。

**未测（需人工 Play，不能由我代测）**：仪式演出手感、渡河避障实际操作手感、连续失败 3 次的体验、失焦/暂停冻结的实机感受、边界试玩（船到横移极限/波间穿行）、**七个机关的 DEV 入口试玩（描画流畅度/拖放吸附/门环点击/七音按钮与数字键）**。规则层面（Trace 判定矩阵/Notes 状态机/种子可达性/碰撞仲裁/种子确定性）已由自动验证覆盖。

### 2.5 C04 新增（2026-10-28，正式 CH01「再吃一顿饭」12 阶段全流程接入）

- **正式章节配置**（`ChapterCatalog.BuildChapter01`）：12 阶段严格按规范阶段表——Opening（靠近自动对话）/ Prepare（**requireAll 多条件**：眼镜拾取+拐杖描画任意先后，MG01_Cane 完成授予 IT02_Cane）/ ReturnItems（**一次 E 交还两件**）/ RitualTalk（自动段）/ Ritual（CH01 专属锚点 CH01_RITUAL）/ Board（CH01_BOAT）/ River（sourceId 规范为 River）/ Door（MG02_Door）/ ReunionTalk（自动段）/ Stools（MG03_Stools）/ **Farewell（PR_CH01_Farewell 演出驱动，PresentationFinished 才完成）**/ Complete（1.4s 短淡出自动推进→ChapterCompleted）。目标文案全部来自 TXT_CH01 objectives。
- **演出系统**：`Scripts/Presentation/PresentationPlayer.cs`（PresentationMap.json 驱动：座位演出→orderedSegmentIds 逐段手动推进→末句后视觉阶段→恰好一次 PresentationFinished+仅 MainRun 解锁写盘；缺失对白中止不静默通关）；`Scripts/Scenes/FarmCourtyardCast.cs`（MG02 后孙子出门走位、MG03 后三凳锁定显示、PR_CH01_Farewell 座位→对白→两老人 1.2s 淡出+孙子留座 0.8s）；`UI/FadeOverlay.cs`（章节收尾淡出，暂停冻结同步）。
- **R01 正式文本接入**：`ProjectSetupUtility.EnsureDialogueImports` 把 TXT_CH01 / TXT_UI（**正式 201 key 版**）/ PresentationMap / CharacterDisplayNames 复制进 Resources（字节一致幂等）；DialogueLibrary 增演出映射+全局演员名解析；**全部 UiText 调用点迁移到正式点分 key**（speaker.*/hint.*/feedback.*/minigame.*/river.*/button.*/volume.*/confirm.*/chapters.* 等）。
- **R04 布局装配**（`worldX=(px-640)/32, worldY=(360-py)/32`）：Underworld 复用已验收位置+新增拐杖台 (480,400)px→(−5,−1.25)、奶奶 (748,424)px→(3.375,−2)；FarmCourtyard 全量装配（出生 FC_Spawn→(−11,−6.75)、门 (640,152)→(0,6.5)、门交互区中心 (640,216)→(0,4.5)、桌 (736,400)→(3,−1.25)+桌碰撞 (4,2)u、桌交互 (736,472)→(3,−3.5)、三凳位 (672/800/736,448/448/496)px→(1/5,−2.75)/(3,−4.25) 淡影+锁定实凳、孙子 (0,6.5)→(0,2.75)、房墙三箱+桌箱静态碰撞）；River 与 C02 实装一致（船 (640,456)px=(0,−3) ✓ 布局对表通过）。新增占位图 Cane/Door/DiningTable（稳定文件名替换不动代码）。
- **C03 遗留不可见 UI 修复**：复核 y 符号约定（负 y=自顶向下可见）确认 Drag/Notes/Door 三面板的标题/状态行/关闭按钮自 C03 起一直用正 y（屏外不可见，因交互元素可见未被发现）——本轮全部修正为可见；MG03 增正式说明行（minigame.stools.instructions）、凳子起始位下移避让；门特写「关闭」按钮修复后"关闭可重开"验收项可真实执行。
- **健壮性**：暂停-离开确认强制清理（MinigameHost.Close + DialogueRunner.ForceStop 无事件 + PresentationPlayer.ForceAbort）防运行残留泄漏进菜单；选章改**正式章优先**（CH01_DEV 不再挡正式链；正式全完成回放 CH01）；InteractionController 多物品交还+按上下文 promptKey 提示（hint.pickup/return_items/ritual/board/cane_station/door/table）；RitualTrigger 按 anchorId 精确匹配控制器（DEV/CH01 双仪式区共存零串扰）。
- **修改文件清单**：`ChapterConfig.cs`（StageCondition/requireAll/grantsItemId/autoStartPresentationId/autoAdvanceSeconds/entryHintKey）、`ChapterCatalog.cs`、`ChapterDirector.cs`、`GameSession.cs`/`GameBootstrap.cs`（Presentations 接线）、`DialogueData.cs`、`DialogueRunner.cs`（ForceStop）、`DialogueView.cs`、`Interactable.cs`/`InteractionController.cs`、`SpiritEscort.cs`（Seat/IsSeated）、`RitualTrigger.cs`/`RitualController.cs`、`RiverController.cs`、`MinigameHost.cs`/`MinigameConfig.cs`/`TraceMinigame.cs`/`DragPlacementMinigame.cs`/`DoorKnockMinigame.cs`/`NoteSequenceMinigame.cs`、`MainMenuPanel.cs`/`PauseMenu.cs`/`ObjectiveHud.cs`（+任务物图标行）/`UiText.cs`、`PlaceholderArt.cs`、`ProjectSetupUtility.cs`、`DialogueJsonValidator.cs`、`DevSmokeTest.cs`；新增 `UI/FadeOverlay.cs`、`Presentation/PresentationPlayer.cs`、`Scenes/FarmCourtyardCast.cs`。
- **自动验证（全实跑，2026-10-28）**：DZ_VALIDATION=PASS（4 文件导入+Underworld/FarmCourtyard CH01 装配+TXT_UI 201 key 校验）；DZ_RIVER_SEEDS=PASS（64 种子双规则组）；DZ_MINIGAMES=PASS；DZ_SMOKE=PASS（新增：CH01 12 阶段结构+链闭合、requireAll 条件+cane 授予、演出映射与段存在、正式 UI key 19 项、CH01 目标/物品、选章=CH01 断言）；构建 Succeeded errors=0 107,567,284 字节。
- **资源缺项（如实，见 StageCoverage.md 第三节）**：正式背景 BG01/02/03、IT01/IT02、PRP03_DoorCloseup/PRP04_Stool/PRP05_DiningTable、CG01_Farewell、音频包未交付——占位方案运行中。

## 3. River 配置与占位水域布局导出（给资源方 R04/R06/R13/R16 对表）

| 参数 | 值 | 说明 |
|---|---|---|
| 航道中心 x | -3 / 0 / 3（laneSpacing=3） | 虚拟三航道，不显示格子 |
| 船可控横向范围 | x ∈ [-4.5, 4.5] | 连续移动，steerSpeed=4 u/s |
| 前进速度 | 5 u/s | 船体固定船线 y=-3，障碍相对下移表现前进 |
| 航程时长 | 10 秒有效游戏时间 | Briefing 确认后开始计时；说明/加载不计时；无可见倒计时 |
| 波次到达时间 | 第 3/5/7/9 秒（间隔 2s） | 波表由种子生成 |
| 封道规则 | DEV(=CH01 规则)每波最多封 1 条；其余章最多 2 条；安全道随机游走相邻波移动 ≤1 条 | 正式章由 ChapterConfig.riverSettings 按章配置 |
| 漩涡生成 | 到达前 2.5 秒生成于屏幕带外（spawnLead=2.5） | 可见窗口 1.525s ≥1.5s（相机 yOffset=2.0） |
| 船碰撞盒 | 宽 0.8 × 高 0.5（半宽高 0.4/0.25） | 取船体内部；纯 AABB 数学判定 |
| 漩涡碰撞盒 | 宽 0.9 × 高 0.9（半宽高 0.45） | **可视轮廓略大于碰撞区**；视觉 x 抖动 ±0.4 |
| 相机 | 渡河期间 y 偏移 +2.0，离开复位 | 保证预警可见窗口 |
| 乘客 | DEV 2 位（占位 NPC 图，船上两侧） | CH01 两位/CH02-04 一位/CH05 无 |
| 种子策略 | 进章生成一次；**失败重试复用（可学习）**；重玩/重新开始该章换新种子 | ChapterRuntimeState.riverSeed |
| 暂停/失焦 | timeScale=0 与 runInBackground=false 双冻结（计时/障碍/演出全停） | ESC 暂停渡河中同样生效 |
| 失败/重试 | 碰撞→冻结→"被漩涡卷回了渡口"面板→「重新渡河」→同种子从航程起点重来；**不重播说明/开场/谜题/仪式**；无 HP、无死亡画面、无次数惩罚 | RiverSession 与 ChapterRuntimeState 分离 |
| 正式素材需求 | 船图（**俯视 2D 小渡船**，PRP01_Boat 96×64——与 R02 母图一致；乘客站位与碰撞参数沿用现有配置）、漩涡图、渡口水岸细节、水面流动层（可选）、渡河 BGM/SFX（开航/撞击/重试/成功——沿用既定 12 SFX 复用映射，不新增必需资源数量） | 占位现为程序生成纯色图，替换按同路径保留 .meta |
| **C03 机关几何导出（R03 UI / R13 道具对表）** | 机关面板底图与热点的占位几何如下，正式图按同尺寸替换 | 说明 |

### 3.1 机关几何规格（占位实现，正式图按此替换）

| 机关 | 面板尺寸（UI 像素） | 绘图区/热点 | 备注 |
|---|---|---|---|
| MG01_Cane（描画拐杖） | 面板卡 280×550；**绘图区 240×~385**（纹理 480×420） | 模板 Cane：单元1 竖柄、单元2 顶部钩弧；走廊半宽=绘图区短边 5%（约 21px） | 重画按钮 180×44 |
| MG02_Door（敲门） | **PRP03_DoorCloseup 640×360** | 门环热点 48×48，中心在底图 (320+60, 360/2−20) 附近——**正式热点位置由程序配置，可改** | 关闭按钮 130×44 |
| MG03_Stools（摆凳子） | 面板卡 680×470 | 3 个槽位轮廓 40×40，x=面板 22%/50%/78%、y=面板 38%；凳子起点 y=14% | PRP04_Stool 32×32；桌面装饰 96×64 |
| MG04_Heart（描画爱心） | 面板卡 480×550；**绘图区 440×~440**（纹理 440×440） | 模板 Heart：单闭合单元 | 同走廊规则 |
| MG05_Notes（七音） | 面板卡 760×400 | 七音按钮 86×72 横排（间距 8）；进度点 14×14 顶部 | 占位序列 14 音；PRP 类无图，程序按钮 |
| MG06_Photo（合照） | 面板卡 680×470 | 照相框（面板内缩 20/40）；3 个固定站位 48×64（24%/50%/76% 宽、55% 高）+鼓手缺口 48×64（50% 宽、30% 高）；吸附半径 24px | PRP06_PhotoFrame 640×360 底图；鼓手=可拖 |
| MG07_Goodbye（写再见） | 面板卡 680×430；**绘图区 640×~320**（纹理 480×420） | 模板 Goodbye：5 笔画骨架占位（正式 CFG_TraceGoodbye 替换形状）；PRP07_BlackboardCloseup 640×360 底图 | 重画/关闭按钮 |

正式图未到时全部用几何占位（色块/引导线），交互不受阻。

正式五关的 riverSettings（乘客数/封道数）由 C04–C08 接入章节配置时按规范设定；R04 交付 River 布局时只需固定可视水域与边界，**船与随机障碍由脚本控制，布局不固定障碍位置**（已按此约定实现）。

## 4. 资源方交接约定落实情况

- **R02 风格批准包 v1.3 已接收（2026-10-27，用户已确认画风）**：暂存 `SourceAssets\R02_Style_Approved_v1.3\`（母图 VisualGuide.png 仅作视觉参考，不作正式地图/图集）；StyleLock/StylePromptBlock/VisualQA/ApprovalRecord/C01 集成评审归档 `Docs/Spec\`（R02_ 前缀）。风格基线（暗橙暖色阴间/伪三维混合俯视/32×48×4 帧行走/pivot(0.5,0)/5 色板/渡客-奶奶-眼镜识别锁定）已在 R05–R13 资源生成与 C10 导入时执行；母图不直接进 Assets。
- **R01 正式文本包 v1.0.0 已接收（2026-10-27）**：原始包暂存 `SourceAssets\R01_Text_v1.0\`；JSON 归档 `Assets/_Project/Data/Dialogue/`（TXT_CH01–05/TXT_UI/PresentationMap/CharacterDisplayNames/AssetDelivery）；剧本与核文档归档 `Docs/Spec/`（R01_ 前缀）。**只读结构预检全绿**：237 对白 lineId 零重复、53 阶段 objectives 与阶段总表一致（12/11/12/12/6）、201 UI 键零重复（objective.* 53 个）、PresentationMap 6 项、12 演员正式 ID（Ferryman 等）。**未接入运行时**——按约定 C04–C08 逐章启用（当前 ChapterCatalog 仅 CH01_DEV，直接进 Resources 会被校验器报未知章）；接入时将做点分键/占位键显式映射与 speaker 显示名映射，无对白阶段不注入空段；正式 CH01 12 阶段（含 Door/Stools 机关段）由 C03 机关组件 + C04 接入装配。
- 正式 actorId（Ferryman 等）与点分 UI 键（menu.start 等）待接入时做显式映射或改调用，不与占位键混用 ✓
- CH01_DEV 仅用于开发，不计入正式五关完成数（**主菜单已按 FormalChapterIds 过滤，DEV 完成显示开发标注**）；开发入口（DEV 按钮/探针）按最终发布检查清理 ✓
- 主菜单复用冥河背景的约定已记录：正式版主菜单背景将采用 R06 冥河素材（当前占位 BG_MainMenu_Placeholder 在 C10 替换时移除，不增加正式背景数量）✓
- C02 试玩反馈修复记录（2026-10-27）：仪式时"老人本体与灵魂重复出现"——已加 hideOnRitual 连线（仪式瞬间隐藏本尊）；根因是构建流程缺场景装配步骤（EnsureScenes 未随构建执行导致场景未更新），已把 EnsureScenes 加入 DuzheBuildTools.Build 流程，从此构建保证场景与代码同步。
- **C02 审查补正（2026-10-27，C02_ReviewFix）四块全部完成**：① 渡河面板生命周期（OnDestroy 销毁 briefing/retry、重试面板复用不重建、River 面板置 Canvas 底层、PauseMenu 打开置顶且遮罩恢复模态拦截）；② DEV 与正式进度分离（FormalChapterIds 过滤，DEV 完成显示"不计入正式五关"标注）；③ 验证器反应时机修正为可见性公平模型（可见带+0.5s 反应、全轨迹箱边扫描），双组各 32 种子（封1/封2）全 PASS；④ 资源同步（R02 归档、俯视 2D 措辞、12 SFX 复用）。
- **船占位升级（2026-10-27 下午，试玩反馈）**：用户报"开船时船不见了"——排查确认装配/引用/连线全部正常，真因是旧占位船仅 40×24px 且渲染在角色之上，三人站立后视觉不可辨。已按 PRP01_Boat 96×64 规格重画俯视 2D 渡船（船体/甲板/座板/船头灯），渲染层次改为船与角色同层 Y 排序（人站船上、船在脚下），乘客站位外移。渡口（Underworld）与河面（River）两处同步更新。字节数统一为当前构建 107,441,272。

## 5. 给资源方的对接信息（延续 C01）

- **文本**：正式 R01 交付时按 `TXT_CH01.json…TXT_CH05.json` 放入 `Assets/_Project/Resources/Data/Dialogue/`（UTF-8 无 BOM；schemaVersion "1.0"；lineId 全项目唯一；speakerId 只用规范人物 ID）。校验器报具体文件/字段/ID。
- **占位资源 ID（已建，正式图按同路径替换、保留 .meta）**：角色 `Art/Characters/Duke_Placeholder`、`Npc_Placeholder`；道具 `Art/Props/Item_Placeholder`、`Boat_Placeholder`、`Whirlpool_Placeholder`、`RitualAnchor_Placeholder`；背景 `BG_Underworld/River/FarmCourtyard/Park/RecordingStudio/Classroom/MainMenu_Placeholder`
- **坐标约定**：背景中心=世界原点；worldX=(px-640)/32，worldY=(360-py)/32；碰撞层 Player=8/WorldSolid=9/Interactable=10/Trigger=11/Boat=12/RiverHazard=13；排序层 Background→GroundProps→Actors→Foreground→Effects
- **尚需资源 ID**：R01 剧本五关、R03 UI 皮肤、R04 布局六场景、R05–R10 背景、R11 角色、R12 行走表（frameLayout 3×4，rowOrder=[Down,Left,Right,Up]）、R13 道具（含船/漩涡）、R15 音符、R16 音效（含渡河相关音效触发点：开航/撞击/重试/成功）、R17 配乐

## 6. 当前问题与风险（无阻塞项；2026-10-30 更新）

1. **音频整体未接线**（C10）：`AudioService` 框架与触发点就绪，但 BGM（R17）、12 个 SFX（R16）、7 音 WAV（R15）都没有导入播放；MG05 仍是程序正弦占位音，CH04 铃声/CH03 快门调用会留"未导入"日志。
2. **C09 已完成**：主菜单四页（首页/章节/回忆/设置）、暂停菜单章节入口、回忆馆 6 条白名单回放、运行重置与 RunId 代次、场景 Ready 顺序与显隐刷新。剩余：若后续正式交付 R19 新演出，需追加到 `GalleryController.Entries`；实机逐条回忆待用户复验。
3. **临时诊断残留**（C11 清理）：`ClickProbe`、启动探针日志、暂停菜单的 DEV 区（完成当前章/清空存档/机关测试/选择章节）、`MinigameTesterPanel`、`DiagDump`；本轮用过的 `SaveProbe` 与 `-dzprobe` 场景探针**已删除**，不随构建发布。
4. **机关面板视觉**：MG06/MG07 已用 R13 正式底图；MG01/MG02/MG03/MG04 仍为色块+引导线，R03 UI 皮肤与 PRP03/04/05 待 C10 替换（判定按几何，替换不影响逻辑）。
5. **美术缺项**：CHR02/03/05/07/09/10/11 与 CHR04/06/08/12 为单帧站姿，除主角 CHR01 外无行走表；R11 明示 32×48 下辨识度有限，**不得宣称美术最终验收通过**。
6. **渡河表现**：背景静止、船前进用障碍下移表现；水面流动层待 R06 条带素材。
7. **环境**：Hub CLI `projects create` 在 Cowork 常驻时不可用（不影响其他操作）；编辑器偶发 LicensingClient 校验失败导致启动慢，Play 测试超时已放宽。

## 7. 可用入口（2026-10-30 更新）

- **游戏**：双击桌面快捷方式「渡者-Duzhe」（指向旧版）或**直接用本次候选** `Builds\Candidates\20261030_134744_dff9fcb8\Duzhe.exe`；编辑器打开 MainMenu 场景 Play 亦可。
- **机关测试（DEV）**：游戏中 ESC → DEV:机关测试 → 点选任一机关试玩（完成事件在测试里被阶段过滤）；Notes(off) 模拟 enablePerformance=false 路径。
- **选章（DEV）**：游戏中 ESC → DEV:选择章节 → 直接进入 CH01–CH05（已通关=回放模式，不写档）。正式入口：主菜单「章节目录」与暂停菜单「章节」（C09）。
- **批处理**：装配校验 `Duzhe.EditorTools.ProjectSetupUtility.RunFromCommandLine`；种子验证 `Duzhe.EditorTools.RiverSeedValidator.RunFromCommandLine`；机关判定 `Duzhe.EditorTools.MinigameValidator.RunFromCommandLine`；冒烟 `Duzhe.EditorTools.DevSmokeTest.RunFromCommandLine`（存档隔离到 `Temp/DuzheSmoke/<guid>`）；构建 `Duzhe.EditorTools.DuzheBuildTools.BuildWindowsFromCommandLine`（新候选目录）。
- **GUI Play 验证（非 batchmode，报告写 `Logs/`）**：
  - `Duzhe.EditorTools.CH03PlayTest.RunFromCommandLine` → `DZ_CH03_PLAY`（MG03/MG06/MG05/演出/快门/面板几何）
  - `Duzhe.EditorTools.CH04PlayTest.RunFromCommandLine` → `DZ_CH04_PLAY`（MG01/MG04 无回归、写字门禁、14 笔「再见」、演出分流、选章）
  - `Duzhe.EditorTools.CH05PlayTest.RunFromCommandLine` → `DZ_CH05_PLAY`（回忆按行切图、零乘客、CG05→船远去、制作名单）
  - `Duzhe.EditorTools.ChapterFlowPlayTest.RunFromCommandLine` → `DZ_CHAPTER_FLOW`（主线连续、回放回目录、不写档）
  - `Duzhe.EditorTools.MenuGalleryPlayTest.RunFromCommandLine` → `DZ_MENU_GALLERY`（菜单四页、章节/回忆列表、回忆回放路由与中途退出）

## 8. 最新一轮验收步骤（2026-10-30，五章完整版）

> 逐章细节见 §8N.9 / §8O.9 / §8R.8 与 `Docs/ProductionLogs/StageCoverage_CH0x.md`。下面是最短路径。

1. 用候选 `Builds\Candidates\20261030_134744_dff9fcb8\Duzhe.exe` 启动 → 主菜单「开始故事」。
2. 当前存档已记录 CH01–CH05 全部通关，所以会**重玩第 1 章**；这次重玩会**连续**进入第 2/3/4/5 章（每章结束直接进下一章，不再回主菜单），只有第 5 章结束回主菜单。
3. 想单独看某一章：进游戏后 ESC →「DEV:选择章节」→ 点 CH01–CH05（已通关=回放，不写档）。
4. 重点复核（每章）：机关可完成且只结算一次；告别演出"先收 CG、只淡亡者"；暂停/恢复与返回菜单无残留；CH05 制作名单必须点确认才通关。
5. 已知未接线：全部音频（C10）；章节目录/回忆馆（C09）。

---

## 8-历史. 用户验收步骤（C02 + 审查补正回归 + C03 机关，约 8 分钟）

1. **走到仪式**：主菜单点「开始」→ 走近老人完成对白 → 拾取眼镜 → 交还。感谢对白后目标变"在渡口按 E 举行送别仪式"。此前若靠近渡口石灯按 E **不应有反应**（阶段未授权——前置校验）。
2. **仪式与登船**：靠近左下石灯按 E（**连按几次 E 也不应重复触发**）→ **老人本体应立即消失**、两位灵魂淡入走上站位（短演出，无对白，约 2 秒）→ 目标变"登船"，灵魂开始跟随你。对渡口旁的渡船按 E 登船——**仪式完成前船不应响应**。
3. **说明面板与暂停层级**：进入冥河弹出说明面板（不计时）→ **此时按 ESC**：暂停菜单必须完整盖在说明面板之上，且暂停期间点「开航」位置**不应有反应**；ESC 关闭暂停后说明面板仍在、可正常开航。用 A/D 或左右键横移避漩涡（至少 1.5 秒预警可见）。
4. **失败重试与面板复用**：故意撞漩涡 → 弹"被漩涡卷回了渡口"+「重新渡河」→ 点击后从本航程起点同布局重来，**不重播说明/不回阴间**；**连续失败 3 次**（面板每次正常显示与关闭、不应越叠越多）；失败面板显示时按 ESC 再验证暂停盖在它之上；「返回主菜单」离开后**重新进入渡河**，说明面板应正常再次出现且操作无异常（面板无残留/无重复回调）。
5. **成功与开发进度显示**：撑过 10 秒 → 回主菜单——**预期显示"开发测试路线已完成（Placeholder，不计入正式五关）"**（不再显示"已完成章节：1"，DEV 不计入正式进度）→ 重启游戏该行仍在（存档保留）。
6. **七个机关试玩（DEV 入口）**：ESC 暂停 → 点「DEV:机关测试」→ 逐一打开试玩：**描画拐杖/爱心/再见**（沿亮黄引导线描，试重画按钮与抬笔；故意画直线/乱涂应无法过关）；**摆凳子/合照**（拖拽吸附、错放回原位、合照只有鼓手可拖）；**敲门**（点门环完成）；**七音**（点按钮或按数字键 1-7，错音试一下不应推进）；**MG05_Notes（enablePerformance=false 关闭路径）** 应直接完成。

---

**C02 结论**：范围（仪式/灵魂/登船/10 秒渡河/局部重试）全部实现；审查补正四块全部落地（面板生命周期/暂停层级、DEV 与正式进度分离、验证器可见性公平模型+双组 64 种子、资源同步与 R02 归档）；自动验证全绿。

**C03 结论**：七种机关的 4 类共用组件全部实现（描画×3/拖放×2/敲门/七音），三套描画模板真实可玩，判定矩阵自动验证全过（完美轨迹过、直线/乱涂/半程/错序全防），测试入口与关闭路径就绪；**机关面板的实机手感待用户用 DEV 入口试玩**。C01–C03 全部完成后执行 C04（第 1 关全流程接入，正式 CH01 12 阶段含门环/摆凳两个机关段）。

## 8A. C04 用户验收步骤（2026-10-28，约 12 分钟）

> 前置：桌面「渡者-Duzhe」启动最新构建（107,567,284 字节）。冒烟测试曾清空存档，本次从全新进度开始即为目标状态。

1. **正式主线开场**：主菜单点「开始故事」→ 进入阴间（出生 (0,-6)）→ 走近两位老人 → 自动播放正式开场对白（12 句，逐句点击推进，无跳段）→ 顶部目标依次变化。【已测=结构断言；待测=实机触发】
2. **找物与描画（任意顺序）**：右上前拾取「老花镜」（提示"按 E 拾取"）→ 左侧拐杖台 E 打开描画 → 沿亮线画出拐杖（可抬笔）→ **两项都完成才进入交还**（HUD 右上出现任务物图标）；只完成其一时观察目标不变。【待测】
3. **交还与仪式**：回到老人旁（提示"按 E 交还物品"）→ 一次 E 交还两件（图标收起）→ 自动播放仪式引导对白（9 句）→ 仪式锚点 E → 两位老人淡出、双灵魂跟随 → 「仪式完成」提示 → 渡口 E 上船。【待测】
4. **渡河（含故意失败）**：说明面板（正式文案）→ 开航 → **故意撞一次漩涡** → 重试面板（确认"之前的任务已完成"）→ 重新渡河成功 → 到达农家院。【待测】
5. **敲门与摆凳**：左下出生 → 走到院门（提示"点击院门"）→ E 开门特写 → 点门环（**确认先点门/点空白不会误完成**）→ 点「关闭」→ 再次 E 可重开（复验 C03 修复）→ 敲门完成 → 孙子开门走出 → 相认对白（10 句）→ 餐桌 E → 拖 3 把凳子入空位（标题/说明/进度/关闭按钮本轮已修复为可见）→ 凳子锁定显示。【待测】
6. **告别演出与通关**：演出自动开始（三人入座）→ 26 句告别对白手动推进 → 末句后两位老人 1.2 秒淡出、孙子留座约 1 秒 → 黑场短淡出 → 「这一程，已经完成。」→ 回主菜单，进度显示「已通关：1」。【待测】
7. **暂停回归（穿插在第 2/5/6 步）**：对白中、机关面板中、演出中各按一次 ESC → 面板置顶、游戏冻结 → 恢复后原状态继续（拖拽中物品回原位、描画当前笔清空为既定规则）；再在暂停里选「返回开始界面」→ 确认弹窗 → 回菜单后**无任何对话/机关/演出面板残留**。【待测】
8. **回归抽查**：ESC → DEV:机关测试 → 随机开两个机关确认无回归；重新「开始故事」→ 此时 CH01 已通关，因 CH02 未注册会以回放方式重进 CH01（预期行为，C05 注册 CH02 后自动衔接下一关）。【待测】

---

**C04 结论**：正式 CH01 十二阶段全流程接入完成（多条件阶段/演出阶段/自动推进阶段均落地），R01 四份文本配置接入运行时并完成全部 UI key 迁移，R04 布局按像素换算装配两场景，MG01/02/03 挂接正式流程；自动验证 DZ_VALIDATION / DZ_RIVER_SEEDS / DZ_MINIGAMES / DZ_SMOKE 全 PASS + 构建 Succeeded（107,567,284 字节）；实机全流程与验收清单已测/未测明细见 `Docs/ProductionLogs/StageCoverage.md`。资源缺项（正式背景/道具图/CG01/音频）以占位方案运行，替换不动代码。
## 8B. 资源接收登记（2026-10-28，R05-R13/R15-R19 共 7 批，全部收货入 SourceAssets）

> 接收动作：解压至 `SourceAssets\<批次>\`、清单盘点、交接文档通读、静态预检。**均未导入引擎**——导入接入随 C05-C10 各任务执行。用户指令登记：R07-R10 接入时核对桌椅/长椅等画面与实际碰撞一致；R12 接入时在引擎里播放检查动作和方向。

| 批次 | 内容 | 交接声明 | 静态预检结果 |
|---|---|---|---|
| R05 阴间背景 | BG01_Underworld.png 1280×720 + QA 叠图 + raw | 河岸与 R04 渡口存在视觉偏移，勿按矩形盲标可行走 | 已登记接入核对项（见下） |
| R06 冥河背景 | BG02_River.png 1280×720 + 三船位 QA + raw | 无船/漩涡烘焙；非循环水纹勿垂直循环 | 与 C02 船线 (640,456)px=(0,-3) 对表一致；接入时核对岸边可视边界 |
| R07-R10 四景背景 | BG03_FarmCourtyard/BG04_Park/BG05_RecordingStudio/BG06_Classroom + BackgroundQA.json | 全部 1280×720 RGB、生成标记已修、R08 长椅与 R10 黑板/课桌已局部校正到 R04 区域 | 资源侧全 PASS；Codely 预检发现 3 项（见核对清单） |
| R11 七位配角 | CHR02/03/05/07/09/10/11 站姿 32×48 RGBA + 母图/QA | 真 Alpha、单主体、脚底对齐、≤24 色 | 文件齐（7/7）；引擎内透明边缘/脚底/排序接入时查 |
| R12 五位配角行走表 | CHR02/03/05/07/11 96×192（3列×4行 32×48，Down/Left/Right/Up × WalkA/Idle/WalkB，6fps）+ AnimationManifest/QA + 每人 walk_preview.gif | 60/60 帧格通过；Down/Idle 与 R11 站姿逐像素一致 | **静态抽查通过**：CHR02（资源侧纠偏过左右行）与 CHR05 四行方向与标签一致、WalkA≠WalkB 真实跨步；**接入时必须在引擎里播放核验动作与方向（用户指令）** |
| R13 道具/船/漩涡/凳/桌 | IT01-05、PRP01_Boat 96×64、PRP02_Whirlpool 64×64、PRP04_Stool 32×32（底部中心锚 (16,32)）、PRP05_DiningTable 96×64 + PropsHotspots.json | 9 项全真 Alpha；PRP03/06/07 三面板 PendingC03（等 C03 门环/合照缺口/黑板描画区最终导出） | 与现有占位规格一一对应（船 96×64 ✓）；三面板缺项已登记——C05-C08 接入机关面板时需先由资源方续做 |
| R15-R19 音频 | R15 七音+示范旋律+CFG_Notes（78BPM 16音）；R16 12 SFX+SFX_CueMap；R17 3 BGM 72s 循环+MusicCueMap；R19 可选狗叫 | 44.1k/16bit，循环接缝数值检查过；**未试听** | 文件齐（24 WAV）；接入时人工试听+循环听感 |

### R07-R10 接入核对清单（用户指令：画面与实际碰撞一致；预检发现以干净背景+实机复核为准）

1. **BG03 门洞宽窄**：干净背景实测双扇木门中心 ≈x640（与 R04 FC_Door 锚点一致），门宽 ≈100px（x≈588-692）；R04 碰撞缺口 528-752px 比画出来的门宽——接入时把农家院两侧房墙碰撞收窄到实际门洞（约 x 580-700），防止"走进画着的墙"。
2. **BG03 桌区确认为空地**（干净背景实证，此前对照图上的"桌椅"是 QA 叠图误判）：按 R04 点位叠放 R13 桌凳（C04 装配已就位），核对桌凳画面落点与 Solid_FC_Table (4,2)u 碰撞盒、三凳点位是否与画面地砖/空地吻合。
3. **BG03 新增实体补碰撞**：井台/木架（约 x 17-23% 宽带）、右侧水缸（约 90-95%）、陶罐两处、左下篱笆——接入时逐项补碰撞或标注纯装饰；同批长椅（R08）预检提示长椅绘制中心与锚点 (864,256) 疑似偏上偏右、线框 solid 矩形与背景实体不一一对应——接入时以干净背景逐个矩形核对。
4. **BG06 黑板/课桌**：黑板已校正至 (544,40,448,112)；接入 MG07 时核对黑板画面与描画 UI 映射。
5. 全部四景：32 PPU/Point/关 Mip/无损、保留 .meta、按 assetId 替换占位、不按画面猜热点。

### R12 接入核对清单（用户指令：引擎播放检查动作与方向）

1. 静态预检已通过（CHR02/CHR05 抽查四行方向=Down/Left/Right/Up、WalkA≠WalkB）；仍须在引擎中实机播放逐套核验（含 CHR03/07/11 两套未抽查表），确认八方向移动四向资源复用、斜向切换、脚底 pivot=(0.5,0) 贴地、6fps 节奏、排序层。
2. 切片固定网格 3×4、32×48、无 spacing；导入后抽查CHR02（曾被资源侧纠偏左右行的表）是否仍有镜像问题。
3. CH01 立即受益项：爷爷/奶奶站姿与行走表替换 NPC 占位（保持 actorId 门禁与仪式隐藏逻辑不动）。

### 其他接入备忘

- **R13**：凳子单张实例化 3 把（底部中心锚 (16,32)）；船/漩涡按 C02 碰撞盒（0.8×0.5 / 0.9×0.9）独立对象接入；PRP03/06/07 面板 PendingC03——**需要 Codely 先把 C03 门环 (380,200)/(356,176,48,48)、MG06 缺口 UI、MG07 描画区导出给资源方**（C05-C08 前置）。
- **R15**：CFG_Notes 正式序列 16 音（78BPM）替换占位 14 音（MG05 判定数据），noteBeats 仅供示范播放不作失败判定；N01-N07 WAV 接入后 NoteSequenceMinigame 自动优先加载。
- **R16**：按 SFX_CueMap 接线（脚步停止即停、粉笔松开停、划水靠岸/暂停/切场景停；重试点击复用 UIConfirm、成功复用 PuzzleSuccess）；R17 三 BGM 72s 循环，BGM03 自第 17 小节回收 R15 动机，终章淡出由引擎做不预烘焙。
- **R11**：7 站姿按 actorId 接入；CHR01_Ferryman/CHR04/06/08/12 由资源助手后续交付（缺项如实登记）。
- **音频版权**：R16/R19 为 AI 生成，使用条款标记待核实（资源方备注）。
## 8C. C04 试玩修复记录（2026-10-28）

用户实机验收反馈三条，已定位根因并修复：
1. **拾取眼镜/画完拐杖后原位置物品未消失**——残留的是 DEV 路线眼镜（与 CH01 眼镜同坐标）；拐杖台与视觉未在完成时移除。修复：DEV 对象在正式章运行时隐藏；MG01_Cane 完成后拐杖台与视觉一并消失。
2. **仪式后原老人站位仍有一个人**——CH01 老人本体已由 hideOnRitual 正确隐藏，残留的是 DEV 爷爷（同坐标）。修复：路线可见性门禁。
3. **农家院只有一位老人**——两位幽灵跟随偏移相同（默认 (-0.65,-0.2)），跟随时重叠成一个可见精灵。修复：跟随偏移分离为 (-0.65,-0.2)/(0.65,-0.2)。

新增组件：`Chapters/ChapterObjectVisibility`（路线可见性门禁：启用时+章切换时按 ActiveChapterId 恢复本路线对象默认可见性；不覆盖仪式/事件的中途隐藏）、`Chapters/HideOnChapterEvent`（指定事件后隐藏，MG01 拐杖台）。ChapterDirector 新增静态 ActiveChapterId 与 ChapterChanged 事件；装配工具为 Underworld 全部 DEV/CH01 对象挂接门禁（FarmCourtyard 仅 CH01 无需门禁）。装配校验 PASS。
4. **引导文案与实际交互对齐**（用户指出"点击院门"实为靠近按 E）：`hint.door` 改为"按 E 敲门"；`objective.CH01_Door` 改为"靠近院门按 E 开门，再点击放大画面中的门环。"（数据源 Data/Dialogue 与配置兜底同步，导入器重新导入；门特写面板内的"点击门环"不变，该处确为点击操作）。
## 8D. 资源接收与接入：R12 主角行走表 + R14 告别插画（2026-10-28，本批已导入引擎）

| 批次 | 内容 | 接入状态 |
|---|---|---|
| R12_Main（CHR01_Ferryman） | 主角四向行走表 96×192（3列×4行 32×48，行 Down/Left/Right/Up × 列 WalkA/Idle/WalkB，6fps，pivot(0.5,0)） | **已导入并接线**：Multiple 切片 12 帧（SerializedObject 写 m_SpriteSheet，pivot 0.5,0）→ `Resources/Art/Animations/CHR01_Ferryman`；PlayerSpriteAnimator（WalkA→Idle→WalkB→Idle 循环/静止 Idle 列/暂停冻结）驱动；主角视觉改子物体（feet pivot 补偿 -0.5u，脚底与原占位底边对齐，碰撞体/出生点语义不变） |
| R14 告别插画 | CG01–CG05 五张 640×360 RGB + PresentationLayers.json | **已导入**：`Resources/Art/Illustrations/` + `Resources/Data/Presentations/PresentationLayers.json`；演出规则按交接执行（见下） |
| R11 七站姿（此前接收） | CHR02/03/05/07/09/10/11 32×48 | **已导入**：`Resources/Art/Characters/`；CH01 爷爷/奶奶/双幽灵已换正式站姿（feet pivot 子物体视觉），孙子待 CHR04 交付仍为占位 |

**R14 演出规则落地**（PresentationLayers.json 契约）：
- CG 为烘焙人物的静态定格——**告别对白期全屏显示 CG 定格**（PresentationCG，Canvas 最底层，对白/暂停在上层）；**亡者淡出前先隐藏 CG 切回干净场景**，只淡 spiritActorIds（CH01=两位老人），persistentActorIds（孙子）与背景不动；**绝不对整张 CG 降透明**。
- CG03 合照永久保留四人（CH03 接入时不在照片上做淡出）；CG05 终章允许船远去暖光停留后全屏淡出进轮回（不复活不回阳间）；PR_CH05_Recall 复用 CG01–04。
- CG04 黑板空白——**黑板字层由引擎渲染**（BlackboardTextLayer：粉笔色引擎字体，黑板区 R04 坐标 (544,40,448,112)，"这就是，最后一课了。"与"再见"等由 UI/轨迹层绘制，不烘焙进 PNG）。
- 清理路径：演出强制中止/暂停-离开时 CG 与字层一并隐藏（防泄漏到菜单）。

**验证**：DZ_VALIDATION=PASS（正式资源导入+场景装配）；DZ_SMOKE=PASS（新增断言：CHR01 表 12 帧且帧名符合网格、R11 站姿存在、5 张 CG 640×360、图层契约 CG01 只淡双老人且禁全 CG 淡出/CG03 保留合照/CG05 唯一允许全屏淡出/CG04 程序渲染黑板字）。引擎实测（R14PlayTest：主角四向行走/CG 定格/亡者单独淡出/黑板字层截图验证）结果见 §8E。
## 8E. R12/R14 接入实测记录（2026-10-28，用户指定三项优先实测）

**实测方法**：`渡者/R14 实测(主角行走/亡者淡出/黑板字层)`（R14PlayTest，编辑器 GUI 进真实 Play 模式驱动真实组件，截图与断言双路验证；批处理入口 `-executeMethod Duzhe.EditorTools.R14PlayTest.RunFromCommandLine`）+ `渡者/R14 黑板字层批处理验证`（R14BlackboardBatchTest，确定性批处理验证 UI Text 网格/字形）。

**结果（三项全过）**：
1. **主角行走**：CHR01 正式行走表实测——四方向（正脸/左侧/右侧/兜帽背影）+ Idle 截图逐张 VLM 核验方向与步态正确；PlayerSpriteAnimator 走 WalkA→Idle→WalkB→Idle 循环，feet pivot 子物体补偿后脚底与既有地面线对齐。
2. **亡者单独淡出**：PR_CH01_Farewell 全流程实测——座位演出（双老人入座）→ **CG01 定格全屏显示**（VLM 核验：四人围桌、清晰无变形、无 UI 泄漏）→ 末句后 **CG 隐藏切回干净场景 → 只淡两位老人（1.2s，淡后整根隐藏）→ 孙子与场景保留**（VLM 核验 fade_end：两位老人彻底消失、渡客在场、桌凳门保留、无全屏变黑）。**绝未对整张 CG 降透明**（PresentationLayers 契约：neverFadeEntireCGForSpiritDeparture）。
3. **黑板字层**：批处理确定性验证 PASS——`text mesh: chars=13 verts=48 font=simhei`（网格与粉笔白顶点色正常）、`font glyphs: missing=0`（simhei 全字形可用）；字层矩形按 CG04 实测黑板面 (265,25)-(503,95)@640×360 → 1280×720 参考系 (530,50)-(1006,190) 对位。注：Play 截图中黑板字不可见系编辑器 Game 视图为用户拉伸过的 1536×630 非参考比例所致（CG 被拉伸后黑板偏移），**游戏实际 1280×720 窗口下对齐**——正式构建按 1280×720 运行。

**实测环境坑（记录）**：后台无焦点编辑器实测需 PlayerSettings.runInBackground=true（测试内改回）+ 失焦自动暂停会被测试驱动即时关闭 + 座位等待用结果轮询不用墙钟（失焦拉伸游戏时间）。本轮全流程 PASS 轮：第八/十一/十三轮（R14PlayTest 报告 Logs/r14_playtest_report.txt）。

**回归与构建**：DZ_VALIDATION / DZ_SMOKE（含正式资源断言）/ DZ_R14_BLACKBOARD / DZ_MINIGAMES 全 PASS；构建 Succeeded errors=0 **111,181,416 字节**（Builds\Windows_x64\Duzhe.exe，含 R11 七站姿+R12 主角行走表+R14 五 CG 全部正式美术）。

**待用户实机复核（1280×720 构建窗口）**：1) 主角四向行走与步态；2) CH01 告别演出：CG 定格→末句后两位老人单独淡出、孙子留座；3) CH04 黑板字层（"这就是，最后一课了。"与 MG07 描画结果）——CH04 主线接入在 C07，当前可经 DEV:机关测试 MG07 看"再见"描画。
## 8F. 行走动画修复（2026-10-28 深夜，用户录屏反馈"角色移动不是动画"）

**现象**：实机录屏显示主角朝向随移动方向切换（正面/侧面/背影）但**行走循环不生效**（保持同一静止姿势滑动）。
**根因（双处）**：
1. **绑定时机错误**：`PlayerSpriteAnimator.Bind` 在 `GameBootstrap.BuildPlayer` 中于 `playerGo.AddComponent<PlayerController2D>()` **之前**调用，`GetComponentInParent<PlayerController2D>()` 返回 **null**——动画器永远走 `ctrl==null` 分支。实测探针实锤：`ctrl=NULL`。
2. **移动判定帧序竞争**：`ctrl.IsMoving` 由 `PlayerController2D.FixedUpdate` 赋值（`input.Move`），而动画器在 `Update` 读取——`InputRouter.Update` 每帧覆盖 `Move`，FixedUpdate/Update 的执行顺序造成 `IsMoving` 读到上一帧旧值，移动时仍常为 false。
**修复**：
- `ctrl` 改为 **Update 内惰性解析**（`if (ctrl == null) ctrl = GetComponentInParent<PlayerController2D>()`），PlayerController2D 添加后首帧即生效。
- 移动检测改为**世界坐标位置差分**（`(pos - lastPosition).sqrMagnitude > 1e-8`），与 FixedUpdate/Update 帧序无关。
**验证**：生产路径实测（编辑器 Play，主角每帧右移 1.6u/s 不旁路 DebugDrive）探针显示 `ctrl=bound`、`moving=True`、`cycleIndex` 循环、sprite 依次 `CHR01_Down_WalkA → Idle → WalkB → Idle`——**帧循环真实生效**；四向+Idle 截图核验方向正确。R14PlayTest 全流程 PASS，最终构建 111,181,928 字节。
## 8G. 机关面板几何导出（2026-10-28 深夜，供 Kimi 美术 A 使用）

**文件**：`Docs/ProductionLogs/MinigamePanelGeometry.md`（完整坐标表）+ 标注图 `Logs/panel_geometry/{MG02_Door,MG06_Photo,MG07_Goodbye}_annotated.png`。

**导出方法**：不照抄旧报告——以当前 `Scripts/Minigames/*.cs` 代码为准 + 编辑器 Play 实测截图 + 像素级扫描（门环金色块/鼓手金色块/槽位轮廓）三重验证。坐标统一为**各自 640×360 底图左上角原点、X 向右、Y 向下**，矩形 `[x,y,width,height]`。

**关键结论**：
1. **PRP03_DoorCloseup**：门环中心 **(380,200)**、点击区 **[356,176,48,48]**。旧报告 (380,160) 系 Unity y 向上坐标未做顶左原点换算的错误，像素扫描实测确认为 (380,200)。底图在 1280×720 参考画布 (320,180) 原尺寸显示、无缩放裁切。
2. **PRP06_PhotoFrame**：照片框可用区 **[20,40,600,300]**；固定成员槽 [128,198,48,64]/(152,230)、[256,198,48,64]/(280,230)、[512,198,48,64]/(536,230)；**鼓手缺口 [384,198,48,64]/(408,230)**；鼓手起始 [296,11,48,64]/(320,43)；**吸附半径 80px**（非旧 24px，解决"对准缺口中心却弹回"）。吸附为屏幕空间判定，显示=命中严格一致。
3. **PRP07_BlackboardCloseup**：书写区/模板显示范围 **[0,80,640,320]**；Redraw 按钮 [30,300,180,44]、Close 按钮 [430,300,180,44]、状态行 [320,350,700,30]（下沿溢出底图 20px）、标题 [320,32,680,36]、卡片背景 [0,20,680,430]。墨迹纹理 480×420→640×320 非等比拉伸（x1.333/y0.762）。

**验证**：编辑器 Play 实测截图（`Logs/panel_geometry/`）+ 像素扫描（门环金色块中心=(380,200)、鼓手金色块中心=(319.2,42.5)≈(320,43)、缺口轮廓 x=407.5≈408）+ 标注图 VLM 复核全部对齐。**未改动任何关卡流程。**

**给 Kimi 的说明**：占位绘制（DoorLeaf 等）的 y 符号问题不影响判定与正式底图替换；MG06 照片框占位为 600×300（非全幅），正式 PRP06 640×360 按同原点对齐可用区；吸附半径 80 为最终生效值，勿按旧 24 出图。
## 8H. 资源接收：R11 主角组（CHR04/06/08/12，2026-10-28）

**收货**：`Duzhe_R11_MainCharacters_v1.0.zip` → `SourceAssets/R11_MainCharacters_v1.0/`。内容：
- `Characters/CHR04_PastSelfFamily.png`（成年孙子，灰棕外套）
- `Characters/CHR06_PastSelfRomance.png`（恋爱期男孩，灰绿外套）
- `Characters/CHR08_PastSelfBand.png`（乐队期队友，灰紫夹克）
- `Characters/CHR12_PastSelfSchool.png`（中学学生，蓝白校服）
- `Characters/CHR01_Ferryman.png`（渡客，与 R12_Main 同源站姿）
- 各 1024×1536 AI 母图（`SourceAssets/Characters/*_master.png`）+ `Previews/MainCharacters_Overview.png` + `AssetDelivery.json` + `ResourcesHandoff.md`。

**静态预检**：
- 尺寸/通道：五张均 **32×48 RGBA 真透明**（已程序化逐像素验证）。
- 外观区分度：渡客兜帽剪影与四阶段区分极佳；CHR08（紫）与 CHR12（蓝白校服+矮一头）区分最高；**CHR04（棕）与 CHR06（橄榄绿）为最易混淆对**，接入 CH01/CH02 时需在实机尺寸复核其区分度（必要时按母图微调色相/配件）。
- 资源方明示限制：32×48 缩小后部分人物眼睛辨识度不足、四阶段体态相近——**不得宣称美术最终验收通过**；CH01–CH05 接入时须实机检查人物辨识度（用户指令）。

**接入必查项（用户指令：随各关接入时检查人物辨识度）**：
1. CH01：孙子 CHR04 与两位老人 CHR02/CHR03 同桌时，孙子与老人的区分、CHR04 与渡客的区分。
2. CH02：男孩 CHR06 与 CHR04 的色相区分（棕 vs 橄榄绿，易混对）。
3. CH03：队友 CHR08 与鼓手 CHR07、两队友 CHR09/CHR10 的四人合照辨识度。
4. CH04：学生 CHR12 与老师 CHR11 的区分；CG04 黑板字层不遮脸。
5. CH05：四阶段与渡客对照时的身份可读性（只在第五关揭示）。
6. 全部按 actorId/assetId 接入（pivot(0.5,0)、32 PPU、Point、保留 .meta），不按中文名猜引用；眼睛辨识度不足处按母图微调正式像素图，不宣称通过。

**状态**：已收货归档，未导入引擎；导入接入随 C05-C08 各章执行。R11 七配角站姿此前已导入（CHR02/03/05/07/09/10/11）。
## 8I. C05 交付：第2关「真心为你」全流程接入（2026-10-29）

**正式 CH02「真心为你」11 阶段全流程接入**（ChapterCatalog.BuildChapter02）：Opening（女孩自动对话）→ FindNecklace（拾项链）→ ReturnNecklace（一次 E 交还）→ RitualTalk（自动段）→ Ritual（CH02 专属锚点 CH02_RITUAL）→ Board（CH02_BOAT）→ River（封 2 道，乘客 1 人）→ ReunionTalk（男孩靠近自动对话）→ Heart（相会点 E 打开 MG04_Heart 描画）→ **Farewell（PR_CH02_Farewell 演出驱动，CG02 定格→切回干净场景只淡女孩、男孩与公园保留）** → Complete（1.4s 短淡出→ChapterCompleted→主线 CH03 待 C06 注册）。

**实现要点**：
- **路线可见性门禁**：CH01/CH02 对象同坐标共存但按 ActiveChapterId 互斥（DEV 与 CH01 亦互斥）——女孩/项链/仪式/渡口/幽灵各自路线门禁，互不串扰。
- **仪式控制器**：RitualController_CH02（ritualAnchorId=CH02_RITUAL，单灵魂跟随），RitualTrigger 按 anchorId 精确匹配控制器。
- **公园装配**（R04 Park.layout.json）：出生点 PK_Spawn (-14,-6.75)、长椅碰撞 Solid_PK_Bench (4,1.5)u（与 BG04 长椅画面一致，R07-R10 交接已局部校正）、男孩 NPC PK_PastSelf (8,0.5) 自动相认对白（13 句）、相会点 PK_Reunion (6,-0.75) MG04_Heart 挂接（仅 CH02_Heart 阶段激活）。
- **演出规则**（PresentationLayers.json）：PR_CH02_Farewell 对白期显示 CG02 定格 → 末句后切回干净场景 → **只淡 CHR05_LoverSpirit（女孩）**，persistentActorIds（CHR06_PastSelfRomance 男孩）与公园背景保留；绝不对整张 CG 降透明。
- **资源导入**：TXT_CH02 进 Resources；BG04_Park、IT03_Necklace、CHR06_PastSelfRomance 正式导入；CG02 此前已导入；CHR05 站姿此前已导入。

**修改文件**：`ChapterCatalog.cs`（+BuildChapter02）、`ProjectSetupUtility.cs`（+TXT_CH02 导入、+BG04/IT03/CHR06 导入、+EnsureUnderworldCh02、+EnsureParkCh02）、新增 `Scenes/ParkCast.cs`（ParkCast+CH02FarewellCast）、`DevSmokeTest.cs`（+CH02 结构断言，修正 ReunionTalk 为男孩靠近自动对话而非阶段自动启动）。

**自动验证（全实跑）**：DZ_VALIDATION=PASS（含 3 项新资源导入+双场景 CH02 装配断言）；DZ_SMOKE=PASS（新增 CH02 11 阶段结构/项链/画心/演出映射/选章断言）；DZ_MINIGAMES=PASS；DZ_RIVER_SEEDS=PASS（64 种子含 CH02 规则组：乘客 1 人/封 2 道）。构建 Succeeded errors=0 **113,989,032 字节**。

**未测（需您实机）**：CH02 全流程试玩、故意渡河失败、机关未完成时推进尝试、对话/机关中暂停恢复、事件重复与残留检查。明细见 `Docs/ProductionLogs/StageCoverage_CH02.md`（11 阶段覆盖表+验收对照表）。

**资源缺项（如实）**：CHR06 男孩无四向行走表（R12 配角行走表仅 CHR02/03/05/07/11 五位，用站姿滑动即可）；音频包未接线（C10）；可选回忆蒙太奇 PR_CH02_Memory 未制作（按规范默认不制作，无空槽位）。
## 8J. 交互点全图引导标记（2026-10-29，用户反馈"只有站在特定位置才能看见引导"）

**现象**：CH02_Heart 阶段顶部目标栏有文字，但画面中没有任何交互点标记——旧 E 提示只在 1.2u 交互距离内显示，远离时看不到相会点在哪。
**修复**：新增 `Interaction/InteractableHintMarker.cs`（世界空间金色"!"引导标记，缓慢上下浮动），**无论玩家站在哪里（相机内可见）都显示在可交互点上方**；装配器为所有 Interactable 自动挂接。显示规则与阶段门禁一致：
- 物品/道具：当前阶段激活即显示；
- NPC 自动对话：本段未播且属当前阶段时显示；
- NPC 交还：全部任务物已收集才显示（与 E 提示一致）。
拾取/机关完成/阶段切换后自动隐藏。不改变关卡流程与交互逻辑，仅增加可见性引导。
**验证**：DZ_VALIDATION=PASS（挂接全量交互点），构建 Succeeded。待用户实机复核相会点等引导是否远距离可见。

## 8K. CH01 渡河后 FarmCourtyard 闪退修复（2026-10-29）

**崩溃证据**：`River succeeded after 10.01s` 后，玩家日志报告 `Duzhe_Data/level3 is corrupted!`、`[Position out of bounds!]`，随后原生崩溃。`level3` 即 Build Settings 中的 FarmCourtyard；不是 `FarmCourtyardCast.Start` 的 C# 异常。

**根因**：`CH01FarewellCast` 与 `FarmCourtyardCast` 共用一个 `.cs` 文件，`CH02FarewellCast` 与 `ParkCast` 同样共用。次要 MonoBehaviour 的场景脚本绑定退化为内嵌 `MonoScript`，每次装配再增一份：FarmCourtyard 的 `SceneRoot` 上实际累积了 **5 个** `CH01FarewellCast`，Park 也有同型重复，最终导致构建版场景序列化数据无法读取。

**修复**：两个 Farewell 类拆成同名独立 `.cs`；装配器清理失效/重复组件并强制只保留一个外部脚本资产引用。重存后两场景各仅一个 Farewell 组件、无内嵌 `MonoScript`，后续装配幂等。

**实测与回归**：
- 构建版直接加载 FarmCourtyard（原损坏 level3）与 Park（同类风险 level4）均 PASS，无 corrupted/Crash；临时探针已删除，最终构建不含该入口。
- `DZ_VALIDATION` / `DZ_SMOKE` / `DZ_MINIGAMES` / `DZ_RIVER_SEEDS`（64 种子）/ `DZ_R14_PLAYTEST` 全 PASS；冒烟断言确认 CH01 完成后菜单选择 CH02。
- 最终无探针构建启动进入 MainMenu 正常；`Build result: Succeeded | errors=0 | size=113,990,700 bytes`（`Logs/build_crashfix_final_clean.log`）。

**仍需用户实机复验**：从 CH01 渡口正常登船并渡河成功，进入农家院后走完敲门→摆凳→告别→通关，主菜单开始故事应进入 CH02。此次已验证“构建版场景可加载”和“CH01 完成后选 CH02”两个关键环节，但未代替用户重走整段 12 分钟流程。

## 8L. CH02 项链图标与 CH01 孙子立绘/重聚站位（2026-10-29）

- **CH02 项链图标**：`PlaceholderArt.QuestIconSprite` 原先只映射 IT01/IT02，`IT03_Necklace` 落入默认分支而显示 CH01 眼镜占位图。现已显式映射到 `Art/Props/IT03_Necklace`，并加入冒烟断言防止回退。
- **孙子正式立绘**：农家院 `NPC_CH01_GRANDSON` 原用 `Npc_Placeholder`，现导入并接入 R11 `CHR04_PastSelfFamily`（32×48、feet pivot、原色不染色）。
- **重聚站位**：开门完成时，渡者从门前交互点瞬移到 `(0,2.25)`；孙子在门下 `(0,5.1)` 出现并走到 `(0,4.25)`，两位老人跟随下移到渡者两侧。画面自上而下为门 `6.5` → 孙子 `4.25` → 渡者/老人 `2.25`，不再与门重叠。
- **验证**：`DZ_VALIDATION=PASS`；`DZ_R14_PLAYTEST=PASS`（新增断言：渡者已后退、门>孙子>渡者、老人跟随到位，并输出 `Logs/r14_playtest/farm_reunion_layout.png`）；`DZ_SMOKE=PASS`（含 CHR04/CHR06 资源断言）。构建 Succeeded errors=0 **113,998,492 字节**。

## 8M. R13 Panels v1.0：MG06 合照重影与 MG07 书写区（2026-10-29）

**MG06 合照**：正式 `PRP06_PhotoFrame` 已烘焙三位固定成员，旧程序层又画了三个 `BandmateFixed` 色块，会导致重影。现改为只用正式底图，删除程序固定成员与四槽框线，仅保留唯一缺口热点 `[384,198,48,64]` 和一个可拖鼓手（CHR07 正式站姿，起点 `[296,11,48,64]`）。同时修复完成判定：MG06 虽有四个照片槽，但只有一个可拖物，旧逻辑按 `slotCount=4` 永远不会完成，现按实际可拖数量判定。

**MG07 黑板**：正式 `PRP07_BlackboardCloseup` 为完整 640×360 空白黑板。旧书写区 `[0,80,640,320]` 底边到 y=400，越出底图 40px，并压住按钮带。现书写区收为 `[0,80,640,210]`（底边 y=290），按钮固定为 `[30,300,180,44]` 与 `[430,300,180,44]`，状态行移到板下 y≈368；标题/状态文本居中。轨迹判定仍在 480×420 墨迹纹理空间执行，机关验证矩阵同步按该空间校验。

**实机验证**（真实 Play 会话）：
- `DZ_MG06_DRAG=PASS`：自动把鼓手拖入唯一缺口后，面板正常完成。
- `DZ_MG07_LAYOUT=PASS`：读取实际屏幕矩形，书写区在黑板底图内且在按钮上方（`boardBottom=76.7 draw=148.4..363.6 buttonsTop=138.1`）。
- 截图：`Logs/panel_geometry/MG06_Photo.png`、`MG07_Goodbye.png`；截图可见三位固定成员、一个鼓手、一个缺口，黑板字迹完全在板内。
- `DZ_VALIDATION` / `DZ_SMOKE` / `DZ_MINIGAMES` 全 PASS；构建 Succeeded errors=0 **115,382,612 字节**。
## 8N. C06 交付：第3关「一辈子的乐队」全流程接入（2026-10-29）

### 8N.1 前置保护（先于一切测试/构建）
- `Core/SaveService`：新增编辑器专属 `public static string TestSaveDirectoryOverride`，`SaveDirectoryPath` 按覆盖目录派生，`Save()` 自动建目录。
- `Editor/DevSmokeTest`：原 `Run` 主体改名 `RunIsolatedBody`；新 `Run` 包裹 `Temp/DuzheSmoke/<guid>` 目录并在 `finally` 还原覆盖，坏档路径与 `TestDialogueLibrary` 的 saveProbe 一并落在隔离目录。
- `Editor/DuzheBuildTools`：删除清空旧构建目录的代码，每次构建到 `Builds/Candidates/<yyyyMMdd_HHmmss>_<guid8>`。
- **证据**：真实 `duzhe_save.json` / `.bak` 运行前后 SHA256 一致（`0105DA2A…`，130 字节）；旧 `Builds/Windows_x64/Duzhe.exe` 仍在；新候选目录独立生成。

### 8N.2 正式 CH03（12 阶段，代码 `ChapterCatalog.BuildChapter03`）
`CH03_Opening`（鼓手自动对话 11 句）→ `FindDrumsticks`（IT04 一对鼓槌）→ `ReturnDrumsticks`（一次 E 交还，`requiredItemIds=[IT04_Drumsticks]`）→ `RitualTalk`（自动段 7 句）→ `Ritual`（CH03_RITUAL）→ `Board`（CH03_BOAT）→ `River`（封 2 道 / 乘客 1）→ `ReunionTalk`（队友靠近自动相认 13 句）→ `Perform`（MG05_Notes）→ `Photo`（MG06_Photo）→ `Farewell`（PR_CH03_Farewell）→ `Complete`（1.4s 淡出）。`enablePerformance=true`；`objectiveText` 留空，目标只来自 TXT_CH03（缺失即冒烟失败）。

### 8N.3 数据与美术接入
- `TXT_CH03.json` → `Resources/Data/Dialogue`（DialogueJsonValidator PASS）。
- `CFG_Notes.json` → `Assets/_Project/Data/Minigames` 工作副本 + `Resources/Data/Minigames`（新 `EnsureMinigameImports` 步骤）：78 BPM、16 音 `[1,2,3,5,3,2,1,2,3,5,6,5,3,2,1,1]`、noteBeats 16 项。
- 新增导入：`BG05_RecordingStudio`（R09，1280×720）、`IT04_Drumsticks`（R13）、`CHR08_PastSelfBand`（R11_MainCharacters）。
- **顺带修正 C05 遗留**：`EnsureOneScene` 过去无条件赋 `BG_<scene>_Placeholder`，C05 只导入了 BG04 却从未应用。现改为正式背景表（Park→BG04、RecordingStudio→BG05，其余占位），两场景均已使用正式背景。

### 8N.4 场景装配（幂等，坐标来自 R09 BackgroundQA.json）
- **Underworld**：`NPC_CH03_DRUMMER`（(2,−2)，autoTalk=CH03_Opening，`returnItemIds=[IT04_Drumsticks]`，activeStageIds 仅 Opening/ReturnDrumsticks）、`Item_CH03_DRUMSTICKS`（(−4,1)，仅 FindDrumsticks）、`RitualAnchor_CH03`+`CH03_RITUAL`（(−2,−5)）、`BoatDock_CH03`+`CH03_BOAT`（(2,−6.2)）、`RitualRoot_CH03`（1 灵魂 / 1 站位 / hideOnRitual=鼓手本体）。全部挂 `ChapterObjectVisibility("CH03")`，与 CH01/CH02/DEV 同坐标互斥。
- **RecordingStudio**：出生点 `(−13,−6.75)`（门槛 224,576）；`PastSelfBand (2,−0.75)`（autoTalk=CH03_ReunionTalk，仅该阶段）、`BandmateA (4,−0.75)`、`BandmateB (6,−0.75)`、`Seat_CH03_Drummer (8,−0.75)`（鼓手空位）、鼓手灵魂（跟随玩家）、`Zone_CH03_PERFORM (4,1.25)`→MG05_Notes（仅 Perform）、`Zone_CH03_PHOTO (10,−2.75)`→MG06_Photo（仅 Photo）。
- **幂等证据**：连续两次 `DZ_VALIDATION` → `Underworld CH03 objects=5`、`RecordingStudio objects=6 casts=1/1`，第二轮全部 `already complete`；场景内嵌 `MonoScript` 数量 0（脚本引用为资产型）。

### 8N.5 机关修复
- **MG06 死锁**：完成判定改为 `DragPlacementMinigame.RequiredPlacements(cfg)`（MG06=1，MG03=slotCount=3），`slotCount=4` 仍表示合照人数；`DZ_MINIGAMES` 增加回归断言。
- **MG05 正式谱**：`NotesConfig` 补全 `schemaVersion/minigameId/demoAssetId/tempoBpm/noteAssetIds/sequence/noteBeats`，严格校验（7 个音 id 顺序、12–16 音、noteBeats 等长、音域 1–7、无 0/NaN/Inf）；不合格时打印具体字段并显式回退占位序列（不静默冒充正式谱）。`ReadFormalSequence`/`ValidateNotesConfigText` 同时供运行时与验证器使用。
- **Notes 面板 UI**：`NotesCard` 原用 `+top`（面板飞到屏幕上方外），标题/状态行原以 x=640 当左上角（右侧溢出）；现按卡宽居中，卡片/标题/状态/关闭按钮统一对齐；Drag 面板同类问题一并修正（卡片原本也在屏幕上方外，不可见）。

### 8N.6 演出
- `Scenes/RecordingStudioCast.cs`：鼓手灵魂进门跟随；`MG06_Photo` 成功（`stageId==CH03_Photo` 判定，不依赖已推进的 CurrentStageId）后鼓手走到 (8,−0.75) 并就位；快门 `ShutterFireCount` 只加 1（重复通知不重复响）。
- `Scenes/CH03FarewellCast.cs`（独立文件，资产型脚本引用）：先就位 → `CG03_BandFarewell` 定格 → 19 句手动推进 → **先隐藏 CG** → 只淡鼓手 1.2s → 三位生者与录音室保留 → 停留 0.8s。就位等待设 3 秒上限，避免演出被卡死。

### 8N.7 验证（本次真实执行）
| 项 | 结果 | 证据 |
|---|---|---|
| `DZ_VALIDATION` | PASS | `Logs/duzhe_validation.txt`（含两次幂等装配） |
| `DZ_SMOKE` | PASS | `Logs/duzhe_smoke.txt`（CH03 结构/目标/演出/音符/菜单选择 + CH03 美术；存档隔离） |
| `DZ_MINIGAMES` | PASS | `Logs/minigames_report.txt`（正式谱 16 音 78BPM；MG03 3 凳 / MG06 1 次落位） |
| `DZ_RIVER_SEEDS` | PASS | `Logs/river_seeds_report.txt`（64 种子，含 CH02–04 规则组） |
| `DZ_CH03_PLAY` | PASS | `Logs/ch03_playtest.txt`（真实 Play：MG03/MG06/MG05/演出/快门/面板几何 21 项断言） |
| 候选构建 | Succeeded errors=0 **118,196,320 字节** | `Logs/duzhe_build.txt`；`Builds/Candidates/20261029_183952_a6123452/Duzhe.exe` |
| 构建版启动探针 | PASS | `Player.log`：Bootstrap→Services→Camera→Player→UI→MainMenu，无异常 |
| 构建版场景加载探针 | PASS | 临时 `-dzprobe <sceneId>`：Underworld（roots=3 children=43）、Park（10）、FarmCourtyard（23）、RecordingStudio（12）全部 `DZ_SCENE_LOAD_PROBE=PASS`，无 `corrupted`/崩溃；探针已删除，最终候选 `183952` 不含探针（探针构建 `182519` 已删除） |

### 8N.8 未测 / 缺项（如实）
- 实机全流程（含暂停恢复、返回菜单再进、渡河故意失败、CH03 通关写盘）**待用户试玩**。
- 「缺鼓槌不能交还 / 不能仪式」为代码路径（`CanManuallyInteract` + 阶段门禁）与结构断言，**未在 Play 中单独复验**。
- 7 音 WAV 与 `SFX08_Shutter` 未接线（C10）：当前为程序正弦占位音 + 占位静音（留日志）。
- 鼓手/三位队友无四向行走表（R12 未含 CHR07/08/09/10），使用站姿跟随。
- `CompleteChapter()` 仍回主菜单；CH03→CH04 连续转场由 C07/C09 处理。
- 构建版只验证了「场景能加载 + 启动无异常」；构建版的完整 CH03 流程（含写盘与转场）由用户试玩覆盖。

### 8N.9 用户试玩步骤（约 10–12 分钟）
候选：`Builds\Candidates\20261029_183952_a6123452\Duzhe.exe`

1. **到达 CH03**：主菜单「开始」→ CH01 → ESC →「DEV:完成当前章」→ 回主菜单 → 再「开始」进 CH02 → 同样 DEV 完成 → 第三次「开始」即 CH03。⚠ DEV 完成会写真实存档，需保留原进度请先备份 `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Duzhe\duzhe_save.json`。
2. **阴间**：走近鼓手听完开场 → 到左侧捡鼓槌（右上角出现鼓槌图标）→ 回鼓手按 E 交还 → 听完引导 → 渡口按 E 仪式 → 上船；渡河**故意失败一次**确认鼓槌与已播对白仍在。
3. **录音室**：鼓手灵魂跟随进门；走近队友触发相认对白 → 中央演奏区按 E，按亮起的音完成 16 音（故意按错，进度不应清空）→ 右侧合照点按 E，把**唯一可拖的鼓手**拖进空位松手。
4. **告别**：确认合照只有一位可拖、照片四人；CG03 定格 → 对白结束后切回干净场景、**只有鼓手淡出**，三位队友与录音室保留。
5. **回归**：暂停/恢复一次、返回主菜单再进，确认无残留面板/角色；CH03 完成后主菜单应显示已完成。

## 8O. C07 交付：第4关「最后一课」全流程接入（2026-10-29）

### 8O.1 正式 CH04（12 阶段，`ChapterCatalog.BuildChapter04`）
`CH04_Opening`（老师自动对话 9 句）→ `FindLessonPlan`（IT05 教案）→ `ReturnLessonPlan`（一次 E 交还，`requiredItemIds=[IT05_LessonPlan]`）→ `RitualTalk`（自动段 7 句）→ `Ritual`（CH04_RITUAL）→ `Board`（CH04_BOAT）→ `River`（封 2 道 / 乘客 1）→ `ReunionTalk`（教室自动 8 句）→ `Lesson`（自动 21 句，末句 `CH04_Lesson_021`「这就是，最后一课了。」）→ `WriteGoodbye`（MG07_Goodbye）→ `Farewell`（PR_CH04_Farewell）→ `Complete`（1.4s 淡出）。`objectiveText` 留空，目标只来自 TXT_CH04（缺失即冒烟失败）。

### 8O.2 课堂→写字门禁（真实门禁，不是提示）
- `ChapterRuntimeState.lessonFinished`（运行时不落盘）+ `ChapterDirector.LessonFinished`；
- 只有 `CH04_Lesson` 的**完整** `DialogueFinished` 才置位（打开对白/显示末句/进入教室都不置位）；
- `EnterStage` 在进入 `CH04_WriteGoodbye` 前校验，未上完课直接拒绝并打印具体阶段错误；
- `MinigameLauncher` 对 MG07 追加（章节 / 阶段 / `LessonFinished`）校验，且 `PresentationReplay` 永不打开机关；
- 黑板热点用 `Scenes/CH04GoodbyeGate`：课堂结束自动开一次、关闭后 E 重开、不每帧弹回、铃声只在首次成功打开时响。

### 8O.3 数据与美术接入
- `TXT_CH04.json` → Resources（DialogueJsonValidator PASS：12 目标 / 1 物品 / 5 段 / 3 演员）。
- 新增导入：`BG06_Classroom`（R10，1280×720，黑板已局部校正至 (544,40,448,112)）、`IT05_LessonPlan`（R13）、`CHR12_PastSelfSchool`（R11_MainCharacters）。`CHR11_TeacherSpirit`、`CG04_TeacherFarewell`、`PRP07_BlackboardCloseup` 此前已导入。
- 正式背景表新增 Classroom（此前 Park/RecordingStudio 已用正式图）。

### 8O.4 场景装配（幂等，坐标来自 R04 Classroom.layout.json）
- **Underworld**：`NPC_CH04_TEACHER`（(2,−2)，autoTalk=CH04_Opening，`returnItemIds=[IT05_LessonPlan]`）、`Item_CH04_LESSONPLAN`（(−4,1)）、`RitualAnchor_CH04`+`CH04_RITUAL`（(−2,−5)）、`BoatDock_CH04`+`CH04_BOAT`（(2,−6.2)）、`RitualRoot_CH04`（1 灵魂 / 1 站位 / hideOnRitual=老师本体）；全部挂 `ChapterObjectVisibility("CH04")`。
- **Classroom**：出生点 (−13,−7.5)（CL_Spawn 224,600）；七张课桌 + 顶部墙带的静态碰撞；学生 (−1,−1.75)（CL_Student）、老师 (4,5.25)（CL_Teacher）、黑板热点 (4,4.0)（可达的前排，黑板本体 (4,8.25) 烘焙在背景里）；`ClassroomCast` + `CH04FarewellCast`。
- **幂等证据**：连续两次 `DZ_VALIDATION` → `Underworld CH04 objects=5`、`Classroom objects=3 casts=1/1`，第二轮全部 `already complete`；场景内嵌 `MonoScript` 数量 0。

### 8O.5 演出分流（主线 ≠ 回看）
- `PresentationPlayer` 新增 `activeSegmentIds` + `ResolveSegmentIds`：`PR_CH04_Farewell` 在主线/章回放只播 `[CH04_Farewell]`（课堂已在第 9 阶段上过），只有 `PresentationReplay` 播完整 `[CH04_Lesson, CH04_Farewell]`；JSON 未被改动，回看不丢半段。
- 修复 `Play()` 中 `lib == null` 时 `Load()` 后未重新取实例的空引用；`HandleEvent` 增加 `runId` 匹配（不再只比 sourceId）。
- 新增 `PresentationCast.OnPresentationSegmentFinished(presId, segmentId)`，在每段结束、下一段开始前调用。
- `CH04FarewellCast`：主线/章回放开始即显示 CG04 + 板上已完成「再见」；回看在课堂段结束后才显示字迹并响铃（SFX10_SchoolBell 未接线，占位静音）；末句后**先**隐藏 CG 与字层，**再**只淡老师 1.2s，学生与教室保留。

### 8O.6 共用组件修复（直接影响本章）
1. **CG04 字层**：`BlackboardTextLayer` 锚点 `(530,120)` → `(530,-120)`（顶部锚点下 +y 是屏外），`text.raycastTarget=false`，错误注释一并更正。
2. **Trace 面板几何**：`TraceCard` 原用 `+top`（卡片整块在屏幕上方外）、标题/状态以 640 当左边、状态宽超卡；改为按卡宽居中（MG01/MG04/MG07 通用），并在 Play 中断言标题/画面/按钮/状态都在卡内。
3. **MG07 书写区**：声明 `writeRect=[0,80,640,320]` 与底板 `[0,0,640,360]` 求交得 `[0,80,640,280]`，再让出按钮带（300..344），实际书写区 y 80..294（214 高）。
4. **引导线 ≠ 判定线的真实缺陷**：`TraceMinigame.ToTex` 曾把模板内缩 8px 绘制，而 `TraceEvaluator` 按 0..480/0..420 判定；短笔画（「见」的内横）末端只能到 95%，**玩家与自动测试都无法完成**。现改为与判定完全同一映射。

### 8O.7 验证（本次真实执行）
| 项 | 结果 | 证据 |
|---|---|---|
| `DZ_VALIDATION` | PASS | `Logs/duzhe_validation.txt`（含两次幂等装配） |
| `DZ_SMOKE` | PASS | `Logs/duzhe_smoke.txt`（CH04 结构/12 目标/21 行/第 21 行文本/演出顺序/选章） |
| `DZ_MINIGAMES` | PASS | `Logs/minigames_report.txt`（含 Goodbye 判定矩阵） |
| `DZ_RIVER_SEEDS` | PASS | `Logs/river_seeds_report.txt`（64 种子） |
| `DZ_CH03_PLAY` | PASS（回归） | `Logs/ch03_playtest.txt`（PresentationPlayer 改动未破坏 CH03） |
| `DZ_CH04_PLAY` | PASS | `Logs/ch04_playtest.txt`（MG01/MG04 无回归；无章节时黑板 E 不开；MG07 几何；真实描完 14 笔「再见」恰好 1 次完成；主线 8 句 / 回看 29 句 + 字迹时机；只淡老师、学生保留） |
| 候选构建 | Succeeded errors=0 **121,009,692 字节** | `Builds/Candidates/20261029_212829_a5994efb/Duzhe.exe` |
| 构建版启动探针 | PASS | `Player.log`：Bootstrap→…→MainMenu，无异常 |
| 构建版场景加载探针 | PASS | 临时 `-dzprobe`：Classroom（roots=3 children=16）、Underworld（52）、RecordingStudio（12）全部 `DZ_SCENE_LOAD_PROBE=PASS`；探针与探针构建已删除 |

### 8O.8 未测 / 缺项（如实）
- 实机全流程（叙旧→21 句课堂→写字→告别、暂停/关闭重开、渡河故意失败、通关写盘）**待用户试玩**。
- 「课堂未结束时点黑板」在 Play 中只验证了「无章节时不开」；阶段内的拦截由同一 `Allowed()` 合取条件与 Director 守卫保证，**未在真实章节中复验**。
- 音频（SFX10_SchoolBell / BGM04）未接线（C10）：占位静音并留日志。
- 老师/学生无四向行走表（R12 未含 CHR11/CHR12），教室使用固定站位。
- `CompleteChapter()` 仍回主菜单；CH04→CH05 连续转场由 C08/C09 处理。
- 当前存档已含 CH01–CH03 完成（用户自己试玩的结果），主菜单「开始」会直接进入 CH04。

### 8O.9 用户试玩步骤（约 10 分钟）
候选：`Builds\Candidates\20261029_212829_a5994efb\Duzhe.exe`

1. 主菜单「开始」→ CH04 阴间：走近老师听叙旧 → 左侧捡教案 → 回老师按 E 交还 → 听引导 → 渡口 E 仪式 → 上船；渡河**故意失败一次**，确认教案与已播对白仍在。
2. 教室：学生与老师已就位，相认对白自动开始 → 接着自动进入**完整 21 句课堂**，逐句点完（最后一句是「这就是，最后一课了。」）。**课堂途中按 E / 点黑板都不应打开写字面板。**
3. 课堂结束后黑板自动打开写字面板：沿亮起的笔画写完「再见」（14 笔）；只写一笔不会完成。关闭后可按 E 重开，已完成的笔保留。
4. 告别：CG04 定格、板上显示「再见」→ 8 句 → 结束后切回干净教室，**只有老师淡出，学生与教室保留**。
5. 回归：暂停/恢复一次、返回主菜单再进，确认无残留；CH04 通关后主菜单应显示 CH04 已完成。

## 8P. C07 试玩反馈修复：通关后无法进入其他章节（2026-10-29）

**现象**：用户报告「第一章结束后进入不了其他章节」。

**只读探针证据**（`Logs/save_probe.txt`，探针已删除）：真实存档 `completedChapterIds=[CH01,CH02,CH03,CH04]`，本版注册的正式章只有 CH01–CH04（CH05 由 C08 加入）。因此 `ChapterCatalog.GetFirstUnfinishedChapterId` 走完循环后按规范回退 `CH01`，主菜单「开始故事」每次都以 `ChapterReplay` 重开第 1 章——不是场景或状态机坏了，而是「全部已通关 + 第 5 章尚未接入」时的既有回退行为。日志（`Player.log`）也显示：完成 CH01 后再次点「开始」只把菜单收起，进入的就是 CH01 重玩。

**修复（不改规范行为、不动 C09 范围）**：
1. 主菜单进度行在「本版已全部通关」时明确写出：`已通关：N ／ M（本版全部完成；开始故事将重玩第 1 章）`，不再让玩家以为卡住（`ChapterCatalog.RegisteredFormalChapterCount()` 新增）。
2. 暂停菜单 DEV 区新增「DEV:选择章节」：列出本版已注册的正式章（CH01–CH05 中有配置的），点击即进入（已通关用 ChapterReplay、未完成用 MainRun）；进入前先关闭机关面板/中止演出/隐藏 CG 与字层/强制结束对白/`AbandonCurrentRun`，避免旧章 UI 泄漏。DEV 入口，C11 清理。
3. `CH04PlayTest` 增加对应 Play 断言：当前存档下菜单回退值确为 CH01；暂停菜单出现「DEV:选择章节」；选择面板列出 CH02；点 CH02 后 `CurrentChapterId == CH02`（测试期间存档写入重定向到临时目录，不碰用户存档）。

**验证**：`DZ_VALIDATION=PASS`；`DZ_CH04_PLAY=PASS`（含上述 5 条选章断言，及其余 CH04 面板/描画/演出分流断言全部复跑通过）。新候选 `Builds\Candidates\20261030_134744_dff9fcb8\Duzhe.exe`（140,523,400 字节，errors=0），启动进入 MainMenu 正常。

**用户现在可以**：用最新候选（见 §8R.8）启动 → 主菜单「开始故事」会**直接续到第 5 章 CH05**（C08 已注册）；要复验前面几章，进游戏后按 ESC →「DEV:选择章节」。§8R 记录了 C08 之后的最新构建。

## 8Q. 试玩反馈二：切章后画面无人物（2026-10-29 深夜，已修）

**现象**：用「DEV:选择章节」进入 CH02/CH03/CH04 后，画面只有渡客与 HUD 目标，NPC/道具/背景装饰全都不显示（截图 1–3 为 CH03/CH04 开场）。

**根因**：`Chapters/ChapterObjectVisibility`（路线可见性门禁）把章节事件订阅放在 `OnEnable/OnDisable`，而它自己会把宿主 GameObject 隐藏——隐藏的瞬间 `OnDisable` 退订，之后 `ChapterDirector.ChapterChanged` 再也叫不醒它。同场景切章（Underworld→Underworld，DEV 选章走的就是这条路）时，前一章的物件全部进入「已隐藏且不再监听」状态，新章物件在 `AbandonCurrentRun()` 的空章切换里也被隐藏，于是整张地图只剩未受门禁的渡客。正常主线（主菜单→章节→换场景）每次都是新场景重建门禁，所以之前没暴露；这正是 CodeReview_Findings 里给 C09 记的那条风险。

**修复**：订阅改到 `Awake` / `OnDestroy`（不再随自身隐藏退订），`OnEnable` 只做一次 `Apply()`。隐藏的物件仍能收到章节切换并自行恢复默认可见性。

**顺带**：主菜单进度行改为列出已通关的章号（如「已通关：1 2 3 4 ／ 共 4 章（本版全部完成；开始故事将重玩第 1 章）」），避免「4／4」被误读成"打完第一关就自动全通关"。

**验证（真实 Play）**：`DZ_CH04_PLAY=PASS`，新增断言：Classroom→Underworld 选 CH03 后鼓手可见；**同一场景内**再切 CH04 后老师可见、CH03 鼓手重新隐藏。`DZ_VALIDATION=PASS`。新候选 `Builds\Candidates\20261030_134744_dff9fcb8\Duzhe.exe`（140,523,400 字节，errors=0），启动正常。

**关于「全部通关」**：探针显示存档里 `completedChapterIds=[CH01,CH02,CH03,CH04]`，是之前逐章用「DEV:完成当前章」标记的结果（该按钮会写真实存档）。要恢复干净进度可在暂停菜单点「DEV:清空存档」，但那会清掉已解锁的回忆与章节记录；不点也可以，用「DEV:选择章节」进任意章复验即可。

## 8R. C08 交付：第5关「自渡」全流程接入（2026-10-30）

### 8R.1 六阶段（`ChapterCatalog.BuildChapter05`）
`CH05_Recall`（自动播 PR_CH05_Recall，16 句）→ `CH05_LetGo`（自动 11 句）→ `CH05_Board`（CH05_BOAT）→ `CH05_River`（**零乘客**、封 2 道）→ `CH05_Reincarnation`（同场景 CG05 → 4 句）→ `CH05_Complete`（制作名单，必须确认）。`GameEventType` 末尾新增 `CreditsConfirmed`，旧枚举顺序未动。

### 8R.2 状态与门禁
`ChapterRuntimeState.memoryRestored / letGoFinished`（运行时不落盘）+ `ChapterDirector.MemoryRestored / LetGoFinished`；只在 Recall/LetGo 自身的完成事件后置位；进入 `CH05_Board` 前校验两者，缺一即报具体配置错误（不自动补旗标）；河段重试不重进阶段，旗标与种子不变。

### 8R.3 演出
新增 `Scenes/SelfCrossingCast.cs`（一个 cast 服务两个 PR）：
- 回忆：订阅新加的 `DialogueRunner.LineShown`，只在 004/005/006/007 与 009/010/011/012 切 CG01→CG04，CG04 同时用 `BlackboardTextLayer` 显示「再见」，013 清屏；**图片永远跟着行号，不按计时**；
- 轮回：显示 CG05 → 4 句 → 收起 CG 与字层 → 让**船与渡客同步**缓缓远去 3.5s 并叠加暖色层（可点穿），全程 `Time.deltaTime`，暂停冻结，退出/换场景即销毁。
装配：`EnsureUnderworldCh05`（CH05_BOAT + Recall cast；Underworld 只显示渡客，CH01–04/DEV 物件均按章隐藏）、`EnsureRiverCh05`（Reincarnation cast）。两次装配 objects=1/casts=1、casts=1，第二轮全部 already complete。

### 8R.4 制作名单
`UI/CreditsPanel.cs`：Show 时捕获 runId 与 `CreditsConfirmed` 事件；确认按钮先过 run/stage/Paused/Transitioning/sent 守卫，再 `HideWithoutEvent()` + `PublishEvent`；双击只发一次；只写真实信息（程序与关卡集成 Codely / 美术音频文本 R01–R19 交付批次 / 团结引擎 1.10.4），不编造人员。`GameSession.Credits` 由 `GameBootstrap.BuildUi` 创建；`ChapterDirector.FinishEnterStage` 在 CH05_Complete 显示；`PauseMenu` 离开与选章清理调用 `HideWithoutEvent()`。

### 8R.5 河段收尾
`RiverController.OnSucceeded` 清空漩涡、关闭说明/失败面板；成功后不换场景（Phase=Succeeded 自然锁横移）；乘客数组按 `passengerCount=0` 生成空数组，不造虚拟亡魂。

### 8R.6 验证（本次真实执行）
| 项 | 结果 | 证据 |
|---|---|---|
| `DZ_VALIDATION` | PASS | `Logs/duzhe_validation.txt`（含两次幂等装配） |
| `DZ_SMOKE` | PASS | `Logs/duzhe_smoke.txt`（CH05 结构/6 目标/3 段行数/2 PR/0 乘客/选章） |
| `DZ_MINIGAMES` | PASS | `Logs/minigames_report.txt` |
| `DZ_RIVER_SEEDS` | PASS | `Logs/river_seeds_report.txt`（64 种子） |
| `DZ_CH04_PLAY` | PASS（回归） | `Logs/ch04_playtest.txt`；顺带确认菜单现在续到 CH05 |
| `DZ_CH05_PLAY` | PASS | `Logs/ch05_playtest.txt`（回忆四次切图与行号对应、013 清屏、旗标、上船门禁、零乘客、CG05→船远去、名单双击只发一次、ChapterReplay 不写档） |
| 候选构建 | Succeeded errors=0 **140,523,400 字节** | `Builds/Candidates/20261030_134744_dff9fcb8/Duzhe.exe` |
| 构建版场景探针 | PASS | `-dzprobe`：Underworld（roots=3 children=54）、River（7）、MainMenu（2）均正常；探针与探针构建已删除 |

### 8R.7 未测 / 缺项（如实）
- 实机全流程（含暂停中途退出、失败 3 次）**待用户试玩**。
- 渡河 10 秒航程由 `DZ_RIVER_SEEDS` 覆盖；Play 测试直接投递 `RiverSucceeded` 以验证结尾，未在测试里做 10 秒自动驾驶。
- 四个身份到渡客的"短叠化"用现有插画默认实现（未做额外图层叠化）。
- 音频（BGM05 等）未接线（C10）。
- 完成后仍回主菜单；回忆馆/章节目录与连续路由属 C09。

### 8R.8 用户试玩步骤（约 6–8 分钟）
候选：`Builds\Candidates\20261030_134744_dff9fcb8\Duzhe.exe`

1. 主菜单「开始故事」→ **直接进入 CH05**（存档已记录 CH01–CH04）。回忆对白自动开始，逐句点：CG01/CG02/CG03/CG04 分别在认出爷爷/女孩/乐队/老师的句子出现，CG04 上能看到「再见」，到"这些都过去了"那句画面清空。
2. 自动进入"放下"对白（11 句），点完；**在此之前渡口上船没有反应**，结束后到渡口按 E 上船。
3. 渡河：船上**没有乘客**；避开漩涡，可故意失败一次确认不会重播回忆/放下。
4. 到岸后 CG05 出现，4 句后切回河面，**船与渡客一起远去、画面转暖**。
5. 制作名单出现：点「返回开始界面」，确认 CH05 已记录通关；连点两下不应重复通关。

## 8S. 主线连续进章（2026-10-30，试玩反馈修复）

**现象**：第一章通关后又回到开始界面，五章无法连着玩。

**原因**：`ChapterDirector.CompleteChapter()` 一直无条件 `LoadScene(MainMenu)`——连续转场是 C09 的内容，之前没做。

**修复**：新增 `ChapterCatalog.GetNextFormalChapterId(chapterId)`（按正式章顺序取下一个**本版已注册**的章节，CH05 之后为空）；`CompleteChapter` 在 `MainRun` 与 `ChapterReplay` 下直接 `StartChapter(next, 同模式)`（不写档的 replay 也连续），只有最后一章或 `PresentationReplay` 回主菜单；写盘依旧只在 MainRun。

**验证**：新增 `DZ_CHAPTER_FLOW=PASS`（真实 Play）：CH01→CH02→CH03→CH04→CH05 连续进入且都在 MainRun，CH05 完成回主菜单；章节回放同样连续进入 CH02（replay 模式、不写档）。测试期间存档写入重定向到临时目录。`DZ_SMOKE=PASS` 复跑。新候选 `Builds\Candidates\20261030_134744_dff9fcb8\Duzhe.exe`（140,523,400 字节，errors=0），启动正常。

**仍未做（C09）**：章节目录/回忆馆界面、"回放后回目录"的正式路由、RunId 代次刷新。

## 8T. C09 交付：主菜单、章节目录、回忆馆（2026-10-30）

### 8T.1 运行边界
`GameSession.BeginNewRun(mode)`：先换 `RunId`（旧回调立即失效），再关暂停/机关面板/演出/对白/制作名单/淡出、清 `VisualChapterId`、广播 `RunReset`、设置模式与 Transition 输入。`ReturnToMenu(tab)` 记录 `PendingMenuTab` 后回主菜单。`ChapterDirector.StartChapter` 先校验配置/转场/加载锁，再 `BeginNewRun`，之后才建 runtime、种子、`CurrentChapterId` 与 `VisualChapterId`；**河段重试不调用 BeginNewRun**。

### 8T.2 路由（三种运行）
- **MainRun**：完成一章按 `FormalChapterIds` 继续下一章（缺章显式报错，不静默跳过）；CH05 结束 + 名单确认后回首页。
- **ChapterReplay**：结束回**章节目录**（`PendingMenuTab=Chapters`），不写任何进度。
- **PresentationReplay**：完成或退出都回**回忆列表**；不产生 ChapterRuntimeState、不碰机关/找物/渡河。

### 8T.3 场景 Ready 与显隐
- `SceneTransitionService`：Ready 改到「加载完成 → 玩家落位 → `IsTransitioning=false` → 基础输入态」之后**只发一次**；`OnSceneLoaded` 只更新 `CurrentSceneId`（非受控加载才自行通知）；`OnDestroy` 退订 `sceneLoaded`/`activeSceneChanged`。
- `ChapterDirector.OnSceneReady` 校验 pending 阶段的 `sceneId`，任何无关 Ready 不推进。
- `ChapterObjectVisibility` 改为读 `GameSession.VisualChapterId` + 显式 `RefreshVisibility()` 扫描（含 inactive 对象），新增 `visibleOnChapterEntry`（装配写入；幽灵等"仪式后才出现"的角色为 false）；不再订阅章节事件、不再从 activeSelf 猜默认值。`SetVisualChapter` 在 StartChapter / 场景 Ready / 回忆馆进入时调用。

### 8T.4 演出独立上下文
`PresentationPlayer` 捕获 `contextChapterId/contextStageId/contextMode`；`HandleEvent` 核对 run + 捕获章节 + 当前段；`PresentationFinished` 用**捕获上下文**发布；`Aborted` 同时核对 Mode；`ForceAbort` 清 `waitingSegment`；新增 `PlaybackFailed`（回忆馆据此回列表）。`DialogueRunner` 在 `StartSegment` 捕获 runId，换 run 时**不发** `DialogueFinished`；缺 view 不再假装读完。

### 8T.5 UI
- `MainMenuPanel` 四页：首页（标题/开始/章节/回忆/设置/退出 + 通关进度 + "章节内进度不保存"）、章节（仅已完成章）、回忆（仅已解锁演出）、设置（主/音乐/音效，写盘复用 Save+Audio）。
- `ChapterMenuPanel`：暂停菜单「章节」按钮打开，仅列已完成章；点击二次校验后 `StartChapter(id, ChapterReplay)`。
- `GalleryController`：6 条白名单（PR_CH01/02/03/04_Farewell、PR_CH05_Recall、PR_CH05_Reincarnation）× `Save.HasUnlockedPresentation` 交集；`Play` → `BeginNewRun(PresentationReplay)` → 记录 pending（**在 BeginNewRun 之后写入**，否则被 RunReset 清掉）→ `SetVisualChapter` → `LoadScene`；场景 Ready 且 run/scene 匹配时 `Play` 一次；完成/退出/失败都回回忆列表；双击与重复 Ready 不重放。
- `GameBootstrap` 注册 `CreditsPanel/GalleryController/ChapterMenuPanel`；`PauseMenu` 离开清理三者。

### 8T.6 验证（本次真实执行）
| 项 | 结果 | 证据 |
|---|---|---|
| `DZ_VALIDATION` | PASS | `Logs/duzhe_validation.txt` |
| `DZ_SMOKE` | PASS | `Logs/duzhe_smoke.txt` |
| `DZ_MINIGAMES` / `DZ_RIVER_SEEDS` | PASS | `Logs/minigames_report.txt` / `river_seeds_report.txt` |
| `DZ_MENU_GALLERY` | PASS | `Logs/menu_gallery_playtest.txt`（章节页 5/5、回忆页 3/3、回放进入 PresentationReplay、加载对应场景、CG 显示、中途退出回回忆列表、通关/解锁集合不变） |
| `DZ_CHAPTER_FLOW` | PASS | `Logs/chapter_flow_playtest.txt`（主线连续到 CH05→首页；章回放回目录且不写档） |
| `DZ_CH04_PLAY` / `DZ_CH05_PLAY` | PASS | 回归（含"菜单按存档状态路由"断言） |
| 候选构建 | Succeeded errors=0 **140,523,400 字节** | `Builds/Candidates/20261030_134744_dff9fcb8/Duzhe.exe` |
| 构建版场景探针 | PASS | `-dzprobe`：MainMenu / Underworld(54) / FarmCourtyard(23) 正常；探针已删 |

### 8T.7 未测 / 缺项（如实）
- 六条回忆逐条播放/暂停/退出、1280×720 与 1920×1080 点击清晰度**待用户实机**。
- 全新空存档下的菜单空态（章节/回忆页无行）未在本次 Play 重跑（与已验证路径同一代码）。
- 音频仍未接线（C10）；DEV 入口、ClickProbe、场景 Ready 诊断日志待 C11 清理。

### 8T.8 用户试玩步骤（约 8 分钟）
候选：`Builds\Candidates\20261030_134744_dff9fcb8\Duzhe.exe`

1. 主菜单：标题下有「开始故事 / 章节目录 / 回忆馆 / 设置 / 退出游戏」，底部显示「已通关：1 2 3 4 5 ／ 共 5 章（本版全部完成；开始故事将重玩第 1 章）」与"章节内进度不保存"。
2. 章节目录：只列已完成章；点任意一章 → 该章以回放模式开始，打完回到章节目录（不会继续下一章、不改通关记录）。
3. 回忆馆：只列已解锁演出（当前存档有 3 条）；点一条 → 加载对应场景播放，**不出现机关/找物/避障**；中途 ESC → 返回开始界面可退出回看，回到回忆馆列表；播放完成也回列表。
4. 设置：调主/音乐/音效 → 返回 → 退出游戏再启动，音量保留。
5. 游戏中 ESC：新增「章节」按钮可直接跳到已完成章；「返回开始界面」仍会确认并回到首页。

## 8U. C10 交付：R15–R19 音频接入与音频框架修复（2026-10-30）

### 8U.1 导入
`ProjectSetupUtility.EnsureAudioImports`（新增步骤）把 24 个 WAV 复制到 `Resources/Audio/{Notes,SFX,Music}`，统一 `AudioImporter`（Vorbis / DecompressOnLoad / 预加载）；`CopyIfNeeded` 原地覆盖保留 `.meta`，GUID 不变。逐条登记见 `Docs/ProductionLogs/ResourceIntegration.md`。

### 8U.2 框架修复（AudioService）
- **音量只乘一次**：`PlayOneShot(clip, 1f)`，bus 增益只在 `source.volume`（原来 source.volume 与 PlayOneShot 各乘一次）。
- **七音压低**：`musicDuck`，演奏时音乐 ×0.25，关闭恢复；不缓存旧音量。
- **暂停**：`SetGameplayPaused` 同时暂停/恢复音乐、音效与循环源；暂停中不播新游戏音效。
- **循环**：新增 `PlaySfxLoop/StopSfxLoop`（同一管理器内的循环源），划船与粉笔不再每帧 PlayOneShot。
- **换曲**：`PlayMusic` 先 Stop 再赋 clip，暂停中换曲恢复后能继续；缺失资源会重试加载。
- **场景音乐**：`PlayMusicForScene`（阴间/冥河 BGM01、四个相会场景 BGM02、CH05 BGM03、主菜单停止）。
- `NoteSequenceMinigame` 不再自建 AudioSource，改走共享音效总线；打开/关闭调用 `SetNotesActive`。

### 8U.3 触发点
拾取 SFX03 / 交还 SFX04 / 仪式 SFX05 / 撞漩涡 SFX07 / 敲门 SFX11 / 机关完成 SFX12 / 快门 SFX08 / 下课铃 SFX10 / 描画 SFX09（按住循环）/ 划船 SFX06（开航与重试开始循环，靠岸、失败、离开、OnDestroy 停止）/ 脚步 SFX02（仅实际移动，0.32s 间隔）/ 界面确认 SFX01。

### 8U.4 验证（本次真实执行）
| 项 | 结果 | 证据 |
|---|---|---|
| `DZ_AUDIO` | PASS | `Logs/audio_playtest.txt`：增益单次相乘 0.5、主音量 0 静音、七音压低 0.125、暂停停音乐且不播新音效、恢复继续、单发与循环开停 |
| `DZ_SMOKE` | PASS | 新增断言：8 音符 + 13 SFX + 3 BGM 均按运行时 key 加载，BGM ≥60s |
| `DZ_MENU_GALLERY` / `DZ_CHAPTER_FLOW` | PASS | 回归 |
| 候选构建 | Succeeded errors=0 **140,523,400 字节** | `Builds/Candidates/20261030_134744_dff9fcb8/Duzhe.exe` |
| 构建版启动 | PASS | `Player.log` 无异常、无"音频未导入"日志 |

### 8U.5 未测 / 缺项（如实）
- **全部听感未测**：爆音、循环接缝、音量平衡、7 音演奏听感、快门与铃声实机听感——需用户试听。
- `MEL01_BandPhrase`（示范）与 `SFX13_DogHappy` 已导入但本版无播放场景（未接入）。
- 美术缺项：PRP03 门环面板、PRP04/05 道具面板、R03 UI 皮肤**未交付**；R12 配角行走表**已交付未接入**（NPC 仍单帧站姿，对话场景静止站立）。

### 8U.6 用户试听/试玩步骤
候选：`Builds\Candidates\20261030_134744_dff9fcb8\Duzhe.exe`

1. 主菜单点「开始故事」应有确认音；进入章节后应听到 BGM（阴间/冥河 BGM01，相会场景 BGM02，CH05 BGM03）。
2. 走路有脚步声（停下即停）；拾取/交还/敲门/机关完成各有提示音；撞漩涡有撞击音，开航后有划船循环音、靠岸即停。
3. 七音演奏：按键有声，演奏时 BGM 明显变轻，关闭后恢复；暂停时全部声音停，恢复继续。
4. 设置里把主音量拉到 0 应完全静音（不是变小声），拉到 1 恢复。
5. CH03 合照成功应有快门声；CH04 下课铃在课堂结束节点响一次。

## 8V. C10 补批：R03 UI + PRP03/04/05 接入（2026-10-30 13:10）

**先更正登记**：PRP03 一直在 `SourceAssets/R13_Panels_v1.0/DuzheResources/Props/`，PRP04/05 在 `SourceAssets/R13_Props_v1.0/DuzheResources/Props/`；此前 ResourceIntegration.md 写"未交付/未导入"是错的，已改。

| 资源 | 状态 | 用在哪 |
|---|---|---|
| PRP03_DoorCloseup（640×360） | **已导入 + 已接入** | MG02 门环面板底图；门环热点按导出几何 [356,176,48,48]（板上不可见热点） |
| PRP04_Stool（32×32） | **已导入 + 已接入** | MG03 可拖凳子、农家院世界凳子 |
| PRP05_DiningTable（96×64） | **已导入 + 已接入** | MG03 桌面标记、农家院世界餐桌 |
| UI01_Button / UI01_GenericPanel / UI01_DialoguePanel | **已导入 + 已接入** | `AddPanel/AddButton` 九切片（8/12px 边），对白面板专用底 |
| UI02_Item | 已导入 + 已接入 | 任务物图标兜底 |
| UI02_Continue / Interact / Pause / Volume | 已导入**未接入** | 现有 UI 用文字/程序图形表达 |
| NotoSansCJKsc-Regular.otf | 已导入**未启用** | 运行时仍用已验收的 simhei，避免已验收面板文字度量变化；切换只需改 `GameBootstrap.UiFont` 一行 |
| r03.trace.progress | 已接入 | 描画完成一笔后的状态行 |
| r03.notes.progress / r03.notes.play_demo | 已合并**未接入** | MG05 已用进度点；示范播放按钮本版未做（MEL01 已导入待用） |

**验证**：`DZ_VALIDATION` / `DZ_SMOKE` / `DZ_CH03_PLAY`（MG03 三凳、MG06 单拖、MG05、演出）/ `DZ_CH04_PLAY`（MG01/MG04 面板无回归、MG07 十四笔、演出分流、选章）全 PASS；候选 `Builds/Candidates/20261030_134744_dff9fcb8/Duzhe.exe`（140,523,400 字节，errors=0），启动正常。

**未测（需实机）**：九切片在 1280×720 与 1920×1080 下的拉伸边缘、按钮文字对比、门环在正式底图上的对位、世界桌椅与机关内观感、以及新皮肤下的整体观感是否仍符合 R02/R03 已批准风格。

## 8W. C11 交付：回归与 Windows 提交包（2026-10-30）

### 8W.1 发布清理
- 暂停菜单 DEV 区（完成当前章 / 清空存档 / 选择章节 / 机关测试）全部在 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 内。
- `MinigameHost.OpenTester` 与 `MinigameTesterPanel` 同类编译条件；正式机关入口 `MinigameHost.Open` 不受影响。
- `GameBootstrap` 的 `DZ_ClickProbe`、`TraceMinigame` 的 TRACE 日志、`DragPlacementMinigame` 的 SNAP FAIL、`SceneTransitionService` 的 Scene ready 与 `GalleryController` 的 OnSceneReady 诊断日志均只在开发版输出。
- 主菜单只列正式 CH01–CH05；发行构建启动日志确认无诊断残留。

### 8W.2 构建流程加固
`DuzheBuildTools.Build`：先 `ProjectSetupUtility.RunAll` 并检查返回值（失败不构建）→ 校验 MainMenu + 6 场景存在且注册 → `BuildPipeline` 包在 try/catch → 输出到新候选目录（不删旧版）→ 每次构建写 `Logs/Release/<buildId>/build_report.txt` → 只有 `Succeeded + totalErrors=0 + exe 存在` 才记成功。

### 8W.3 最终验证（本轮真实执行）
`DZ_VALIDATION`（两次幂等）、`DZ_SMOKE`（五章结构 + 美术 + 音频加载）、`DZ_MINIGAMES`、`DZ_RIVER_SEEDS`（64 种子）全部 PASS；同代码基线下的 `DZ_AUDIO` / `DZ_MENU_GALLERY` / `DZ_CHAPTER_FLOW` / `DZ_CH03_PLAY` / `DZ_CH04_PLAY` / `DZ_CH05_PLAY` 均 PASS（见 C09/C10 记录）。

### 8W.4 提交包
| 项 | 值 |
|---|---|
| 候选目录 | `Builds/Candidates/20261030_134744_dff9fcb8/`（Duzhe.exe、Duzhe_Data、MonoBleedingEdge、TuanjiePlayer.dll、README.txt、ReleaseChecklist.md） |
| 压缩包 | `Builds/Duzhe_Demo_20261030_134744.zip`（60.6 MB，根目录 `Duzhe_Demo/`） |
| 构建报告 | `Logs/Release/20261030_134744_dff9fcb8/build_report.txt` |
| 清单 | `Docs/ProductionLogs/ReleaseChecklist.md` |
| 构建版启动 | 原目录与**复制到另一目录**（`Builds/_pkg/Duzhe_Demo`）均启动进主菜单，无异常 |

### 8W.5 未测（需用户实机）
1. 构建版逐关人工通关（自动验证只覆盖结构与规则）。
2. 暂停/失焦/返回菜单逐状态无残留。
3. 听感：音量平衡、循环接缝、七音采样、快门与铃声。
4. 1920×1080 下九切片边缘与按钮文字对比。
5. 复制到别处后的完整流程（仅验证了启动）。
音频素材版权未核实，不以"已授权"表述。

### 8W.6 用户试玩步骤（约 15–20 分钟）
1. 解压 `Duzhe_Demo_20261030_134744.zip`，双击 `Duzhe_Demo/Duzhe.exe`（窗口化 1280×720，可拉伸）。
2. 主菜单：开始故事 / 章节目录 / 回忆馆 / 设置 / 退出；底部显示已通关章号。
3. 依次走 CH01→CH05（每章 6–12 分钟）；每章可故意在河段失败一次，确认任务物与对白不丢。
4. 重点复核：七音错音不清进度、合照只有鼓手可拖且一次完成、CH04 先上课再写字再告别、CH05 零乘客 + 制作名单确认后才通关。
5. 通关后进「章节目录」重玩任意章、进「回忆馆」重看演出；退出重开确认通关与音量保留。

## 8X. C10 补批二：R12 行走表 + R06 河面背景（2026-10-30，**代码完成，未编译/未导入**）

### 8X.1 本轮做了什么（代码层）
- **R12 五张配角行走表**（CHR02_Grandfather / CHR03_Grandmother / CHR05_LoverSpirit / CHR07_DrummerSpirit / CHR11_TeacherSpirit）：
  - `ProjectSetupUtility.EnsureFormalArtImports` 增加导入，`ConfigureSheetImporter` 支持按 assetId 生成帧名（不再硬编码 CHR01 前缀）；切片规则沿用已验收的 3 列×4 行 / 32×48 / 行 Down,Left,Right,Up / 列 WalkA,Idle,WalkB / pivot(0.5,0) / 6fps。
  - 新增 `Player/NpcSpriteAnimator.cs`：按世界坐标位移判定"是否在走"和朝向，走路循环 WalkA→Idle→WalkB→Idle，静止保持 Idle；暂停冻结；**不写颜色**，不影响 `SpiritEscort` 的淡入淡出；`Awake` 重新解析子物体 `SpriteRenderer`，运行时不需要重新装配。
  - `FormalArt.LoadWalkFrames(assetId)` 按 rect 反推行列，不依赖 `LoadAll` 顺序。
  - `ProjectSetupUtility.EnsureNpcWalk(...)` 已接到：阴间 CH01 双老人、CH02 女孩、CH03 鼓手、CH04 老师幽灵；农家院双老人；公园女孩；录音室鼓手。缺图集时保留站姿并移除失效 animator。
- **R06 `BG02_River`**：加入导入与 `FormalBackgrounds`，River 场景将使用正式河面背景（替换占位图；背景本身不含船/漩涡）。
- 冒烟新增 `TestNpcWalkSheets`（5 张图集 12 帧 + BG02_River 尺寸）。

### 8X.2 未完成的原因（如实）
写完代码后，本机 `run_shell_command` 持续返回 `EPERM: operation not permitted (uv_spawn powershell.exe)`，**无法启动团结编辑器**，所以以下都**没有执行**：
- `ProjectSetupUtility.RunFromCommandLine`（导入 R12/R06 并重存场景）；
- `DevSmokeTest.RunFromCommandLine`（新断言）；
- 构建、构建版实机检查。

因此 R12/R06 目前只能记为 **"代码完成"**，不能记为"已导入/已使用/已试玩"。上一版可运行候选 `Builds/Candidates/20261030_134744_dff9fcb8` 与本轮改动无关，仍是当前可交付版本。

### 8X.3 环境恢复后的执行顺序
**一键脚本**（工程根 `_Tools\C10C_ImportVerifyBuild.bat`，双击即可；脚本用 `%USERPROFILE%\Duzhe` 拼路径，避免中文路径问题）：
1. `ProjectSetupUtility.RunFromCommandLine` → `Logs\c10c_import.log`（期望 `DZ_VALIDATION=PASS`，`Resources/Art/Animations/` 出现 5 张图集、`River.unity` 背景换成 `BG02_River`）。
2. `DevSmokeTest.RunFromCommandLine` → `Logs\c10c_smoke.log`（期望 `OK   R12: 5 walk sheets sliced to 12 frames; R06 river background present`）。
3. `DuzheBuildTools.BuildWindowsFromCommandLine` → `Logs\c10c_build.log`（新候选目录，不覆盖旧版本）。
脚本会打印三段关键行；把输出或三个 `Logs\*.txt` 发回即可。

### 8X.4 其余资源的核对结论（本轮）
| 资源 | 结论 |
|---|---|
| R03 图标 UI02_Continue/Interact/Pause/Volume | 已导入，**未接入**：现有 UI 没有图标槽位，接入属于新增版式设计，未擅自改动 |
| R06 水面流动层 | **未交付**（R06 只有静态背景，交接明确无水流层）；不实现滚动水纹以免接缝 |
| R19 狗 SFX13_DogHappy | 已导入，**未接入**：五章没有狗的场景/事件 |
| CH02 恋爱蒙太奇 PR_CH02_Memory | **未交付**（PresentationMap 无条目、无插画），**未实现**，不造空槽位/假按钮 |
| R03 字体 | 按用户要求**保持 simhei**，Noto 仅导入不启用 |
| 七音示范按钮 | 按用户要求**不添加**，七音演奏玩法保留 |

### 8X.5 UI「背景和文字分开」修复（2026-10-30，用户反馈）

**根因（最可能）**：R03 九切片皮肤的 Sprite **PPU 被设成了世界的 32**，而 UGUI 画布参考 PPU 是 100。`Image` 按 `referencePPU / spritePPU ≈ 3.1` 放大九切片边框，卡片边框被画得约 3 倍大、内填充区被挤掉，于是看起来"背景（卡片）和文字分开了"。

**修复**：
- `ConfigureUiSkinImporter` / `ConfigureUiIconImporter` 的 `spritePixelsPerUnit` 改为 **100**（UI 资源不用世界 32 PPU）。
- `DuzheUiFactory.AddPanel / AddButton` 对皮肤显式设置 `pixelsPerUnitMultiplier = 1`，保证边框 1:1 缩放。
- 顺带修两处真实坐标问题：MG02 门环面板的标题/状态行原来用 `x=640` 当左边（整体偏到画面右半），改为按宽度居中；暂停菜单 DEV 机关测试面板标题同样修正。

**最终决定（用户 2026-10-30 选择）**：**关闭 R03 九切片皮肤**，面板/按钮回到原来的深色纯色块。
- `DuzheUiFactory.UseR03Skins = false`（常量开关，改回 true 即可重新启用）；`AddPanel/AddButton` 在关闭时不加载皮肤，走原纯色路径。
- `DialogueView` 的皮肤同样受该开关控制；对白面板位置**恢复原样**（`y = 0`，即此前用户一直在看的位置），本轮不再改动其布局。
- 资源本身仍保留在 `Resources/Art/UI/`（已导入未启用），PRP03/04/05、音频、R12 行走表与 R06 河面不受影响。

**状态**：以上均为**代码修改，尚未编译/重建**（本机 `run_shell_command` 仍返回 `EPERM`，编辑器无法启动）。执行 `_Tools\C10C_ImportVerifyBuild.bat` 会导入 R12/R06 并构建新候选；UI 将回到纯色面板。

> **8X.5 的皮肤结论已被 §8Y 推翻**：用户随后要求**恢复**九切片皮肤。真正的错位根因是卡片面板 `anchoredPosition.y` 写成正数（左上锚点的面板被画到画布上方之外），与 PPU 无关。详见 §8Y.1。

## 8Y. C12 试玩反馈修复（2026-10-30，**代码完成，未编译/未构建**）

> 本轮全部改动**只写了代码**：本机 `run_shell_command` 持续返回 `EPERM`（无法启动 powershell/编辑器），因此**没有编译、没有导入、没有构建、没有实机验证**。下表"结果"列只写代码层面的结论，凡是需要引擎才能确认的，一律标为**待编译/待试玩**。

### 8Y.1 UI：皮肤恢复 + 对白框下移 + ESC 路由

| # | 问题 | 修改位置 | 验证方式 | 结果 |
|---|---|---|---|---|
| 1 | 卡片"背景与文字分开" | `DuzheUiFactory.UseR03Skins = true`（恢复皮肤）；**真因是 `AddPanel` 的卡片 y 写成正数**：`PauseMenu` Card/Card2/Card3、`MinigameHost` 测试卡、`RiverController` 说明/重试卡、`DoorKnockMinigame` 提示与占位门全部改为负 y。`UI01_*.png.meta` 的 `spritePixelsToUnits` 32→100 | 代码推导（左上锚点 pivot(0,1) 正 y = 向上）+ 用户截图反算（卡片底边落在「音乐」行 = 画布 y≈356，与 +120 完全吻合） | 代码已改；**待编译后实机确认** |
| 8 | 对白框移到下方、去掉进度数字 | 新增 `DuzheUiFactory.AddBottomPanel`（锚定底边、横向拉伸）；`DialogueView` 改用该面板（200px 高），对白正文改为左右 70px 内缩自适应宽度，提示行按画布中心锚定；`ShowLine` 不再拼 `(21/26)` | 代码审阅：底部锚点 + 全宽拉伸在任何比例下都贴底、不跑出右边缘 | 代码已改；**待编译后确认**（编辑器/桌面版不同比例） |
| 9 | 回忆馆 ESC / 播放中退出 | `MainMenuPanel.Update`：ESC 在 回忆馆/章节目录/设置 页返回首页；`PauseMenu.Update`：`Mode == PresentationReplay` 时 ESC 直接 `ReturnToMenu(Gallery)`；`GalleryController` 新增右上角「返回回忆馆（ESC）」按钮（播放开始时显示，完成/失败/重置时隐藏）；`ChapterMenuPanel.Update`：ESC 关闭章节列表回到暂停界面；`PauseMenu` 在章节列表显示时不再抢 ESC | 代码审阅：ESC 单帧内只有一个消费者；`BeginNewRun` 会广播 `RunReset` 关闭播放按钮；不写任何存档字段 | 代码已改；**待试玩** |
| 10 | 窗口化/全屏/分辨率 | 新增 `Core/DisplaySettings.cs`（PlayerPrefs 存窗口模式与 1280×720/1600×900/1920×1080 三档，`GameBootstrap.Start` 改为 `Load()+Apply()`）；`MainMenuPanel` 设置页新增「窗口（切换全屏）」「分辨率（◀ ▶）」两行；`EnsureProjectSettings` 的 `resizableWindow` 改为 true | 代码审阅：显示设置存 PlayerPrefs，**不进存档文件**；CanvasScaler 仍为 match=0.5 | 代码已改；**待编译后确认窗口缩放与文字** |

### 8Y.2 场景与角色

| # | 问题 | 修改位置 | 验证方式 | 结果 |
|---|---|---|---|---|
| 1 | 主菜单显示上一场景 | `GameBootstrap.NotifySceneReady`：进菜单时 `CameraFollow.followTarget = null` + `yOffset = 0` + 相机复位到 (0,0)（此前 `followTarget` 一直绑着已隐藏的玩家，相机停在上一场景的位置）；主菜单背景改为**预留正式槽**：`FormalBackgrounds` 新增 `(MainMenu, "Art/Backgrounds/BG00_MainMenu")`，文件不存在时回退占位图 | 代码推导（相机位置继承 + 无 followTarget 复位） | 代码已改；**待资源（BG00_MainMenu.png）+ 待试玩** |
| 1b | 开始界面仍可能露出"其他背景"（用户追问后的加固） | 新增 `GameBootstrap.EnsureMenuBackdrop`：**每次进菜单都重新绑定背景**——优先 `BG00_MainMenu`、否则 `BG_MainMenu_Placeholder`；找不到 Background 渲染器就现场建一个；并强制 Background 层、排序 0、白色、位置归零、启用。新增 `HideRunOverlays`：进菜单时逐个关闭 暂停/对白视图/机关面板/制作名单/章节列表/淡出层/CG/黑板字层；`GameSession.BeginNewRun` 追加 `ChapterMenu.Hide()` | 逐条核对"哪些覆盖层挂在持久 Canvas 上"：River 两个面板已在 `RiverController.OnDestroy` 销毁；其余全部在进菜单时强制关闭 | 代码已改；**待试玩**（重点：任意章节中途返回菜单、通关回菜单、回忆馆退出回菜单） |
| 2 | 阴间背景/船/石灯是占位 | ① 导入并绑定 `BG01_Underworld`（`FormalBackgrounds` 新增 Underworld 条目）；② 导入 `PRP01_Boat`、`PRP02_Whirlpool`、`IT01_Glasses`、`IT02_Cane`；③ 7 处 `boatSprite` 改走 `LoadFormalOrPlaceholder("PRP01_Boat", …)`；④ 眼镜/拐杖道具改用正式图；⑤ 运行时 `FormalArt.LoadBoatSprite/LoadWhirlpoolSprite` | 冒烟新增断言：BG01 存在且 1280×720、PRP01/PRP02/IT01/IT02 存在、`LoadBoatSprite()` 返回 PRP01 | **石灯仍无正式资源**（R13 只有 PRP01/02/04/05，石灯是程序绘制的 `RitualAnchor_Placeholder`），已登记待资源；其余待编译 |
| 3 | 第二关女孩太靠左 | `ParkCast.ActivateGhost`：改为**以玩家实际位置 + (1.2, 0.25)** 生成（原固定 `(-14.6,-6.4)` 比公园出生点 (0,-6) 左 14.6 单位 = 屏幕外）；并调用 `ChapterObjectVisibility.ShowRuntime()` 防止入场扫描再次隐藏 | 代码推导（相机半宽 10 单位）+ 场景 YAML 核对（SP_Main=(0,-6)） | 代码已改；**待试玩** |
| 4 | 第三关鼓手不见 | `RecordingStudioCast.ActivateGhost`：同样改为玩家旁生成 + `ShowRuntime()`；`ChapterObjectVisibility` 新增 `shownAtRuntime`（运行时被 cast 显示过的对象，后续章节扫描不再按 `visibleOnChapterEntry=false` 隐藏） | 代码审阅：场景内 `Ghost_CH03_Studio` 已正确绑定 SpiritEscort/ChapterObjectVisibility/NpcSpriteAnimator + CHR07 帧，且 `RecordingStudioCast.drummerGhost` 已序列化 → 剩下的可见性风险就是"入场扫描把 cast 刚激活的幽灵又隐藏" | 代码已改；**待试玩确认根因** |
| 5 | 第四关出场位置 | `SpawnPlan` 教室 (-13,-7.5) → **(-3,-1)**（相机上移 4.6 单位，学生/老师/黑板同时入画）；老师 (4,5.25) → **(3,2.6)**（原位置在相机上边缘之外） | 相机半高 5.625、半宽 10 反算：出生点 (-3,-1) 时视野 x[-13,7] y[-6.6,4.6]，学生 (-1,-1.75) 与老师 (3,2.6)+1.5 身高均在画内，且都在对白框（底部 3.125 单位）之上 | 代码已改；**待试玩** |
| 6 | 第二关长椅左侧被遮 | 结论：**场景里没有任何遮挡层**（Park 场景只有 Background/Walls/隐形碰撞盒/角色），唯一盖住长椅的是**原来贴在屏幕顶部的对白框**——站在男孩处时相机视野 y 上限 6.1，长椅（世界 y 2.3~4.06）落在屏幕 18%~34%，正好被顶部 0~28% 的对白框切掉上半部；对白框移到屏幕底部后不再覆盖。另外把长椅碰撞盒对齐到画面实际范围 (7,3.18) 4.4×1.76（原 (7,3.5) 4×1.5 上缘超出画面 0.19） | 场景 YAML 逐对象核对 + 相机/世界坐标换算 | 代码已改；**待试玩确认观感** |
| 7 | 渡河船与乘客 | 船改用 PRP01（层级 -2），漩涡 -3；`RiverController.CreatePassengers` 按章节取乘客立绘：CH01 爷爷奶奶、CH02 女孩、CH03 鼓手、CH04 老师、CH05 零乘客；乘客层级 2（必在船之上），并按 sprite 的 pivot/PPU 把脚落到甲板线（`PassengerDeckLocalY=-0.2`） | 代码审阅 + 章节配置 `passengerCount` 核对 | 代码已改；**待试玩** |

### 8Y.3 流程与机关

| # | 问题 | 修改位置 | 验证方式 | 结果 |
|---|---|---|---|---|
| 11 | 章节解锁/菜单文字 | 连续进章已由 `ChapterDirector.CompleteChapter` 保证（CH01→…→CH05，回放回目录、不写档）；本轮只去掉文字：`PauseMenu` DEV 选章行的「（已通关·重玩）/（未完成·正式）」后缀删除，主菜单进度行「已通关：」改为「进度：」 | 代码审阅 | 代码已改；**待试玩** |
| 12 | 「再见」描画重做 | `TraceTemplates.BuildGoodbye` 重写为**标准笔顺 10 笔**（再 6 笔：横/竖/横折/中竖/中横/底横；见 4 笔：竖/横折/撇/竖弯钩），每字宽度压到归一化 0.34（绘制区 640×214 是 3:1，原模板字形被拉宽）；新增 `Line()` 两笔直笔助手；角笔保留原始折线（引导线画得出真实折角） | `MinigameValidator` 的"完美描画必须通过 + 直线/涂鸦不得通过"矩阵（未执行）；`CH04PlayTest` 按模板笔数逐笔描画（未执行） | 代码已改；**待编译后跑 DZ_MINIGAMES / DZ_CH04_PLAY** |
| 13 | 合照素材站位 | `MinigameConfig` 新增 8 个 MG06 布局字段（缺口 x/y/w/h、鼓手起点 x/y/w/h，顶左原点），`DragPlacementMinigame.BuildPhoto` 改为读配置；默认值仍是已定稿的缺口 [384,198,48,64] / 起点 [296,11,48,64] | 代码审阅：默认值等价于原硬编码，行为不变 | **新素材未交付**；接口已留，替换时只改配置+PRP06 图，**未改最终合照插画** |

### 8Y.4 执行状态与后续

- **未执行**：`ProjectSetupUtility.RunFromCommandLine`（导入 BG01/PRP01/PRP02/IT01/IT02 并重存 6 个场景）、`DevSmokeTest`、`MinigameValidator`、各 Play 测试、构建。原因：`run_shell_command` 仍 `EPERM`。
- **执行顺序（环境恢复后）**：`_Tools\C10C_ImportVerifyBuild.bat`（**四步**：导入→冒烟→构建新候选→把桌面快捷方式指向最新候选，不覆盖旧版）→ 需要时再跑 `DZ_MINIGAMES` / `DZ_CH04_PLAY` / `DZ_CHAPTER_FLOW`。
- **桌面快捷方式**：`桌面\渡者-Duzhe.lnk` 目前指向 **`Builds\Windows_x64\Duzhe.exe`（旧版，115MB）**，不是任何候选版；脚本第 4 步会自动改指最新候选。**在构建成功之前，桌面上没有包含 §8Y 改动的可执行文件。**
- **待资源**：`BG00_MainMenu.png`（主菜单背景，路径已预留）、MG06 合照新素材、石灯正式图。
- **待试玩**：主菜单是否彻底干净、阴间背景与岸线/碰撞是否一致、女孩与鼓手出现位置、教室构图、长椅、渡河乘客层级、对白框底部位置、回忆馆/章节列表 ESC、窗口缩放后的 UI。




## 8Z. Codex 接手（2026-10-30）
已实际完成工程编译/资源导入及菜单、CH03、CH04、CH05、五章流转运行回归。补修 ESC 双重触发、制作名单遮罩、连续笔迹判定，并接入主菜单、长椅和合照新图。详见 Docs/ProductionLogs/CodexTakeover.md；本轮构建路径见 Logs/duzhe_build.txt。未宣称人工逐关画面与听感验收。

