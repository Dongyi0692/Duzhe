from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import numpy as np
import json

ROOT = Path(__file__).resolve().parents[1]
if (ROOT / "Illustrations").is_dir() and (ROOT / "SourceAssets").is_dir():
    PACKAGE = ROOT
else:
    PACKAGE = ROOT / "outputs" / "DuzheResources"
SOURCE = PACKAGE / "SourceAssets" / "Illustrations"
FINAL = PACKAGE / "Illustrations"
IDS = ["CG01_FamilyFarewell", "CG02_LoversFarewell", "CG03_BandFarewell", "CG04_TeacherFarewell", "CG05_SelfCrossing"]
# The generated corner label occupies x=14..87, y=673..698 in the 1280x720 originals.
# A small boundary-conditioned harmonic fill reconstructs only that non-subject corner.
X0, Y0, X1, Y1 = 10, 669, 93, 704

def clean_corner(image: Image.Image, preserve_horizontal_lines: bool = False) -> Image.Image:
    rgb = np.array(image.convert("RGB"), dtype=np.float64)
    region = rgb[Y0:Y1, X0:X1].copy()
    height, width = region.shape[:2]
    # Initialize by horizontal interpolation between untouched pixels at either edge.
    left = rgb[Y0:Y1, X0 - 1]
    right = rgb[Y0:Y1, X1]
    weights = np.linspace(0, 1, width + 2)[1:-1][None, :, None]
    region[:] = left[:, None, :] * (1 - weights) + right[:, None, :] * weights
    # Studio skirting boards are horizontal: preserve them rather than blur their height.
    for _ in range(0 if preserve_horizontal_lines else 500):
        padded = np.pad(region, ((1, 1), (1, 1), (0, 0)), mode="edge")
        # Use original neighbouring pixels at the four boundaries.
        padded[0, 1:-1] = rgb[Y0 - 1, X0:X1]
        padded[-1, 1:-1] = rgb[Y1, X0:X1]
        padded[1:-1, 0] = left
        padded[1:-1, -1] = right
        region = (padded[:-2, 1:-1] + padded[2:, 1:-1] + padded[1:-1, :-2] + padded[1:-1, 2:]) / 4
    rgb[Y0:Y1, X0:X1] = region
    return Image.fromarray(np.clip(np.rint(rgb), 0, 255).astype(np.uint8), "RGB")

def main():
    FINAL.mkdir(parents=True, exist_ok=True)
    records = []
    for asset_id in IDS:
        source = SOURCE / f"{asset_id}_a01.png"
        original = Image.open(source).convert("RGB")
        if original.size != (1280, 720):
            raise ValueError(f"Unexpected source size for {asset_id}: {original.size}")
        horizontal_only = asset_id == "CG03_BandFarewell"
        cleaned = clean_corner(original, horizontal_only)
        cleaned_path = SOURCE / f"{asset_id}_clean.png"
        cleaned.save(cleaned_path, optimize=True)
        final = cleaned.resize((640, 360), Image.Resampling.BOX)
        final_path = FINAL / f"{asset_id}.png"
        final.save(final_path, optimize=True)
        records.append({"assetId": asset_id, "rawPath": source.name, "cleanedPath": cleaned_path.name, "finalPath": final_path.name, "finalSize": list(final.size), "mode": final.mode, "cleanedCorner": [X0, Y0, X1, Y1], "horizontalInterpolationOnly": horizontal_only})
    # Contact sheet contains only the clean formal images, at native 640x360.
    margin, gap, header = 24, 20, 38
    width = margin * 2 + 2 * 640 + gap
    height = margin * 2 + header + 3 * (360 + 34 + gap)
    sheet = Image.new("RGB", (width, height), (29, 41, 53))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default(size=16)
    draw.text((margin, 12), "R14 Farewell Illustrations | 640 x 360 master images", fill=(220, 225, 221), font=font)
    for index, asset_id in enumerate(IDS):
        row, col = divmod(index, 2)
        x = margin + col * (640 + gap)
        y = margin + header + row * (360 + 34 + gap)
        draw.text((x, y), asset_id, fill=(206, 214, 215), font=font)
        image = Image.open(FINAL / f"{asset_id}.png").convert("RGB")
        sheet.paste(image, (x, y + 26))
    sheet.save(SOURCE / "R14_FarewellIllustrations_Preview.png", optimize=True)
    (SOURCE / "R14_processing.json").write_text(json.dumps({"items": records, "resampling": "BOX 1280x720 to 640x360; no aspect distortion", "cornerCleanup": "Harmonic interpolation limited to generated corner label; raw files retained unchanged"}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(records, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
