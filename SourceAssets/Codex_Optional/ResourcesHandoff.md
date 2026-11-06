# 石灯、田园犬、情侣回忆 — 已接入交接

交付日期：2026-10-31。最终构建：20261031_000633_1eafff9f。

| 资源 | 源文件目录 | 运行时 key | 接入位置 |
|---|---|---|---|
| PRP08_StoneLantern（48×64，透明） | Props | Art/Props/PRP08_StoneLantern | 阴间 CH01–CH04 仪式石灯，替换占位 |
| CHR13_Dog（40×40，透明） | Characters | Art/Characters/CHR13_Dog | 第一关农家院门左侧；无碰撞，不阻塞敲门 |
| CG06_LoversMemory01（640×360） | Illustrations | Art/Illustrations/CG06_LoversMemory01 | 公园初遇 |
| CG07_LoversMemory02（640×360） | Illustrations | Art/Illustrations/CG07_LoversMemory02 | 项链摊位 |
| CG08_LoversMemory03（640×360） | Illustrations | Art/Illustrations/CG08_LoversMemory03 | 雨中共伞 |

狗：靠近 3.2 世界单位内、探索状态、无对白且未暂停时，播放已交付的 SFX13_DogHappy 一次，并轻轻跃动。每次场景访问只触发一次。

剧情：CH02_Heart → CH02_Memory → CH02_Farewell。PR_CH02_Memory 包含 CH02_Memory01/02/03 三段，每段两句，手动推进。最后恢复原告别流程，不提前完成章节。

回忆馆标题：那些平常的日子。正常流程观看后解锁；旧存档若已通关 CH02，可直接在回忆馆回看，不必清档。主菜单与独立回忆馆使用同一只读资格判断。回看/退出不写通关进度。

实现：Scripts/Scenes/CourtyardDog.cs、CH02MemoryCast.cs；ChapterCatalog、GalleryController、MainMenuPanel 和 ProjectSetupUtility 已同步接线。对白与演出表以 Assets/_Project/Data/Dialogue 为源，经装配同步 Resources 副本。

验证：DZ_SMOKE=PASS、DZ_OPTIONAL_PLAY=PASS。实际流程覆盖石灯绑定、狗无碰撞/暂停不触发/靠近招呼、画心后播放、三图顺序、单次完成事件、告别衔接、旧存档菜单可见性、回放与退出不改进度。截图见 Logs/optional_content_play/。

构建：Succeeded，errors=0。桌面“渡者-Duzhe”指向最终构建。新增美术原图由 imagegen 生成；工程用图仅做已授权的裁切和尺寸规整。