namespace PdfSpec.Samples;

/// <summary>
/// Procedural pixel generators reused by the raster-image / image-mask
/// samples. Cheap to run, deterministic output, shared so each sample
/// can keep its own file small.
/// </summary>
internal static class SampleImages
{
    /// <summary>24-bit RGB: a red(x)/green(y) gradient with a blue diagonal band.</summary>
    public static byte[] MakeGradient(int width, int height)
    {
        var rgb = new byte[width * height * 3];
        int i = 0;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            rgb[i++] = (byte)(x * 255 / (width - 1));
            rgb[i++] = (byte)(y * 255 / (height - 1));
            rgb[i++] = (byte)(Math.Abs(x - y) < 12 ? 255 : 40);
        }
        return rgb;
    }

    /// <summary>Solid 24-bit RGB fill.</summary>
    public static byte[] MakeSolid(int w, int h, byte r, byte g, byte b)
    {
        var rgb = new byte[w * h * 3];
        for (int i = 0; i < w * h; i++)
        {
            rgb[i * 3] = r; rgb[i * 3 + 1] = g; rgb[i * 3 + 2] = b;
        }
        return rgb;
    }

    /// <summary>8-bit alpha: opaque at the centre, fading linearly to transparent past a radius.</summary>
    public static byte[] MakeRadialAlpha(int w, int h)
    {
        var a = new byte[w * h];
        double cx = (w - 1) / 2.0, cy = (h - 1) / 2.0, max = Math.Min(cx, cy);
        int i = 0;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            double d = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            double t = 1.0 - d / max;
            a[i++] = (byte)Math.Clamp(t * 255.0, 0, 255);
        }
        return a;
    }

    /// <summary>24-bit RGB: white background with a centred solid blue disc.</summary>
    public static byte[] MakeDiscOnWhite(int w, int h)
    {
        var rgb = new byte[w * h * 3];
        double cx = (w - 1) / 2.0, cy = (h - 1) / 2.0, r = Math.Min(cx, cy) * 0.8;
        int i = 0;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            bool inside = (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
            rgb[i++] = inside ? (byte)30 : (byte)255;
            rgb[i++] = inside ? (byte)90 : (byte)255;
            rgb[i++] = inside ? (byte)220 : (byte)255;
        }
        return rgb;
    }

    /// <summary>1-bit packed stencil (MSB first, rows byte-padded): 0 paints, 1 leaves alone.</summary>
    public static byte[] MakeCheckerBits(int w, int h)
    {
        int rowBytes = (w + 7) / 8;
        var bits = new byte[rowBytes * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            bool paint = ((x / 16) + (y / 16)) % 2 == 0;
            if (!paint) bits[y * rowBytes + (x >> 3)] |= (byte)(0x80 >> (x & 7));
        }
        return bits;
    }
}
