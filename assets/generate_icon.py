"""Generate the Stats Screen icon assets deterministically."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


BASE_SIZE = 1024
SCALE = 4
WORK_SIZE = BASE_SIZE * SCALE
ASSET_DIR = Path(__file__).resolve().parent
ICO_PATH = ASSET_DIR.parent / "src" / "StatsScreen" / "Resources" / "StatsScreen.ico"


def point(x: int, y: int) -> tuple[int, int]:
    return x * SCALE, y * SCALE


def draw_line(draw: ImageDraw.ImageDraw, coordinates, fill, width: int) -> None:
    points = [point(x, y) for x, y in coordinates]
    draw.line(points, fill=fill, width=width * SCALE, joint="curve")
    radius = width * SCALE // 2
    for x, y in points:
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=fill)


def icon(size: int) -> Image.Image:
    image = Image.new("RGBA", (WORK_SIZE, WORK_SIZE), (0, 0, 0, 0))
    pixels = image.load()
    for y in range(WORK_SIZE):
        progress = y / (WORK_SIZE - 1)
        top = (20, 34, 47)
        bottom = (7, 12, 19)
        colour = tuple(round(top[i] * (1 - progress) + bottom[i] * progress) for i in range(3))
        for x in range(WORK_SIZE):
            pixels[x, y] = (*colour, 255)

    mask = Image.new("L", (WORK_SIZE, WORK_SIZE), 0)
    mask_draw = ImageDraw.Draw(mask)
    mask_draw.rounded_rectangle(
        (point(36, 36), point(988, 988)),
        radius=point(206, 0)[0],
        fill=255,
    )
    image.putalpha(mask)

    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle(
        (point(36, 36), point(988, 988)),
        radius=point(206, 0)[0],
        outline=(55, 77, 96, 255),
        width=8 * SCALE,
    )
    draw.rounded_rectangle(
        (point(66, 66), point(958, 958)),
        radius=point(180, 0)[0],
        outline=(31, 51, 68, 255),
        width=3 * SCALE,
    )

    # A quiet instrumentation grid gives the mark a telemetry-panel character.
    for x in (210, 390, 570, 750):
        draw.line((point(x, 230), point(x, 790)), fill=(47, 67, 84, 80), width=2 * SCALE)
    for y in (310, 470, 630, 790):
        draw.line((point(170, y), point(854, y)), fill=(47, 67, 84, 80), width=2 * SCALE)

    cpu = [(166, 676), (276, 614), (372, 642), (484, 494), (588, 530), (704, 388), (852, 278)]
    gpu = [(166, 788), (276, 730), (378, 748), (492, 650), (594, 674), (716, 562), (852, 438)]

    glow = Image.new("RGBA", (WORK_SIZE, WORK_SIZE), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow)
    draw_line(glow_draw, cpu, (44, 198, 207, 150), 30)
    draw_line(glow_draw, gpu, (183, 139, 231, 120), 30)
    glow = glow.filter(ImageFilter.GaussianBlur(18 * SCALE))
    image.alpha_composite(glow)

    draw = ImageDraw.Draw(image)
    draw_line(draw, cpu, (77, 210, 214, 255), 18)
    draw_line(draw, gpu, (186, 139, 231, 255), 18)

    # Small endpoint markers make the two channels legible at 16–32 px.
    for x, y, colour in ((852, 278, (194, 250, 248, 255)), (852, 438, (229, 211, 250, 255))):
        draw.ellipse((point(x - 17, y - 17), point(x + 17, y + 17)), fill=(12, 20, 29, 255))
        draw.ellipse((point(x - 9, y - 9), point(x + 9, y + 9)), fill=colour)

    # A pair of short channel bars anchors the mark without adding text.
    draw.rounded_rectangle((point(166, 182), point(238, 198)), radius=8 * SCALE, fill=(77, 210, 214, 255))
    draw.rounded_rectangle((point(250, 182), point(322, 198)), radius=8 * SCALE, fill=(186, 139, 231, 255))

    return image.resize((size, size), Image.Resampling.LANCZOS)


def main() -> None:
    ASSET_DIR.mkdir(parents=True, exist_ok=True)
    ICO_PATH.parent.mkdir(parents=True, exist_ok=True)
    icon(BASE_SIZE).save(ASSET_DIR / "icon-1024.png", format="PNG", optimize=False)
    icon(256).save(ASSET_DIR / "icon-256.png", format="PNG", optimize=False)
    icon(BASE_SIZE).save(
        ICO_PATH,
        format="ICO",
        sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)],
    )


if __name__ == "__main__":
    main()
