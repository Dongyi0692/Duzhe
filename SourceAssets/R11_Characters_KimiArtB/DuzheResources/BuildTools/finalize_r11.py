from __future__ import annotations

import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
if (ROOT / "Characters").is_dir() and (ROOT / "Delivery").is_dir():
    PACKAGE = ROOT
else:
    PACKAGE = ROOT / "outputs" / "DuzheResources"
DELIVERY = PACKAGE / "Delivery"
CHARACTERS = PACKAGE / "Characters"
SOURCES = PACKAGE / "SourceAssets" / "Characters"

ASSETS = [
    ("CHR02_Grandfather", "Grandfather", ["CH01"], ["REF_R02_VisualGuide", "CHR03_Grandmother"]),
    ("CHR03_Grandmother", "Grandmother", ["CH01"], ["REF_R02_VisualGuide"]),
    ("CHR05_LoverSpirit", "LoverSpirit", ["CH02"], ["REF_R02_VisualGuide", "CHR03_Grandmother"]),
    ("CHR07_DrummerSpirit", "DrummerSpirit", ["CH03"], ["REF_R02_VisualGuide", "CHR03_Grandmother"]),
    ("CHR09_BandmateA", "BandmateA", ["CH03"], ["REF_R02_VisualGuide", "CHR07_DrummerSpirit"]),
    ("CHR10_BandmateB", "BandmateB", ["CH03"], ["REF_R02_VisualGuide", "CHR07_DrummerSpirit"]),
    ("CHR11_TeacherSpirit", "TeacherSpirit", ["CH04"], ["REF_R02_VisualGuide", "CHR03_Grandmother"]),
]

VISUAL_NOTES = {
    "CHR02_Grandfather": "温和农村老人；灰短发与灰橄榄外套清楚；无拐杖、眼镜或手持物。",
    "CHR03_Grandmother": "灰白盘发、灰紫开衫与浅内搭符合母图；无眼镜或手持物；较其他成人略矮。",
    "CHR05_LoverSpirit": "年轻女性、黑色中长发与柔绿开衫清楚；无项链；非透明残影。",
    "CHR07_DrummerSpirit": "年轻男性、蓬松短发与灰锈棕外套清楚；无鼓槌、鼓或武器。",
    "CHR09_BandmateA": "年轻男性、侧分短发与灰牛仔蓝外套清楚；无贝斯或其他手持物。",
    "CHR10_BandmateB": "年轻女性、高马尾与低饱和赭黄外套清楚；无麦克风或乐器。",
    "CHR11_TeacherSpirit": "成年教师、深色低盘发与米色外套清楚；无教案、书本或纸张。",
}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def validate_asset(asset_id: str) -> dict:
    path = CHARACTERS / f"{asset_id}.png"
    master = SOURCES / f"{asset_id}_master.png"
    processing = SOURCES / f"{asset_id}_processing.json"
    im = Image.open(path)
    alpha = im.getchannel("A") if im.mode == "RGBA" else None
    bbox = alpha.getbbox() if alpha else None
    values = set(alpha.getdata()) if alpha else set()
    mask = np.array(alpha) > 0 if alpha else np.zeros((48, 32), dtype=bool)
    _, components = ndimage.label(mask)
    bottom_x = np.where(mask[-1, :])[0] if mask.size else []
    foot_center = float(bottom_x.mean()) if len(bottom_x) else None
    colors = im.getcolors(maxcolors=32 * 48) or []
    opaque_color_count = sum(1 for _, rgba in colors if len(rgba) == 4 and rgba[3] == 255)
    master_im = Image.open(master)
    master_alpha = master_im.getchannel("A") if master_im.mode == "RGBA" else None
    checks = {
        "fileExists": path.is_file(),
        "size32x48": im.size == (32, 48),
        "rgba": im.mode == "RGBA",
        "trueTransparentAndOpaqueAlpha": values == {0, 255},
        "contentInsideCanvas": bool(bbox and bbox[0] > 0 and bbox[1] > 0 and bbox[2] < 32 and bbox[3] <= 48),
        "singleConnectedComponent": components == 1,
        "footBaselineAtBottom": bool(bbox and bbox[3] == 48),
        "footCenterNearCanvasCenter": foot_center is not None and abs(foot_center - 15.5) <= 3.0,
        "masterExists1024x1536RGBA": master_im.size == (1024, 1536) and master_im.mode == "RGBA",
        "masterHasTrueAlpha": bool(master_alpha and min(master_alpha.getdata()) == 0 and max(master_alpha.getdata()) > 0),
        "processingReportExists": processing.is_file(),
        "colorCountAtMost24": opaque_color_count <= 24,
    }
    return {
        "assetId": asset_id,
        "width": im.width,
        "height": im.height,
        "mode": im.mode,
        "alphaValues": sorted(values),
        "contentBBox": list(bbox) if bbox else None,
        "connectedComponents": int(components),
        "footCenterX": foot_center,
        "opaqueColorCount": opaque_color_count,
        "masterWidth": master_im.width,
        "masterHeight": master_im.height,
        "masterMode": master_im.mode,
        "masterAlphaExtrema": list(master_alpha.getextrema()) if master_alpha else None,
        "checks": checks,
        "technicalStatus": "Passed" if all(checks.values()) else "Failed",
        "visualStatus": "Reviewed",
        "visualNotes": VISUAL_NOTES[asset_id],
        "sha256": sha256(path),
    }


def make_preview() -> None:
    scale = 4
    cell_w, cell_h = 176, 238
    sheet = Image.new("RGB", (cell_w * len(ASSETS), cell_h), (38, 53, 68))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default(size=12)
    for index, (asset_id, _, _, _) in enumerate(ASSETS):
        im = Image.open(CHARACTERS / f"{asset_id}.png").convert("RGBA")
        enlarged = im.resize((32 * scale, 48 * scale), Image.Resampling.NEAREST)
        x = index * cell_w + (cell_w - enlarged.width) // 2
        sheet.paste(enlarged, (x, 12), enlarged)
        label = asset_id
        bbox = draw.textbbox((0, 0), label, font=font)
        tx = index * cell_w + (cell_w - (bbox[2] - bbox[0])) // 2
        draw.text((tx, 214), label, fill=(183, 197, 204), font=font)
    sheet.save(SOURCES / "R11_Characters_Preview.png", optimize=True)

    contrast = Image.new("RGB", (cell_w * len(ASSETS), cell_h * 2), (38, 53, 68))
    cdraw = ImageDraw.Draw(contrast)
    for row, bg_color in enumerate(((38, 53, 68), (215, 188, 141))):
        for index, (asset_id, _, _, _) in enumerate(ASSETS):
            cell = Image.new("RGBA", (cell_w, cell_h), (*bg_color, 255))
            im = Image.open(CHARACTERS / f"{asset_id}.png").convert("RGBA")
            enlarged = im.resize((32 * scale, 48 * scale), Image.Resampling.NEAREST)
            cell.alpha_composite(enlarged, ((cell_w - enlarged.width) // 2, 12))
            contrast.paste(cell.convert("RGB"), (index * cell_w, row * cell_h))
            cdraw.text((index * cell_w + 8, row * cell_h + 214), asset_id, fill=(20, 28, 36) if row else (183, 197, 204), font=font)
    contrast.save(SOURCES / "R11_AlphaContrast_QA.png", optimize=True)


def main() -> None:
    DELIVERY.mkdir(parents=True, exist_ok=True)
    results = [validate_asset(asset_id) for asset_id, _, _, _ in ASSETS]
    if any(item["technicalStatus"] != "Passed" for item in results):
        (DELIVERY / "VisualQA_R11.json").write_text(json.dumps({"items": results}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        failed = [item["assetId"] for item in results if item["technicalStatus"] != "Passed"]
        raise SystemExit(f"Technical QA failed: {failed}")
    make_preview()

    qa_doc = {
        "schemaVersion": "1.0",
        "batchId": "R11_Characters_KimiArtB_20261027_v1.0",
        "technicalStatus": "Passed",
        "visualStatus": "Reviewed",
        "engineImportStatus": "Not imported; Codely must validate in the actual Unity project",
        "items": results,
    }
    (DELIVERY / "VisualQA_R11.json").write_text(json.dumps(qa_doc, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    lines = [
        "# R11 七位配角视觉与技术检查",
        "",
        "- 技术结果：7/7 通过尺寸、RGBA、真 Alpha、画布边界、单连通主体、脚底基线、中心偏移、母图与颜色数量检查。",
        "- 视觉结果：7/7 已逐张查看原始母图和最终放大预览；轮廓、年龄、服装、无多余手持物已复核。",
        "- 边界：未在 Unity 工程中导入，未做实机运行验收；状态不等于 Imported/Verified。",
        "- 对照图：`SourceAssets/Characters/R11_Characters_Preview.png` 与 `SourceAssets/Characters/R11_AlphaContrast_QA.png`。",
        "",
        "| assetId | 技术 | 视觉复核 | 脚底中心 x | 主体 bbox |",
        "|---|---|---|---:|---|",
    ]
    for item in results:
        lines.append(f"| {item['assetId']} | {item['technicalStatus']} | {item['visualStatus']} | {item['footCenterX']:.2f} | {item['contentBBox']} |")
    lines += ["", "## 逐角色视觉说明", ""]
    for item in results:
        lines.append(f"- **{item['assetId']}**：{item['visualNotes']}")
    (DELIVERY / "VisualQA_R11.md").write_text("\n".join(lines) + "\n", encoding="utf-8")

    items = []
    for asset_id, actor_id, chapters, references in ASSETS:
        path = CHARACTERS / f"{asset_id}.png"
        items.append({
            "assetId": asset_id,
            "actorId": actor_id,
            "category": "Character",
            "relativePath": f"Characters/{asset_id}.png",
            "chapterIds": chapters,
            "version": "1.0.0",
            "status": "Reviewed",
            "width": 32,
            "height": 48,
            "frameLayout": None,
            "durationSeconds": None,
            "sourceTool": "image_generation via agent-gw; deterministic Pillow normalization",
            "promptId": "R11_KimiArtB_Characters_CG_v2",
            "referenceIds": references,
            "usageStatus": "Reviewed source asset; ready for Codely import validation, not engine-imported or runtime-tested",
            "sourceAssetPath": f"SourceAssets/Characters/{asset_id}_master.png",
            "processingReportPath": f"SourceAssets/Characters/{asset_id}_processing.json",
            "previewPath": f"SourceAssets/Characters/{asset_id}_preview.png",
            "notes": "Single formal R11 idle stance, 32x48 RGBA true-alpha PNG, 32 PPU, pivot=(0.5,0). Not an R12 walk sheet. Master is 1024x1536 RGBA; final frame uses deterministic crop, cleanup, resize and 24-color quantization.",
            "sha256": sha256(path),
        })
    manifest = {
        "schemaVersion": "1.0",
        "batchId": "R11_Characters_KimiArtB_20261027_v1.0",
        "items": items,
        "scope": "Seven supporting characters assigned to Kimi Art B; five protagonist-related characters are assigned to the resource assistant and are not included.",
        "taskStatus": "Complete for assigned seven-character scope",
    }
    (DELIVERY / "AssetDelivery.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"technicalPassed": len(results), "manifestItems": len(items)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
