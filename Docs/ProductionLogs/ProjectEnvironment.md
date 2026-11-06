# ProjectEnvironment.md — 《渡者》工程环境记录

> 检查点 C01 盘点产物。本文件记录实际读取到的工程环境，作为后续任务的基线；版本基线冻结，不随任务随意变更。

- 更新时间：2026-10-26
- 记录人：Codely（自动化盘点）

## 工程路径与标识

| 项 | 值 |
|---|---|
| 工程根目录 | `C:\Users\東壹\Duzhe` |
| projectName | `Duzhe`（模板克隆自动生成，companyName 为 DefaultCompany 占位） |
| 创建方式 | 编辑器命令行 `-createproject -cloneFromTemplate cn.tuanjie.template.2d-7.0.4.tgz`（Hub CLI `projects create` 因 IPC 管道被 Cowork 常驻 Hub 占用而 CREATE_FAILED，改用与 Hub 完全一致的编辑器命令行路径创建，模板与版本与此前各工程一致） |

## 引擎版本（冻结基线）

| 项 | 值 |
|---|---|
| 团结引擎语义版本 | 1.10.4 |
| 编辑器内部版本 | 2022.3.62t16 |
| 版本修订 | ab17684acf1d（`ProjectVersion.txt` 实读） |
| 编辑器路径 | `C:\Program Files\Tuanjie\Hub\Editor\2022.3.62t16\Editor\Tuanjie.exe` |
| 项目模板 | cn.tuanjie.template.2d 7.0.4（2D 内置渲染管线模板） |
| Codely 版本 | 未知（CLI 运行环境未暴露自身版本号，如实标注） |

## 输入与渲染方案（全工程统一）

| 项 | 值 | 依据 |
|---|---|---|
| 输入方案 | 旧版 Input Manager（`UnityEngine.Input`：GetAxisRaw / GetKeyDown / GetMouseButtonDown） | `ProjectSettings.asset` 中 `activeInputHandler: 0`（仅旧版；无 InputSystem 包，模板 manifest 实读无 com.unity.inputsystem） |
| 渲染方案 | 内置渲染管线（Built-in）2D 正交相机 | 模板为 2D 内置管线模板；manifest 无 URP/HDRP 包 |
| 2D 物理 | Physics2D，角色 gravityScale=0（运行时由 PlayerController2D 设置） | 规范：不用重力 |
| 参考分辨率 | 视口 640×360 @ 32 PPU（相机正交半高 5.625 世界单位）；窗口默认 1280×720（模板原为 1920×1080，由 ProjectSetupUtility 改为 1280×720）；UI Canvas 参考尺寸 1280×720 | TechnicalAndInteractionSpec §1 |

## 关键包依赖（Packages/manifest.json 实读）

- com.unity.feature.2d 2.0.1（2D 功能集）
- com.unity.modules.physics2d、com.unity.modules.tilemap、com.unity.modules.particlesystem
- com.unity.ugui 1.0.0、com.unity.textmeshpro 3.0.9
- com.unity.test-framework 1.1.33
- 其余为引擎基础模块（audio/ui/uielements 等）；无 URP、无 InputSystem、无付费依赖

## 构建目标与模块状态（实读 `tuanjie install-modules 1.10.4 --list`）

- 目标平台：Windows x64（StandaloneWindows64）
- 模块状态：所有可选模块均未安装（windows-il2cpp 未安装）
- 结论：Windows x64 构建使用编辑器内建 Mono 脚本后端先行尝试；若构建报缺模块，需要的用户动作是运行 `tuanjie install-modules 1.10.4 --module windows-il2cpp`（或按实际报错模块补装）。首次构建结果见 Progress.md 与 CodelyHandoff.md，真实记录不粉饰。

## 目录约定（正式资源导入根 = Assets/_Project）

```
C:\Users\東壹\Duzhe\
├── Assets\_Project\
│   ├── Scenes\        # MainMenu + 六环境（.unity）
│   ├── Scripts\       # Core/Player/Interaction/Dialogue/Chapters/UI/Audio/Editor
│   ├── Data\          # Chapters/Dialogue/Minigames/Presentations/Audio（JSON 交付 + 导入产物）
│   ├── Art\           # Characters/Backgrounds/Props/Illustrations/Effects/UI
│   ├── Audio\         # Music/Notes/SFX
│   └── Prefabs\       # 后续任务按需
├── Docs\
│   ├── Spec\          # （资源方交付后存档）
│   ├── ProductionLogs\# ProjectEnvironment.md / Progress.md
│   └── Acceptance\    # 验收记录
├── SourceAssets\      # AI 原始输出与中间大图（资源方工作区，不进 Assets）
├── Builds\            # Windows 构建输出，不放回 Assets
└── Logs\              # 批处理校验/构建报告
```

## 场景注册约定

| sceneId | 场景文件 | 用途 |
|---|---|---|
| MainMenu | `Assets/_Project/Scenes/MainMenu.unity` | 主菜单（最小版，完整菜单属 C09） |
| Underworld | `Assets/_Project/Scenes/Underworld.unity` | 阴间（与冥河共用渡口；CH01 占位探索在此） |
| River | `Assets/_Project/Scenes/River.unity` | 冥河（C01 仅骨架，渡河玩法属 C02） |
| FarmCourtyard | `Assets/_Project/Scenes/FarmCourtyard.unity` | 农家院（C01 仅骨架） |
| Park | `Assets/_Project/Scenes/Park.unity` | 公园（C01 仅骨架） |
| RecordingStudio | `Assets/_Project/Scenes/RecordingStudio.unity` | 录音室（C01 仅骨架） |
| Classroom | `Assets/_Project/Scenes/Classroom.unity` | 教室（C01 仅骨架） |

## 碰撞层与渲染层约定

- Physics2D User Layers：8=Player、9=WorldSolid、10=Interactable、11=Trigger、12=Boat、13=RiverHazard（ProjectSetupUtility 写入 TagManager，重复执行不重写）
- 碰撞组合在运行时由 GameBootstrap 通过 `Physics2D.IgnoreLayerCollision` 配置一次：Player↔WorldSolid、Player↔Interactable（trigger 提示）、Player↔Trigger（自动对话/区域）保留，其余按需关闭
- SortingLayers：Background、GroundProps、Actors、Foreground、Effects（UI 走 Canvas，不占 sorting layer）

## 版本冻结声明

以上引擎/模板/输入/渲染基线自本文件写入后冻结；后续任务不得升级引擎或引入新的付费依赖。变更仅允许由用户明确指示并同步更新本文件。

## 任务更新记录

- **2026-10-28（C04）**：环境零变更（引擎/模板/输入/渲染/构建目标均未动）。新增运行时数据导入约定：正式交付 JSON（TXT_CHxx / TXT_UI / PresentationMap / CharacterDisplayNames）由 `ProjectSetupUtility.EnsureDialogueImports` 从 `Assets/_Project/Data/Dialogue/` 幂等复制到 `Assets/_Project/Resources/Data/Dialogue/`（Resources 为运行时唯一读取路径）；CH01 场景装配坐标遵循 R04 布局换算 `worldX=(px-640)/32, worldY=(360-py)/32`（Underworld 沿用已验收位置+新增拐杖点，FarmCourtyard 按 FC_* 全量装配）。CH01 阶段覆盖表见 `StageCoverage.md`。
