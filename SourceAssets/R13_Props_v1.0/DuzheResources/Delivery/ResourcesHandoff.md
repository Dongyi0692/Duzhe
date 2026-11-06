# R13 道具与机关素材交接 · v1.0

日期：2026-10-27  
批次：`R13_PropsAndPanels_20261027_v1.0`  
范围：按 R02 Approved v1.3、R04 布局交接及 C02 河流接口制作。资源侧状态为 **Reviewed**；**未导入团结引擎，未进行运行时验收，不标记 Imported／Verified**。

## 1. 本次实际交付：9 个独立 PNG

| assetId | 相对路径 | 实际尺寸 | Alpha | 资源侧检查 |
|---|---|---:|---|---|
| IT01_Glasses | `Props/IT01_Glasses.png` | 32×32 | RGBA 真透明 | 已打开原图及明暗底成品预览 |
| IT02_Cane | `Props/IT02_Cane.png` | 32×64 | RGBA 真透明 | 已打开原图及明暗底成品预览 |
| IT03_Necklace | `Props/IT03_Necklace.png` | 32×32 | RGBA 真透明 | 已打开原图及明暗底成品预览 |
| IT04_Drumsticks | `Props/IT04_Drumsticks.png` | 32×32 | RGBA 真透明 | 一对鼓槌合为一个任务物 |
| IT05_LessonPlan | `Props/IT05_LessonPlan.png` | 32×32 | RGBA 真透明 | 无字封面，已检查预览 |
| PRP01_Boat | `Props/PRP01_Boat.png` | 96×64 | RGBA 真透明 | 空船；乘客不烘焙 |
| PRP02_Whirlpool | `Props/PRP02_Whirlpool.png` | 64×64 | RGBA 真透明 | 无船；单帧供程序旋转／缩放 |
| PRP04_Stool | `Props/PRP04_Stool.png` | 32×32 | RGBA 真透明 | 只交 1 张，由 Codely 实例化 3 把 |
| PRP05_DiningTable | `Props/PRP05_DiningTable.png` | 96×64 | RGBA 真透明 | 不含凳子、人物或餐具 |

所有成品均保留原图或处理用源图于 `SourceAssets/R13/`，没有把拼图冒充独立资源。原始大图经透明噪点清理、必要裁切、目标尺寸最近邻缩放及有限色处理；每个成品均为真实 Alpha，且半透明像素数为 0。详细实测值和 SHA-256 见 `Production/R13_Validation.json` 与 `Delivery/AssetDelivery.json`。

总对照图：`Previews/R13_ContactSheet.png`。每项另有 `Previews/<assetId>_preview.png`，左侧米色底、右侧深蓝灰底，均为最近邻放大预览，不代替实际尺寸 PNG。

## 2. 已读取并对齐的接口

- `REF_R02_VisualGuide`：实际打开并使用 `Style/VisualGuide.png` 作为全部生成项参考；沿用清晰俯视 2D、克制轮廓和冷暖关系。
- `REF_R04_River`：船中心为 `(640,456)`；可控中心范围 `x=496–784`，三航道中心为 `544/640/736`。船与漩涡均为独立动态对象。
- `REF_C02_River`：船碰撞盒为 `0.8×0.5` 世界单位；漩涡碰撞盒为 `0.9×0.9` 世界单位，视觉略大于碰撞区。乘客由代码独立叠加，本船图不包含人物。
- `REF_R04_FarmCourtyard`：餐桌视觉锚点 `(736,400)`；三个凳子参考点为左 `(672,448)`、右 `(800,448)`、前 `(736,496)`。这些是世界布局点，不冒充 C03 UI 拖放槽坐标。

`PRP02_Whirlpool` 的原始生成图左下角带有生成标记；正式 PNG 使用保留完整漩涡主体的中央裁切源图 `SourceAssets/R13/PRP02_Whirlpool_cropped.png`，已实际查看成品，正式图不含该标记。原始生成图仍保留用于溯源，不应直接导入。

## 3. 热点与锚点交接

见 `Props/PropsHotspots.json`：

- `PRP04_Stool` 资源画布底部中心锚点为 `(16,32)`，原点左上；`y=32` 表示画布下边界。
- 门环、合照可用区域／鼓手缺口、黑板书写区均保持 `null + PendingC03`，没有把建议值写成已接入坐标。
- R04 交接记录的门环代码推导候选为中心 `(380,200)`、48×48 区域 `(356,176,48,48)`，同段又指出旧报告 `(380,160)` 与代码符号不一致。该差异必须由 Codely 实机确认后再定稿。

## 4. 待做：3 个机关面板

| assetId | 目标尺寸 | 当前状态 | 阻塞原因 |
|---|---:|---|---|
| PRP03_DoorCloseup | 640×360 | PendingC03 | 缺少 C03 最终门环热点导出，且现有交接存在坐标差异 |
| PRP06_PhotoFrame | 640×360 | PendingC03 | 缺少合照可用区域及鼓手唯一缺口的最终 UI 导出 |
| PRP07_BlackboardCloseup | 640×360 | PendingC03 | 缺少黑板描画区与逐笔模板的最终 UI 导出 |

本批没有为这 3 项生成假装已对齐的正式面板，也没有烘焙“再见”、鼓手或重复人物。收到最新 C03 `CodelyHandoff.md`／配置导出后，从 `PRP03_DoorCloseup` 续做即可，不需要重做本批 9 项。

## 5. 给 Codely 的导入说明

1. 按稳定 `assetId` 映射到工程现有占位资源，替换时保留 `.meta`；不要根据中文显示名猜引用。
2. 世界素材使用 32 PPU、Point 过滤、关闭 Mip Map、无压缩或无损；透明 PNG 启用 Alpha。正式采用前由 Codely 检查实际导入设置。
3. 船、乘客和漩涡保持独立对象；不要将本素材重新烘焙进背景。按 C02 现有碰撞盒与脚本控制接入，并实测视觉轮廓和碰撞差异。
4. 凳子只实例化同一张 `PRP04_Stool`，使用底部中心锚点；餐桌不包含凳子。R04 世界摆位与 C03 UI 拖放槽分别核对。
5. 本批只完成资源侧生成与静态检查。工程导入、碰撞、排序层、交互和实际鼠标试玩仍由 Codely 验证并更新交接。
