# 《渡者》R12 主角行走动画交接 v1.0

批次：R12_MainCharacter_20261028_v1.0  
范围：使用新提供的 R11 `CHR01_Ferryman` 形象制作四向行走动画。

## 已交文件

- `Characters/Animations/CHR01_Ferryman.png`：96×192 RGBA 正式图集，共 12 个不同帧。
- `Characters/CHR01_Ferryman.png`：对应 R11 32×48 站姿，供核对与替换。
- `SourceAssets/Animations/CHR01_Ferryman/CHR01_Ferryman_walk_source.png`：2048×2048 原始动画源图。
- `SourceAssets/Animations/CHR01_Ferryman/Frames/`：12 张正式 32×48 RGBA 分帧。
- `SourceAssets/Animations/CHR01_Ferryman/SourceFrames/`：对应清理、裁切后的源帧。
- 同目录另附静态放大预览、四步循环 GIF 和处理报告；`SourceAssets/Characters/` 保留主角母图。
- `Delivery/R12_Main/` 内含 AssetDelivery、AnimationManifest、AnimationQA 和本说明。

本包没有占位图，也不包含此前五位配角的行走表；那些资源沿用各自已交付包。

## Codely 接入

1. 将正式图集接入 `Assets/_Project` 的既定动画路径，替换同名资源时保留 `.meta`。
2. Multiple 切片：3 列×4 行，每格 32×48；行序 Down、Left、Right、Up；列序 WalkA、Idle、WalkB。
3. 使用 32 PPU、Point 过滤、关闭 Mip Map、无损或不压缩，pivot=(0.5,0)，不要自动紧裁帧格。
4. 行走循环 WalkA→Idle→WalkB→Idle，6 fps；静止使用 Idle 列。方向与 `Ferryman` actorId 对接。
5. 检查 Game 视图中的脸、步态、脚底、排序层和场景缩放。当前未 Unity 导入、未实机验证，不能标记 Imported/Verified。
