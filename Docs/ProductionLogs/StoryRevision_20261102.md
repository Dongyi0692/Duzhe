# 《渡者》剧本扩写与第五关走马灯接入记录

日期：2026-11-02。工程：C:\Users\東壹\Duzhe。

## 已接入内容

- 五章新版剧情共 623 句对白，已同步到 Data/Dialogue 与 Resources/Data/Dialogue。纯剧情源文件保留在 SourceAssets/Codex_StoryRevision_20261102/剧本.txt；不把制作说明写进游戏对白。
- 前四章扩展配角互动和告别，并在合适语境中让玩家知道亡者已逝。第三章新增 CH03_PhotoTalk，位于七音演奏之后、合照操作之前。
- 第五关按新行号配置记忆切图：先恢复生活记忆和各时期身份，再回想自己已经死去、曾抵达冥河的经历。没有新增复活或改写过去的设定。
- 第五关成功渡河后播放家人、恋人、朋友、老师四段回忆，共 28 句，再接原结尾 6 句。切图随对白推进，图间淡入淡出；播放期间不生成漩涡，暂停冻结演出与船体前移。
- 新走马灯图片保持完整比例，摆在下方对白框之上；隐藏本段演出的顶部目标条，避免压住人物。
- 回忆馆保持五个章节入口，第五章回放包含记忆恢复、释怀、四段回忆和最终自渡；回放不会写入通关进度。
- 开始界面右下角加入 AI 制作说明：首页显示，其他菜单页隐藏，不拦截鼠标。

## 四张正式图片

| 资源 ID | 内容 |
| --- | --- |
| CG18_FamilyRecollection | 家人：农家院吃饭 |
| CG19_LoverRecollection | 恋人：雨中共伞 |
| CG20_FriendsRecollection | 朋友：乐队相聚 |
| CG21_TeacherRecollection | 老师：课堂教写字 |

图片使用内置 image_gen，参考已确认的火柴人构图、现有 CG05 与章节人物图。导入版本为 1280×720 PNG；保留生成原图。乐队人物从左至右为红衣鼓手、紫灰衣棕发主角、蓝衣男队友、黄衣女队友。完整生成提示词见同批 ImagePrompts.md。

## 开始界面 AI 制作说明

> 本作的剧情文本、美术、音乐、音效及程序代码使用生成式 AI 辅助制作，经开发者筛选、调整与整合。
> 使用工具：ChatGPT、Kimi、DeepSeek、团结 Codely。
> 内容在开发阶段生成，游玩时不实时调用 AI。

说明采用公开游戏披露中列出使用环节、人工整合及是否实时生成的写法，按本项目实际情况重新撰写。参考：[Nonogram Nonsense Demo](https://store.steampowered.com/app/3746960/Nonogram_Nonsense_Demo/)、[Shadow Empire: Oceania](https://store.steampowered.com/app/2183640?l=english)。

## 验证与构建

- 资源导入与场景装配：`DZ_VALIDATION=PASS`；记录 `Logs/duzhe_validation.txt`。
- 对白、图片及基础系统检查：`DZ_SMOKE=PASS`；记录 `Logs/duzhe_smoke.txt`。
- 第五关完整运行检查：`DZ_CH05_PLAY=PASS`；记录 `Logs/ch05_playtest.txt`。
- 五章回忆馆运行检查：`DZ_CHAPTER_GALLERY=PASS`；记录 `Logs/chapter_gallery_playtest.txt`。
- Windows x64：Succeeded，errors=0。构建 `20261102_015530_20b488d0`。
- 可执行文件：`C:\Users\東壹\Duzhe\Builds\Candidates\20261102_015530_20b488d0\Duzhe.exe`。

桌面“渡者-Duzhe”快捷方式已指向上述新版本，原快捷方式已备份。构建版使用 Direct3D 11 启动并到达 MainMenu，短暂启动检查期间保持运行，无 Exception 或场景损坏错误；检查后只关闭本次测试进程。

第五关实际运行检查覆盖：扩写对白、记忆切图、登船条件、零乘客、四图与最终 CG 顺序、暂停、制作名单及返回首页。回忆馆检查覆盖：五个入口、完整回放顺序、ESC 中断及存档不变。已检查四幅实际运行截图和首页说明的显示。

这些为引擎内自动运行、构建版启动和截图检查；本轮未声称人工逐关通读和全部音频听感验收。桌面快捷方式详情见 DesktopShortcut.txt。

## 文件与回退

- 正式素材：SourceAssets/Codex_StoryRevision_20261102/Illustrations/。
- 运行时素材：Assets/_Project/Resources/Art/Illustrations/。
- 新剧本与播放配置：Assets/_Project/Data/Dialogue/、Assets/_Project/Resources/Data/Dialogue/。
- 第五关视觉触发点：StoryVisualCues.json；演出脚本 SelfCrossingCast.cs。
- 首页 AI 说明：MainMenuPanel.cs。
- 本批改前备份位于工作区 Integration/Backup；旧候选构建未覆盖。
- 本批源文件副本位于工作区 Integration/ProjectFiles，验证日志及画面位于 Integration/Verification。
