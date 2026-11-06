# R11 七位配角外观与连续性记录

版本：1.0.0｜批次：R11_Characters_KimiArtB_20261027_v1.0  
统一风格：R02-Approved-1.3；实际参考 `Style/VisualGuide.png`。  
共同规格：现代中国日常人物、俯视 2D、约 2.5 头身、正面/Down 主站姿、32×48 RGBA 真透明 PNG、32 PPU、脚底中心 `pivot=(0.5,0)`。  
共同处理：原始母图均为 1024×1536 RGBA；正式帧经主体清理、裁切、LANCZOS 缩放、无抖动 24 色量化和透明通道检查。以下色值从最终帧提取，仅作连续性参考，不替代 R02 全局色板。  
实际状态：7 张正式帧均已逐张视觉复核并通过技术检查；尚未由 Codely 在 Unity 中导入或实机验证。

## CHR02_Grandfather

- actorId：Grandfather；显示称呼：爷爷；使用章节：CH01。
- 发型：灰色短发，顶部稍稀，整齐自然。
- 服装：灰橄榄日常外套、暖白内搭、深灰长裤、深色布鞋。
- 体形与比例：约 2.5 头身；稳重、略宽厚；与奶奶相比身高略高。
- 标志物：灰短发与灰橄榄外套；无夸张年龄符号。
- 主要色板：`#6D655B`、`#A69F9F`、`#49474B`、`#E0B397`、`#2C2829`。
- 阶段限制：丢失的拐杖不在基础站姿中；未画眼镜、帽子或手持物。
- 参考：REF_R02_VisualGuide、已验收 CHR03_Grandmother。
- 母图：`SourceAssets/Characters/CHR02_Grandfather_master.png`；正式帧：`Characters/CHR02_Grandfather.png`。
- 脚底定位：主体底缘对齐画布底部，脚底中心接近 x=15.5；顶部与左右留安全边距。

## CHR03_Grandmother

- actorId：Grandmother；显示称呼：奶奶；使用章节：CH01。
- 发型：灰白头发后梳，低盘发/发髻轮廓清楚。
- 服装：灰紫/藕色日常开衫、浅暖白内搭、深色裤子与平底鞋。
- 体形与比例：约 2.5 头身；在本批成人中略矮，最终主体高 43 像素。
- 标志物：灰白盘发、灰紫开衫、温和老年面孔。
- 主要色板：`#A29E9F`、`#8C686B`、`#DDB095`、`#4F4546`、`#252020`。
- 阶段限制：脸上不戴眼镜，基础站姿中也没有另一副眼镜或其他任务物。
- 参考：REF_R02_VisualGuide；本角色也是后续配角的一致性参考。
- 母图：`SourceAssets/Characters/CHR03_Grandmother_master.png`；正式帧：`Characters/CHR03_Grandmother.png`。
- 脚底定位：主体底缘对齐画布底部，脚底中心接近 x=15.5；顶部保留 5 像素安全空间。

## CHR05_LoverSpirit

- actorId：LoverSpirit；显示称呼：女孩；使用章节：CH02。
- 发型：黑色中长发，简单侧分，肩部自然收束。
- 服装：低饱和柔绿开衫、暖象牙白内搭、深蓝灰直筒裤、深色平底鞋。
- 体形与比例：约 2.5 头身；年轻成年女性，身形修长，区别于老人与学生。
- 标志物：黑色中长发与柔绿开衫；神情温和、略带安静的不舍。
- 主要色板：`#757766`、`#665F51`、`#353947`、`#FACCAF`、`#2C282D`。
- 阶段限制：项链作为独立任务物，不画在基础站姿；无手持物、无发光、非透明残影。
- 参考：REF_R02_VisualGuide、已验收 CHR03_Grandmother。
- 母图：`SourceAssets/Characters/CHR05_LoverSpirit_master.png`；正式帧：`Characters/CHR05_LoverSpirit.png`。
- 脚底定位：主体底缘对齐画布底部，脚底中心接近 x=15.5；头发与鞋底未裁切。

## CHR07_DrummerSpirit

- actorId：DrummerSpirit；显示称呼：鼓手；使用章节：CH03。
- 发型：黑色短发，轻微蓬松，年轻但不过度夸张。
- 服装：灰锈棕休闲外套、炭灰 T 恤、深灰长裤、灰运动鞋。
- 体形与比例：约 2.5 头身；年轻成年男性，姿态放松。
- 标志物：蓬松短发、灰锈棕外套、轻微友好笑意。
- 主要色板：`#9F5D4C`、`#975344`、`#34323B`、`#5C5456`、`#2B2830`。
- 阶段限制：鼓槌与鼓均不在基础站姿中；无武器、无发光、非透明残影。
- 参考：REF_R02_VisualGuide、已验收 CHR03_Grandmother；本角色也是后续乐队角色的一致性参考。
- 母图：`SourceAssets/Characters/CHR07_DrummerSpirit_master.png`；正式帧：`Characters/CHR07_DrummerSpirit.png`。
- 脚底定位：主体底缘对齐画布底部，鞋底在同一基线，中心接近 x=15.5。

## CHR09_BandmateA

- actorId：BandmateA；显示称呼：队友；职责：贝斯手；使用章节：CH03。
- 发型：黑色短发，较长侧分刘海，与鼓手的蓬松轮廓区分。
- 服装：灰牛仔蓝休闲夹克、暖白圆领内搭、深炭灰直筒裤、深色运动鞋。
- 体形与比例：约 2.5 头身；年轻成年男性，轮廓偏修长。
- 标志物：侧分刘海与灰牛仔蓝夹克；不新增主线身份。
- 主要色板：`#384A67`、`#435777`、`#2E394E`、`#E1CBB4`、`#2A272D`。
- 阶段限制：基础站姿不固定携带贝斯或其他物品，便于后续合影与动作层复用。
- 参考：REF_R02_VisualGuide、已验收 CHR07_DrummerSpirit。
- 母图：`SourceAssets/Characters/CHR09_BandmateA_master.png`；正式帧：`Characters/CHR09_BandmateA.png`。
- 脚底定位：主体底缘对齐画布底部，双脚同一基线，中心接近 x=15.5。

## CHR10_BandmateB

- actorId：BandmateB；显示称呼：队友；职责：主唱；使用章节：CH03。
- 发型：黑色高马尾，顶部与后束轮廓清楚。
- 服装：低饱和赭黄休闲夹克、炭灰圆领内搭、深海军蓝直筒裤、深色运动鞋。
- 体形与比例：约 2.5 头身；年轻成年女性，轮廓简洁、自然。
- 标志物：高马尾与赭黄夹克；不新增主线身份。
- 主要色板：`#DFA75A`、`#B18654`、`#654B2E`、`#31364A`、`#2A282E`。
- 阶段限制：基础站姿不固定携带麦克风、乐器或其他物品。
- 参考：REF_R02_VisualGuide、已验收 CHR07_DrummerSpirit。
- 母图：`SourceAssets/Characters/CHR10_BandmateB_master.png`；正式帧：`Characters/CHR10_BandmateB.png`。
- 脚底定位：马尾与鞋底均完整，主体底缘对齐画布底部，中心接近 x=15.5。

## CHR11_TeacherSpirit

- actorId：TeacherSpirit；显示称呼：老师；使用章节：CH04。
- 发型：深棕黑发低盘发，少量灰色高光；与奶奶的灰白盘发区分。
- 服装：暖米棕日常外套、灰蓝内搭、深炭灰直筒裤、深色平底鞋。
- 体形与比例：约 2.5 头身；成年教师，年龄感低于老人、高于学生。
- 标志物：深色低盘发、米棕外套与灰蓝内搭；亲切、克制。
- 主要色板：`#9D745F`、`#B1876F`、`#72574B`、`#493731`、`#302727`。
- 阶段限制：教案、书本、纸张与教鞭均不在基础站姿中。
- 参考：REF_R02_VisualGuide、已验收 CHR03_Grandmother。
- 母图：`SourceAssets/Characters/CHR11_TeacherSpirit_master.png`；正式帧：`Characters/CHR11_TeacherSpirit.png`。
- 脚底定位：主体底缘对齐画布底部，双脚同一基线，中心接近 x=15.5。

## 后续连续性约束

- R12 行走帧必须实际引用本批对应正式帧，保持发型、服装、标志色、比例与脚底定位，不从文本重新随机设计。
- CHR02、CHR03、CHR05、CHR07、CHR11 的 R12 为 96×192、3 列×4 行；本批没有交付这些图集。
- CHR09 与 CHR10 本轮仅交静态主站姿；不得把本批 PNG 误当作完整动画。
- R14 合影与告别画须按 actorId 使用本批角色，不自行更换服装、年龄或身份。
