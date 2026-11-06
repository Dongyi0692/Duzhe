# 《渡者》R12 五位配角行走动画交接

批次：R12_SupportingWalking_20261028_v1.0  
范围：Kimi 美术 B 负责的 5 位移动配角；基于已验收 R11 站姿继续制作。  
风格基线：R02-Approved-1.3。

## 已交正式图集

| assetId | actorId | 章节 | 正式文件 | 规格 | 状态 |
|---|---|---|---|---|---|
| CHR02_Grandfather | Grandfather | CH01 | `Characters/Animations/CHR02_Grandfather.png` | 96×192 RGBA，12 帧 | Reviewed |
| CHR03_Grandmother | Grandmother | CH01 | `Characters/Animations/CHR03_Grandmother.png` | 96×192 RGBA，12 帧 | Reviewed |
| CHR05_LoverSpirit | LoverSpirit | CH02 | `Characters/Animations/CHR05_LoverSpirit.png` | 96×192 RGBA，12 帧 | Reviewed |
| CHR07_DrummerSpirit | DrummerSpirit | CH03 | `Characters/Animations/CHR07_DrummerSpirit.png` | 96×192 RGBA，12 帧 | Reviewed |
| CHR11_TeacherSpirit | TeacherSpirit | CH04 | `Characters/Animations/CHR11_TeacherSpirit.png` | 96×192 RGBA，12 帧 | Reviewed |

统一图集规则：

- 3 列×4 行，单帧 32×48，整表 96×192，无间隔、无隐藏边框。
- 行序：`Down`、`Left`、`Right`、`Up`。
- 列序：`WalkA`、`Idle`、`WalkB`；Idle 列索引为 `1`。
- 默认 6 fps，三帧循环时长 0.5 秒；脚底 pivot=`(0.5,0)`，32 PPU。
- 每套 Down/Idle 均与对应 R11 正式站姿逐像素一致。
- 每套 12 帧均唯一，每个方向均有真实相反跨步，不是复制站姿或整体上下抖动。

## 原始分帧与预览

每个角色在 `SourceAssets/Animations/<assetId>/` 下均有：

- `<assetId>_walk_source.png`：2048×2048 原始生成图。
- `SourceFrames/`：清理后的高分辨率独立分帧，保留透明通道。
- `Frames/`：实际装表的 32×48 RGBA 独立分帧。
- `<assetId>_processing.json`：来源格、纠偏、尺寸、哈希与逐帧差异记录。
- `<assetId>_sheet_preview.png`：带方向/列名的放大静态检查图。
- `<assetId>_walk_preview.gif`：WalkA→Idle→WalkB→Idle 步态预览。

全员预览：

- `SourceAssets/Animations/R12_WalkSheets_Preview.png`：五套完整图集对照。
- `SourceAssets/Animations/R12_AllCharacters_Preview.gif`：五人四向步态循环，约 6 fps。

CHR02、CHR03、CHR07 原始图中的左右行 WalkB 朝向互换，装表前已交换到正确方向；CHR05、CHR11 无需此纠偏。原始图角落的非角色标记已在独立主体提取时排除，正式帧和装表分帧不包含该标记。

## QA 结果

- 5/5 图集、60/60 帧格通过尺寸、RGBA、真 Alpha、主体边界、单连通主体、底部基线和中心偏移检查。
- 5/5 图集均有 12 个唯一帧；四个方向的 WalkA/Idle/WalkB 均实际不同。
- 5/5 Down/Idle 与 R11 对应正式站姿完全一致。
- 已逐套查看放大静态图，并核对全员对照和 GIF 文件帧数。
- 详细记录见 `Delivery/R12/AnimationQA.md` 与 `Delivery/R12/AnimationQA.json`。
- 当前未在 Unity 工程中导入或实机播放验证，不能视为 Imported/Verified。

## Codely 导入与接入

1. 将 5 张图集接入既定 `Assets/_Project` 动画路径；同 assetId 替换旧资源时保留 `.meta`，不要通过中文名猜引用。
2. 使用 32 PPU、Point 过滤、关闭 Mip Map、无损或不压缩，启用透明 Alpha。
3. 按固定网格切片：3 列×4 行、每格 32×48、无 spacing；每帧 pivot=`(0.5,0)`，不要自动紧裁。
4. 动画默认 6 fps，使用 WalkA、Idle、WalkB 循环；Idle 使用列索引 1。方向映射严格按 Down/Left/Right/Up 行序。
5. 在实机检查八方向移动对四向资源的复用、斜向切换、脚底定位、排序层和帧速率；不得为适配图片修改关卡流程或角色 ID。

## 范围与缺项

- CHR01_Ferryman 由资源助手负责，不在本包；本交接不宣称 R12 全部六套均已完成。
- 其余 6 位静态角色不扩展动画，符合 R12 范围。
- 包内 5 张 R11 站姿及其原始母图仅作为复现参考保留，不属于本批新增交付项。
- `BuildTools/` 提供分帧装表与 QA 脚本；正式引擎导入、动画状态机和实机验收由 Codely 处理。
