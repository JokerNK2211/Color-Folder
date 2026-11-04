"""Generate the Color Folder icon set.

Outputs (relative to repo root):
  icons/folders/<color>.ico          plain colored folder, sizes 16-256
  icons/folders/<color>.svg          same folder as SVG
  icons/badged/<color>-<badge>.ico   colored folder with a badge
  icons/preview.png                  contact sheet of the whole set

Run:  python tools/make_icons.py   (needs Pillow)
"""
from pathlib import Path
from PIL import Image, ImageDraw
import colorsys
import math

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "icons"
S = 1024                      # master canvas, downscaled for each size
ICO_SIZES = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]

PALETTE = [
    ("red",    "#E5484D"), ("orange", "#F76B15"), ("amber",  "#FFB224"),
    ("yellow", "#F5D90A"), ("lime",   "#8DC63F"), ("green",  "#30A46C"),
    ("teal",   "#12A594"), ("sky",    "#0EA5E9"), ("blue",   "#3E63DD"),
    ("indigo", "#5B5BD6"), ("purple", "#8E4EC6"), ("pink",   "#D6409F"),
    ("brown",  "#A0704C"), ("gray",   "#8B8D98"), ("black",  "#2E2E33"),
    ("white",  "#E8E8EC"),
]
BADGES = ["star", "heart", "check", "cross", "warning", "lock",
          "cloud", "code", "music", "image", "play", "download",
          "work", "home", "clock", "flag"]


def hex2rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def shade(rgb, f):
    """f<0 darkens, f>0 lightens."""
    if f < 0:
        return tuple(int(c * (1 + f)) for c in rgb)
    return tuple(int(c + (255 - c) * f) for c in rgb)


# Folder geometry on a 1024 canvas (Windows 11-ish: back with tab, front panel)
BACK = (64, 176, 960, 848)          # back body
TAB = [(64, 216), (64, 168), (88, 144), (380, 144), (440, 208)]  # tab polygon top-left
FRONT = (64, 288, 960, 864)
R = 56


def draw_folder(rgb):
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    back = shade(rgb, -0.22)
    d.rounded_rectangle((64, 144, 440, 300), radius=R, fill=back)          # tab
    d.rounded_rectangle(BACK, radius=R, fill=back)
    d.rounded_rectangle((96, 232, 928, 820), radius=24, fill=(255, 255, 255, 235))  # paper
    d.rounded_rectangle(FRONT, radius=R, fill=rgb)
    d.rounded_rectangle((64, 288, 960, 320), radius=16, fill=shade(rgb, 0.25))  # top highlight
    d.rectangle((64, 304, 960, 330), fill=rgb)
    return img


def badge_glyph(name, size=420, circle=True):
    """Glyph drawn at `size`, on a white circle unless circle=False."""
    g = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(g)
    ink = (40, 40, 48, 255)
    if circle:
        d.ellipse((0, 0, size - 1, size - 1), fill=(40, 40, 48, 255))
        d.ellipse((18, 18, size - 19, size - 19), fill=(255, 255, 255, 255))
    cut = ink if circle else (0, 0, 0, 0)  # inner details punch through in mono mode
    w = int(size * 0.07)
    u = size / 100  # unit

    def P(x, y):
        return (x * u, y * u)

    if name == "star":
        pts = []
        for i in range(10):
            r = 32 if i % 2 == 0 else 13
            a = -math.pi / 2 + i * math.pi / 5
            pts.append(P(50 + r * math.cos(a), 52 + r * math.sin(a)))
        d.polygon(pts, fill=(245, 166, 35))
    elif name == "heart":
        red = (229, 72, 77)
        d.ellipse((*P(24, 26), *P(52, 54)), fill=red)
        d.ellipse((*P(48, 26), *P(76, 54)), fill=red)
        d.polygon([P(25.5, 45), P(74.5, 45), P(50, 76)], fill=red)
    elif name == "check":
        d.line([P(28, 52), P(44, 68), P(73, 34)], fill=(48, 164, 108), width=int(w * 1.4), joint="curve")
    elif name == "cross":
        d.line([P(32, 32), P(68, 68)], fill=(229, 72, 77), width=int(w * 1.4))
        d.line([P(68, 32), P(32, 68)], fill=(229, 72, 77), width=int(w * 1.4))
    elif name == "warning":
        d.polygon([P(50, 20), P(82, 76), P(18, 76)], fill=(255, 178, 36))
        d.rectangle((*P(47, 38), *P(53, 60)), fill=cut)
        d.ellipse((*P(46.5, 64), *P(53.5, 71)), fill=cut)
    elif name == "lock":
        d.arc((*P(34, 22), *P(66, 56)), 180, 360, fill=ink, width=w)
        d.line([P(34 + 3.5, 39), P(34 + 3.5, 48)], fill=ink, width=w)
        d.line([P(66 - 3.5, 39), P(66 - 3.5, 48)], fill=ink, width=w)
        d.rounded_rectangle((*P(28, 46), *P(72, 78)), radius=int(5 * u), fill=ink)
    elif name == "cloud":
        blue = (14, 165, 233)
        d.ellipse((*P(22, 44), *P(46, 70)), fill=blue)
        d.ellipse((*P(34, 28), *P(66, 62)), fill=blue)
        d.ellipse((*P(54, 40), *P(80, 70)), fill=blue)
        d.rectangle((*P(34, 56), *P(68, 70)), fill=blue)
    elif name == "code":
        d.line([P(38, 32), P(22, 50), P(38, 68)], fill=ink, width=w, joint="curve")
        d.line([P(62, 32), P(78, 50), P(62, 68)], fill=ink, width=w, joint="curve")
        d.line([P(55, 28), P(45, 72)], fill=ink, width=int(w * 0.8))
    elif name == "music":
        d.ellipse((*P(26, 60), *P(44, 76)), fill=ink)
        d.ellipse((*P(56, 54), *P(74, 70)), fill=ink)
        d.rectangle((*P(39, 26), *P(44, 68)), fill=ink)
        d.rectangle((*P(69, 20), *P(74, 62)), fill=ink)
        d.polygon([P(39, 26), P(74, 20), P(74, 30), P(39, 36)], fill=ink)
    elif name == "image":
        d.rounded_rectangle((*P(22, 26), *P(78, 74)), radius=int(5 * u), outline=ink, width=int(w * 0.7))
        d.polygon([P(27, 69), P(44, 46), P(56, 60), P(63, 52), P(73, 69)], fill=(48, 164, 108))
        d.ellipse((*P(58, 32), *P(68, 42)), fill=(255, 178, 36))
    elif name == "play":
        d.polygon([P(38, 28), P(74, 50), P(38, 72)], fill=(229, 72, 77))
    elif name == "download":
        d.rectangle((*P(45, 22), *P(55, 52)), fill=ink)
        d.polygon([P(30, 48), P(70, 48), P(50, 68)], fill=ink)
        d.rectangle((*P(26, 72), *P(74, 78)), fill=ink)
    elif name == "work":
        d.rounded_rectangle((*P(40, 24), *P(60, 40)), radius=int(3 * u), outline=ink, width=int(w * 0.6))
        d.rounded_rectangle((*P(22, 34), *P(78, 74)), radius=int(6 * u), fill=(160, 112, 76))
        d.rectangle((*P(22, 50), *P(78, 53)), fill=(110, 76, 50) if circle else cut)
    elif name == "home":
        d.polygon([P(50, 22), P(80, 50), P(20, 50)], fill=ink)
        d.rectangle((*P(28, 48), *P(72, 76)), fill=ink)
        d.rectangle((*P(44, 58), *P(56, 76)), fill=(255, 255, 255) if circle else cut)
    elif name == "clock":
        d.ellipse((*P(22, 22), *P(78, 78)), outline=ink, width=int(w * 0.8))
        d.line([P(50, 50), P(50, 32)], fill=ink, width=int(w * 0.8))
        d.line([P(50, 50), P(63, 58)], fill=ink, width=int(w * 0.8))
    elif name == "flag":
        d.rectangle((*P(28, 20), *P(34, 80)), fill=ink)
        d.polygon([P(34, 22), P(74, 22), P(64, 36), P(74, 50), P(34, 50)], fill=(229, 72, 77))
    return g


def with_badge(folder, badge):
    img = folder.copy()
    g = badge_glyph(badge, 460)
    img.alpha_composite(g, (S - 460 - 8, S - 460 - 8))
    return img


def save_ico(img, path):
    img.resize((256, 256), Image.LANCZOS).save(path, format="ICO", sizes=ICO_SIZES)


def folder_svg(rgb):
    h = lambda c: "#%02X%02X%02X" % c
    back, light = h(shade(rgb, -0.22)), h(shade(rgb, 0.25))
    return f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 1024">
  <rect x="64" y="144" width="376" height="156" rx="56" fill="{back}"/>
  <rect x="64" y="176" width="896" height="672" rx="56" fill="{back}"/>
  <rect x="96" y="232" width="832" height="588" rx="24" fill="#FFFFFF" fill-opacity="0.92"/>
  <rect x="64" y="288" width="896" height="576" rx="56" fill="{h(rgb)}"/>
  <rect x="64" y="288" width="896" height="32" rx="16" fill="{light}"/>
  <rect x="64" y="304" width="896" height="26" fill="{h(rgb)}"/>
</svg>
"""


def main():
    (OUT / "badges").mkdir(parents=True, exist_ok=True)
    for b in BADGES:   # alpha masks used by the app to emboss badges on custom templates
        m = badge_glyph(b, 512, circle=False).getchannel("A")
        Image.merge("RGBA", (m, m, m, m)).save(OUT / "badges" / f"{b}.png")
    (OUT / "folders").mkdir(parents=True, exist_ok=True)
    (OUT / "badged").mkdir(parents=True, exist_ok=True)
    folders = {}
    for name, hx in PALETTE:
        rgb = hex2rgb(hx)
        f = draw_folder(rgb)
        folders[name] = f
        save_ico(f, OUT / "folders" / f"{name}.ico")
        (OUT / "folders" / f"{name}.svg").write_text(folder_svg(rgb))
        for b in BADGES:
            save_ico(with_badge(f, b), OUT / "badged" / f"{name}-{b}.ico")

    # contact sheet: row 1-2 plain colors, then badges on blue, then 16px check
    cell, pad = 128, 16
    cols = 8
    rows = 2 + 2 + 1
    sheet = Image.new("RGBA", (cols * (cell + pad) + pad, rows * (cell + pad) + pad + 40), (250, 250, 252, 255))
    def put(img, i, row_off, sz=cell):
        x = pad + (i % cols) * (cell + pad)
        y = pad + (row_off + i // cols) * (cell + pad)
        sheet.alpha_composite(img.resize((sz, sz), Image.LANCZOS), (x, y))
    for i, (name, _) in enumerate(PALETTE):
        put(folders[name], i, 0)
    for i, b in enumerate(BADGES):
        put(with_badge(folders[PALETTE[i % len(PALETTE)][0]], b), i, 2)
    small = Image.new("RGBA", (cell, cell), (0, 0, 0, 0))
    for i, (name, _) in enumerate(PALETTE):
        small.alpha_composite(folders[name].resize((32, 32), Image.LANCZOS), ((i % 4) * 32, (i // 4) * 32))
    put(small, 0, 4)
    for i, b in enumerate(BADGES[:7]):
        put(with_badge(folders["amber"], b).resize((32, 32), Image.LANCZOS).resize((cell, cell), Image.NEAREST), i + 1, 4)
    sheet.convert("RGB").save(OUT / "preview.png")


# ---------------------------------------------------------------- macOS style
# Our own drawing of a macOS-style folder, using the user's reference folders
# as the shape guide. Badges are embossed tone-on-tone on the front panel.

def _gradient(size, top, bottom):
    g = Image.new("RGBA", size)
    w, h = size
    d = ImageDraw.Draw(g)
    for y in range(h):
        t = y / max(1, h - 1)
        d.line([(0, y), (w, y)], fill=tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,))
    return g


def _paste_shape(img, box, radius, fill_img):
    x0, y0, x1, y1 = box
    m = Image.new("L", (x1 - x0, y1 - y0), 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, x1 - x0 - 1, y1 - y0 - 1), radius=radius, fill=255)
    img.paste(fill_img.resize(m.size), (x0, y0), m)


# Reference tones measured from the user's sample folder (sky blue base).
_REF_BASE = (14, 165, 233)
_REF = {
    "front":     (96, 208, 255),
    "front_bot": (86, 200, 250),
    "edge":      (60, 182, 236),
    "lip":       (74, 188, 237),
    "lip_hi":    (99, 206, 250),
    "back":      (59, 174, 228),
    "back_dark": (33, 165, 225),
    "rim":       (4, 152, 217),
}


def _tone(rgb, key):
    """Map a reference tone onto the palette color: keep the reference
    lightness/saturation structure, swap in the palette hue."""
    rh, rs, rv = colorsys.rgb_to_hsv(*[c / 255 for c in _REF[key]])
    bh, bs, bv = colorsys.rgb_to_hsv(*[c / 255 for c in _REF_BASE])
    ph, ps, pv = colorsys.rgb_to_hsv(*[c / 255 for c in rgb])
    s_ = min(1.0, rs * ps / bs)
    v_ = min(1.0, rv * pv / bv)
    return tuple(int(round(c * 255)) for c in colorsys.hsv_to_rgb(ph, s_, v_))


def _bezier(p0, p1, p2, p3, n=48):
    out = []
    for i in range(n + 1):
        t = i / n
        a, b, c, d_ = (1 - t) ** 3, 3 * (1 - t) ** 2 * t, 3 * (1 - t) * t ** 2, t ** 3
        out.append((a * p0[0] + b * p1[0] + c * p2[0] + d_ * p3[0],
                    a * p0[1] + b * p1[1] + c * p2[1] + d_ * p3[1]))
    return out


def _mask_rr(box, radius):
    m = Image.new("L", (S, S), 0)
    ImageDraw.Draw(m).rounded_rectangle(box, radius=radius, fill=255)
    return m


def draw_mac_folder(rgb, badge=None, paper=False):
    """Own drawing of a macOS-style folder. Proportions and tones were
    measured from the reference folders the user supplied (1024 = 4 x 256)."""
    from PIL import ImageFilter, ImageChops
    T = lambda k: _tone(rgb, k)
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))

    # shadow: short and soft, mostly under the bottom edge
    sh = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(sh).rounded_rectangle((44, 200, 980, 924), radius=96, fill=(0, 0, 0, 90))
    img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(10)))

    # back body + tab (one silhouette)
    back = Image.new("L", (S, S), 0)
    bd = ImageDraw.Draw(back)
    bd.rounded_rectangle((36, 176, 988, 910), radius=84, fill=255)
    bd.rounded_rectangle((36, 118, 440, 330), radius=112, fill=255)
    curve = _bezier((320, 118), (420, 118), (440, 194), (540, 196))
    bd.polygon([(320, 118)] + curve + [(540, 330), (320, 330)], fill=255)
    back = back.filter(ImageFilter.GaussianBlur(1))
    img.paste(Image.new("RGBA", (S, S), T("back") + (255,)), (0, 0), back)
    d = ImageDraw.Draw(img)
    # dark rim where the front meets the back
    d.rectangle((60, 222, 964, 264), fill=T("rim"))
    if paper:
        d.rectangle((92, 224, 944, 262), fill=(255, 255, 255, 255))

    # front panel
    fm = _mask_rr((40, 258, 984, 906), 96).filter(ImageFilter.GaussianBlur(1))
    front = Image.new("RGBA", (S, S))
    fd = ImageDraw.Draw(front)
    ft, fb = T("front"), T("front_bot")
    for y in range(250, 907):
        t = max(0.0, (y - 250) / 560)
        t = min(1.0, t)
        fd.line([(0, y), (S, y)], fill=tuple(int(ft[i] + (fb[i] - ft[i]) * t) for i in range(3)) + (255,))
    # bottom lip: highlight line, then a slightly darker band
    fd.rectangle((0, 812, S, 820), fill=T("lip_hi") + (255,))
    fd.rectangle((0, 821, S, 878), fill=T("lip") + (255,))
    fd.rectangle((0, 879, S, 907), fill=T("back_dark") + (255,))
    fd.rectangle((0, 258, S, 268), fill=T("lip_hi") + (255,))
    # side shading: darker toward the left/right edges
    edge = T("edge")
    for x in range(0, 110):
        a = int(255 * (1 - x / 110) ** 1.6)
        ov = Image.new("RGBA", (1, 657), edge + (a,))
        front.alpha_composite(ov, (40 + x, 250))
        front.alpha_composite(ov, (983 - x, 250))
    # fine grain like the reference's paper texture
    noise = Image.effect_noise((S // 2, S // 2), 18).resize((S, S))
    grain = Image.merge("RGBA", (noise, noise, noise, Image.new("L", (S, S), 5)))
    front.alpha_composite(grain)
    img.paste(front, (0, 0), fm)

    if badge:
        gl = badge_glyph(badge, 440, circle=False).getchannel("A")
        x, y = (S - 440) // 2, 320
        hi = Image.new("RGBA", gl.size, (255, 255, 255, 255))
        ink = Image.new("RGBA", gl.size, T("rim") + (255,))
        img.paste(hi, (x, y + 6), gl.point(lambda v: v * 0.55))
        img.paste(ink, (x, y), gl.point(lambda v: v * 0.95))
    return img


def main_mac():
    (OUT / "mac" / "folders").mkdir(parents=True, exist_ok=True)
    (OUT / "mac" / "badged").mkdir(parents=True, exist_ok=True)
    cell, pad, cols = 128, 16, 8
    sheet = Image.new("RGBA", (cols * (cell + pad) + pad, 5 * (cell + pad) + pad), (250, 250, 252, 255))
    for i, (name, hx) in enumerate(PALETTE):
        rgb = hex2rgb(hx)
        f = draw_mac_folder(rgb)
        save_ico(f, OUT / "mac" / "folders" / f"{name}.ico")
        save_ico(draw_mac_folder(rgb, paper=True), OUT / "mac" / "folders" / f"{name}-paper.ico")
        sheet.alpha_composite(f.resize((cell, cell), Image.LANCZOS), (pad + (i % cols) * (cell + pad), pad + (i // cols) * (cell + pad)))
        for b in BADGES:
            save_ico(draw_mac_folder(rgb, b), OUT / "mac" / "badged" / f"{name}-{b}.ico")
    for i, b in enumerate(BADGES):
        rgb = hex2rgb(PALETTE[(i + 8) % len(PALETTE)][1])
        f = draw_mac_folder(rgb, b)
        sheet.alpha_composite(f.resize((cell, cell), Image.LANCZOS), (pad + (i % cols) * (cell + pad), pad + (2 + i // cols) * (cell + pad)))
    for i, b in enumerate(BADGES[:8]):
        f = draw_mac_folder(hex2rgb("#0EA5E9"), b, paper=(i % 2 == 1)).resize((32, 32), Image.LANCZOS).resize((cell, cell), Image.NEAREST)
        sheet.alpha_composite(f, (pad + i * (cell + pad), pad + 4 * (cell + pad)))
    sheet.convert("RGB").save(OUT / "mac" / "preview.png")


if __name__ == "__main__":
    main()
    main_mac()
