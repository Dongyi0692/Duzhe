# ResourceIntegration.md — 正式资源接入登记

> 每次资源批次接入后更新。**"放在 SourceAssets" ≠ "已导入" ≠ "已接入运行时" ≠ "已试听/已试玩"**，四者分开记录。
> 路径约定：运行时资源一律在 `Assets/_Project/Resources/`，`Resources.Load` 使用不含前缀/扩展名的 key。

## 批次：R15 / R16 / R17 / R19 音频（2026-10-30，C10）

来源：`SourceAssets/R15_R19_Audio_20261027/Duzhe_Audio_20261027/`（44.1 kHz / 16-bit WAV）

| assetId | 来源 | Assets 目标 | 运行时 key | 使用触发点 | 状态 |
|---|---|---|---|---|---|
| N01_Do … N07_Si | R15_Notes/*.wav | Resources/Audio/Notes/ | Audio/Notes/N0x_* | MG05 七音按键/数字键（AudioService.PlayGameplayClip） | **已导入 + 已接入**；听感待用户 |
| MEL01_BandPhrase | R15_Notes | 同上 | Audio/Notes/MEL01_BandPhrase | 仅登记（示范音频，按用户要求不做播放按钮） | 已导入；**未接入（按需求不接）** |
| SFX01_UIConfirm | R16_SFX | Resources/Audio/SFX/ | Audio/SFX/SFX01_UIConfirm | 主菜单开始、章节回放、回忆回放 | 已导入 + 已接入 |
| SFX02_Footstep | R16_SFX | 同上 | …/SFX02_Footstep | 玩家实际移动时每 0.32s 一次（停止/暂停即停） | 已导入 + 已接入 |
| SFX03_Pickup | R16_SFX | 同上 | …/SFX03_Pickup | 拾取任务物（InteractionController） | 已导入 + 已接入 |
| SFX04_ReturnItem | R16_SFX | 同上 | …/SFX04_ReturnItem | 向 NPC 交还物品 | 已导入 + 已接入 |
| SFX05_Ritual | R16_SFX | 同上 | …/SFX05_Ritual | 仪式开始（RitualController） | 已导入 + 已接入 |
| SFX06_Rowing | R16_SFX | 同上 | …/SFX06_Rowing | 渡河开始/重试→循环；靠岸/失败/离开即停 | 已导入 + 已接入（循环源） |
| SFX07_Whirlpool | R16_SFX | 同上 | …/SFX07_Whirlpool | 撞漩涡 | 已导入 + 已接入 |
| SFX08_Shutter | R16_SFX | 同上 | …/SFX08_Shutter | MG06 成功落位一次（RecordingStudioCast） | 已导入 + 已接入 |
| SFX09_Chalk | R16_SFX | 同上 | …/SFX09_Chalk | MG07 按住描画→循环；松手/暂停/关闭即停 | 已导入 + 已接入（循环源） |
| SFX10_SchoolBell | R16_SFX | 同上 | …/SFX10_SchoolBell | CH04 下课铃（主线开始 / 回看课堂后） | 已导入 + 已接入 |
| SFX11_Knock | R16_SFX | 同上 | …/SFX11_Knock | MG02 敲门 | 已导入 + 已接入 |
| SFX12_PuzzleSuccess | R16_SFX | 同上 | …/SFX12_PuzzleSuccess | 所有机关完成（MinigameHost） | 已导入 + 已接入 |
| SFX13_DogHappy | R19_Optional | 同上 | …/SFX13_DogHappy | 本版五章没有狗的场景/事件 | 已导入；**未接入（无触发点）** |
| BGM01_RiverMist | R17_Music | Resources/Audio/Music/ | Audio/Music/BGM01_RiverMist | 阴间/冥河（CH01–CH04）场景就绪 | 已导入 + 已接入 |
| BGM02_Reunion | R17_Music | 同上 | …/BGM02_Reunion | 农家院/公园/录音室/教室（CH01–CH04 相会） | 已导入 + 已接入 |
| BGM03_SelfCrossing | R17_Music | 同上 | …/BGM03_SelfCrossing | CH05 全部场景 | 已导入 + 已接入 |

**框架修复（C10）**：`AudioService` 去掉音量二次相乘（`PlayOneShot(clip, 1f)`，bus 增益只在 source.volume）、新增 `musicDuck`（七音演奏时音乐 ×0.25）、`SetGameplayPaused` 同时暂停音乐/音效/循环源、`PlayGameplayClip`、`PlaySfxLoop/StopSfxLoop`、`PlayMusicForScene`；`PlayMusic` 换曲先 Stop 再赋 clip 并处理暂停中换曲。`NoteSequenceMinigame` 不再自建 AudioSource，改走共享音效总线，打开/关闭时 `SetNotesActive`。

**验证证据**：`DZ_AUDIO=PASS`（`Logs/audio_playtest.txt`：音量单次相乘 0.5、主音量 0 真静音、七音压低 0.125、暂停/恢复、单发与循环）；`DZ_SMOKE` 音频加载断言（8 音符 + 13 SFX + 3 BGM）PASS；`DZ_MENU_GALLERY` / `DZ_CHAPTER_FLOW` 回归 PASS。
**未测**：实际听感（爆音、循环接缝、音量平衡）、7 音在演奏中的听感、CH04 铃声与 CH03 快门的实机听感——**需用户试听**。

## 批次：R03 UI + PRP03/PRP04/PRP05（2026-10-30，C10 补批）

来源：`SourceAssets/R03_UI_v1.0/UI/`、`SourceAssets/R13_Panels_v1.0/DuzheResources/Props/PRP03_DoorCloseup.png`、`SourceAssets/R13_Props_v1.0/DuzheResources/Props/PRP04_Stool.png`、`PRP05_DiningTable.png`。

| assetId | 来源 | Assets 目标 | 运行时 key | 使用触发点 | 状态 |
|---|---|---|---|---|---|
| PRP03_DoorCloseup | R13_Panels | Resources/Art/Props/ | Art/Props/PRP03_DoorCloseup | MG02 门环面板底图（640×360 板） | **已导入 + 已接入**（门环热点用导出几何 [356,176,48,48]） |
| PRP04_Stool | R13_Props | 同上 | Art/Props/PRP04_Stool | MG03 可拖凳子 + 农家院世界凳子 | **已导入 + 已接入** |
| PRP05_DiningTable | R13_Props | 同上 | Art/Props/PRP05_DiningTable | MG03 桌面标记 + 农家院世界餐桌 | **已导入 + 已接入** |
| UI01_Button | R03 Skins | Resources/Art/UI/ | Art/UI/UI01_Button | 全部代码构建按钮（九切片） | **已导入 + 已启用**（`UseR03Skins=true`，PPU 100 + border 8） |
| UI01_GenericPanel | R03 Skins | 同上 | Art/UI/UI01_GenericPanel | 全部代码构建面板/卡片（九切片） | **已导入 + 已启用**（PPU 100 + border 12） |
| UI01_DialoguePanel | R03 Skins | 同上 | Art/UI/UI01_DialoguePanel | 对白面板底（九切片） | **已导入 + 已启用**（PPU 100 + border 12） |
| UI02_Item | R03 Icons | 同上 | Art/UI/UI02_Item | 任务物图标兜底 | 已导入 + 已接入 |
| UI02_Continue / Interact / Pause / Volume | R03 Icons | 同上 | Art/UI/UI02_* | 现有 UI 无图标槽位（按钮用文字、HUD 用任务物图标） | 已导入；**未接入**（接入属于新增设计，未擅自改版式） |
| UI03_Font（NotoSansCJKsc-Regular.otf） | R03 Fonts | Resources/Fonts/ | Fonts/NotoSansCJKsc-Regular | —（按用户要求保留现有 simhei） | **已导入，未启用（按要求保留现字体）** |
| r03.trace.progress | R03 UI_TextAdditions | Data/Dialogue/TXT_UI.json | r03.trace.progress | 描画机关完成一笔后的状态行 | **已接入** |
| r03.notes.progress | 同上 | 同上 | r03.notes.progress | MG05 已用进度点显示 | 已合并；**未接入** |
| r03.notes.play_demo | 同上 | 同上 | r03.notes.play_demo | 按用户要求**不做示范按钮** | 已合并；**不接入（需求取消）** |

**接入方式**：`DuzheUiFactory` 的 `AddPanel/AddButton` 在有皮肤时改用九切片（`Image.Type.Sliced`，颜色只保留 alpha）；`DialogueView` 对白底单独用 UI01_DialoguePanel；`DoorKnockMinigame` 用 PRP03 底图 + 导出几何门环；`DragPlacementMinigame.BuildStools` 与 `EnsureFarmCourtyardCh01` 用 PRP04/PRP05（缺失回退占位）。

**2026-10-30 修正（皮肤重新启用）**：`UseR03Skins` 改回 `true`。此前"卡片边框与文字错位"的真因不是九切片缩放，而是**卡片面板把 `anchoredPosition.y` 写成了正数**——`AddPanel` 的 pivot/anchor 都是左上角，正 y 表示"向上"，卡片被画到画布顶部之外（暂停卡片 `+120` → 实际占 -120~360，下边框正好压在「音乐」那一行）。已按调用点改为负 y：暂停卡片/离开确认/DEV 选章、DEV 机关测试面板、渡河说明与重试卡片、门环面板提示与占位门。三张皮肤的 `.meta` 已把 `spritePixelsToUnits` 由 32 改为 100（UI 用画布参考 PPU），九切片边框按 1:1 绘制。

**验证**：`DZ_VALIDATION=PASS`、`DZ_SMOKE=PASS`、`DZ_CH03_PLAY=PASS`、`DZ_CH04_PLAY=PASS`；候选 `Builds/Candidates/20261030_130853_5e998df5/Duzhe.exe`（140,525,448 字节，errors=0）启动正常。
**未测**：九切片在 1280×720 / 1920×1080 的拉伸边缘、按钮/文字对比、门环在真实底图上的对位、世界桌椅观感——需用户实机确认。

## 批次：R12 配角行走表 + R06 冥河背景（2026-10-30，C10 补批二）

来源：`SourceAssets/R12_SupportingWalking/DuzheResources/Characters/Animations/`、`SourceAssets/R06_River_v1.0/Backgrounds/BG02_River.png`。

| assetId | 来源 | Assets 目标 | 运行时 key | 使用触发点 | 状态 |
|---|---|---|---|---|---|
| CHR02_Grandfather | R12 | Resources/Art/Animations/ | Art/Animations/CHR02_Grandfather | 阴间 CH01 老人幽灵、农家院老人 A | **代码已接入；未编译/未导入/未实测** |
| CHR03_Grandmother | R12 | 同上 | Art/Animations/CHR03_Grandmother | 阴间 CH01 奶奶幽灵、农家院老人 B | 同上 |
| CHR05_LoverSpirit | R12 | 同上 | Art/Animations/CHR05_LoverSpirit | 阴间 CH02 女孩幽灵、公园女孩幽灵 | 同上 |
| CHR07_DrummerSpirit | R12 | 同上 | Art/Animations/CHR07_DrummerSpirit | 阴间 CH03 鼓手幽灵、录音室鼓手幽灵 | 同上 |
| CHR11_TeacherSpirit | R12 | 同上 | Art/Animations/CHR11_TeacherSpirit | 阴间 CH04 老师幽灵 | 同上 |
| BG02_River | R06 | Resources/Art/Backgrounds/ | Art/Backgrounds/BG02_River | River 场景背景（替换占位图） | **代码已接入；未编译/未导入** |

**实现（代码完成，未经引擎编译）**：
- `ProjectSetupUtility.EnsureFormalArtImports`：新增 R12 五张图集导入（`ConfigureSheetImporter(path, report, assetId)`，帧名前缀改为对应 assetId，12 帧、3×4、pivot(0.5,0)、Point/无 mipmap/无损）；新增 `BG02_River` 导入并加入 `FormalBackgrounds`（River 场景用正式背景）。
- 新增 `Player/NpcSpriteAnimator.cs`：按世界坐标位移判定移动/朝向（Down/Left/Right/Up），6 fps 循环 WalkA→Idle→WalkB→Idle，静止保持 Idle 列；暂停冻结；不写颜色（不影响 SpiritEscort 的淡入淡出）；`Awake` 重新解析子物体 SpriteRenderer（运行时引用不依赖装配）。
- `FormalArt.LoadWalkFrames(assetId)`：按 rect 计算 [row*3+col]，不依赖 LoadAll 顺序。
- `ProjectSetupUtility.EnsureNpcWalk(go, assetId, ref changed)`：给幽灵挂 animator 并绑定 12 帧；缺图集时保留站姿（并移除失效 animator）。已接线：阴间 CH01 双老人、CH02 女孩、CH03 鼓手、CH04 老师幽灵；农家院双老人；公园女孩；录音室鼓手。
- 冒烟新增 `TestNpcWalkSheets`：5 张图集均能解析为 12 帧、`BG02_River` 存在且宽 1280。

**未完成（环境阻塞）**：本轮写完代码后，`run_shell_command` 持续返回 `EPERM: operation not permitted (uv_spawn powershell.exe)`，**无法启动编辑器**，因此以下步骤尚未执行：① `ProjectSetupUtility.RunFromCommandLine`（导入 R12/R06、重存场景）；② `DevSmokeTest.RunFromCommandLine`（新断言）；③ 构建与实机播放检查。**在此之前 R12/R06 只能算"代码完成"，不能算"已导入/已使用"**。

**恢复后应执行的命令**（工程根目录）：
1. `Tuanjie.exe -batchmode -quit -projectPath C:\Users\東壹\Duzhe -executeMethod Duzhe.EditorTools.ProjectSetupUtility.RunFromCommandLine -logFile Logs\c10c_import.log`
2. 检查 `Logs/duzhe_validation.txt` 为 `DZ_VALIDATION=PASS`，并确认 `Assets/_Project/Resources/Art/Animations/CHR02_Grandfather.png` 等 5 个文件已生成、`River.unity` 背景已换成 `BG02_River`。
3. `-executeMethod Duzhe.EditorTools.DevSmokeTest.RunFromCommandLine`（应出现 `OK   R12: 5 walk sheets sliced to 12 frames; R06 river background present`）。
4. 构建候选后实机检查：幽灵跟随时四向走路、停下回 Idle、脚底不漂移、河面背景无船/漩涡叠图。

## 其余资源的核对结论（2026-10-30）

| 资源 | 交付情况 | 结论 |
|---|---|---|
| R03 图标 UI02_Continue / Interact / Pause / Volume | 已导入 `Art/UI/` | **未接入**：现有 UI 没有图标槽位（按钮用文字、HUD 用任务物图标），接入属于新增设计，未擅自改动；UI02_Item 已作为任务物图标兜底 |
| R06 水面流动层 | **未交付**：R06 只有 `BG02_River.png` 静态背景（交接明确"未交独立水流层或前景层"） | 不实现滚动水纹（强行垂直循环会有明显接缝）；渡河表现仍为船横移 + 障碍下移 |
| R19 狗（SFX13_DogHappy） | 已导入 `Audio/SFX/` | **未接入**：本版五章没有狗的场景/事件，无触发点 |
| CH02 恋爱蒙太奇（PR_CH02_Memory） | **未交付**：PresentationMap.json 无该条目，也没有对应插画 | **未实现**（规范允许默认不制作）；不造空槽位、不放假按钮 |
| R03 字体 NotoSansCJKsc | 已导入 | 按用户要求**保持现有 simhei 字体**，不切换 |
| 七音示范播放按钮 | — | 按用户要求**不添加**；保留现有七音演奏玩法（MEL01 已导入但无播放入口） |

## 批次记录规则

1. 每批一行一个 assetId，写清来源、目标、运行时 key、触发点、状态（已导入/已接入/已试听）。
2. 同 ID 覆盖必须先确认版本，不能旧包覆盖新包；`CopyIfNeeded` 保留目标 `.meta`（GUID 不变）。
3. 场景引用（背景 SpriteRenderer）必须在 `EnsureScenes` 里替换，不能只改 `FormalArt`。

## Codex C12 接入（2026-10-30）
BG00_MainMenu、BG04_Park 修复版、PRP06_PhotoFrame 修复版从 SourceAssets/Codex_C12 导入并实际绑定。原始批次保留。CHR07 鼓手未改；新合照空位及尺寸见 MinigameConfig。来源及生成提示词摘要见 SourceAssets/Codex_C12/README.md。资源与运行规则检查通过，素材观感待用户试玩。


## 2026-10-31 石灯、狗与情侣蒙太奇已完成（取代此前未接入记录）
- 正式石灯 PRP08_StoneLantern 已导入，接入阴间 CH01–CH04。
- 田园犬 CHR13_Dog 已导入，放在农家院门旁；靠近招呼一次并触发 SFX13_DogHappy，不挡路，暂停/对白期间不触发。
- CG06/07/08 三张回忆及六句对白已交付和接入：画心 → 初遇/项链/共伞 → 原告别。
- PR_CH02_Memory“那些平常的日子”已加入回忆馆；已通关 CH02 的旧档可直接回看。主菜单与回忆馆均已兼容，不改旧通关记录。
- DZ_SMOKE=PASS；DZ_OPTIONAL_PLAY=PASS。运行检查覆盖切图、衔接、狗与暂停、旧存档入口和回放退出不写进度。
- 最终构建 20261031_000633_1eafff9f，Succeeded，errors=0。详细交接见 SourceAssets/Codex_Optional/ResourcesHandoff.md。
