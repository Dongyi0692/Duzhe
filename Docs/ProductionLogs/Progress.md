# Progress.md — 《渡者》Codely 生产日志

> 本文件是 Codely 侧的连续进度记录。每项任务后更新；中断续做从本文件对齐状态。
> 规范基线：2026-10-23 最终规范 v1.0 + 2026-10-26 执行补充。目标：10/31 提交完整版。

## 环境基线

见同目录 `ProjectEnvironment.md`（团结 1.10.4 / 2022.3.62t16 · cn.tuanjie.template.2d 7.0.4 · 内置管线 · 旧版 Input Manager · 窗口 1280×720）。

## 任务状态总览

| 任务 | 状态 | 说明 |
|---|---|---|
| C01 工程盘点与可运行基础层 | **已完成（2026-10-26）** | 四检查点全部执行；自动验证全部 PASS；用户鼠标/键盘全流程验收通过（含黑屏五根因与 EventSystem 跨场景销毁修复） |
| C02 仪式、登船、10秒渡河与局部重试 | **已完成（2026-10-27 凌晨交付+同日审查补正）** | 自动验证全 PASS；运行时试玩待用户验收（步骤见 CodelyHandoff.md 第 8 节） |
| C03 七种机关的共用组件与配置 | **已完成（2026-10-27）** | 4 类面板+7 配置+判定矩阵全 PASS（三模板完美轨迹过/直线乱涂半程错序全防、Notes 状态机）；Windows 构建 Succeeded；机关面板交互待用户用 DEV 入口试玩（步骤见 CodelyHandoff.md 第 8 节） |
| C04 第1关全流程接入（正式 CH01 12 阶段） | **已完成（2026-10-28，代码与自动验证；实机全流程待用户试玩）** | R01 四份文本/配置接入运行时；R04 布局坐标装配 Underworld/FarmCourtyard；MG01/02/03 挂接正式流程；PR_CH01_Farewell 演出系统；DZ_VALIDATION/RIVER_SEEDS/MINIGAMES/SMOKE 全 PASS+构建 Succeeded；阶段覆盖表 Docs/ProductionLogs/StageCoverage.md |
| C05 第2关全流程接入（正式 CH02 11 阶段） | **已完成（2026-10-29，代码与自动验证；实机全流程待用户试玩）** | TXT_CH02/BG04_Park/IT03_Necklace/CHR06 接入；公园装配+PR_CH02_Farewell（CG02→只淡女孩）；DZ_VALIDATION/SMOKE/MINIGAMES/RIVER_SEEDS 全 PASS；阶段覆盖表 StageCoverage_CH02.md |
| C06 第3关全流程接入（正式 CH03 12 阶段） | **已完成（2026-10-29，代码与自动验证；实机全流程待用户试玩）** | TXT_CH03/CFG_Notes/BG05_RecordingStudio/IT04_Drumsticks/CHR08 接入；Underworld+RecordingStudio 装配；PR_CH03_Farewell（CG03→只淡鼓手）；MG05 正式 16 音校验；MG06 单鼓手落位；DZ_VALIDATION/SMOKE/MINIGAMES/RIVER_SEEDS/CH03_PLAY 全 PASS；阶段覆盖表 StageCoverage_CH03.md |
| C07 第4关全流程接入（正式 CH04 12 阶段） | **已完成（2026-10-29，代码与自动验证；实机全流程待用户试玩）** | TXT_CH04/BG06_Classroom/IT05_LessonPlan/CHR12 接入；Underworld+Classroom 装配；课堂→写字门禁（lessonFinished + Director 守卫 + Gate/Launcher）；PR_CH04_Farewell 主线只播告别、回看播完整；MG07 面板几何与 CG04 字层坐标修正；DZ_VALIDATION/SMOKE/MINIGAMES/RIVER_SEEDS/CH03_PLAY/CH04_PLAY 全 PASS；阶段覆盖表 StageCoverage_CH04.md |
| C08 第5关全流程接入（正式 CH05 6 阶段） | **已完成（2026-10-30，代码与自动验证；实机全流程待用户试玩）** | TXT_CH05 接入；回忆按行切 CG01–04；零乘客河段不换场景；CG05→船远去+暖色；制作名单需确认（CreditsConfirmed）；DZ_VALIDATION/SMOKE/MINIGAMES/RIVER_SEEDS/CH04_PLAY/CH05_PLAY 全 PASS；阶段覆盖表 StageCoverage_CH05.md |
| C09 主菜单/章节目录/回忆馆 | **已完成（2026-10-30，代码与自动验证；实机待试玩）** | 运行重置与 RunId 代次、主线连续/回放回目录、回忆馆 6 条白名单与 PresentationReplay 路由、菜单四页与暂停章节入口；DZ_MENU_GALLERY / DZ_CHAPTER_FLOW 等全 PASS；候选 20261030_041332_3e51cab3 |
| C10 音频与资源接入 | **已完成音频部分（2026-10-30，代码与自动验证；听感待试听）** | R15/R16/R17/R19 共 24 WAV 导入；AudioService 增益/压低/暂停/循环修复；SFX 与 BGM 触发点接线；DZ_AUDIO/DZ_SMOKE 等 PASS；ResourceIntegration.md 登记 |
| C11–C12 | 进行中 | C11 发布清理与提交包已完成（2026-10-30）；C12 试玩反馈修复**代码完成、未编译**（本机 shell EPERM），逐项记录见 CodelyHandoff.md §8Y |

## 资源接收：R11 主角组（2026-10-28，CHR04/06/08/12）

`Duzhe_R11_MainCharacters_v1.0.zip` 解压至 `SourceAssets/R11_MainCharacters_v1.0/`：CHR04 成年孙子（灰棕）、CHR06 恋爱男孩（灰绿）、CHR08 乐队队友（灰紫）、CHR12 中学生（蓝白校服）+ CHR01 渡客站姿 + 各 1024×1536 母图。五张均 32×48 RGBA 真透明（程序化逐像素验证）。预检：CHR04/CHR06 为最易混淆对（棕 vs 橄榄绿），接入 CH01/CH02 时实机复核区分度；资源方明示 32×48 后眼睛辨识度不足，不宣称美术验收通过。用户指令已登记：随各关接入时检查人物辨识度。明细见 CodelyHandoff.md §8H。
## 资源接收登记（2026-10-28，R05-R13/R15-R19 共 7 批）

用户送达 7 批正式资源包，全部解压至 `SourceAssets\`（R05_Underworld_v1.0 / R06_River_v1.0 / R07_R10_Backgrounds_v1.0 / R11_Characters_KimiArtB / R12_SupportingWalking / R13_Props_v1.0 / R15_R19_Audio_20261027），交接文档与 QA 数据通读完毕，**均未导入引擎**（随 C05-C10 接入）。静态预检：R12 行走表抽查（CHR02/CHR05）四行方向=Down/Left/Right/Up 且 WalkA≠WalkB 通过；BG03 干净背景实证桌区为空地（对照图桌椅为 QA 叠图）、门中心 ≈x640 与 R04 锚点一致但门宽 ≈100px 窄于 R04 碰撞缺口 528-752。用户指令已登记：R07-R10 接入时核对桌椅/长椅画面与实际碰撞一致；R12 接入时引擎播放检查动作方向。R13 的 PRP03/06/07 面板 PendingC03（等 C03 门环/合照缺口/黑板描画区导出）。明细见 CodelyHandoff.md §8B。
## 行走动画修复（2026-10-28 深夜，用户录屏反馈）

录屏实锤主角朝向切换正常但行走循环不生效。根因两处：Bind 先于 AddComponent<PlayerController2D> 调用导致 ctrl=null（探针实锤）；IsMoving 由 FixedUpdate 赋值而动画器 Update 读取存在帧序竞争。修复：ctrl 改 Update 惰性解析，moving 改世界坐标位置差分（与帧序无关）。生产路径实测探针显示 ctrl=bound/moving=True/cycleIdx 循环/WalkA→Idle→WalkB→Idle 帧循环真实生效，R14PlayTest 全流程 PASS，最终构建 111,181,928 字节。待用户实机复核行走动画。
## C05 交付记录（2026-10-29，第2关「真心为你」全流程接入）

正式 CH02「真心为你」11 阶段全流程接入（ChapterCatalog.BuildChapter02）：女孩自动对话→拾项链→一次交还→仪式引导→CH02 专属仪式锚点→登船→封 2 道渡河（乘客 1 人）→男孩靠近自动相认对白→相会点 MG04_Heart 描画→PR_CH02_Farewell 演出（CG02 定格→只淡女孩、男孩与公园保留）→短淡出通关。路线门禁保证 CH01/CH02 对象同坐标互斥；公园按 R04 布局装配（出生点/长椅碰撞/男孩/相会点）；演出按 PresentationLayers 规则只淡女孩。资源导入：TXT_CH02、BG04_Park、IT03_Necklace、CHR06_PastSelfRomance。修改：ChapterCatalog、ProjectSetupUtility、新增 Scenes/ParkCast.cs、DevSmokeTest（CH02 断言，修正 ReunionTalk 为男孩靠近自动对话而非阶段自动启动）。验证：DZ_VALIDATION/SMOKE/MINIGAMES/RIVER_SEEDS 全 PASS，构建 Succeeded 113,989,032 字节。待用户实机全流程验收（步骤见 CodelyHandoff.md §8I 与 StageCoverage_CH02.md）。资源缺项：CHR06 男孩无行走表（用站姿滑动）；音频未接线（C10）；PR_CH02_Memory 未制作（规范默认不制作）。

## CH01 渡河后 FarmCourtyard 原生崩溃修复（2026-10-29）

用户构建版在 CH01 `RiverSucceeded` 后加载 FarmCourtyard 时闪退。崩溃日志明确为 `Duzhe_Data/level3 is corrupted` + `Position out of bounds`，不是组件 `Awake` 的 C# 异常。实读场景发现 `SceneRoot` 重复挂着 5 个 `CH01FarewellCast`，其 `m_Script` 指向场景内嵌 `MonoScript`；Park 的 `CH02FarewellCast` 存在同型问题。根因是两个次要 `MonoBehaviour` 分别与 `FarmCourtyardCast`/`ParkCast` 共用 `.cs` 文件，重复装配后脚本绑定失效并累积，导致玩家构建的场景序列化数据损坏。

修复：`CH01FarewellCast`/`CH02FarewellCast` 拆为同名独立脚本；`ProjectSetupUtility` 新增资产引用型单组件保障，清理失效/重复组件并要求 Farewell 脚本引用外部资产。重存后 FarmCourtyard/Park 各仅剩一个 Farewell 组件，场景内嵌 `MonoScript` 全部消失；第二轮装配全部 `already complete`。

验证：临时构建版场景加载探针分别直接加载 FarmCourtyard（level3）与 Park（level4），均 `DZ_SCENE_LOAD_PROBE=PASS`，无 corrupted/Crash；探针随后删除。`DZ_VALIDATION`、`DZ_SMOKE`（含 CH01 完成后菜单选择 CH02）、`DZ_MINIGAMES`、`DZ_RIVER_SEEDS`（64 种子）、`DZ_R14_PLAYTEST`（座位→CG 定格→只淡双老人→孙子保留）全 PASS。最终无探针构建启动进入 MainMenu 正常，`Build result: Succeeded | errors=0 | size=113,990,700 bytes`（`Logs/build_crashfix_final_clean.log`）。用户仍需从正常 CH01 渡河入口实机复验完整通关链。

## CH02 项链图标与 CH01 孙子立绘/重聚站位（2026-10-29）

用户截图反馈两项：CH02 右上角任务物显示成眼镜；农家院“孙子”资源与站位不对。修复：`PlaceholderArt.QuestIconSprite` 显式映射 `IT03_Necklace → Art/Props/IT03_Necklace`（原默认分支为 CH01 眼镜占位图），并加入冒烟断言；导入 R11 `CHR04_PastSelfFamily` 替换孙子通用占位图并保持 feet pivot 原色；开门时渡者瞬移到 `(0,2.25)`，孙子从门下 `(0,5.1)` 走到 `(0,4.25)`，两位老人跟随下移，形成门 `6.5` > 孙子 `4.25` > 渡者/老人 `2.25` 的构图。`PlayerController2D.TeleportTo` 同步 Rigidbody2D 与 Transform。验证：DZ_VALIDATION/SMOKE/R14_PLAYTEST 全 PASS（R14 新增纵向顺序与老人跟随断言，截图 `Logs/r14_playtest/farm_reunion_layout.png`），构建 Succeeded errors=0 113,998,492 字节。

## R13 Panels v1.0 接入：MG06 重影与 MG07 书写区（2026-10-29）

接收 `Duzhe_R13_Panels_v1.0.zip` 至 `SourceAssets/R13_Panels_v1.0/`，导入 PRP06_PhotoFrame/PRP07_BlackboardCloseup（640×360）到 `Resources/Art/Props/`。MG06 改为正式底图承载三位固定成员，删除程序固定成员色块，只保留唯一缺口热点与 CHR07 鼓手可拖物；修复完成判定由 `slotCount(4)` 改为实际可拖物数量。MG07 改为正式黑板底图，书写区由旧 `[0,80,640,320]`（越出底图 40px）收为 `[0,80,640,210]`，按钮固定在底图 `[30,300]`/`[430,300]`，状态行移到板下；标题与状态居中。实机断言：`DZ_MG06_DRAG=PASS`（鼓手拖入唯一缺口并完成）、`DZ_MG07_LAYOUT=PASS`（书写区在底图内且在按钮上方）；DZ_VALIDATION/SMOKE/MINIGAMES 全 PASS，构建 Succeeded errors=0 115,382,612 字节。截图：`Logs/panel_geometry/MG06_Photo.png`、`MG07_Goodbye.png`。
## R12 主角行走 + R14 告别插画接入与实测（2026-10-28）

用户送达 R12_Main（主角 CHR01 行走表）与 R14（CG01-CG05 五张告别插画+PresentationLayers.json），并指定优先实测：主角行走、亡者单独淡出、黑板字层；R14 交接规则（CG 为烘焙定格，禁整图降透明，淡出前切回干净场景只淡 spiritActorIds；CG04 黑板字引擎渲染）全部落地。接入：EnsureFormalArtImports（R11 七站姿 pivot(0.5,0)/R12 主角表 Multiple 3×4 切片 12 帧/R14 五 CG+图层清单 → Resources）+ PlayerSpriteAnimator（6fps WalkA→Idle→WalkB→Idle，主角视觉改子物体 feet pivot 补偿 -0.5u）+ CH01 老人/幽灵换正式站姿 + PresentationCG（全屏定格，Canvas 最底层）+ BlackboardTextLayer（按 CG04 实测黑板面对位，anchor(0,1)+pivot(0,0.5) 时 y 从顶向上为正——曾写反 720-120 导致字层飞到底部，修正为 +120）+ PresentationLayerCatalog。实测：R14PlayTest 编辑器 GUI 实测三轮全流程 PASS（四向行走/CG 定格/只淡双老人/孙子保留）+ R14BlackboardBatchTest 确定性批测 PASS（text mesh 48 verts、simhei missing=0 字形）；Play 截图黑板字不可见系编辑器 Game 视图 1536×630 非参考比例拉伸 CG 所致，正式 1280×720 构建窗口对齐（待用户实机复核）。实测环境坑：无焦点后台跑 Play 测试需 runInBackground=true（测后改回）+ 驱动即时关失焦自动暂停 + 结果轮询替代墙钟（失焦拉伸游戏时间）。全套验证 PASS + 构建 Succeeded 111,181,416 字节。
## C04 试玩修复一（2026-10-28，用户三条反馈）

用户实机反馈：①拾取眼镜/画完拐杖后原位置的物品应消失；②仪式后原老人站位还有一个人；③农家院只有一位老人。截图复核根因：
- **DEV 与 CH01 两套对象同坐标共存、只有交互门禁没有可见性隔离**——CH01 运行时 DEV 眼镜/NPC/仪式点/渡口仍显示（残留眼镜=Item_DEV_GLASSES、残留老人=NPC_DEV_GRANDPA）。
- **两位幽灵跟随偏移相同**（FarmCourtyard elderA/elderB 都用了默认 (-0.65,-0.2)）→ 跟随时重叠成一个可见精灵。
修复：新增 `Chapters/ChapterObjectVisibility`（路线可见性门禁：按 ActiveChapterId 显示本路线对象默认可见性，启用时+章切换时评估，仪式/事件隐藏不被覆盖）+ `Chapters/HideOnChapterEvent`（MG01_Cane 完成后拐杖台与视觉一并消失）；装配为全部 Underworld DEV/CH01 对象挂路线门禁；elderA/elderB 跟随偏移分离 (-0.65,-0.2)/(0.65,-0.2)。ChapterDirector 新增静态 ActiveChapterId+ChapterChanged 事件。DZ_VALIDATION=PASS。
## C04 交付记录（2026-10-28）

- **正式 CH01「再吃一顿饭」12 阶段全流程接入**（ChapterCatalog.BuildChapter01）：Opening（自动对话）→ Prepare（**requireAll 多条件阶段**：眼镜拾取+拐杖描画任意先后，MG01 完成授予 IT02_Cane）→ ReturnItems（**一次 E 交还两件**，缺一不可）→ RitualTalk（自动段）→ Ritual（CH01 专属仪式锚点，与 DEV 锚点按 id 精确匹配互不串扰）→ Board → River（sourceId 规范化为 River）→ Door（MG02 两次点击隔离+可重开）→ ReunionTalk（自动段）→ Stools（MG03 三凳）→ **Farewell（演出驱动）** → Complete（1.4s 短淡出→ChapterCompleted）。每阶段带 TXT objectives 目标文案；事件链五重过滤幂等不变。
- **演出系统（新）**：`Scripts/Presentation/PresentationPlayer.cs`——PresentationMap.json 驱动（座位演出→orderedSegmentIds 逐段手动推进→末句后视觉阶段→恰好一次 PresentationFinished+MainRun 解锁写盘）；`Scripts/Scenes/FarmCourtyardCast.cs`——MG02 后孙子出门走位、MG03 后凳子锁定、PR_CH01_Farewell 座位/淡出/停留；`UI/FadeOverlay.cs`——章节收尾淡出（暂停冻结同步）。
- **R01 正式文本接入运行时**：EnsureDialogueImports 把 TXT_CH01/TXT_UI/PresentationMap/CharacterDisplayNames 从 Data/Dialogue 复制进 Resources（字节一致幂等）；DialogueLibrary 新增演出映射与全局演员名解析；**TXT_UI 正式 201 key 替换开发子集，全部 UiText 调用点迁移到正式点分 key**（speaker.*/hint.*/feedback.*/minigame.*/river.*/button.* 等）。
- **R04 布局装配**（TopLeft 像素→世界换算 `worldX=(px-640)/32, worldY=(360-py)/32`）：Underworld 复用已验收位置+新增拐杖台 (−5,−1.25)、奶奶 (3.375,−2)；FarmCourtyard 全量装配（出生点 (−11,−6.75)、门/交互区/桌/桌碰撞/三凳目标位淡影+锁定实凳/孙子/房墙三箱+桌箱碰撞/演出 cast）；River 布局与 C02 实装一致（船 (640,456)px=世界 (0,−3) ✓）。新增占位图 Cane/Door/DiningTable（稳定文件名）。
- **C03 遗留不可见 UI 修复**：核对 y 符号约定（负 y=顶向下可见）后确认 Drag/Notes/Door 三面板的标题/状态行/关闭按钮一直用了正 y（屏外不可见，交互元素可见故未察觉）——全部修正；MG03 增加正式说明行、凳子起始位下移避让；MG06 说明并入状态行。
- **健壮性**：暂停-离开确认时强制关闭机关面板/对话视图（DialogueRunner.ForceStop 无事件）/演出（PresentationPlayer.ForceAbort），防运行残留泄漏进菜单；选章逻辑改**正式章优先**（CH01_DEV 不再挡住正式链；正式章全完成回放 CH01）；InteractionController 支持多物品交还与按上下文 promptKey 提示。
- **自动验证（全实跑）**：DZ_VALIDATION=PASS（含 4 文件导入+双场景 CH01 装配+TXT_UI 201 key）；DZ_RIVER_SEEDS=PASS（64 种子双规则组）；DZ_MINIGAMES=PASS；DZ_SMOKE=PASS（新增 CH01 12 阶段结构/requireAll/演出映射/正式 UI key/选章断言）；构建 Succeeded errors=0（Logs/duzhe_build.txt）。
- **未测（人工）**：CH01 实机全流程（含故意渡河失败、机关未完成时推进尝试、对话/机关中暂停恢复、事件重复与残留检查）——验收步骤见 CodelyHandoff.md 第 8 节 C04 部分；阶段级覆盖明细见 StageCoverage.md。
- **资源缺项（如实）**：正式背景（BG01/02/03）、任务物/道具图（IT01/IT02、PRP03/04/05）、CG01_Farewell 插画、音频包未交付——占位方案运行，替换不动代码（详见 StageCoverage.md 第三节）。
- **C03 试玩修复确认（沿用）**：MG03 拖拽偏移与 MG06 吸附修复保留；用户对 MG06 的复验可在 DEV:机关测试 或正式 CH01_Stools 流程中进行。

## C03 交付记录（2026-10-27）

- **新模块 `Assets/_Project/Scripts/Minigames/`**（9 文件）：MinigameConfig（7 配置目录：MG01_Cane/MG02_Door/MG03_Stools/MG04_Heart/MG05_Notes/MG06_Photo/MG07_Goodbye，type/templateId/slotCount/sequence/panelW·H）；MinigameHost（唯一开关门：防重入、InputMode.MiniGame、一次 MinigameCompleted 事件带 runId/stageId/sourceId=minigameId、暂停边沿通知面板、无残留、DEV OpenTester）；MinigamePanelBase（挂持久 Canvas SetAsFirstSibling 底层=暂停置顶永远盖住）；TraceTemplates（三套真实可玩模板：Cane 两单元竖柄+钩弧、Heart 参数方程闭合心形、Goodbye 占位 5 笔画骨架全标 Placeholder）；TraceEvaluator（像素空间判定：覆盖率≥75%/走廊占比≥65%/走廊半宽=短边 5%/起点容差/终点到达/防直线乱涂）；TraceMinigame（RawImage+Texture2D 双层绘制：模板引导线分色=完成绿/当前黄/未始灰、玩家墨迹、逐笔判定、重画只清当前笔画、抬笔保留、暂停清当前笔）；DragPlacementMinigame（MG03 3 凳任意空位满 3 完成/MG06 仅鼓手可拖唯一缺口；24px 吸附、错放回原位、已放锁定、暂停打断未完成回原位已完成保留）；DoorKnockMinigame（门环热点点击完成、其他区域无进度、可关闭重开、自动退出）；NoteSequenceMinigame（七音按钮 Do–Si+数字 1-7、目标高亮进度点、错音发声不推进不清进度、程序生成七音正弦占位音、CFG_Notes.json 读取骨架、enablePerformance=false 关闭路径）。
- **触发与测试入口**：MinigameLauncher（IPropInteractable，C04+ 正式章装配用）；PauseMenu DEV 区"机关测试"按钮（正式构建不出现）→MinigameTester 选择面板（7 机关+Notes(off) 模拟 enablePerformance=false 关闭路径）。7 份机关配置内置于 MinigameCatalog（C06 起 CFG_* JSON 接入替换数据不改 ID）。
- **自动验证**：DZ_MINIGAMES=PASS（`Logs/minigames_report.txt`）——TraceEvaluator 判定矩阵（三模板：完美轨迹 PASS；直线/乱涂 8 组/半程/错序全 FAIL）+Notes 状态机（正确序列完成/错音不推进）；幂等装配 PASS；冒烟 PASS；构建 Succeeded 107,472,352 字节。验证矩阵曾抓出 EvaluateStroke 坐标空间混用（归一化 vs 像素）与验证器抖动空间两处真 bug 并修正。
- **未测（人工）**：机关面板实际操作手感（描画笔迹流畅度/吸附手感/音量）、暂停打断的实际体验——DEV 入口即测。

## C03 试玩反馈修复五（2026-10-27 深夜，第五轮复验问题）

用户第五轮复验（视频+反馈）："3 好了"；MG06"能拖但松手后弹回"；MG07 两竖太短；"拖动的时候也显示"。

- **吸附半径 24→80**：SNAP FAIL 探针实锤用户松手位置距缺口中心 63.1px > 24——**吸附精度不是本机关的挑战**，放宽到槽位附近即吸附。SNAP FAIL 探针保留（C11 清理）。
- **拖起时物品置顶**（SetAsLastSibling）：拖动过程物品始终可见。
- **MG07"再"字两竖翻倍**（用户命令：固定上端、长度×2）：左右两短竖下端延伸到 y=0（与底横相交）。
- 当前构建：Succeeded，errors=0，107,474,400 字节。
- 待用户复验：MG06 拖放（宽松吸附）、MG07 字形。

## C03 试玩反馈修复四（2026-10-27 深夜，第四轮复验问题）

用户第四轮复验（截图+描述）："再"字缺两竖（已加但太短）；MG06"能拖但松手后弹回原位"；MG07 重画按钮不挡线（确认已修）。

- **"再"字两竖加长**：左右两短竖延伸（y 0.72→0.36，长度 0.36）。
- **MG06 吸附失败根因（SNAP FAIL 探针实锤）**：松手位置 anchoredPos=(668,−325)、最近槽位判定点 (640,−288)、距离 46.4>24——**缺口判定坐标存的是"左上角"（AddPanel pivot 语义）而物品判定用"中心"（pivot 0.5,0.5）**，锚点语义不一致导致吸附永远差约半格（用户把鼓手中心对准灰块中心时距判定点仅 5px！）。修复：BuildPhoto 的 gap 判定坐标改为**缺口中心**（gapCenter），缺口视觉块按中心对齐绘制。
- 当前构建：Succeeded，errors=0，107,474,400 字节。
- 待用户复验 MG06 拖放与 MG07 字形。

## C03 试玩反馈修复三（2026-10-27 晚，第三轮复验问题）

用户第三轮复验（截图+标注）："再"字少两竖；MG06 仍未好。
- **Goodbye 模板补齐**："再"字加左右两短竖（现 13 单元：顶横/中竖/中横/左右两短竖/短横/底横 + "见"闭合方框 4 笔 + 内横 + 儿双腿 2 笔）；"见"的框修正为**闭合方框**（此前误画成开口朝上的 U 形）。
- **MG06 排查**：与已修复的 MG03 共用同一套拖放代码（CreateDraggable/拖拽跟随 y 轴修正/吸附判定），无专属差异代码路径。用户复验时如仍有问题需提供具体现象（拖不动/吸附不了/看不见鼓手）。
- 当前构建：Succeeded，errors=0，107,473,888 字节。
- 运行时证据：用户描画"再"字的逐笔推进被正确记录（日志 strokeIdx 0→1→2）——描画判定工作正常。

## C03 试玩反馈修复二（2026-10-27 晚，第二轮复验问题）

用户第二轮复验（截图+描述）：MG02/MG05 通过；MG06 未好；**MG01/04/07 描画"只能左右移动+画笔与鼠标位置偏差"**；Goodbye 形状仍需改进；标题重复。

- **描画纵向映射错乱根因**：ToAreaPixels 的 GetWorldCorners 版混用两套原点——GetWorldCorners 返回**以屏幕中心为原点**的 Canvas 世界坐标，而 EventSystem 的 screenPos 以**屏幕左下**为原点，纵向相减后 v 值整体偏移出 0..1（被 clamp 压成常数）→ 表现为"只能左右移动"。修正：改用 **RectTransformUtility.ScreenPointToLocalPointInRectangle**（引擎正确处理 Overlay 坐标系）+ 明确的局部→纹理映射公式（pivot 顶部中心、y 向上：texX=W×(0.5+local.x/rectW)、texY=H×(1+local.y/rectH)）。
- **标题重复**：displayName 已含"（Placeholder）"，BuildUi 又追加一次——去掉追加。
- **MG06 复验**：本轮修改后待用户再验（拖放与 MG03 同代码路径，理论上同步修复）。
- 当前构建：Succeeded，errors=0，107,473,888 字节；DZ_MINIGAMES 回归 PASS。

## C02 试玩反馈修复（2026-10-27 下午，船占位升级）

- 用户报告"开船时船的模型不见了"并附截图。DiagDump 批处理诊断 + 场景 YAML 解析确认：装配、sprite 引用、仪式隐藏连线全部正常（引擎视角 GUID 为 hex 格式、meta 内 base64 只是文件存储形态——两套编码曾导致误判"悬空"）。截图复核真因：**旧占位船仅 40×24 像素且渲染在角色之上（order 2）**，三个角色站上去后视觉上"看不出是船"。
- 修复：**占位船按 PRP01_Boat 96×64 规格重画**（俯视 2D 渡船：船体/甲板/两排座板/船头灯，=3×2 世界单位）；**渲染层次改为船 order=0 与角色同层 Y 排序**（渡客/乘客自动渲染在船之上，正确俯视层次）；乘客站位外移 ±0.85、贴甲板 y=0.1。删旧图重生成，两处场景引用经装配自愈重连。
- 待用户复核：新船是否清晰可辨；**渡河时顶部目标横幅是否显示**（用户截图疑似缺失——日志证明 SetObjective 已执行，需实机确认）。
- 当前构建：Succeeded，errors=0，**107,441,272 字节**（此后引用以此为准）。临时诊断 Editor/DiagDump.cs 留至 C11 清理。

## C02 审查补正（2026-10-27，资源方 C02_ReviewFix）

资源方只读核对交接/脚本/Logs 后指出四块问题，全部修复并重建（最新构建 107,435,960 字节）：

1. **渡河弹窗生命周期**（真实 bug）：briefing/retry 面板挂在持久 UiCanvas 下，场景切换不会自动销毁——任何退出路径都会把面板残留到主菜单，连续失败每次新建 retry 面板累积隐藏对象。修复：RiverController.OnDestroy 显式 Destroy 两个面板（成功/失败/暂停返回都经过场景卸载）；retry 面板整次停留只建一次、后续失败复用 SetActive(true)；面板创建 SetAsFirstSibling 置于 Canvas 底层。PauseMenu.Open/ShowConfirmLeave 时 SetAsLastSibling 置顶（永远在河面/机关面板之上），Pause/Confirm 遮罩恢复 blocksRaycast=true（暂停是模态的，拦截下层开航/重试点击；C01"点不动"真根因是 EventSystem 销毁，遮罩拦截本身是正确行为）。
2. **开发进度与正式五关分离**：MainMenuPanel.Refresh 按 ChapterCatalog.FormalChapterIds（CH01–CH05）过滤统计；CH01_DEV 完成后主菜单显示"开发测试路线已完成（Placeholder，不计入正式五关）"而非"已完成章节：1"；存档不清空不迁移。DEV 河段成功回菜单保留为开发路线（正式章 RiverSucceeded 后仍需相会/告别，由各章阶段链决定）。
3. **种子验证器反应时机修正**（资源方指出的真问题）：旧 SimulateBoat 以 arrival−spawnLead+reaction 转向=提前 2.5 秒感知屏外障碍（作弊）。修正：可见窗口公式 RiverRules.VisibleWindow=(cameraYOffset+5.625)/fwd=1.525s 成为运行时与验证器共享几何契约（spawnLead 只决定生成位置在屏外，不作反应依据）；模拟船从 arrival−1.025s 开始转向、碰撞改为全下落轨迹箱边扫描。**双组各 32 种子全 PASS：GroupA 封1道（CH01/DEV 规则）+ GroupB 封2道（CH02–04 规则）**，报告 Logs/river_seeds_report.txt（两组参数+每种子波表+模型说明）；时间余量 0.275s（需求 0.75s）。若未来调参导致不可达，验证器会 FAIL 并给出 wave 级原因。
4. **资源与报告同步**：R02_Style_Approved_v1.3 已解压暂存 SourceAssets（母图 VisualGuide.png 仅视觉参考，不作正式地图/图集）；风格文档（StyleLock/StylePromptBlock/VisualQA/ApprovalRecord/C01 集成评审）归档 Docs/Spec（R02_ 前缀）；交接"侧视小渡船"更正为**俯视 2D 小渡船**（PRP01_Boat 96×64，乘客站位与碰撞参数沿用现有配置）；渡河音效沿用既定 12 SFX 复用映射；字节引用统一最新构建。
- 补正后回归：DZ_RIVER_SEEDS=PASS（64 种子双组）、DZ_SMOKE=PASS、构建 Succeeded errors=0。
- 待人工试玩回归（资源方指定）：说明界面→暂停→返回菜单→重进（对象数量稳定、按钮回调一次、无残留/穿透）；失败界面同流程；连续失败 3 次后退出。

## C02 交付记录（2026-10-27 凌晨）

- **新模块**：`Ritual/`（IPropInteractable 接口、RitualTrigger、BoardingSpot、RitualController 短演出状态机、SpiritEscort 灵魂站位/跟随队列）；`River/`（RiverRules 种子波次规划+可达性几何验证、RiverSession 航程独立状态、WhirlpoolSpawner、BoatController、RiverController 状态机+代码构建 UI）。
- **规范落实**：E 仪式前置由阶段授权+连按不重复（相位守卫）；演出无对白、暂停兼容（deltaTime 冻结）；灵魂两位乘客（DEV 按 CH01 规则）淡入站位后简单跟随，无碰撞体不挡玩家；登船 = Boarded 事件（阶段门禁）→ ChapterDirector 切 River；渡河 Briefing 确认后开航且首次说明不重复、说明不计时；10 秒有效游戏时间（deltaTime，暂停/失焦天然冻结）；碰撞为纯数学 AABB 且先于成功判定（同帧唯一仲裁）；失败只重置 RiverSession（船位/时间/障碍/重试 UI），章内解谜/交还/仪式/对白 flags 全保留；成功只发一次 RiverSucceeded（不直接标章完成）；种子进章生成、失败复用、重玩更换（ChapterRuntimeState.riverSeed）；无 HP、无可见倒计时。
- **开发测试章扩展**：CH01_DEV 现 6 阶段：INTRO→FIND→RETURN→RITUAL→BOARD→RIVER→章完成（riverSettings：乘客 2、每波最多封 1 道）。
- **装配**：Underworld 仪式区（石灯锚点/渡船/两位灵魂/站位，RitualController 挂场景根）；River 渡河区（RiverRoot+RiverController+WhirlpoolSpawner+Boat）；新增占位图 Boat/Whirlpool/RitualAnchor；TXT_UI 28 键；幕等两轮验证通过。
- **可达性验证**：RiverSeedValidator 批处理 32 种子全 PASS（`Logs/river_seeds_report.txt`：每种子波表+安全道+模拟船跟随安全道全程无碰撞）；验证器曾抓出可见窗口公式方向写反的 bug（0.98s<1.5s）并修正为 (cameraYOffset+半屏高)/速度=1.525s。
- **构建**：Windows x64 Succeeded，107,434,800 字节；启动探针正常。
- **未测（人工）**：仪式演出手感、渡河避障实际手感、连续失败重试体验、失焦暂停冻结实测——验收步骤见 CodelyHandoff.md。

### R01 正式文本包已接收（2026-10-27 凌晨，v1.0.0）

- 包暂存 `SourceAssets\R01_Text_v1.0\`；JSON 已归档 `Assets/_Project/Data/Dialogue/`（TXT_CH01–05、TXT_UI、PresentationMap、CharacterDisplayNames、AssetDelivery——**静态归档目录，不在 Resources 运行时扫描内，不影响校验器**）；文档归档 `Docs/Spec/`（加 R01_ 前缀）。
- **只读结构预检全绿**：237 条对白 lineId 全局零重复；53 阶段 objectives（12/11/12/12/6）与阶段总表一致；TXT_UI 201 键零重复、objective.* 53 个；PresentationMap 6 项；12 位演员正式 ID（Ferryman/Grandfather/Grandmother/PastSelfFamily/LoverSpirit/PastSelfRomance/DrummerSpirit/PastSelfBand/BandmateA/BandmateB/TeacherSpirit/PastSelfSchool）。
- 接入时点按约定：C04–C08 逐章启用（当前 ChapterCatalog 仅 CH01_DEV）；接入时做点分 UI 键与占位下划线键的显式映射、speaker.<actorId> 显示名映射、无对白阶段不注入空段；正式 CH01 为 12 阶段（含 Door/Stools 机关段），由 C03 机关组件+C04 接入装配。
- 用户 C02 试玩反馈修复：仪式时老人本体应隐藏（hideOnRitual 连线）；根因=构建流程缺 EnsureScenes 导致场景装配未随构建更新——已在 DuzheBuildTools.Build 中加入 EnsureScenes，场景连线已实证落盘。

## C01 检查点记录

### 检查点 1 — 工程盘点 ✅（2026-10-26）

- 无已存在的 Duzhe 工程（`C:\Users\東壹\Duzhe` 原不存在，Hub 注册表亦无），按任务前置完成最小创建。
- 创建路径说明：Hub CLI `projects create` 连续三次 CREATE_FAILED，根因是 Cowork 常驻 Hub 进程占用 `\\.\pipe\Tuanjie-hubIPCService` 命名管道（Hub 日志实读 `Failed to listen at`；`projects list/info` 不受影响）。改用与 Hub 完全一致的编辑器命令行 `Tuanjie.exe -createproject <path> -cloneFromTemplate <模板tgz>` 创建成功；模板 tgz 实体在 `C:\Program Files\Tuanjie\Hub\Editor\2022.3.62t16\Editor\Data\Resources\PackageManager\ProjectTemplates\cn.tuanjie.template.2d-7.0.4.tgz`。工程已用 `tuanjie.exe projects add` 注册回 Hub。
- 盘点产物：`ProjectEnvironment.md`（版本/输入/渲染/包/构建模块全部实读记录）。
- 构建模块：全部可选模块未安装（含 windows-il2cpp）；Windows x64 构建以 Mono 后端成功（见检查点 2），无需安装任何模块。

### 检查点 2 — 最小探索 ✅（2026-10-26）

- 约定目录：`Assets/_Project/`（Scenes/Scripts/Data/Art/Audio + Resources 运行时加载路径）+ `Docs/` + `Builds/` 全部建立。
- 7 个场景由 `ProjectSetupUtility` 幂等装配：MainMenu + Underworld（含占位 NPC「老人」与占位物品「眼镜」+ 边界墙 + 出生点）+ River/FarmCourtyard/Park/RecordingStudio/Classroom（骨架：背景+边界+出生点）。
- 运行时层：GameBootstrap（[RuntimeInitializeOnLoadMethod] 唯一 GameRoot：GameSession / InputRouter / SaveService / SceneTransitionService / AudioService / DialogueRunner / ChapterDirector / 唯一玩家 / 持久正交相机 / UGUI Canvas）。
- 玩家：八方向移动（斜向归一化）、四向朝向（两轴相等保持上次朝向防抖）、脚底小碰撞体、无重力、冻结旋转；相机半高 5.625 且按背景 bounds 钳制。
- 交互：E 提示（1.2 手动交互 / 1.4 自动对话，可配置）、阶段授权（activeStageIds 外不响应）、NPC 合法阶段自动对话且本段只播一次（playedSegmentIds 按 run 记录）。
- 幂等验证：第二次运行装配全部 "already complete"，无重复对象。
- **首次 Windows x64 构建：Succeeded，errors=0，97,650,035 字节，1 分 51 秒，输出 `Builds/Windows_x64/Duzhe.exe`（Mono 后端，无需 IL2CPP 模块）。** 报告 `Logs/duzhe_build.txt`。

### 检查点 3 — 对话和输入 ✅（2026-10-26）

- TXT_CHxx.json / TXT_UI.json 为唯一文本真相（`Assets/_Project/Resources/Data/Dialogue/`），运行时 JsonUtility 解析 + ID 索引（DialogueLibrary）；编辑器 `DialogueJsonValidator` 校验 schemaVersion/必填字段/lineId 全局唯一/speakerId 合法性/stageId 存在性，错误报具体文件+字段+ID。
- DialogueRunner/DialogueView：整句显示（无打字机）、点击推进区域或空格逐条推进、同帧重复按下丢弃、按住不可能连跳（GetKeyDown 语义）、关闭对话的那次输入被模式切换消费不穿透。
- InputRouter 统一输入入口；PauseMenu ESC 覆盖层（不堆叠、timeScale=0、音乐暂停、恢复按键消费、返回主菜单带确认提示、失焦自动暂停）；AudioService 三组音量（master/music/sfx，UI 可调，关闭暂停面板时写盘）。
- 测试对白已导入：`TXT_CH01_DEV.json`（全部标注 Placeholder）+ `TXT_UI.json`（23 个 key）——校验 OK，冒烟测试确认段/行/文案查找可用。
- 本任务无任何正式音频文件；AudioService 只搭框架，音频为占位静默（已在代码与 handoff 标明，不报完成音频）。

### 检查点 4 — 章节基础 ✅（2026-10-26）

- ChapterDirector 唯一章节状态拥有者：事件过滤链（runId → chapterId → stageId → completionEvent → expectedSourceId），阶段推进后旧 stage 事件自然失效 = 幂等；仅本类可标记 ChapterCompleted。
- MainRun / ChapterReplay / PresentationReplay 三模式分离：回放不写主线存档；全通关后"开始"以回放方式进第一章（保留解锁）。
- SceneTransitionService：sceneId→场景名静态绑定（7 场景）、异步加载 + Transition 输入锁 + 出生点落位；SceneContent/SceneIdentity 场景侧注册表。
- SaveService：saveVersion=1、completedChapterIds、unlockedPresentationIds、三音量；临时文件替换 + .bak 备份 + 坏档自动恢复（冒烟测试实证：污染主档后 Load 从 .bak 恢复成功）。
- 开发测试章 CH01_DEV（Placeholder）：ST01_INTRO（NPC 自动对话一次）→ ST02_FIND（物品拾取）→ ST03_RETURN（E 交还+感谢对白）→ 章完成写档回主菜单。
- 开发入口：暂停菜单 DEV 区（仅 UNITY_EDITOR/DEVELOPMENT_BUILD）提供"完成当前章/清空存档"；正式菜单无跳关。
- 冒烟测试（批处理）：DZ_SMOKE=PASS（存档写读/幂等/备份恢复/对白库/章节目录）。

## 自动验证记录（全部真实执行）

| 验证 | 结果 | 报告 |
|---|---|---|
| 批处理装配+校验 `ProjectSetupUtility.RunFromCommandLine` | DZ_VALIDATION=PASS（两轮，第二轮验证幂等） | `Logs/duzhe_validation.txt` |
| Windows x64 构建 `DuzheBuildTools.BuildWindowsFromCommandLine` | Build Succeeded, errors=0, 97.6 MB | `Logs/duzhe_build.txt` |
| 开发冒烟测试 `DevSmokeTest.RunFromCommandLine` | DZ_SMOKE=PASS | `Logs/duzhe_smoke.txt` |

**未测（需人工 Play）**：移动碰撞手感、NPC 自动对话只弹一次、对白推进节奏、章节完成回菜单、重开后已完成记录显示——步骤见 `CodelyHandoff.md` 验收清单。（菜单显示、场景渲染、中文字体、暂停菜单与音量 UI、失焦自动暂停已通过构建版窗口截屏实证，见下方追加记录。）

## C01 追加记录 — 黑屏问题修复（2026-10-26 深夜）

用户首次验收构建版报告黑屏。经构建产物截屏 + 探针日志 + 窗口像素分析定位出**五个叠加根因**，全部修复并重建验证：

| # | 根因 | 修复 |
|---|---|---|
| 1 | 窗口模式为无边框全屏（fullscreenMode=1）+ 占位背景过暗，观感即"黑屏" | `PlayerSettings.fullScreenMode = Windowed` + `resizableWindow = true`（规范本就要求 1280×720 窗口）；菜单背景调亮 |
| 2 | `ProjectSetupUtility.LoadSprite` 用 `Path.GetFileName` 折叠子目录，拼出 `.../Art/BG_xxx.png`（实际在 `Art/Backgrounds/`）→ **七个场景的全部 sprite 引用（背景/NPC/物品）装配时均为空**，窗口只剩相机清屏色 | LoadSprite 保留完整相对路径 |
| 3 | 菜单背景文件名不一致：装配按 `BG_<sceneId>` 拼（MainMenu→`BG_MainMenu_Placeholder`），生成时却叫 `BG_Menu_Placeholder` → MainMenu 场景背景引用空且幂等轮误判 already complete | 统一 sceneId 驱动命名 `BG_MainMenu_Placeholder`（PlaceholderArt.MenuBgPath 同步） |
| 4 | `Font.CreateDynamicFontFromOSFont("Microsoft YaHei")` 在玩家构建中创建成功但 **CJK 字符无法光栅化**（构建内不可靠）→ 全部 UI 文字不显示 | 打包 TTF 字体：`Assets/_Project/Resources/Fonts/simhei.ttf`（沿用本机既有项目同源文件），UiFont 改为 Resources.Load 打包字体优先，OS 字体仅兜底；正式字体待资源方交付替换 |
| 5 | UI 载体 RectTransform 陷阱：`EnsureComponent`/`new GameObject` 的面板载体在 Canvas 下未保证获得 RectTransform（引擎自动转换非同步），全部子元素偏移半屏到窗口右缘外 | `EnsureComponent` 主动 `AddComponent<RectTransform>` 并全屏拉伸；新增 `MakeFullStretch` 应用到 DialoguePanel/PausePanel/ConfirmPanel/MainMenuPanel |
| 6 | 标签坐标语义二次修正（2026-10-26 深夜用户复验反馈"标题/暂停文本仍偏右"）：上一轮把 AddLabel 锚点改为"顶部中心"却未换算调用坐标，坐标再叠加半屏偏移 | AddLabel 语义定稿为**左上角**（与 AddPanel/AddButton/AddSlider 完全一致）；全部居中意图调用点按 `左上x = 中心x - 宽/2` 换算：主菜单标题/副标题(340)、进度(240)、暂停标题(420)、音量标签(460)、确认文案(420)、目标横幅(350)、Toast(240)、对白说话人/正文(70)、对白提示(440)。构建 Succeeded 19.5s 后已重启验证 |
| 7 | 滑条 Fill 残留默认 sizeDelta(100,100)（用户复验"暂停界面点不动、进游戏后靠近老人不触发对话"）：Slider 接管 anchor 后 Fill 变 272×114 大蓝块，盖住"继续游戏"按钮下半并拦截点击（Image.raycastTarget 默认 true）→ 暂停关不掉 → timeScale=0 全游戏冻结（"靠近老人不触发对话"的真正原因）；暂停/确认/主菜单遮罩层同样默认拦截点击 | AddSlider 内部结构重写为固定尺寸容器+相对拉伸子件（Fill/Handle 的 sizeDelta/offset 全部规整为官方 Slider 形态）；AddFullStretchImage 增加 blocksRaycast 参数，三处遮罩（Dim/Dim2/Backdrop）一律不拦截点击（模态拦截由输入模式层负责），对白点击捕获器保留拦截 |

另：用户报"文件不存在"实证为构建时间窗（DuzheBuildTools 构建开头 `Directory.Delete` 清空输出目录，约 20-30 秒内 exe 短暂不存在；桌面快捷方式目标与 exe 均验证完好）。后续约定：**构建期间不要启动游戏，等构建完成回报后再操作**。

- 修复后窗口截屏验证：主菜单"渡者"大标题、副标题、开始/退出按钮、底部进度文字**全部清晰渲染**；用户实际进入游戏触发过暂停菜单（截屏证实"暂停/继续游戏/返回主菜单/音量滑条/目标横幅：走近河岸的老人"均正常显示），场景内金色眼镜占位图与失焦自动暂停功能同时得到实证。
- 最终构建：Succeeded，errors=0，107,398,358 字节（含打包字体 +9.7MB），输出 `Builds/Windows_x64/Duzhe.exe`；桌面快捷方式「渡者-Duzhe」。
- 探针日志（[Duzhe] Bootstrap/Services/Camera/Player/UI/sceneId/Refresh）保留至 C11 清理阶段再移除。

## 已知问题与风险

1. Hub CLI 的 `projects create` 在 Cowork Hub 常驻期间不可用（IPC 管道冲突）；工程已用编辑器命令行创建并注册，规避该问题。`tuanjie.exe open` 不受影响。
2. 团结 API 差异实录（本工程实证）：`TransparencySortMode.CustomSort` 在团结叫 `CustomAxis`；`BuildReport` 等构建报告类型在 `UnityEditor.Build.Reporting` 命名空间（与标准 Unity 相同，但需显式 using）。
3. UI 中文显示：C01 用 OS 动态字体（Microsoft YaHei/SimHei）占位，正式字体随资源批次导入时替换。
4. 模板自带的 `Assets/Scenes/SampleScene.scene` 保留未动（未注册进 Build Settings）；正式场景在 `Assets/_Project/Scenes/`。
5. 全部对白/目标文案均为 Placeholder（CH01_DEV 开发测试章），正式五关剧情由 R01 交付后接入（C04–C08）。

## 中断续做入口

- 工程路径：`C:\Users\東壹\Duzhe`
- 批处理校验：`Tuanjie.exe -batchmode -quit -projectPath C:\Users\東壹\Duzhe -executeMethod Duzhe.EditorTools.ProjectSetupUtility.RunFromCommandLine -logFile Logs\duzhe_setup.log`
- Windows 构建：`-executeMethod Duzhe.EditorTools.DuzheBuildTools.BuildWindowsFromCommandLine`（每次输出到新的 `Builds/Candidates/<yyyyMMdd_HHmmss>_<id8>/`，不再删除旧可玩版本）
- 冒烟测试：`-executeMethod Duzhe.EditorTools.DevSmokeTest.RunFromCommandLine`（存档已隔离到 `Temp/DuzheSmoke/<guid>`，不触碰真实存档）
- CH03 机关/演出实测：`-executeMethod Duzhe.EditorTools.CH03PlayTest.RunFromCommandLine`（GUI，非 batchmode；报告 `Logs/ch03_playtest.txt`）
- 编辑器菜单：「渡者/工程装配与校验」「渡者/构建/Windows x64」「渡者/开发冒烟测试(存档与对白)」

## C06 交付记录（2026-10-29，第3关「一辈子的乐队」全流程接入）

**前置保护（检查点0）**：`SaveService` 增加编辑器专属 `TestSaveDirectoryOverride`（`MainPath` 由覆盖目录派生，`Save()` 自动建目录）；`DevSmokeTest.Run` 改为包裹 `Temp/DuzheSmoke/<guid>` 隔离目录、`finally` 还原覆盖，坏档路径也落在覆盖目录，`TestDialogueLibrary` 的 saveProbe 一并隔离；`DuzheBuildTools` 删除清空旧构建目录的代码，改为每次构建到 `Builds/Candidates/<yyyyMMdd_HHmmss>_<guid8>`。证据：真实 `duzhe_save.json`/`.bak` 运行前后 SHA256 完全一致（0105DA2A…），旧 `Builds/Windows_x64/Duzhe.exe` 仍在，新候选 `20261029_170447_58e12763` 单独生成。

**正式 CH03「一辈子的乐队」12 阶段**（ChapterCatalog.BuildChapter03）：Opening（鼓手自动对话 11 句）→ FindDrumsticks（IT04 一对鼓槌）→ ReturnDrumsticks（一次 E 交还）→ RitualTalk（自动段 7 句）→ Ritual（CH03_RITUAL 锚点）→ Board（CH03_BOAT）→ River（封 2 道/乘客 1 人）→ ReunionTalk（队友靠近自动相认 13 句）→ Perform（MG05_Notes 正式 16 音）→ Photo（MG06_Photo 单鼓手落位）→ Farewell（PR_CH03_Farewell：CG03 定格→只淡鼓手）→ Complete（1.4s 淡出）。objectiveText 全部留空，目标只来自 TXT_CH03；冒烟对 12 个阶段逐一断言目标非空。

**数据接入**：TXT_CH03.json 经 `EnsureDialogueImports` 进入 Resources（DialogueJsonValidator PASS：12 目标/1 物品/4 段/12 演员合法）；CFG_Notes.json 建立 `Data/Minigames` 工作副本并经 `EnsureMinigameImports` 进入 Resources（78 BPM、16 音 `[1,2,3,5,3,2,1,2,3,5,6,5,3,2,1,1]`、noteBeats 16 项）。

**场景装配（幂等）**：`EnsureUnderworldCh03`（NPC_CH03_DRUMMER / Item_CH03_DRUMSTICKS / RitualAnchor_CH03+CH03_RITUAL / BoatDock_CH03+CH03_BOAT / RitualRoot_CH03+1 灵魂+1 站位+hideOnRitual）与 `EnsureRecordingStudioCh03`（PastSelfBand/BandmateA/BandmateB、鼓手灵魂、Seat_CH03_Drummer、Zone_CH03_PERFORM→MG05_Notes、Zone_CH03_PHOTO→MG06_Photo、RecordingStudioCast + CH03FarewellCast）。录音室坐标取 R09 `BackgroundQA.json`：门槛 (224,576)→(−13,−6.75)（=出生点）、乐队站位 x=704/768/832/896 @ y=384 → (2/4/6/8,−0.75)、RS_Performance (768,320)→(4,1.25)、RS_PhotoApproach (960,448)→(10,−2.75)。全部对象挂 `ChapterObjectVisibility`，与 CH01/CH02/DEV 同坐标互斥。连续两次装配：Underworld CH03 objects=5、RecordingStudio objects=6、casts=1/1，第二轮全部 `already complete`，场景内嵌 `MonoScript` 数量 0。

**机关缺陷修复**：MG06 完成判定改为 `RequiredPlacements()`（MG06=1、MG03=slotCount=3），保持 slotCount=4 表示合照人数；MG05 的 `NotesConfig` 补全 schema 并严格校验（schemaVersion/minigameId/demoAssetId/tempo/7 个 noteAssetIds/12–16 音/noteBeats 等长/音域 1–7），失败时输出具体字段原因并显式回退占位序列；修正 Notes 面板 `NotesCard` 的 y 符号（原为 +top 出屏）与标题/状态行 x（原以 640 为左上角导致右侧溢出），并把 Drag/Notes 的卡片与标题/状态/关闭按钮统一按卡宽居中。

**演出**：新增 `Scenes/RecordingStudioCast.cs`（鼓手灵魂跟随、MG06 成功后走到 (8,−0.75)、快门 `ShutterFireCount` 只加 1）与 `Scenes/CH03FarewellCast.cs`（CG03 定格→19 句→先隐藏 CG→只淡鼓手 1.2s→三位生者与录音室保留；就位等待有 3 秒上限，避免演出被卡死）。

**验证**：`DZ_VALIDATION=PASS`、`DZ_SMOKE=PASS`（含 CH03 结构/目标/演出/音符/菜单选择断言与 CH03 美术断言）、`DZ_MINIGAMES=PASS`（正式谱 16 音 78BPM + MG03 3 凳/MG06 1 次落位）、`DZ_RIVER_SEEDS=PASS`（64 种子）、`DZ_CH03_PLAY=PASS`（真实 Play：MG03 一凳不完成/三凳完成且 1 次事件；MG06 仅 1 可拖 1 缺口、无重影、单次落位完成、重复通知快门仍 1 次；MG05 错音不推进、16 音完成 1 次事件、面板内容全在卡片内；PR_CH03_Farewell CG 显示→19 句→CG 先隐藏→只淡鼓手→三位队友仍在→1 次 PresentationFinished）。候选构建 `Builds/Candidates/20261029_183952_a6123452/Duzhe.exe`，`Succeeded | errors=0 | 118,196,320 字节`；构建版启动探针进入 MainMenu 无异常，并用临时 `-dzprobe` 场景探针确认 Underworld（43 子物体）/Park（10）/FarmCourtyard（23）/RecordingStudio（12）四个场景在构建版均正常加载、无 corrupted，探针随后删除并重建最终候选（探针构建 `20261029_182519_4c5e22c6` 已删除，避免误启动）。

**未测/缺项（如实）**：实机全流程（含暂停/返回菜单、渡河故意失败）待用户试玩；缺鼓槌不能交还/不能仪式为代码路径+结构断言，未在 Play 单独复验；7 音 WAV 与 SFX08_Shutter 未接线（C10），当前为程序正弦占位音与占位静音；鼓手/队友无四向行走表（站姿跟随）；`CompleteChapter()` 仍回主菜单，连续转场由 C09 完成。顺带修正：C05 只导入了 BG04_Park 却未应用到 Park 场景（`EnsureOneScene` 仍赋占位图），本次改为正式背景表驱动，Park 与 RecordingStudio 均已使用正式背景。

## C07 交付记录（2026-10-29，第4关「最后一课」全流程接入）

**前置复核**：C06 的存档隔离（`SaveService.TestSaveDirectoryOverride` + `DevSmokeTest.Run` 包裹 `Temp/DuzheSmoke/<guid>`）与候选构建目录（`Builds/Candidates/<时间戳>_<id8>`）已就位，本轮沿用；构建不再删除旧版。说明：本轮结束时真实存档已含 `completedChapterIds=[CH01,CH02,CH03]` 与 `unlockedPresentationIds=[PR_CH03_Farewell]`——这是**用户自己试玩 CH03 的结果**，不是自动测试写入（自动测试只在 `Temp/DuzheSmoke` 内操作，Play 测试以 MainMenu 模式运行不会解锁/写盘）。

**正式 CH04「最后一课」12 阶段**（ChapterCatalog.BuildChapter04）：Opening（老师自动对话 9 句）→ FindLessonPlan（IT05 教案）→ ReturnLessonPlan（一次 E 交还）→ RitualTalk（自动段 7 句）→ Ritual（CH04_RITUAL）→ Board（CH04_BOAT）→ River（封 2 道/乘客 1）→ ReunionTalk（教室自动 8 句）→ Lesson（自动 21 句，末句 CH04_Lesson_021「这就是，最后一课了。」）→ WriteGoodbye（MG07_Goodbye）→ Farewell（PR_CH04_Farewell）→ Complete（1.4s 淡出）。目标文案只取 TXT_CH04。

**课堂→写字真实门禁**：`ChapterRuntimeState.lessonFinished`（运行时不落盘）+ `ChapterDirector.LessonFinished`；仅在 `CH04_Lesson` 的完整 `DialogueFinished` 时置位；`EnterStage` 在进入 `CH04_WriteGoodbye` 前校验，未上完课直接拒绝并报具体阶段错误；`MinigameLauncher` 对 MG07 追加（章节/阶段/lessonFinished）校验且 `PresentationReplay` 永不打开机关；黑板热点改用 `Scenes/CH04GoodbyeGate`（课堂结束自动开一次、关闭后 E 重开、不每帧弹回、铃声只在首次成功打开时响）。

**场景装配（幂等）**：`EnsureUnderworldCh04`（NPC_CH04_TEACHER / Item_CH04_LESSONPLAN / RitualAnchor_CH04 / BoatDock_CH04 / RitualRoot_CH04+1 灵魂）+ `EnsureClassroomCh04`（R04 七张课桌 + 顶部墙带碰撞、学生 (−1,−1.75)、老师 (4,5.25)、黑板热点 (4,4.0)、ClassroomCast + CH04FarewellCast）。教室出生点 CL_Spawn (224,600)→(−13,−7.5)。两次装配 objects=5/3、casts=1/1，第二轮全部 already complete，场景内嵌 MonoScript=0。

**演出分流**：`PresentationPlayer` 新增 `activeSegmentIds` + `ResolveSegmentIds`（PR_CH04_Farewell 主线/章回放只播 CH04_Farewell，只有 `PresentationReplay` 播完整两段）；修复 `Play()` 中 `lib` 空引用后未重新赋值；`HandleEvent` 增加 runId 匹配；新增 `PresentationCast.OnPresentationSegmentFinished` 钩子（每段结束、下一段前调用）。`CH04FarewellCast`：主线/章回放开始即显示 CG04 + 已完成「再见」；回看在课堂段结束后才显示字迹并响铃；末句后先隐 CG 与字层，再只淡老师，学生与教室保留。

**共用组件修复**：① `BlackboardTextLayer` 的 CG04 字层锚点由 `+120`（屏外）改为 `-120`、`text.raycastTarget=false`、更正错误注释；② `TraceMinigame` 的 `TraceCard` 原用 `+top`（卡片在屏幕上方外）与标题/状态以 640 为左边、状态宽度超卡——改为按卡宽居中；③ MG07 书写区按「声明 writeRect ∩ 底板」= [0,80,640,280] 再减去按钮带，取 y 80..294（214 高），按钮带 300..344；④ **引导线与判定不一致的真实缺陷**：`ToTex` 曾把模板内缩 8px 绘制，而 `TraceEvaluator` 按 0..480 判定，短笔画（「见」的内横）末端只能到 95%，玩家与自动测试都过不去——改为与判定完全同一映射。

**验证（本次真实执行）**：`DZ_VALIDATION=PASS`、`DZ_SMOKE=PASS`（CH04 结构/12 目标/21 行课堂/第 21 行文本/演出顺序/选章）、`DZ_MINIGAMES=PASS`、`DZ_RIVER_SEEDS=PASS`（64 种子）、`DZ_CH03_PLAY=PASS`（回归：PresentationPlayer 改动未破坏 CH03）、`DZ_CH04_PLAY=PASS`（MG01/MG04 面板无回归；无章节时黑板 E 不开；MG07 面板与书写区几何；**真实描完 14 笔「再见」并恰好 1 次完成**；主线演出只播 8 句、回看 29 句且字迹在课堂后出现；只淡老师、学生保留；各恰好 1 次事件）。候选构建 `Builds/Candidates/20261029_212829_a5994efb/Duzhe.exe`，`Succeeded | errors=0 | 121,009,692 字节`；构建版启动正常，临时 `-dzprobe` 确认 Classroom（16 子物体）/Underworld（52）/RecordingStudio（12）均正常加载，探针已删除并重建最终候选。

**未测/缺项（如实）**：实机全流程（叙旧→21 句课堂→写字→告别、暂停/关闭重开、渡河故意失败）待用户试玩；「课堂未结束点黑板」在 Play 中只验证了「无章节时不开」，阶段内路径由同一合取条件+Director 守卫保证，未在真实章节中复验；7 音外的 SFX（SFX10_SchoolBell）与 BGM 未接线（C10）；老师/学生无行走表（固定站位）；`CompleteChapter()` 仍回主菜单，连续转场由 C09。

## C07 试玩反馈修复（2026-10-29 晚，通关后无法进入其他章节）

用户报告「第一章结束后进入不了其他章节」。只读探针（`Logs/save_probe.txt`）确认真实存档 `completedChapterIds=[CH01,CH02,CH03,CH04]`，本版正式章只注册到 CH04（CH05 待 C08），`GetFirstUnfinishedChapterId` 按规范回退 CH01 → 主菜单「开始故事」每次都重开第 1 章。不是场景/状态机故障，而是「全部已通关 + 第 5 章尚未接入」的既有回退行为在误导玩家。

修复：① 主菜单进度行在「本版已全部通关」时明确显示 `已通关：N ／ M（本版全部完成；开始故事将重玩第 1 章）`（新增 `ChapterCatalog.RegisteredFormalChapterCount()`）；② 暂停菜单 DEV 区新增「DEV:选择章节」，列出本版已注册的正式章，点击直接进入（已通关=ChapterReplay，未完成=MainRun），进入前关闭机关/中止演出/隐藏 CG 与字层/强制结束对白/AbandonCurrentRun，DEV 入口待 C11 清理；③ `CH04PlayTest` 增加 5 条选章断言（菜单回退值、按钮存在、面板列出 CH02、点击后 CurrentChapterId==CH02），测试期间存档写入重定向到临时目录。

验证：`DZ_VALIDATION=PASS`、`DZ_CH04_PLAY=PASS`（其余 CH04 断言全部复跑通过）。新候选 `Builds/Candidates/20261029_222638_98c9994f/Duzhe.exe`（121,011,228 字节，errors=0），启动正常。C08 接入 CH05 后「开始故事」会自动续到第 5 章。

## C07 试玩反馈修复二（2026-10-29 深夜，切章后画面无人物）

根因：`ChapterObjectVisibility` 的章节事件订阅在 OnEnable/OnDisable，而组件会隐藏自己的宿主 → 隐藏即退订 → 同场景切章后再也唤不醒，前一章物件永久隐藏、新章物件也在 `AbandonCurrentRun()` 的空章切换中被隐藏，画面只剩渡客。修复：订阅移到 Awake/OnDestroy，OnEnable 只 Apply 一次。顺带把主菜单进度行改为列出已通关章号（避免「4／4」被误读为打完第一关自动全通关）。验证：DZ_CH04_PLAY=PASS（新增 Classroom→Underworld 选 CH03 鼓手可见；同场景再切 CH04 老师可见、CH03 鼓手隐藏），DZ_VALIDATION=PASS；新候选 Builds/Candidates/20261029_232735_ca1b20af/Duzhe.exe（121,011,740 字节，errors=0），启动正常。

## C08 交付记录（2026-10-30，第5关「自渡」全流程接入）

**正式 CH05「自渡」6 阶段**（ChapterCatalog.BuildChapter05）：Recall（自动播 PR_CH05_Recall，16 句，CG01–CG04 按行切换、CG04 显示引擎字层「再见」、第 013 句清屏）→ LetGo（自动 11 句）→ Board（CH05_BOAT，仅本阶段可交互）→ River（**乘客 0 人**、封 2 道，成功后不换场景）→ Reincarnation（同场景 CG05 → 4 句 → 收起 CG → 船与渡客一起远去 + 暖色渐隐）→ Complete（制作名单，必须点确认才发 CreditsConfirmed）。

**状态与门禁**：`ChapterRuntimeState.memoryRestored/letGoFinished`（不落盘）+ `ChapterDirector.MemoryRestored/LetGoFinished`；只在 Recall/LetGo 自身完成事件后置位；进入 CH05_Board 前校验两者，缺一即报配置错误（不自动补旗标）；河段重试不调用 StartChapter/EnterStage(0)，旗标与种子不变。`GameEventType` 末尾新增 `CreditsConfirmed`（不改旧顺序）。

**演出与装配**：新增 `Scenes/SelfCrossingCast.cs`（一个 cast 同时服务两个 PR：回忆按 `DialogueRunner.LineShown` 行号切图；轮回显示 CG05，末句后隐藏 CG，让船与渡客同步平移远去并叠加暖色层，全部用 Time.deltaTime、暂停冻结、退出即销毁）。`DialogueRunner` 新增只读视觉事件 `LineShown`。`EnsureUnderworldCh05`（CH05_BOAT + Recall cast，Underworld 只显示渡客）与 `EnsureRiverCh05`（Reincarnation cast）。两次装配 objects=1/casts=1 与 casts=1，第二轮全部 already complete。

**制作名单**：新增 `UI/CreditsPanel.cs`（Show 时捕获 runId 与 CreditsConfirmed 事件；确认按钮先过 run/stage/Paused/Transitioning/sent 守卫，再 HideWithoutEvent + PublishEvent；双击只发一次；只写真实制作信息，不编造人员）。`GameSession.Credits` + `GameBootstrap.BuildUi` 创建；`ChapterDirector.FinishEnterStage` 在 CH05_Complete 显示；`PauseMenu` 离开与选章清理时调用 `HideWithoutEvent()`。

**河段收尾**：`RiverController.OnSucceeded` 清空漩涡、关闭说明/失败面板（成功后不再换场景，Phase=Succeeded 自然锁横移）；乘客数组按 `passengerCount=0` 生成空数组，不造虚拟亡魂。

**验证**：`DZ_VALIDATION=PASS`、`DZ_SMOKE=PASS`（CH05 结构/6 目标/3 段行数/2 PR/0 乘客/选章）、`DZ_MINIGAMES=PASS`、`DZ_RIVER_SEEDS=PASS`（64 种子）、`DZ_CH04_PLAY=PASS`（回归；菜单现在正确续到 CH05）、新增 `DZ_CH05_PLAY=PASS`（真实 Play：回忆四次切图与行号对应、第 013 句清屏、旗标、上船门禁、零乘客、CG05→船远去、名单双击只发一次、ChapterReplay 不写档）。候选构建 `Builds/Candidates/20261030_012825_fb762981/Duzhe.exe`（121,029,996 字节，errors=0）；构建版启动正常，临时 `-dzprobe` 确认 Underworld（54 子物体）/River（7）/MainMenu 均正常加载，探针已删除并重建最终候选。

**未测/缺项（如实）**：实机全流程与暂停中途退出待用户试玩；渡河 10 秒航程由种子验证器覆盖，Play 测试直接投递成功事件验证结尾；四个身份到渡客的"短叠化"用现有插画默认实现；音频未接线（C10）；完成后仍回主菜单，回忆馆/目录路由属 C09。

## C08 补丁：主线连续进章（2026-10-30，用户反馈"第一章通关后又回开始界面"）

`ChapterDirector.CompleteChapter()` 原实现无论模式都 `LoadScene(MainMenu)`（连续转场原计划留给 C09），导致每章打完就回主菜单。新增 `ChapterCatalog.GetNextFormalChapterId(id)`（按 FormalChapterIds 取下一个**已注册**章节，CH05 之后为空），`CompleteChapter` 在 `MainRun` **与** `ChapterReplay` 下直接 `StartChapter(next, 同模式)`（只清任务物图标，不弹"已完成"提示），仅最后一章或 `PresentationReplay` 回主菜单；写盘仍只在 MainRun。新增 Play 验证 `DZ_CHAPTER_FLOW=PASS`：CH01→CH02→CH03→CH04→CH05 全在 MainRun 连续进入、CH05 完成回主菜单；章节回放也连续进入 CH02（replay 模式、不写档）。测试期间存档写入重定向到临时目录，真实存档未被改动。新候选 `Builds/Candidates/20261030_020530_ead42e02/Duzhe.exe`（121,030,508 字节，errors=0），启动正常；DZ_SMOKE 复跑 PASS。C09 仍需补：章节目录/回忆馆界面与"回放后回目录"的正式路由。

## C09 交付记录（2026-10-30，主菜单/章节目录/回忆馆/运行重置）

**运行边界**：`GameSession.BeginNewRun(mode)`（先换 RunId 让旧回调失效，再关暂停/机关/演出/对白/积分名单/淡出、清 VisualChapterId、广播 RunReset、设模式与 Transition 输入）与 `ReturnToMenu(tab)`（记录 PendingMenuTab 后回主菜单）。`ChapterDirector.StartChapter` 先校验 cfg/Transitions/加载锁，再 BeginNewRun，之后才建 runtime/种子/章节与 VisualChapterId；河段重试不调用。

**路由**：`CompleteChapter` 主线按 FormalChapterIds 继续下一章（缺章显式报错不跳过），最后一章回首页；`ChapterReplay` 结束回**章节目录**（PendingMenuTab=Chapters）且不写档；`PresentationReplay` 回**回忆列表**。`SceneTransitionService` 的 Ready 改到「落位 + IsTransitioning=false + 基础输入态」之后只发一次，OnSceneLoaded 只更新 CurrentSceneId（非受控加载才自行通知），OnDestroy 退订两个 SceneManager 事件；`ChapterDirector.OnSceneReady` 校验 pending 阶段的 sceneId。

**显隐**：`ChapterObjectVisibility` 改为读 `GameSession.VisualChapterId` + 显式 `RefreshVisibility()` 扫描（含 inactive），新增 `visibleOnChapterEntry`（装配写入，幽灵等后出现角色为 false）；不再订阅 ChapterChanged、不再从 activeSelf 猜默认值。`SetVisualChapter` 在 StartChapter、场景 Ready、回忆馆进入时调用。

**演出上下文**：`PresentationPlayer` 捕获 contextChapterId/contextStageId/contextMode；HandleEvent 核对 run+chapter+segment；完成事件用捕获上下文发布；Aborted 核对 Mode；ForceAbort 清 waitingSegment；新增 `PlaybackFailed` 供回忆馆回列表。`DialogueRunner` 捕获 runId，换 run 时不发 DialogueFinished；缺 view 不再假装读完。

**UI**：`MainMenuPanel` 四页（首页/章节/回忆/设置，含音量滑块与「章节内进度不保存」提示）；`ChapterMenuPanel`（暂停菜单「章节」按钮，仅列已完成章）；`GalleryController`（6 条白名单 × Save 解锁交集，BeginNewRun(PresentationReplay) → 记录 pending → 场景 Ready 后 Play 一次 → 完成/退出回回忆列表，双击与重复 Ready 均不重放）；GameBootstrap 注册两者。

**验证**：`DZ_VALIDATION` / `DZ_SMOKE` / `DZ_MINIGAMES` / `DZ_RIVER_SEEDS` 全 PASS；新增 `DZ_MENU_GALLERY=PASS`（章节页 5/5、回忆页 3/3、回忆回放进入 PresentationReplay、加载对应场景、CG 显示、中途退出回回忆列表且解锁/通关集合不变）；`DZ_CHAPTER_FLOW=PASS`（主线 CH01→…→CH05→首页；章回放回目录且不写档）；`DZ_CH04_PLAY` / `DZ_CH05_PLAY` 复跑 PASS。候选 `Builds/Candidates/20261030_041332_3e51cab3/Duzhe.exe`（121,047,056 字节，errors=0），启动正常，`-dzprobe` 确认 MainMenu/Underworld/FarmCourtyard 加载正常（探针已删）。

**未测/缺项（如实）**：实机逐条回忆播放/暂停/退出待用户；新档（无任何通关）样本未在本次 Play 中重跑（章节页/回忆页空态逻辑为同一代码路径）；音频仍未接线（C10）；DEV 入口与诊断日志待 C11 清理。

## C10 交付记录（2026-10-30，R15/R16/R17/R19 音频接入与音频框架修复）

**导入**：`EnsureAudioImports`（新增）把 24 个 WAV 复制到 `Resources/Audio/{Notes,SFX,Music}`，`ConfigureAudioImporter` 统一 Vorbis/DecompressOnLoad/预加载；`CopyIfNeeded` 原地覆盖保留 `.meta`，GUID 不变。清单见 `Docs/ProductionLogs/ResourceIntegration.md`。

**框架修复（AudioService）**：① 去掉音量二次相乘（`PlayOneShot(clip, 1f)`，bus 增益只在 `source.volume`）；② 新增 `musicDuck`，七音演奏时音乐 ×0.25；③ `SetGameplayPaused` 同时暂停/恢复音乐、音效与循环源；④ 新增 `PlayGameplayClip`、`PlaySfxLoop/StopSfxLoop`（循环源，不新建管理器）、`PlayMusicForScene`；⑤ `PlayMusic` 换曲先 Stop 再赋 clip，暂停中换曲恢复后能继续；缺失资源重试不再被旧 id 提前 return 挡住。`NoteSequenceMinigame` 不再自建 AudioSource，改走共享音效总线，并在打开/关闭时 `SetNotesActive`。

**触发点**：拾取 SFX03 / 交还 SFX04 / 仪式 SFX05 / 撞漩涡 SFX07 / 敲门 SFX11 / 机关完成 SFX12 / 快门 SFX08（C06 已有）/ 下课铃 SFX10（C07 已有）/ 描画 SFX09 循环 / 划船 SFX06 循环（开航、重试开始；靠岸、失败、离开、OnDestroy 停止）/ 脚步 SFX02 每 0.32s（仅实际移动）/ 界面确认 SFX01；BGM 按场景：阴间与冥河 BGM01、四个相会场景 BGM02、CH05 BGM03，主菜单停止。

**验证**：`DZ_AUDIO=PASS`（`Logs/audio_playtest.txt`：单次增益 0.5、主音量 0 静音、七音压低 0.125、暂停停音乐且不播新音效、恢复继续、单发与循环开停）；`DZ_SMOKE=PASS`（新增 8 音符 + 13 SFX + 3 BGM 加载断言）；`DZ_MENU_GALLERY` / `DZ_CHAPTER_FLOW` 回归 PASS；构建 `Builds/Candidates/20261030_122934_e524a336/Duzhe.exe`（123,058,448 字节，errors=0），启动正常。

**未测/缺项（如实）**：全部听感（爆音/循环接缝/音量平衡）、7 音演奏听感、快门与铃声实机听感需用户试听；MEL01 示范与 SFX13 狗叫已导入未接入；美术缺 PRP03 门环面板、PRP04/05 道具面板、R03 UI 皮肤，R12 配角行走表已交付未接入（NPC 仍单帧站姿）。

## C10 补批：R03 UI 皮肤/图标/字体 + PRP03/04/05（2026-10-30 13:10）

**更正**：此前 ResourceIntegration.md 把 PRP03 记为"未交付"、PRP04/05 记为"未导入"是错的——三张图都在 R13 包内（PRP03 在 R13_Panels，PRP04/05 在 R13_Props），已更正登记。

**导入**：PRP03_DoorCloseup（背景导入器）、PRP04_Stool / PRP05_DiningTable（道具导入器，maxSize 128，底部中心 pivot）；R03 三张九切片皮肤（按钮 8px、面板/对白 12px 边）与 5 个图标（中心 pivot）到 `Resources/Art/UI/`；Noto Sans CJK SC 到 `Resources/Fonts/`（导入未启用）。

**接入**：`DuzheUiFactory.AddPanel/AddButton` 在有皮肤时改用九切片（颜色只保留 alpha）；`DialogueView` 对白底用 UI01_DialoguePanel；`DoorKnockMinigame` 用 PRP03 底图 + 导出几何门环热点；`DragPlacementMinigame` 与 `EnsureFarmCourtyardCh01` 用 PRP04/PRP05；任务物图标兜底用 UI02_Item；`r03.trace.progress` 接入描画状态行（另两键已合并未接入）。

**验证**：DZ_VALIDATION / DZ_SMOKE / DZ_CH03_PLAY / DZ_CH04_PLAY 全 PASS；候选 `Builds/Candidates/20261030_130853_5e998df5/Duzhe.exe`（140,525,448 字节，errors=0），启动正常。**未测**：九切片在 1280×720 与 1920×1080 的拉伸边缘、按钮文字对比、门环在底图上的对位、世界桌椅观感——需用户实机确认。

## C11 交付记录（2026-10-30，回归与 Windows 提交包）

**发布清理**：暂停菜单 DEV 区（完成当前章/清空存档/选择章节/机关测试）全部包进 `#if UNITY_EDITOR || DEVELOPMENT_BUILD`；`MinigameHost.OpenTester` 与 `MinigameTesterPanel` 同类处理（正式 `Open` 不受影响）；`GameBootstrap` 的 `DZ_ClickProbe`、`TraceMinigame` 的 TRACE 日志、`DragPlacementMinigame` 的 SNAP FAIL、场景 Ready / Gallery 诊断日志均只在开发版输出；主菜单仍只列正式 CH01–CH05。发行构建启动日志已确认无 CLICK/TRACE/SNAP/Scene ready 残留。

**构建流程加固**：`DuzheBuildTools.Build` 改为先 `ProjectSetupUtility.RunAll` 并检查返回值（失败不构建），校验 MainMenu + 6 场景存在且已注册，`BuildPipeline` 异常被捕获，每次构建写 `Logs/Release/<buildId>/build_report.txt`，只有 `Succeeded + totalErrors=0 + exe 存在` 才记成功；输出仍是新候选目录，不删旧版本。

**最终验证（本轮真实执行）**：`DZ_VALIDATION`（两次幂等）、`DZ_SMOKE`（五章结构 + 美术 + 音频）、`DZ_MINIGAMES`、`DZ_RIVER_SEEDS`（64 种子）全 PASS；此前同代码基线下的 `DZ_AUDIO` / `DZ_MENU_GALLERY` / `DZ_CHAPTER_FLOW` / `DZ_CH03_PLAY` / `DZ_CH04_PLAY` / `DZ_CH05_PLAY` 均 PASS（C10 与 C09 记录）。构建 `Builds/Candidates/20261030_134744_dff9fcb8/Duzhe.exe`（140,523,400 字节，errors=0），启动进主菜单无异常；打包副本在另一目录启动同样正常（不依赖工程绝对路径）。

**提交包**：`Builds/Duzhe_Demo_20261030_134744.zip`（60.6 MB，根目录 `Duzhe_Demo/`，含 exe、Duzhe_Data、MonoBleedingEdge、引擎 DLL、README.txt、ReleaseChecklist.md）。清单见 `Docs/ProductionLogs/ReleaseChecklist.md`。

**未测（如实）**：构建版逐关人工通关、暂停/失焦/返回菜单逐状态、听感（音量平衡/循环接缝）、1920×1080 下的九切片与文字、复制到别处启动后的完整流程；音频素材版权未核实，不以“已授权”表述。
