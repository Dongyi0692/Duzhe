# R03 内置图像生成提示词记录

模式：内置 image_gen，8 次独立生成；均请求透明背景。R02 母图与前一批图形依次作为画风参考。下面保存每次使用的实际提示词。获得用户同意后，仅做 alpha 阈值规整、裁切、最近邻缩放、面板中心区域九宫拉伸或图标留白；没有重绘。实际尺寸和步骤见 ProcessingLog.jsonl。

## UI01_DialoguePanel
参考：Style/VisualGuide.png。

Use case: ui-mockup. Asset: UI01_DialoguePanel, ONE production game UI skin on a truly transparent background.
Use the input image ONLY as a color and pixel-border reference for its empty dialogue box; do not include the scene or any characters.
Generate a standalone empty horizontal rectangular dialogue panel, aspect ratio 8:3, intended final size256x96 pixels. It occupies almost the full image with a small even transparent margin.
Flat deep blue-grey interior #263544, muted light blue-grey #B7C5CC single thin pixel border and a narrow darker outer outline. Small restrained stair-step corner cuts, about4 final pixels, like the reference. Broad empty interior; very subtle two-tone shading only. Long edges are perfectly straight, axis aligned and uniform so they can be nine-sliced. All ornament confined to the outermost12 final pixels. No shadow extending outside, no glow, no gradient, no texture/noise, no internal dividing lines.
Deliberate chunky pixel art, crisp square pixels and stepped corners, not glossy beveled fantasy metal. The panel itself is opaque; only the exterior and cutout corners have real alpha transparency. No text, symbols, portraits, logos, buttons, background scene, mock checkerboard, palette swatches or comparison panels. Exactly ONE panel. Render a large clean source suitable for deterministic nearest-neighbor size normalization.

## UI01_GenericPanel
参考：Skins/UI01_DialoguePanel.png。

Use case: ui-mockup. Asset: UI01_GenericPanel. Generate ONE empty square pixel-art game UI panel as a separate transparent PNG.
The input is the dialogue-panel style reference. Match its restrained blue-grey interior and fine pale blue-grey border exactly, but make this panel SQUARE, intended final size192x192.
One straight axis-aligned rectangle with very small stepped corner cutouts. Flat #263544 fill, thin #B7C5CC border and narrow dark outer outline. Keep all decorative pixels in the outer12 final pixels. Edges uniform and center empty for nine-slice scaling. No rounded smooth corners, glow, ornate metalwork, drop shadow or extra inset panels. At most a subtle two-tone fill. Exterior truly transparent alpha; panel interior opaque. No text, icons, logo, mock checkerboard or additional objects. Large clean pixel-art source for deterministic normalization.

## UI01_Button
参考：Skins/UI01_GenericPanel.png。

Use case: ui-mockup. Asset: UI01_Button. Generate ONE standalone empty horizontal game UI button skin on a genuinely transparent exterior.
Use the input panel only for consistent colors and stepped-corner pixel border. The button is a small rectangular strip, aspect10:3, intended final160x48 pixels. Medium muted slate-blue #3E5365 interior, fine pale blue-grey #B7C5CC outline, narrow dark #263544 outer edge. Tiny2–3 final-pixel stair-step corner cuts. Straight perfectly horizontal and vertical sides; all details confined to outer8 final pixels for nine-slice scaling. Broad empty center for engine-rendered labels. No text, icon, logo, elaborate ornament, gloss, drop shadow, glow, noise or extra states. Only one normal-state button; hover and focus will be handled by the engine. Match the simple restrained pixel UI of the reference. Panel body opaque, exterior/cut corners true alpha; no checkerboard painted into pixels.

## UI02_Interact
参考：Skins/UI01_GenericPanel.png。

Use case: ui-mockup. Asset: UI02_Interact. ONE small pixel-game interaction icon on true transparent alpha.
Subject: a simple upright hand cursor, index finger pointing UP, thumb distinct, other fingers folded. Easy readable compact silhouette. Pale desaturated blue-grey #B7C5CC fill, a small #718596 shadow cluster, thin dark blue-grey #263544 outline. Neutral graphic symbol, not a realistic skin-colored hand.
Intended final icon32x32 with shape inside24x24 and4px empty padding. Enlarge the simple low-resolution pixel pattern cleanly for source output. Chunky intentional square pixel clusters, one logical pixel outline, no antialias painterly detail. Centered on a square canvas. The attached panel is only the palette/style reference; do NOT include any panel or border around the icon.
No text, letters, click rays, sparkle, numbers, circle badge, button background, logo, gradients or drop shadow. Exactly one symbol, no variations. Exterior truly transparent, no fake checkerboard.

## UI02_Item
参考：SourceAssets/UI02_Interact.png。

Use case: ui-mockup. Asset UI02_Item: ONE simple pixel-art small cloth pouch/inventory bag icon, front view, on true transparent background. Small rounded rectangular bag body with folded flap and one simple central fastening; no straps spreading outside, no coins or tools. Intended32x32 icon, silhouette in24x24 with4px transparent padding; draw large but with clear chunky low-resolution pixel clusters. Pale desaturated blue-grey #B7C5CC fill, one #718596 shadow shade, thin #263544 dark outline. The attached hand icon is only a palette, outline and pixel-density reference. Do not include the hand. Keep the bag recognizable and clean at tiny size. No text, letters, numbers, labels, glow, background tile, circle badge, drop shadow, checkerboard or extra icons. Exactly one isolated game UI icon.

## UI02_Pause
参考：SourceAssets/UI02_Interact.png。

Use case: ui-mockup. Asset UI02_Pause. ONE isolated simple pixel-game pause icon on genuinely transparent background: exactly TWO equal upright rectangular pause bars, equal widths and heights, with a clearly visible gap between them. Symmetrical and centered. Pale desaturated blue-grey #B7C5CC inside, narrow dark blue-grey #263544 outline, at most one muted #718596 side shade. Tiny stepped pixel corners if needed. Intended32x32 icon, symbol within24x24 with4px transparent padding; source can be large but must resemble a crisp enlarged low-resolution pixel pattern. Reference hand icon supplies only palette and pixel density: do NOT draw a hand, panel, round badge, frame or button. No text, no numbers, no glow, gradient or cast shadow, no fake checkerboard. Only the two bars.

## UI02_Volume
参考：SourceAssets/UI02_Pause.png。

Use case: ui-mockup. Asset UI02_Volume. Exactly ONE simple readable pixel-art volume icon on transparent alpha: a compact side-facing loudspeaker pointing RIGHT, with exactly two separated short sound-wave arcs on its right. Speaker contains a small rectangle joined to a short widening cone. No realistic speaker hardware.
Use pale blue-grey #B7C5CC fill and a thin #263544 dark outline, at most one #718596 shade; matching the reference's quiet pixel UI palette. Intended final32x32 icon, shape fits24x24 with4px padding; draw as a clean enlarged low-resolution sprite with stair-step arcs and chunky square pixels. Ensure waves remain separated and legible at tiny size.
Reference is palette/outline only, do not depict its pause bars. No circle, button background, badge, mute slash, letters, text, glow, drop shadow, extra icon or fake checkerboard. Real alpha exterior.

## UI02_Continue
参考：SourceAssets/UI02_Volume.png。

Use case: ui-mockup. Asset UI02_Continue. ONE simple pixel-art dialogue-continue icon: a single solid RIGHT-POINTING triangular arrowhead with stair-step diagonal sides, no stem, no double chevrons. Clearly centered and recognizable at32x32.
Use pale desaturated blue-grey #B7C5CC inside, thin dark blue-grey #263544 outline, one small #718596 shade at most. Reference image supplies palette and pixel feel only; do not reproduce its speaker. Symbol fits24x24 inside a32x32 conceptual canvas with4px empty padding. Large source is okay, keep the appearance of an enlarged coarse pixel sprite. No text, label, number, circle, button panel, badge, light rays, glow, drop shadow, antialias blur, extra icons or checkerboard. Exterior true transparent alpha.

