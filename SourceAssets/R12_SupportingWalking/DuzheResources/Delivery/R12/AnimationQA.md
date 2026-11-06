# R12 五套配角行走动画 QA

- 技术结果：5/5 图集、60/60 帧通过检查。
- 每套均为 96×192 RGBA，3 列×4 行，帧格 32×48，Alpha 仅含 0/255。
- 每套 12 帧唯一；每个方向的 WalkA、Idle、WalkB 均实际不同。
- 每套 Down/Idle 与对应 R11 正式站姿逐像素一致。
- 已逐套查看 12 帧放大图；另交每角色 4 步循环 GIF 与全员动画预览。
- 未在 Unity 中导入或实机播放验证，状态不等于 Imported/Verified。

| assetId | 技术 | 视觉 | 唯一帧 | R11 Idle | 侧向 WalkB 纠正 |
|---|---|---|---:|---|---|
| CHR02_Grandfather | Passed | Reviewed | 12 | True | True |
| CHR03_Grandmother | Passed | Reviewed | 12 | True | True |
| CHR05_LoverSpirit | Passed | Reviewed | 12 | True | False |
| CHR07_DrummerSpirit | Passed | Reviewed | 12 | True | True |
| CHR11_TeacherSpirit | Passed | Reviewed | 12 | True | False |

## 视觉复核

- **CHR02_Grandfather**：灰短发、灰橄榄外套与老人比例保持一致；四向跨步与手臂变化可读。
- **CHR03_Grandmother**：灰白盘发、灰紫开衫和较矮比例保持一致；Down/Idle 与 R11 完全一致。
- **CHR05_LoverSpirit**：中长发、柔绿开衫和裤装保持一致；侧面发束与步态方向正确。
- **CHR07_DrummerSpirit**：蓬松短发、灰锈棕外套与运动鞋保持一致；左右 WalkB 已纠正朝向。
- **CHR11_TeacherSpirit**：深色低盘发、米棕外套和灰蓝内搭保持一致；四向步态清楚。
