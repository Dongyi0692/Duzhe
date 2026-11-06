# R01 文本接入说明

适用最终规范v1.0与2026-10-26交付接口。先导入JSON，不从Markdown中抽取对白。当前没有收到CodelyHandoff.md，因此本批按约定接口交付，实际工程兼容性尚待Codely验证。

## 文件与读法

- TXT_CH01.json至TXT_CH05.json：根字段严格为schemaVersion、chapterId、displayName、segments、objectives、items。segments内lineId、chapterId、stageId、speakerId、text全部齐全。
- TXT_UI.json：schemaVersion和strings；每项key、text。objectives也同步为objective.<stageId>键，由同一内容自动生成，不存在手工维护的第二套文案。
- CharacterDisplayNames.json：actorId→displayName的辅助映射。TXT_UI同时提供speaker.<actorId>，已有UI方案可以直接用该键，不要求再做一套角色系统。
- PresentationMap.json：六条可回看演出的绑定与说明。**orderedSegmentIds定义回忆馆播放次序；主线依章内stageId调度当前段，不把整条回看顺序当作主线下一步。**
- Story_Final.md供阅读审稿，UI_Copy.md供查文案；它们和JSON从同源内容生成。制作指示不出现在text字段里。

## 必须保留的执行边界

1. 阴间开场按合法阶段接近触发。已完成的对白段不因人物仍在触发范围内而重复开启。
2. ReturnItems/ReturnNecklace/ReturnDrumsticks/ReturnLessonPlan不新增独立对白段：完成按E交还事件后，进入RitualTalk读归还回应与仪式说明。
3. 仪式、登船、河流、描画、拖放、敲门、演奏只使用对应UI提示。不能由于“找不到同名segment”就自动跳过整个机关，或播放一段话替代操作。
4. CH01凳子实际摆齐后才开饭；CH02相认后进入拥抱基础姿态，实际画完心形才进告别。
5. CH03默认必须完成七音演奏再拖合照。R15尚未交正式音序时脚本可用已标明的占位音序，文本内容不用改。
6. CH04_Lesson手动读完含必保句后，才置LessonFinished。随后实际写“再见”；CH04_Farewell只读本段，不重播Lesson。回忆馆才按PresentationMap顺序重组课堂、已完成的黑板与告别。
7. CH05_Recall和CH05_LetGo只在本次章的前半部分执行；河段失败保留其状态，重试不复播。
8. 段落最后一句推进完只是DialogueFinished；有灵魂淡出/船远去等演出时须等PresentationFinished，不能提前通关。所有视觉计时受暂停控制。
9. Gallery中不执行ItemCollected、MinigameCompleted、ChapterCompleted或MemoryRestored等主线写入；退出回看不修改故事进度。
10. 菜单字符串内含所有章名，并不授权显示未解锁章节。目录只枚举已通关项，回忆馆只枚举已播放完成的六条实际记录。

## UI与错误反馈

minigame.stools.progress的{count}/{total}由运行时替换，total=3。它是唯一运行时文案占位格式，不能原样露出花括号。credits.author在发布前替换成用户希望的署名，不用系统账户名猜测身份。

“已保存”相关提示只在写盘成功时使用。写盘失败用save.write_failed；坏档恢复成功才用save.restored_backup；无法恢复时保留原文件并显示save.unreadable_*，不得为了让按钮可点就静默覆盖存档。

初始阅读方式为整句显示、点击对白区域或空格逐条推进，不能把按住空格当连跳。谜题UI关闭和对话结束的同一次点击不穿透。UI确认/取消只是操作确认，不是故事分支选项。

## 阶段覆盖

“无”表示该阶段没有对白段，仍必须完成实际玩法/演出，并不表示未制作或可跳过。

| stageId | 对白segmentId | 目标key |
|---|---|---|
| CH01_Opening | CH01_Opening | objective.CH01_Opening |
| CH01_Prepare | 无 | objective.CH01_Prepare |
| CH01_ReturnItems | 无 | objective.CH01_ReturnItems |
| CH01_RitualTalk | CH01_RitualTalk | objective.CH01_RitualTalk |
| CH01_Ritual | 无 | objective.CH01_Ritual |
| CH01_Board | 无 | objective.CH01_Board |
| CH01_River | 无 | objective.CH01_River |
| CH01_Door | 无 | objective.CH01_Door |
| CH01_ReunionTalk | CH01_ReunionTalk | objective.CH01_ReunionTalk |
| CH01_Stools | 无 | objective.CH01_Stools |
| CH01_Farewell | CH01_Farewell | objective.CH01_Farewell |
| CH01_Complete | 无 | objective.CH01_Complete |
| CH02_Opening | CH02_Opening | objective.CH02_Opening |
| CH02_FindNecklace | 无 | objective.CH02_FindNecklace |
| CH02_ReturnNecklace | 无 | objective.CH02_ReturnNecklace |
| CH02_RitualTalk | CH02_RitualTalk | objective.CH02_RitualTalk |
| CH02_Ritual | 无 | objective.CH02_Ritual |
| CH02_Board | 无 | objective.CH02_Board |
| CH02_River | 无 | objective.CH02_River |
| CH02_ReunionTalk | CH02_ReunionTalk | objective.CH02_ReunionTalk |
| CH02_Heart | 无 | objective.CH02_Heart |
| CH02_Farewell | CH02_Farewell | objective.CH02_Farewell |
| CH02_Complete | 无 | objective.CH02_Complete |
| CH03_Opening | CH03_Opening | objective.CH03_Opening |
| CH03_FindDrumsticks | 无 | objective.CH03_FindDrumsticks |
| CH03_ReturnDrumsticks | 无 | objective.CH03_ReturnDrumsticks |
| CH03_RitualTalk | CH03_RitualTalk | objective.CH03_RitualTalk |
| CH03_Ritual | 无 | objective.CH03_Ritual |
| CH03_Board | 无 | objective.CH03_Board |
| CH03_River | 无 | objective.CH03_River |
| CH03_ReunionTalk | CH03_ReunionTalk | objective.CH03_ReunionTalk |
| CH03_Perform | 无 | objective.CH03_Perform |
| CH03_Photo | 无 | objective.CH03_Photo |
| CH03_Farewell | CH03_Farewell | objective.CH03_Farewell |
| CH03_Complete | 无 | objective.CH03_Complete |
| CH04_Opening | CH04_Opening | objective.CH04_Opening |
| CH04_FindLessonPlan | 无 | objective.CH04_FindLessonPlan |
| CH04_ReturnLessonPlan | 无 | objective.CH04_ReturnLessonPlan |
| CH04_RitualTalk | CH04_RitualTalk | objective.CH04_RitualTalk |
| CH04_Ritual | 无 | objective.CH04_Ritual |
| CH04_Board | 无 | objective.CH04_Board |
| CH04_River | 无 | objective.CH04_River |
| CH04_ReunionTalk | CH04_ReunionTalk | objective.CH04_ReunionTalk |
| CH04_Lesson | CH04_Lesson | objective.CH04_Lesson |
| CH04_WriteGoodbye | 无 | objective.CH04_WriteGoodbye |
| CH04_Farewell | CH04_Farewell | objective.CH04_Farewell |
| CH04_Complete | 无 | objective.CH04_Complete |
| CH05_Recall | CH05_Recall | objective.CH05_Recall |
| CH05_LetGo | CH05_LetGo | objective.CH05_LetGo |
| CH05_Board | 无 | objective.CH05_Board |
| CH05_River | 无 | objective.CH05_River |
| CH05_Reincarnation | CH05_Reincarnation | objective.CH05_Reincarnation |
| CH05_Complete | 无 | objective.CH05_Complete |

## 需引擎实际验证

导入、中文字体覆盖、长句换行、控件边界、触发范围、暂停/重试、六条回看与主线隔离全部仍待Codely执行。本批只完成文字与结构检查，不替代游戏测试。
