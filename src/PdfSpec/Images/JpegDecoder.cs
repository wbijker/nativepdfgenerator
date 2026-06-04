namespace PdfSpec.Images;

/// <summary>
/// A minimal JPEG header reader — only parses the Start-of-Frame marker to
/// extract dimensions and component count. JPEG payloads are embedded in
/// PDFs verbatim via the <c>DCTDecode</c> filter; nothing actually needs to
/// be decompressed here.
/// </summary>
internal static class JpegDecoder
{
    /// <summary>
    /// Read width, height, and component count (1 = gray, 3 = YCbCr/RGB,
    /// 4 = CMYK) from a JPEG byte stream by walking the segment headers
    /// until the first Start-of-Frame marker.
    /// </summary>
    public static (int Width, int Height, int Components) ReadInfo(byte[] data)
    {
        // SOI: FF D8
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
            throw new InvalidDataException("Not a JPEG (no SOI marker).");

        int pos = 2;
        while (pos + 4 < data.Length)
        {
            // Find the next marker, skipping any fill 0xFF bytes.
            if (data[pos] != 0xFF) { pos++; continue; }
            while (pos < data.Length && data[pos] == 0xFF) pos++;
            if (pos >= data.Length) break;

            byte marker = data[pos++];

            // Standalone markers: SOI, EOI, TEM, RST0..RST7 — no length, no payload.
            if (marker == 0xD8 || marker == 0xD9 || marker == 0x01 ||
                (marker >= 0xD0 && marker <= 0xD7))
            {
                continue;
            }

            // Everything else has a 2-byte big-endian segment length following the marker.
            if (pos + 1 >= data.Length) break;
            int length = (data[pos] << 8) | data[pos + 1];

            // SOF0..SOF15 (Start of Frame). Exclude 0xC4 (DHT), 0xC8 (JPG), 0xCC (DAC).
            if (marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
            {
                // After 2-byte length: precision(1), height(2), width(2), components(1).
                int height = (data[pos + 3] << 8) | data[pos + 4];
                int width = (data[pos + 5] << 8) | data[pos + 6];
                int components = data[pos + 7];
                return (width, height, components);
            }

            pos += length;
        }
        throw new InvalidDataException("JPEG SOF marker not found.");
    }
}
