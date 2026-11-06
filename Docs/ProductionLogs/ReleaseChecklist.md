# ReleaseChecklist.md — 《渡者》提交前检查（2026-10-30）

构建：`Builds/Candidates/20261030_134744_dff9fcb8/Duzhe.exe`
大小：140,523,400 字节 · 引擎：团结 1.10.4（2022.3.62t16）· Windows x64 · Mono · 内置 2D
构建报告：`Logs/Release/20261030_134744_dff9fcb8/build_report.txt`

## 一、测试安全（C11 检查点 1，已在 C06 完成并本轮复核）
- [x] `SaveService` 的测试目录覆盖仅存在于编辑器编译（`#if UNITY_EDITOR`），发行版无该字段。
- [x] `DevSmokeTest.Run` 整段包裹 `Temp/DuzheSmoke/<guid>`，`finally` 还原；坏档测试也落在覆盖目录。
- [x] 本轮所有自动验证均未改写用户真实存档（运行前后 `duzhe_save.json` 内容一致）。

## 二、构建流程（检查点 2）
- [x] 构建前调用 `ProjectSetupUtility.RunAll` 并检查返回值，失败则不开始构建。
- [x] 场景清单校验：MainMenu + 6 个场景均存在且已注册。
- [x] 输出到新目录 `Builds/Candidates/<yyyyMMdd_HHmmss>_<guid8>`，不删除任何旧版本。
- [x] `BuildPipeline` 异常被捕获并写入报告；每次构建写 `Logs/Release/<buildId>/build_report.txt`。
- [x] 只有 `Succeeded + totalErrors=0 + exe 存在` 才记为成功。

## 三、自动验证（检查点 3，本轮真实执行）
| 项 | 结果 | 证据 |
|---|---|---|
| `DZ_VALIDATION` | PASS | `Logs/duzhe_validation.txt`（两次运行均 `already complete`，幂等） |
| `DZ_SMOKE` | PASS | `Logs/duzhe_smoke.txt`（CH01–CH05 结构/目标/对白/演出/河段规则/美术/音频；存档隔离） |
| `DZ_MINIGAMES` | PASS | `Logs/minigames_report.txt`（三模板判定矩阵、正式 16 音、MG03/MG06 完成规则） |
| `DZ_RIVER_SEEDS` | PASS | `Logs/river_seeds_report.txt`（64 种子，双规则组） |
| `DZ_AUDIO` | PASS | `Logs/audio_playtest.txt`（增益单次、0 静音、七音压低、暂停恢复、循环开停） |
| `DZ_MENU_GALLERY` | PASS | `Logs/menu_gallery_playtest.txt`（菜单四页、章节/回忆列表、回忆回放路由） |
| `DZ_CHAPTER_FLOW` | PASS | `Logs/chapter_flow_playtest.txt`（主线连续、回放回目录不写档） |
| `DZ_CH03_PLAY` / `DZ_CH04_PLAY` / `DZ_CH05_PLAY` | PASS | 各章机关、演出、字幕层、制作名单 |
| 构建版启动 | PASS | `Player.log`：Startup sceneId=MainMenu，无异常，无诊断日志残留 |

## 四、发布清理（检查点 3）
- [x] 暂停菜单的 DEV 区（完成当前章/清空存档/选择章节/机关测试）包在 `#if UNITY_EDITOR || DEVELOPMENT_BUILD`。
- [x] `MinigameHost.OpenTester` 与 `MinigameTesterPanel` 同类编译条件；正式机关入口（`Open`）不受影响。
- [x] `GameBootstrap` 的 `DZ_ClickProbe`、`TraceMinigame` 的 TRACE 日志、`DragPlacementMinigame` 的 SNAP FAIL、场景 Ready / Gallery 诊断日志仅开发版输出。
- [x] 主菜单只列正式 CH01–CH05（`FormalChapterIds`），DEV 章不进入可见目录。
- [x] 发行构建中无 `-dzprobe`/`SaveProbe` 等临时探针（源码已删除）。

## 五、构建版逐关回归（检查点 4）
| # | 项目 | 状态 | 说明 |
|---|---|---|---|
| 1 | 菜单→CH01–CH05 连续通关 | **未测（需人工）** | 自动化以 `DZ_CHAPTER_FLOW` 覆盖章节流转；实际按键通关需人工 |
| 2 | 各河段失败一次、CH03/CH04 连续失败 3 次、CH05 零乘客 | **部分** | 种子与失败重试由 `DZ_RIVER_SEEDS` 覆盖；实际手感需人工 |
| 3 | 七种机关阻止错误输入、MG05/MG06/CH04 顺序 | **部分** | `DZ_MINIGAMES` + `DZ_CH03/04_PLAY` 覆盖规则与真实拖放/描画；人工复验建议 |
| 4 | 暂停/失焦/返回菜单无残留 | **未测（需人工）** | 代码路径与清理已实现（C09 起），未逐状态实测 |
| 5 | 章回放与回忆回看不改通关/解锁 | **PASS** | `DZ_MENU_GALLERY` / `DZ_CHAPTER_FLOW` |
| 6 | 音频可听、七音正确、0 静音、循环无断裂、字体可读 | **未测（需人工听感/看图）** | 数值与加载由 `DZ_AUDIO` 覆盖；听感未测 |
| 7 | 重启恢复通关与音量、脱离工程路径启动 | **部分** | 存档/音量读写有断言；"复制到别处启动"未测 |

## 六、打包
- 交付目录：`Builds/Candidates/20261030_134744_dff9fcb8/`（含 `Duzhe.exe`、`Duzhe_Data/`、`MonoBleedingEdge/`、`TuanjiePlayer.dll`、`README.txt`）。
- 压缩包：`Builds/Duzhe_Demo_20261030_134744.zip`（根目录为 `Duzhe_Demo/`）。
- 不包含：工程源码、`Library/`、`Temp/`、`SourceAssets/`、个人绝对路径日志。

## 七、如实声明
- 音频素材来源为资源方 AI 生成/原创合成，**版权与授权未由我方核实**，不以"已授权"表述。
- 美术/UI 缺项：配角行走表未接入（单帧站姿）、MG01/03/04 面板无正式特写底图、R03 图标仅 Item 使用、Noto 字体已导入未启用。
- 本文件中的"PASS"仅指对应自动验证，**不代表用户试玩验收**。
