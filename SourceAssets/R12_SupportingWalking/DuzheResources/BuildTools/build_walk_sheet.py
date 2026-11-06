from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

DIRECTIONS = ["Down", "Left", "Right", "Up"]
POSES = ["WalkA", "Idle", "WalkB"]


def largest_component(alpha: Image.Image) -> Image.Image:
    labels, count = ndimage.label(np.array(alpha) > 0)
    if count == 0:
        return Image.new("L", alpha.size, 0)
    sizes = np.bincount(labels.ravel())
    sizes[0] = 0
    mask = np.where(labels == int(sizes.argmax()), 255, 0).astype(np.uint8)
    return Image.fromarray(mask, mode="L")


def clean_crop(image: Image.Image, threshold: int = 96) -> tuple[Image.Image, tuple[int, int, int, int]]:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A").point(lambda a: 255 if a >= threshold else 0)
    mask = largest_component(alpha)
    bbox = mask.getbbox()
    if not bbox:
        raise ValueError("No connected character found")
    rgba.putalpha(mask)
    return rgba.crop(bbox), bbox


def normalize(crop: Image.Image, height: int, max_width: int = 30, colors: int = 24) -> Image.Image:
    scale = min(height / crop.height, max_width / crop.width)
    size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
    resized = crop.resize(size, Image.Resampling.LANCZOS)
    alpha = resized.getchannel("A").point(lambda a: 255 if a >= 112 else 0)
    mask = largest_component(alpha)
    resized.putalpha(mask)
    quantized = resized.quantize(colors=colors, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE).convert("RGBA")
    quantized.putalpha(mask)
    canvas = Image.new("RGBA", (32, 48), (0, 0, 0, 0))
    x = (32 - quantized.width) // 2
    y = 48 - quantized.height
    canvas.alpha_composite(quantized, (x, y))
    return canvas


def make_static_preview(sheet: Image.Image, path: Path) -> None:
    scale = 4
    left, top = 86, 32
    enlarged = sheet.resize((sheet.width * scale, sheet.height * scale), Image.Resampling.NEAREST)
    preview = Image.new("RGBA", (left + enlarged.width + 12, top + enlarged.height + 12), (38, 53, 68, 255))
    preview.alpha_composite(enlarged, (left, top))
    preview = preview.convert("RGB")
    draw = ImageDraw.Draw(preview)
    font = ImageFont.load_default(size=13)
    for col, pose in enumerate(POSES):
        x = left + col * 32 * scale + 24
        draw.text((x, 9), pose, fill=(183, 197, 204), font=font)
    for row, direction in enumerate(DIRECTIONS):
        y = top + row * 48 * scale + 82
        draw.text((12, y), direction, fill=(183, 197, 204), font=font)
    preview.save(path, optimize=True)


def make_gif(frames: dict[str, dict[str, Image.Image]], path: Path) -> None:
    scale = 4
    sequence = ["WalkA", "Idle", "WalkB", "Idle"]
    gif_frames = []
    font = ImageFont.load_default(size=13)
    for pose in sequence:
        canvas = Image.new("RGB", (len(DIRECTIONS) * 32 * scale, 48 * scale + 28), (38, 53, 68))
        draw = ImageDraw.Draw(canvas)
        for index, direction in enumerate(DIRECTIONS):
            enlarged = frames[direction][pose].resize((32 * scale, 48 * scale), Image.Resampling.NEAREST)
            canvas.paste(enlarged, (index * 32 * scale, 28), enlarged)
            draw.text((index * 32 * scale + 36, 7), direction, fill=(183, 197, 204), font=font)
        gif_frames.append(canvas.quantize(colors=64, dither=Image.Dither.NONE))
    gif_frames[0].save(path, save_all=True, append_images=gif_frames[1:], duration=167, loop=0, disposal=2, optimize=False)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--asset-id", required=True)
    parser.add_argument("--height", type=int, required=True)
    parser.add_argument("--swap-side-walkb", action="store_true")
    args = parser.parse_args()

    asset_id = args.asset_id
    root = Path(__file__).resolve().parents[1]
    if (root / "Characters").is_dir() and (root / "SourceAssets").is_dir():
        package = root
    else:
        package = root / "outputs" / "DuzheResources"
    source_dir = package / "SourceAssets" / "Animations" / asset_id
    source_path = source_dir / f"{asset_id}_walk_source.png"
    r11_path = package / "Characters" / f"{asset_id}.png"
    r11_master_path = package / "SourceAssets" / "Characters" / f"{asset_id}_master.png"
    raw_dir = source_dir / "SourceFrames"
    frame_dir = source_dir / "Frames"
    output_dir = package / "Characters" / "Animations"
    raw_dir.mkdir(parents=True, exist_ok=True)
    frame_dir.mkdir(parents=True, exist_ok=True)
    output_dir.mkdir(parents=True, exist_ok=True)

    source = Image.open(source_path).convert("RGBA")
    if source.size != (2048, 2048):
        raise ValueError(f"Expected 2048x2048 source, got {source.size}")
    x_edges = [round(i * source.width / 3) for i in range(4)]
    y_edges = [round(i * source.height / 4) for i in range(5)]

    frames: dict[str, dict[str, Image.Image]] = {d: {} for d in DIRECTIONS}
    records = []
    for row, direction in enumerate(DIRECTIONS):
        for col, pose in enumerate(POSES):
            source_row, source_col = row, col
            if args.swap_side_walkb and pose == "WalkB" and direction == "Left":
                source_row = DIRECTIONS.index("Right")
            elif args.swap_side_walkb and pose == "WalkB" and direction == "Right":
                source_row = DIRECTIONS.index("Left")

            if direction == "Down" and pose == "Idle":
                crop, source_bbox = clean_crop(Image.open(r11_master_path))
                final = Image.open(r11_path).convert("RGBA")
                origin = "R11 accepted idle and master"
            else:
                cell = source.crop((x_edges[source_col], y_edges[source_row], x_edges[source_col + 1], y_edges[source_row + 1]))
                crop, source_bbox = clean_crop(cell)
                final = normalize(crop, args.height)
                origin = f"source cell row={source_row}, col={source_col}"

            raw_path = raw_dir / f"{direction}_{pose}.png"
            frame_path = frame_dir / f"{direction}_{pose}.png"
            crop.save(raw_path, optimize=True)
            final.save(frame_path, optimize=True)
            frames[direction][pose] = final
            alpha = final.getchannel("A")
            bbox = alpha.getbbox()
            values = sorted(set(alpha.getdata()))
            labels, components = ndimage.label(np.array(alpha) > 0)
            records.append({
                "direction": direction,
                "pose": pose,
                "origin": origin,
                "sourceContentBBox": list(source_bbox),
                "sourceFrameWidth": crop.width,
                "sourceFrameHeight": crop.height,
                "finalBBox": list(bbox) if bbox else None,
                "alphaValues": values,
                "connectedComponents": int(components),
                "sha256": hashlib.sha256(frame_path.read_bytes()).hexdigest(),
            })

    sheet = Image.new("RGBA", (96, 192), (0, 0, 0, 0))
    for row, direction in enumerate(DIRECTIONS):
        for col, pose in enumerate(POSES):
            sheet.alpha_composite(frames[direction][pose], (col * 32, row * 48))
    sheet_path = output_dir / f"{asset_id}.png"
    sheet.save(sheet_path, optimize=True)
    make_static_preview(sheet, source_dir / f"{asset_id}_sheet_preview.png")
    make_gif(frames, source_dir / f"{asset_id}_walk_preview.gif")

    hashes = [record["sha256"] for record in records]
    direction_checks = []
    for direction in DIRECTIONS:
        direction_records = [r for r in records if r["direction"] == direction]
        direction_hashes = [r["sha256"] for r in direction_records]
        arrays = [np.array(frames[direction][p]) for p in POSES]
        direction_checks.append({
            "direction": direction,
            "uniqueFrames": len(set(direction_hashes)),
            "walkAIdleDifferentPixels": int(np.count_nonzero(np.any(arrays[0] != arrays[1], axis=2))),
            "walkBIdleDifferentPixels": int(np.count_nonzero(np.any(arrays[2] != arrays[1], axis=2))),
            "walkAWalkBDifferentPixels": int(np.count_nonzero(np.any(arrays[0] != arrays[2], axis=2))),
        })

    report = {
        "assetId": asset_id,
        "sourceFile": source_path.name,
        "sourceWidth": source.width,
        "sourceHeight": source.height,
        "swapSideWalkB": args.swap_side_walkb,
        "sheetPath": f"Characters/Animations/{asset_id}.png",
        "sheetWidth": sheet.width,
        "sheetHeight": sheet.height,
        "sheetMode": sheet.mode,
        "sheetSha256": hashlib.sha256(sheet_path.read_bytes()).hexdigest(),
        "uniqueFrameCount": len(set(hashes)),
        "frameLayout": {
            "columns": 3,
            "rows": 4,
            "cellWidth": 32,
            "cellHeight": 48,
            "rowOrder": DIRECTIONS,
            "columnOrder": POSES,
        },
        "directionChecks": direction_checks,
        "frames": records,
    }
    (source_dir / f"{asset_id}_processing.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"assetId": asset_id, "uniqueFrameCount": len(set(hashes)), "directionChecks": direction_checks}, ensure_ascii=False))


if __name__ == "__main__":
    main()
