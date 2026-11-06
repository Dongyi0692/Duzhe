from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage


def largest_component(alpha: Image.Image) -> Image.Image:
    labels, count = ndimage.label(np.array(alpha) > 0)
    if count == 0:
        return Image.new("L", alpha.size, 0)
    sizes = np.bincount(labels.ravel())
    sizes[0] = 0
    main_label = int(sizes.argmax())
    mask = np.where(labels == main_label, 255, 0).astype(np.uint8)
    return Image.fromarray(mask, mode="L")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--report", required=True)
    parser.add_argument("--height", type=int, default=45)
    parser.add_argument("--max-width", type=int, default=28)
    parser.add_argument("--colors", type=int, default=24)
    args = parser.parse_args()

    source_path = Path(args.input)
    output_path = Path(args.output)
    report_path = Path(args.report)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.parent.mkdir(parents=True, exist_ok=True)

    source = Image.open(source_path).convert("RGBA")
    original_alpha_extrema = source.getchannel("A").getextrema()
    source_alpha = source.getchannel("A").point(lambda a: 255 if a >= 96 else 0)
    source_mask = largest_component(source_alpha)
    source_bbox = source_mask.getbbox()
    if not source_bbox:
        raise ValueError(f"No opaque character pixels found in {source_path}")
    source.putalpha(source_mask)
    crop = source.crop(source_bbox)

    scale = min(args.max_width / crop.width, args.height / crop.height)
    target_size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
    resized = crop.resize(target_size, Image.Resampling.LANCZOS)
    resized_alpha = resized.getchannel("A").point(lambda a: 255 if a >= 112 else 0)
    component_mask = largest_component(resized_alpha)
    resized.putalpha(component_mask)

    quantized = resized.quantize(
        colors=args.colors,
        method=Image.Quantize.FASTOCTREE,
        dither=Image.Dither.NONE,
    ).convert("RGBA")
    quantized.putalpha(component_mask)

    canvas = Image.new("RGBA", (32, 48), (0, 0, 0, 0))
    x = (32 - quantized.width) // 2
    y = 48 - quantized.height
    canvas.alpha_composite(quantized, (x, y))
    canvas.save(output_path, optimize=True)

    final_alpha = canvas.getchannel("A")
    bbox = final_alpha.getbbox()
    colors = canvas.getcolors(maxcolors=32 * 48) or []
    opaque_colors = sum(1 for count, rgba in colors if rgba[3] == 255)
    report = {
        "sourceFile": source_path.name,
        "sourceWidth": source.width,
        "sourceHeight": source.height,
        "sourceMode": source.mode,
        "sourceAlphaExtrema": list(original_alpha_extrema),
        "sourceContentBBox": list(source_bbox),
        "outputWidth": canvas.width,
        "outputHeight": canvas.height,
        "outputMode": canvas.mode,
        "outputAlphaExtrema": list(final_alpha.getextrema()),
        "outputContentBBox": list(bbox) if bbox else None,
        "characterPixelWidth": quantized.width,
        "characterPixelHeight": quantized.height,
        "placement": {"x": x, "y": y},
        "footBaseline": "bottom canvas edge; pivot=(0.5,0)",
        "opaqueColorCount": opaque_colors,
        "resizeFilter": "LANCZOS",
        "alphaThreshold": 112,
        "quantizationColors": args.colors,
        "dithering": "none",
        "cleanup": "largest 4-connected opaque component retained",
    }
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
