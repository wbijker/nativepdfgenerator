using System.IO.Compression;
using System.Text;

namespace PdfSpec.Images;

/// <summary>
/// A minimal PNG decoder for 8-bit, non-interlaced RGB or RGBA PNGs.
/// Returns the decoded pixels as raw DeviceRGB samples (alpha is dropped).
/// Built on the BCL only — no external dependencies.
/// </summary>
internal static class PngDecoder
{
    /// <summary>Decode a PNG byte array to (rgbSamples, width, height). Alpha is stripped.</summary>
    public static (byte[] Rgb, int Width, int Height) DecodeToRgb(byte[] file)
    {
        // PNG signature: 89 50 4E 47 0D 0A 1A 0A
        if (file.Length < 8) throw new InvalidDataException("Not a PNG.");
        if (file[0] != 0x89 || file[1] != 0x50 || file[2] != 0x4E || file[3] != 0x47
            || file[4] != 0x0D || file[5] != 0x0A || file[6] != 0x1A || file[7] != 0x0A)
            throw new InvalidDataException("Not a PNG.");

        int width = 0, height = 0;
        int bitDepth = 0, colorType = 0, interlace = 0;
        var idat = new MemoryStream();

        int pos = 8;
        while (pos + 8 <= file.Length)
        {
            int length = ReadU32(file, pos); pos += 4;
            string type = Encoding.ASCII.GetString(file, pos, 4); pos += 4;

            if (type == "IHDR")
            {
                width = ReadU32(file, pos);
                height = ReadU32(file, pos + 4);
                bitDepth = file[pos + 8];
                colorType = file[pos + 9];
                interlace = file[pos + 12];
            }
            else if (type == "IDAT")
            {
                idat.Write(file, pos, length);
            }
            else if (type == "IEND")
            {
                break;
            }

            pos += length + 4; // chunk data + CRC
        }

        if (bitDepth != 8) throw new NotSupportedException($"Unsupported PNG bit depth: {bitDepth}.");
        if (interlace != 0) throw new NotSupportedException("Interlaced PNGs not supported.");
        if (colorType != 2 && colorType != 6)
            throw new NotSupportedException($"Unsupported PNG colour type {colorType}. Only 2 (RGB) and 6 (RGBA) supported.");

        int bpp = colorType == 6 ? 4 : 3;
        int rowBytes = width * bpp;

        // zlib-decompress the concatenated IDAT data.
        idat.Position = 0;
        using var zlib = new ZLibStream(idat, CompressionMode.Decompress);
        using var raw = new MemoryStream(height * (rowBytes + 1));
        zlib.CopyTo(raw);
        var rawBytes = raw.GetBuffer();
        int rawLen = (int)raw.Length;
        int expected = height * (rowBytes + 1);
        if (rawLen < expected)
            throw new InvalidDataException($"PNG decompressed payload too short: {rawLen} < {expected}.");

        // Apply per-row filters into a contiguous unfiltered buffer.
        var pixels = new byte[height * rowBytes];
        for (int row = 0; row < height; row++)
        {
            int rowStart = row * (rowBytes + 1);
            byte filter = rawBytes[rowStart];
            int dstRow = row * rowBytes;
            int prevRow = (row - 1) * rowBytes;

            for (int col = 0; col < rowBytes; col++)
            {
                byte left = col >= bpp ? pixels[dstRow + col - bpp] : (byte)0;
                byte up = row > 0 ? pixels[prevRow + col] : (byte)0;
                byte upLeft = (col >= bpp && row > 0) ? pixels[prevRow + col - bpp] : (byte)0;
                byte v = rawBytes[rowStart + 1 + col];

                pixels[dstRow + col] = filter switch
                {
                    0 => v,                                        // None
                    1 => (byte)(v + left),                         // Sub
                    2 => (byte)(v + up),                           // Up
                    3 => (byte)(v + (left + up) / 2),              // Average
                    4 => (byte)(v + Paeth(left, up, upLeft)),      // Paeth
                    _ => throw new InvalidDataException($"Unknown PNG filter {filter}.")
                };
            }
        }

        if (colorType == 2) return (pixels, width, height);

        // RGBA → RGB (drop alpha).
        var rgb = new byte[width * height * 3];
        for (int i = 0, j = 0; i < pixels.Length; i += 4, j += 3)
        {
            rgb[j]     = pixels[i];
            rgb[j + 1] = pixels[i + 1];
            rgb[j + 2] = pixels[i + 2];
        }
        return (rgb, width, height);
    }

    private static byte Paeth(byte a, byte b, byte c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return a;
        if (pb <= pc) return b;
        return c;
    }

    private static int ReadU32(byte[] data, int offset) =>
        (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
}
