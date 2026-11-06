# R02 正式交接 · v1.3

日期：2026-10-27。用户已确认画风，R02 完成。制作参考 ID：REF_R02_VisualGuide；审批状态 Approved；文件检查状态 Reviewed；未标记 Imported 或引擎 Verified。

## 使用入口

- Style/VisualGuide.png：固定母图，与用户确认的v3完全一致。
- Style/StyleLock.md：完整规范。
- Style/StylePromptBlock.txt：后续美术提示词必须带上的已确认约束。
- Style/ApprovalRecord.json：确认依据、日期、范围与图片哈希。
- Style/VisualQA.md、Style/AssetDelivery.json：检查记录与文件清单。
- Style/GenerationPrompts.md、FerrymanRevisionPrompt_v2.md、FerrymanRevisionPrompt_v3.md：历史生成过程，不用于重新随机生成母图。

路径相对于资源包根目录。旧候选包仅作历史记录，后续引用本正式包。

## 给 Codely

把本包保存为项目的SourceAssets视觉参考和Docs规范，按自身目录习惯归档即可。样板不进入自动加载数据目录，不装作BG01背景、不裁成角色或UI，不据此变更碰撞及触发点。正式背景、角色和UI由后续资源批次独立交付。

渡客造型现已固定：略浅的低饱和灰蓝、小圆兜帽、短披肩、脸和眼睛清楚，神秘但不遮脸也不阴暗。这条用户修订优先于旧规范与旧提示词。

## 下一批

R03制作UI，R04确定场景布局，之后依次生产背景、角色等正式资源。Codely可继续占位开发，不需要等待全部美术。

## R01 文本补交时机


可以等 C02 完成后再补交 R01。收到 C02 交接后，给 Codely 完整 Duzhe_R01_Text_v1.0.zip，并同时提供 Production/C01_IntegrationReview.md。后者位于资源主目录，另有一份随本 R02 包附带。

先将完整文本包保存为 SourceAssets 源资源；C04–C08 注册正式章节、阶段与角色后按章接入。正式章节使用 CH01–CH05 和既定 actorId/stageId，不能改为 CH01_DEV 等占位 ID。现有项目从 Assets/_Project/Resources/Data/Dialogue 读取，按实际加载器处理导入位置。TXT_UI 的点号键与旧界面下划线键不同，先适配再替换。

不要把五章 JSON 立即丢进自动加载目录覆盖占位数据，否则现有校验器可能因正式章节/角色尚未注册而报错。以上适配由 Codely 完成，用户不用手工修改 JSON。最迟应在 C04 正式第一关接入前补发；不需要重做已完成的 C02。
