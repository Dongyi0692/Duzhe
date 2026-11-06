# R11 七位配角视觉与技术检查

- 技术结果：7/7 通过尺寸、RGBA、真 Alpha、画布边界、单连通主体、脚底基线、中心偏移、母图与颜色数量检查。
- 视觉结果：7/7 已逐张查看原始母图和最终放大预览；轮廓、年龄、服装、无多余手持物已复核。
- 边界：未在 Unity 工程中导入，未做实机运行验收；状态不等于 Imported/Verified。
- 对照图：`SourceAssets/Characters/R11_Characters_Preview.png` 与 `SourceAssets/Characters/R11_AlphaContrast_QA.png`。

| assetId | 技术 | 视觉复核 | 脚底中心 x | 主体 bbox |
|---|---|---|---:|---|
| CHR02_Grandfather | Passed | Reviewed | 15.00 | [4, 3, 27, 48] |
| CHR03_Grandmother | Passed | Reviewed | 14.29 | [5, 5, 26, 48] |
| CHR05_LoverSpirit | Passed | Reviewed | 16.00 | [8, 3, 24, 48] |
| CHR07_DrummerSpirit | Passed | Reviewed | 15.00 | [6, 3, 25, 48] |
| CHR09_BandmateA | Passed | Reviewed | 15.50 | [7, 3, 25, 48] |
| CHR10_BandmateB | Passed | Reviewed | 15.00 | [8, 3, 23, 48] |
| CHR11_TeacherSpirit | Passed | Reviewed | 15.50 | [7, 3, 25, 48] |

## 逐角色视觉说明

- **CHR02_Grandfather**：温和农村老人；灰短发与灰橄榄外套清楚；无拐杖、眼镜或手持物。
- **CHR03_Grandmother**：灰白盘发、灰紫开衫与浅内搭符合母图；无眼镜或手持物；较其他成人略矮。
- **CHR05_LoverSpirit**：年轻女性、黑色中长发与柔绿开衫清楚；无项链；非透明残影。
- **CHR07_DrummerSpirit**：年轻男性、蓬松短发与灰锈棕外套清楚；无鼓槌、鼓或武器。
- **CHR09_BandmateA**：年轻男性、侧分短发与灰牛仔蓝外套清楚；无贝斯或其他手持物。
- **CHR10_BandmateB**：年轻女性、高马尾与低饱和赭黄外套清楚；无麦克风或乐器。
- **CHR11_TeacherSpirit**：成年教师、深色低盘发与米色外套清楚；无教案、书本或纸张。
