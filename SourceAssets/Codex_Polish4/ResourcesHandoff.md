# 本轮资源与实现交接
日期：2026-10-31

BG06_Classroom.png：图像生成工具编辑原教室背景，缩小七组桌椅，保留原位置与房间布置。母图 BG06_Classroom_original.png；导入图 1280×720，最近邻规整。旧图保存在 BG06_Classroom_previous.png。
提示词要点：Preserve exact camera, room, board, door, windows, cabinets, lighting, tile grid and seven desk centers. Reduce each desk-chair set to about 55% width and height. Repair exposed floor tiles. Pixel art; no people, UI or text.
使用方式：导入 BG06_Classroom，同步缩小七张课桌的碰撞区域。

关卡：主线与章节回放都连续进入下一章，第五章完成统一返回 Home。
渡河：船缩至72%，乘客保持原比例并排站在前部，渡客在后部；前四关六波，漩涡速度6、转向5.5、漩涡判定半宽/高0.6、斜向横速±0.28。保留原章节乘客数量与失败局部重试。128种子检查可达路线。
描画：判定走廊从短边5%增为7.5%；抬笔保留当前笔画，各次落笔分开记录，评分不在空中连线；暂停也保留当前笔画。

图像工具：内置 image_gen（edit 模式），基于工程原 BG06_Classroom.png 编辑。
完整生成提示词：
Edit target: attached 1280x720 pixel art top-down classroom game background. Preserve EXACT camera angle, room size, board, door, windows, cabinets, lighting, tile grid, and positions and count of all seven desk-chair sets. Only make each of the seven student desk-and-chair sets 55 percent of its current width and height, centered at its original footprint. Repaint exposed floor naturally with continuous matching floor tiles. Desks currently enormous relative to game characters; make them compact individual student desks. Keep same pixel art style. No people, no UI, no text. Keep each desk center at original coordinates, no additional desks. Return same aspect ratio.

最终船只前进距离：10秒内5单位；运行镜头半高8.5，确保全船可见。
