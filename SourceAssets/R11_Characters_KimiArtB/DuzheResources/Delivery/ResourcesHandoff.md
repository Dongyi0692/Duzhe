# 《渡者》R11 七位配角资源交接

批次：R11_Characters_KimiArtB_20261027_v1.0  
范围：Kimi 美术 B 负责的 7 位配角独立正式站姿。  
风格基线：R02-Approved-1.3，已实际读取并引用 `VisualGuide.png`。

## 已交正式文件

| assetId | actorId | 章节 | 正式文件 | 实际规格 | 状态 |
|---|---|---|---|---|---|
| CHR02_Grandfather | Grandfather | CH01 | `Characters/CHR02_Grandfather.png` | 32×48 RGBA 透明 PNG | Reviewed |
| CHR03_Grandmother | Grandmother | CH01 | `Characters/CHR03_Grandmother.png` | 32×48 RGBA 透明 PNG | Reviewed |
| CHR05_LoverSpirit | LoverSpirit | CH02 | `Characters/CHR05_LoverSpirit.png` | 32×48 RGBA 透明 PNG | Reviewed |
| CHR07_DrummerSpirit | DrummerSpirit | CH03 | `Characters/CHR07_DrummerSpirit.png` | 32×48 RGBA 透明 PNG | Reviewed |
| CHR09_BandmateA | BandmateA | CH03 | `Characters/CHR09_BandmateA.png` | 32×48 RGBA 透明 PNG | Reviewed |
| CHR10_BandmateB | BandmateB | CH03 | `Characters/CHR10_BandmateB.png` | 32×48 RGBA 透明 PNG | Reviewed |
| CHR11_TeacherSpirit | TeacherSpirit | CH04 | `Characters/CHR11_TeacherSpirit.png` | 32×48 RGBA 透明 PNG | Reviewed |

7 项均为独立主站姿，不是拼贴，不是四向行走表；无占位图。每项均有 1024×1536 RGBA 原始母图、处理记录和放大预览，位于 `SourceAssets/Characters/`。

## 随附文件

- `Delivery/AssetDelivery.json`：7 项正式资源的 ID、路径、尺寸、引用、状态与 SHA-256。
- `Delivery/CharacterAppearance_Kimi.md`：发型、服装、色板、体形、标志物、脚底定位和阶段限制。
- `Delivery/VisualQA_R11.md`、`Delivery/VisualQA_R11.json`：逐角色技术与视觉检查。
- `SourceAssets/Characters/R11_Characters_Preview.png`：7 人独立帧对照预览。
- `SourceAssets/Characters/R11_AlphaContrast_QA.png`：深色/浅色背景透明与轮廓对照。
- `tools/process_sprite.py`、`tools/finalize_r11.py`：可复现归一化与验收脚本（交付压缩包内的 `BuildTools/`）。

## 技术检查结果

- 7/7 正式帧均为 32×48、RGBA，Alpha 仅含真实透明 0 与不透明 255。
- 7/7 主体位于画布内，无头顶、衣摆或鞋底被裁切；主体底缘对齐画布底部。
- 7/7 仅保留一个连通主体，已清理孤立杂点；脚底中心相对画布中心偏移在验收阈值内。
- 7/7 使用无抖动量化，最终不透明颜色数不超过 24；未使用棋盘格冒充透明。
- 已逐张查看母图、最终放大预览和深浅底对照；老人、年轻成人与教师的年龄/服装可区分。
- 基础站姿未固定携带拐杖、眼镜、项链、鼓槌、乐器、麦克风或教案。

## Codely 导入要求

1. 将 `Characters/` 中 7 张 PNG 接入既定 `Assets/_Project` 角色路径；替换已有同名资源时保留 `.meta`，不要通过中文显示名猜引用。
2. 使用 32 PPU、Point 过滤、关闭 Mip Map、无损或不压缩，启用透明 Alpha。
3. 单帧无需切片；`frameLayout=null`。Sprite pivot 设为 `(0.5,0)`，保持 32×48 原画布，不要再自动紧裁。
4. 按 actorId/assetId 接入对应章节场景。人物阴影由程序统一处理；本批 PNG 不包含地面阴影。
5. 在实际 Unity 版本检查透明边缘、脚底位置、场景缩放和排序层。当前状态不是 Imported/Verified，尚未实机运行验收。

## 范围、缺项与后续触发点

- CHR01_Ferryman、CHR04_PastSelfFamily、CHR06_PastSelfRomance、CHR08_PastSelfBand、CHR12_PastSelfSchool 由资源助手负责，不在本包；本交接不宣称 R11 全 12 人已经全部完成。
- R12 四向行走表尚未在本批制作。CHR02、CHR03、CHR05、CHR07、CHR11 可分别基于对应正式帧继续 R12；不得复制同一帧冒充步态。
- R14 使用本批角色时应实际读取对应母图/正式帧；乐队合照中的 CHR08 仍由资源助手提供，渡客不得入乐队合照。
- 不需要为接入本批资源修改关卡流程、剧情、角色 ID 或交互规则。
