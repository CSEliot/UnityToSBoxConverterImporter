# Offline stand-in for Editor/TextureChannels.cs ConvertPixels (the engine Bitmap is unavailable headless).
# argv: source destination channel scale invert(0/1). Needs Pillow (pip install pillow).
import sys
from PIL import Image

src, dst, channel, scale, invert = sys.argv[1], sys.argv[2], int(sys.argv[3]), float(sys.argv[4]), sys.argv[5] == "1"
im = Image.open(src).convert("RGBA")
band = im.getchannel("RGBA"[min(channel, 3)])
def f(v):
    x = min(max(v / 255.0 * scale, 0.0), 1.0)
    if invert:
        x = 1.0 - x
    return int(round(x * 255))
g = band.point([f(v) for v in range(256)])
Image.merge("RGBA", (g, g, g, Image.new("L", im.size, 255))).save(dst, "PNG")
