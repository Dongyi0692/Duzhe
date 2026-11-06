# 《渡者》R14 告别插画与终章画面交接 v1.0

批次：R14_FarewellIllustrations_20261028_v1.0  
范围：CG01–CG05 五张必需演出主图；使用已确认 R02 像素风及已提供的角色、背景母图。

## 已交文件

- `Illustrations/CG01_FamilyFarewell.png`：四人团圆饭；两位老人、成年孙子与旁观者渡客。
- `Illustrations/CG02_LoversFarewell.png`：亡者女生与生者男生两人拥抱。
- `Illustrations/CG03_BandFarewell.png`：四位乐队成员完整合照，无渡客、无第五人。
- `Illustrations/CG04_TeacherFarewell.png`：老师与学生两人，黑板空白。
- `Illustrations/CG05_SelfCrossing.png`：渡客独自一人乘船远去，暖光不代表现实复活。
- 五张均为 640×360 RGB PNG。`SourceAssets/Illustrations/` 保留原稿、清理稿、处理记录和五图联系表。
- `Delivery/R14/` 内含 AssetDelivery、IllustrationQA、PresentationLayers 与本说明。没有占位图，本范围没有缺项。

## 必须按此方式做亡者淡出

主图是包含人物的静态定格，不是可拆分的角色透明层。**不要通过降低整张 CG 的 Alpha 来冒充亡者单独消失。**

1. CH01–CH04 的对白与告别先使用对应 CG 定格；需要灵魂淡出时，切回 `PresentationLayers.json` 指定的干净背景与既有 R11/R12 角色 SpriteRenderer 场景。
2. 仅淡出 `spiritActorIds`：CH01 两位老人、CH02 女孩、CH03 鼓手、CH04 老师。`persistentActorIds` 与背景保持不动。
3. CH03 已完成的四人合照永久保留；只在实际录音室场景中淡出鼓手，不能让合照少人或整张消失。
4. CH04 黑板不烘焙文字。“这就是，最后一课了”、正文与“再见”由既定 UI/轨迹层绘制。
5. CH05 船远去、暖光停留后允许全屏淡出进入轮回；不得接到现实醒来、复活或回到阳间。
6. PR_CH05_Recall 复用 CG01–04，不新增地图或必需 CG。

## Codely 接入与验收

- 正式主图接入 `Assets/_Project` 既定演出路径；替换同名资源时保留 `.meta`，不要导入 `_a01` 原稿作为正式图。
- 640×360 全幅显示，Point 过滤、关闭 Mip Map、无损或不压缩；不要拉伸比例或裁掉乐队成员。
- 对话、标题、告别文字均继续由程序渲染；回忆馆缩略图从五张主图裁切，不新增必需图片。
- 在实际 Unity 版本检查全屏适配、淡出触发、角色残留、黑板字层与终章流向。当前未 Unity 导入、未实机验证，状态不是 Imported/Verified。
