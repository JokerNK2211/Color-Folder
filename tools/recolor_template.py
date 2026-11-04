"""Recolor a folder icon you supply into every palette color (and badge).

Use this when you want the exact look of an icon you already have, e.g. a
folder icon from your own PC. The texture, shading and shape of the template
are kept; only the hue/saturation/brightness of the colored parts change.
White and gray parts (like a paper sheet) and the shadow stay as they are.

    python tools/recolor_template.py path/to/template.ico out_dir [--badges]

Output icons are derived from your template, so they carry the template's
license. Do not ship them in the app unless you have the rights to it.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from make_icons import PALETTE, BADGES, badge_glyph, hex2rgb  # noqa: E402


def rgb_to_hsv(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mx, mn = a.max(-1), a.min(-1)
    d = mx - mn
    h = np.zeros_like(mx)
    m = d > 1e-6
    rm = m & (mx == r)
    gm = m & (mx == g) & ~rm
    bm = m & ~rm & ~gm
    h[rm] = ((g - b)[rm] / d[rm]) % 6
    h[gm] = (b - r)[gm] / d[gm] + 2
    h[bm] = (r - g)[bm] / d[bm] + 4
    h = h / 6
    s = np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0)
    return h, s, mx


def hsv_to_rgb(h, s, v):
    i = np.floor(h * 6).astype(int) % 6
    f = h * 6 - np.floor(h * 6)
    p, q, t = v * (1 - s), v * (1 - s * f), v * (1 - s * (1 - f))
    out = np.zeros(h.shape + (3,))
    for k, (R, G, B) in enumerate([(v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q)]):
        sel = i == k
        out[sel] = np.stack([R[sel], G[sel], B[sel]], -1)
    return out


def frames(path):
    im = Image.open(path)
    out = {}
    for size in sorted(im.info["sizes"]):
        im.size = size
        f = im.convert("RGBA")
        f.load()
        out[size] = f.copy()
    return out


def base_of(frame):
    a = np.asarray(frame).astype(float) / 255
    h, s, v = rgb_to_hsv(a[..., :3])
    sel = (a[..., 3] > 0.9) & (s > 0.3)
    hm = float(np.median(h[sel]))
    # Treat the template as already showing the palette color closest in hue,
    # so that color comes out exactly like the template.
    best = None
    for _, hx in PALETTE:
        ph, ps, pv = (float(x) for x in rgb_to_hsv(np.array(hex2rgb(hx), float) / 255))
        if ps < 0.3:
            continue
        dist = min(abs(ph - hm), 1 - abs(ph - hm))
        if best is None or dist < best[0]:
            best = (dist, ph, ps, pv)
    return hm, best[2], best[3]


def recolor(frame, base, target):
    a = np.asarray(frame).astype(float) / 255
    h, s, v = rgb_to_hsv(a[..., :3])
    bh, bs, bv = base
    th, ts, tv = rgb_to_hsv(np.array(target, float) / 255)
    th, ts, tv = float(th), float(ts), float(tv)
    w = np.clip((s - 0.12) / 0.2, 0, 1)          # how "colored" each pixel is
    nh = np.full_like(h, th)
    ns = np.clip(s * ts / bs, 0, 1)
    nv = np.clip(v * tv / bv, 0, 1)
    new = hsv_to_rgb(nh, ns, nv)
    rgb = a[..., :3] * (1 - w[..., None]) + new * w[..., None]
    out = np.concatenate([rgb, a[..., 3:]], -1)
    return Image.fromarray((out * 255).round().astype(np.uint8), "RGBA")


def emboss(frame, badge, target):
    n = frame.size[0]
    g = badge_glyph(badge, 1024, circle=False).getchannel("A")
    side = int(n * 0.43)
    g = g.resize((side, side), Image.LANCZOS)
    x, y = (n - side) // 2, int(n * 0.31)
    tr = np.array(target, float)
    ink = tuple(int(c) for c in tr * 0.62)
    hi = Image.new("RGBA", g.size, (255, 255, 255, 255))
    out = frame.copy()
    out.paste(hi, (x, y + max(1, n // 150)), g.point(lambda v: v * 0.5))
    out.paste(Image.new("RGBA", g.size, ink + (255,)), (x, y), g.point(lambda v: v * 0.85))
    return out


def save_ico(fr, path):
    sizes = sorted(fr)
    big = fr[sizes[-1]]
    big.save(path, format="ICO", sizes=sizes, append_images=[fr[s] for s in sizes[:-1]])


def main():
    src, out = Path(sys.argv[1]), Path(sys.argv[2])
    do_badges = "--badges" in sys.argv
    out.mkdir(parents=True, exist_ok=True)
    fr = frames(src)
    base = base_of(fr[max(fr)])
    for name, hx in PALETTE:
        t = hex2rgb(hx)
        rec = {k: recolor(f, base, t) for k, f in fr.items()}
        save_ico(rec, out / f"{name}.ico")
        rec[max(rec)].save(out / f"{name}.png")
        if do_badges:
            for b in BADGES:
                save_ico({k: emboss(f, b, t) for k, f in rec.items()}, out / f"{name}-{b}.ico")


if __name__ == "__main__":
    main()
