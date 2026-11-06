# R01 资源交接：完整剧本与文字数据

批次：R01_Text_20261026_v1.0　｜　文本版本：1.0.0　｜　状态：文字和结构已核对，待Codely导入/实玩与用户审阅。

## 已交付

- Story_Final.md：五关完整可读剧本，共237条正式对白，不是剧情梗概。
- TXT_CH01.json–TXT_CH05.json：五章数据，包括全部53个阶段目标、实际对白段及5个任务物描述。
- TXT_UI.json、UI_Copy.md：201条菜单/教程/目标/机关/重试/回忆/名单文案；可读版与JSON同步。
- PresentationMap.json：6条主线回看演出的场景、段落和演出指示。
- CharacterDisplayNames.json：12个演员的显示称呼；也可读取TXT_UI中的speaker键。
- TextIntegration.md：主线与回看分工、53阶段对照、导入规则。
- StoryContinuityCheck.md：实际文字与结构核查记录。
- AssetDelivery.json：本批文件登记。

## 给Codely的动作

1. 把本包作为文本批次接收；JSON导入到Assets/_Project/Data/Dialogue，PresentationMap转成现有演出配置，文档归档到Docs。使用既定接口，旧工程字段不同就在导入器做适配，不手工改几百条文字。
2. 主线由stageId调度对白；没有segment的阶段照常执行机关/渡河。不要把PresentationMap的回看顺序整段重放到主线，尤其第四关。
3. 用正式文本替换相同ID的占位内容，保留已实现流程。校验回看、暂停、读完最后一句后的演出完成、河段重试，以及中文排版。返回CodelyHandoff.md中的实际导入结果。

## 当前边界

尚未收到CodelyHandoff，无法确认实际版本/已实现模块。本批没有生成C#、背景、角色图片、UI皮肤或音频。第三关音序和WAV后续交R15；美术从R02风格样板开始。可选蒙太奇与狗不影响本批完整故事。

credits.author里的作者署名待填写，是唯一发布前文本占位；其余对白已写完整。minigame.stools.progress中的{count}/{total}是运行时变量，须由脚本替换。

## 本稿具体创作选择

奶奶用老花镜、爷爷用拐杖；另外两位队友显示为贝斯手与主唱；老师的课写“用具体的小事写一个人”。这些是为既定需求补齐的剧本细节，不是新增机制或新角色。第一关名仍为工作名《再吃一顿饭》。

## 下一步

继续R02确认像素画风样板；Codely可立即用本批文本做数据导入和后续章节接入，不必等全部美术完成。
