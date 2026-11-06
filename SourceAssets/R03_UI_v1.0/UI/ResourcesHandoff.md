# R03 UI 资源交接

批次：R03_UI_20261027_v1.0。资源生成和静态检查完成；未导入团结引擎，未替代 Codely 进行脚本开发或实机验收。

## 文件入口

- UI/UI_Design.md：界面规范、行为与检查范围。
- UI/UI_Layout.json：25个界面、182个控件，1280×720左上原点坐标。
- UI/R03_CodelyIntegration.md：一页接入说明。
- UI/Skins：3张可九切片的透明边角 PNG。
- UI/Icons：5张32×32 RGBA图标。
- UI/Fonts：Noto Sans CJK SC Regular 原版、许可原文与来源说明。
- UI/UI_TextAdditions.json：仅3个新增UI键；UI_Copy_Reference.json 是预览副本，不作为运行时第二份数据。
- UI/UI_Overview.html 与 UI/UI_Wireframes.html：界面预览和线框；浏览器交互尚未在本环境测试。
- UI/Previews/Static：静态效果图；StaticQA.json 和 ValidationReport.json：实际检查记录。
- UI/SourceAssets：8张图像原始生成文件、实际提示词和尺寸/透明度处理记录。
- UI/AssetDelivery.json：相对包根目录的资源清单、尺寸与SHA-256。
- Style：已批准R02母图和规范快照，便于一致引用。

用户允许对AI图形做裁切、缩放与透明边缘规整；实际PNG使用该处理，不是从截图硬切。界面上的场景和机关示意仅为设计参考，不是正式背景或机关资源。

## 检查和接入状态

8个正式图形尺寸/透明通道合格；静态检查含两种分辨率54个界面状态、237条对白，未检测文本溢出；字体覆盖检查697个不同字符无缺字。UI文字字号、焦点、输入/暂停、图形导入、九切片、C03实际热点与团结引擎运行仍需Codely实测。预览HTML做了语法检查，尚未在浏览器中实测交互。

下一批资源：R04场景布局，接下来才根据布局制作背景。Codely应以实际C03交接为准协调机关坐标。本批没有生成C#脚本，也没有写入游戏工程。
