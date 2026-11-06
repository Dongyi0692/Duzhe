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
DELIVERY = PACKAGE / "Delivery" / "R12"
ANIMATIONS = PACKAGE / "Characters" / "Animations"
SOURCES = PACKAGE / "SourceAssets" / "Animations"
DIRECTIONS = ["Down", "Left", "Right", "Up"]
POSES = ["WalkA", "Idle", "WalkB"]
ASSETS = [
    ("CHR02_Grandfather", "Grandfather", "CH01"),
    ("CHR03_Grandmother", "Grandmother", "CH01"),
    ("CHR05_LoverSpirit", "LoverSpirit", "CH02"),
    ("CHR07_DrummerSpirit", "DrummerSpirit", "CH03"),
    ("CHR11_TeacherSpirit", "TeacherSpirit", "CH04"),
]
VISUAL_NOTES = {
    "CHR02_Grandfather": "灰短发、灰橄榄外套与老人比例保持一致；四向跨步与手臂变化可读。",
    "CHR03_Grandmother": "灰白盘发、灰紫开衫和较矮比例保持一致；Down/Idle 与 R11 完全一致。",
    "CHR05_LoverSpirit": "中长发、柔绿开衫和裤装保持一致；侧面发束与步态方向正确。",
    "CHR07_DrummerSpirit": "蓬松短发、灰锈棕外套与运动鞋保持一致；左右 WalkB 已纠正朝向。",
    "CHR11_TeacherSpirit": "深色低盘发、米棕外套和灰蓝内搭保持一致；四向步态清楚。",
}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def validate_asset(asset_id: str) -> dict:
    sheet_path = ANIMATIONS / f"{asset_id}.png"
    source_dir = SOURCES / asset_id
    processing_path = source_dir / f"{asset_id}_processing.json"
    processing = json.loads(processing_path.read_text(encoding="utf-8"))
    sheet = Image.open(sheet_path).convert("RGBA")
    frames = []
    all_hashes = []
    for row, direction in enumerate(DIRECTIONS):
        for col, pose in enumerate(POSES):
            cell = sheet.crop((col * 32, row * 48, (col + 1) * 32, (row + 1) * 48))
            alpha = cell.getchannel("A")
            array = np.array(alpha)
            bbox = alpha.getbbox()
            alpha_values = np.unique(array).tolist()
            _, components = ndimage.label(array > 0)
            bottom_x = np.where(array[-1] > 0)[0]
            foot_center = float(bottom_x.mean()) if len(bottom_x) else None
            bbox_center = (bbox[0] + bbox[2] - 1) / 2 if bbox else None
            cell_path = source_dir / "Frames" / f"{direction}_{pose}.png"
            raw_path = source_dir / "SourceFrames" / f"{direction}_{pose}.png"
            frame_hash = sha256(cell_path)
            all_hashes.append(frame_hash)
            checks = {
                "cell32x48RGBA": cell.size == (32, 48) and cell.mode == "RGBA",
                "trueBinaryAlpha": alpha_values == [0, 255],
                "contentInsideCell": bool(bbox and bbox[0] > 0 and bbox[1] > 0 and bbox[2] < 32 and bbox[3] <= 48),
                "singleConnectedComponent": components == 1,
                "bottomBaseline": bool(bbox and bbox[3] == 48),
                "bodyCenterNearPivot": bbox_center is not None and abs(bbox_center - 15.5) <= 5.0,
                "idleFootCentered": pose != "Idle" or (foot_center is not None and abs(foot_center - 15.5) <= 3.5),
                "finalFrameFileExists": cell_path.is_file(),
                "sourceFrameFileExists": raw_path.is_file(),
            }
            frames.append({
                "direction": direction,
                "pose": pose,
                "bbox": list(bbox) if bbox else None,
                "alphaValues": alpha_values,
                "connectedComponents": int(components),
                "footCenterX": foot_center,
                "bboxCenterX": bbox_center,
                "sha256": frame_hash,
                "checks": checks,
                "status": "Passed" if all(checks.values()) else "Failed",
            })

    r11 = Image.open(PACKAGE / "Characters" / f"{asset_id}.png").convert("RGBA")
    down_idle = sheet.crop((32, 0, 64, 48))
    r11_idle_match = np.array_equal(np.array(r11), np.array(down_idle))
    direction_results = []
    for direction in DIRECTIONS:
        records = [f for f in frames if f["direction"] == direction]
        hashes = [f["sha256"] for f in records]
        arrays = [np.array(sheet.crop((c * 32, DIRECTIONS.index(direction) * 48, (c + 1) * 32, (DIRECTIONS.index(direction) + 1) * 48))) for c in range(3)]
        direction_results.append({
            "direction": direction,
            "uniqueFrames": len(set(hashes)),
            "walkAIdleDifferentPixels": int(np.count_nonzero(np.any(arrays[0] != arrays[1], axis=2))),
            "walkBIdleDifferentPixels": int(np.count_nonzero(np.any(arrays[2] != arrays[1], axis=2))),
            "walkAWalkBDifferentPixels": int(np.count_nonzero(np.any(arrays[0] != arrays[2], axis=2))),
        })

    gif_path = source_dir / f"{asset_id}_walk_preview.gif"
    gif = Image.open(gif_path)
    sheet_checks = {
        "sheet96x192RGBA": sheet.size == (96, 192) and sheet.mode == "RGBA",
        "all60CellChecksPass": all(f["status"] == "Passed" for f in frames),
        "twelveUniqueFrames": len(set(all_hashes)) == 12,
        "threeUniquePosesPerDirection": all(d["uniqueFrames"] == 3 for d in direction_results),
        "allWalkPairsDiffer": all(min(d["walkAIdleDifferentPixels"], d["walkBIdleDifferentPixels"], d["walkAWalkBDifferentPixels"]) > 0 for d in direction_results),
        "downIdleExactlyMatchesR11": r11_idle_match,
        "animatedPreviewFourFrames": gif.n_frames == 4,
        "processingReportExists": processing_path.is_file(),
    }
    return {
        "assetId": asset_id,
        "sheetSha256": sha256(sheet_path),
        "uniqueFrameCount": len(set(all_hashes)),
        "r11IdleMatch": r11_idle_match,
        "directionChecks": direction_results,
        "frameChecks": frames,
        "sheetChecks": sheet_checks,
        "technicalStatus": "Passed" if all(sheet_checks.values()) else "Failed",
        "visualStatus": "Reviewed",
        "visualNotes": VISUAL_NOTES[asset_id],
        "swapSideWalkB": processing["swapSideWalkB"],
        "staticPreview": f"SourceAssets/Animations/{asset_id}/{asset_id}_sheet_preview.png",
        "animatedPreview": f"SourceAssets/Animations/{asset_id}/{asset_id}_walk_preview.gif",
    }


def make_contact_sheet() -> None:
    scale, gap, label_h = 2, 12, 30
    cell_w, cell_h = 96 * scale, 192 * scale
    width = len(ASSETS) * (cell_w + gap) + gap
    height = label_h + cell_h + gap
    image = Image.new("RGBA", (width, height), (38, 53, 68, 255))
    draw = ImageDraw.Draw(image)
    font = ImageFont.load_default(size=13)
    for index, (asset_id, _, _) in enumerate(ASSETS):
        sheet = Image.open(ANIMATIONS / f"{asset_id}.png").convert("RGBA")
        enlarged = sheet.resize((cell_w, cell_h), Image.Resampling.NEAREST)
        x = gap + index * (cell_w + gap)
        image.alpha_composite(enlarged, (x, label_h))
        label = asset_id
        box = draw.textbbox((0, 0), label, font=font)
        draw.text((x + (cell_w - (box[2] - box[0])) // 2, 8), label, fill=(183, 197, 204), font=font)
    image.convert("RGB").save(SOURCES / "R12_WalkSheets_Preview.png", optimize=True)


def make_combined_gif() -> None:
    scale, left, top, row_h = 3, 160, 28, 156
    width, height = left + 4 * 32 * scale, top + len(ASSETS) * row_h
    font = ImageFont.load_default(size=12)
    output_frames = []
    for pose in ["WalkA", "Idle", "WalkB", "Idle"]:
        canvas = Image.new("RGBA", (width, height), (38, 53, 68, 255))
        draw = ImageDraw.Draw(canvas)
        for col, direction in enumerate(DIRECTIONS):
            draw.text((left + col * 32 * scale + 24, 7), direction, fill=(183, 197, 204), font=font)
        for row, (asset_id, _, _) in enumerate(ASSETS):
            sheet = Image.open(ANIMATIONS / f"{asset_id}.png").convert("RGBA")
            draw.text((8, top + row * row_h + 62), asset_id, fill=(183, 197, 204), font=font)
            for col, direction in enumerate(DIRECTIONS):
                direction_row = DIRECTIONS.index(direction)
                pose_col = POSES.index(pose)
                cell = sheet.crop((pose_col * 32, direction_row * 48, (pose_col + 1) * 32, (direction_row + 1) * 48))
                enlarged = cell.resize((32 * scale, 48 * scale), Image.Resampling.NEAREST)
                canvas.alpha_composite(enlarged, (left + col * 32 * scale, top + row * row_h + 6))
        output_frames.append(canvas.convert("RGB").quantize(colors=128, dither=Image.Dither.NONE))
    output_frames[0].save(SOURCES / "R12_AllCharacters_Preview.gif", save_all=True, append_images=output_frames[1:], duration=170, loop=0, disposal=2, optimize=False)


def main() -> None:
    DELIVERY.mkdir(parents=True, exist_ok=True)
    results = [validate_asset(asset_id) for asset_id, _, _ in ASSETS]
    failed = [r["assetId"] for r in results if r["technicalStatus"] != "Passed"]
    if failed:
        (DELIVERY / "AnimationQA.json").write_text(json.dumps({"items": results}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        raise SystemExit(f"QA failed: {failed}")
    make_contact_sheet()
    make_combined_gif()

    qa = {
        "schemaVersion": "1.0",
        "batchId": "R12_SupportingWalking_20261028_v1.0",
        "technicalStatus": "Passed",
        "visualStatus": "Reviewed",
        "reviewedFrameCount": 60,
        "engineImportStatus": "Not imported or runtime-tested",
        "items": results,
    }
    (DELIVERY / "AnimationQA.json").write_text(json.dumps(qa, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    md = [
        "# R12 五套配角行走动画 QA",
        "",
        "- 技术结果：5/5 图集、60/60 帧通过检查。",
        "- 每套均为 96×192 RGBA，3 列×4 行，帧格 32×48，Alpha 仅含 0/255。",
        "- 每套 12 帧唯一；每个方向的 WalkA、Idle、WalkB 均实际不同。",
        "- 每套 Down/Idle 与对应 R11 正式站姿逐像素一致。",
        "- 已逐套查看 12 帧放大图；另交每角色 4 步循环 GIF 与全员动画预览。",
        "- 未在 Unity 中导入或实机播放验证，状态不等于 Imported/Verified。",
        "",
        "| assetId | 技术 | 视觉 | 唯一帧 | R11 Idle | 侧向 WalkB 纠正 |",
        "|---|---|---|---:|---|---|",
    ]
    for result in results:
        md.append(f"| {result['assetId']} | {result['technicalStatus']} | {result['visualStatus']} | {result['uniqueFrameCount']} | {result['r11IdleMatch']} | {result['swapSideWalkB']} |")
    md += ["", "## 视觉复核", ""]
    for result in results:
        md.append(f"- **{result['assetId']}**：{result['visualNotes']}")
    (DELIVERY / "AnimationQA.md").write_text("\n".join(md) + "\n", encoding="utf-8")

    layout = {
        "columns": 3,
        "rows": 4,
        "cellWidth": 32,
        "cellHeight": 48,
        "rowOrder": DIRECTIONS,
        "columnOrder": POSES,
    }
    animation_items = []
    delivery_items = []
    for (asset_id, actor_id, chapter), result in zip(ASSETS, results):
        source_dir = SOURCES / asset_id
        frame_entries = [
            {"direction": d, "pose": p, "relativePath": f"SourceAssets/Animations/{asset_id}/Frames/{d}_{p}.png"}
            for d in DIRECTIONS for p in POSES
        ]
        animation_items.append({
            "assetId": asset_id,
            "relativePath": f"Characters/Animations/{asset_id}.png",
            "frameLayout": layout,
            "fps": 6,
            "idleColumnIndex": 1,
            "footPivot": {"x": 0.5, "y": 0},
            "cycleFrameCount": 3,
            "durationSeconds": 0.5,
            "frames": frame_entries,
        })
        delivery_items.append({
            "assetId": asset_id,
            "actorId": actor_id,
            "category": "CharacterAnimation",
            "relativePath": f"Characters/Animations/{asset_id}.png",
            "chapterIds": [chapter],
            "version": "1.0.0",
            "status": "Reviewed",
            "width": 96,
            "height": 192,
            "frameLayout": layout,
            "durationSeconds": 0.5,
            "sourceTool": "image_generation via agent-gw; deterministic Pillow frame slicing and sheet assembly",
            "promptId": "R12_SupportingWalking_20261028_v1",
            "referenceIds": ["REF_R02_VisualGuide", asset_id],
            "usageStatus": "Reviewed animation source; Codely must slice and validate in the actual Unity project",
            "sourceAssetPath": f"SourceAssets/Animations/{asset_id}/{asset_id}_walk_source.png",
            "processingReportPath": f"SourceAssets/Animations/{asset_id}/{asset_id}_processing.json",
            "previewPath": result["staticPreview"],
            "animatedPreviewPath": result["animatedPreview"],
            "notes": "Four-direction 12-frame walking sheet, 6 fps, idle column index 1, pivot=(0.5,0). Down/Idle exactly matches R11. Original source, cleaned source frames and final 32x48 frames retained. Not engine-imported or runtime-tested.",
            "sha256": result["sheetSha256"],
        })

    animation_manifest = {
        "schemaVersion": "1.0",
        "batchId": "R12_SupportingWalking_20261028_v1.0",
        "items": animation_items,
    }
    (DELIVERY / "AnimationManifest.json").write_text(json.dumps(animation_manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    asset_delivery = {
        "schemaVersion": "1.0",
        "batchId": "R12_SupportingWalking_20261028_v1.0",
        "items": delivery_items,
        "scope": "Five supporting-character walking sheets assigned to Kimi Art B; CHR01 is assigned to the resource assistant and is not included.",
        "taskStatus": "Complete for assigned five-character scope",
    }
    (DELIVERY / "AssetDelivery.json").write_text(json.dumps(asset_delivery, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"sheetsPassed": len(results), "framesPassed": sum(len(r["frameChecks"]) for r in results), "animationItems": len(animation_items)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
