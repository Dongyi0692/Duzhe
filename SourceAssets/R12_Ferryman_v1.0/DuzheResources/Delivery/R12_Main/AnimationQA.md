# CHR01_Ferryman 行走动画 QA

- 技术结果：1/1 图集、12/12 帧通过检查。
- 图集：96×192 RGBA；3 列×4 行；帧格 32×48；真实 Alpha 仅含 0/255。
- 行序 Down、Left、Right、Up；列序 WalkA、Idle、WalkB。
- 12 帧均唯一；每个方向的三个姿势均实际不同。
- Down/Idle 与已提供的 R11 正式站姿逐像素一致：true。
- 已查看四向放大图；小兜帽、短披肩、可见脸和眼睛保持一致，左右朝向与跨步清楚。
- GIF 为 WalkA→Idle→WalkB→Idle 四帧预览，不代表需要第四张行走图。
- 未在 Unity 中导入或实机播放验证，状态不是 Imported/Verified。

## 固定验收值

- sheetSha256：`7b63fea8db42be8aca9a2d19aaf4c75ec6b47a1fc82dd81e139533be977cd607`
- uniqueFrameCount：12
- swapSideWalkB：false
