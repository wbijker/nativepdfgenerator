using System.Text;
using PdfSpec.Filters;
using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace PdfSpec.Images;

/// <summary>
/// A raster image (ISO 32000-1 Chapter 8, "Image XObjects"). Holds the encoded
/// pixel payload plus its descriptive keys (width, height, colour space, bits
/// per component, filter, optional masks).
/// </summary>
public sealed class PdfImage
{
    private readonly byte[] _payload;
    private readonly int _bitsPerComponent;
    private readonly PdfName? _colorSpace;
    private readonly bool _imageMask;
    private readonly PdfName? _filter;
    private readonly PdfArray? _decode;

    private PdfReference? _embeddedRef;

    public int Width { get; }
    public int Height { get; }
    public int EncodedSize => _payload.Length;
    public bool IsStencilMask => _imageMask;

    public PdfImage? SoftMask { get; set; }
    public PdfImage? StencilMaskImage { get; set; }
    public PdfArray? ColorKeyMask { get; set; }

    public bool PreferInline { get; set; }

    private PdfImage(byte[] payload, int width, int height, int bitsPerComponent,
        PdfName? colorSpace, bool imageMask, PdfName? filter, PdfArray? decode = null)
    {
        _payload = payload;
        Width = width;
        Height = height;
        _bitsPerComponent = bitsPerComponent;
        _colorSpace = colorSpace;
        _imageMask = imageMask;
        _filter = filter;
        _decode = decode;
    }

    public static PdfImage Rgb(byte[] samples, int width, int height, bool compress = true) =>
        Build(samples, width, height, new PdfName("DeviceRGB"), bitsPerComponent: 8, imageMask: false, compress);

    public static PdfImage Gray(byte[] samples, int width, int height, bool compress = true) =>
        Build(samples, width, height, new PdfName("DeviceGray"), bitsPerComponent: 8, imageMask: false, compress);

    public static PdfImage Jpeg(byte[] jpegData, int width, int height, ColorSpace colorSpace = ColorSpace.Rgb) =>
        new(jpegData, width, height, bitsPerComponent: 8,
            colorSpace: new PdfName(DeviceColorSpaceName(colorSpace)), imageMask: false,
            filter: new PdfName("DCTDecode"));

    public static PdfImage Alpha(byte[] alpha, int width, int height, bool compress = true) =>
        Gray(alpha, width, height, compress);

    public static PdfImage Stencil(byte[] bits, int width, int height, bool invert = false)
    {
        var decode = invert ? new PdfArray(new PdfNumber(1), new PdfNumber(0)) : null;
        return new PdfImage(bits, width, height, bitsPerComponent: 1,
            colorSpace: null, imageMask: true, filter: null, decode);
    }

    /// <summary>
    /// Load a PNG file and embed as a DeviceRGB image. Supports 8-bit,
    /// non-interlaced RGB or RGBA PNGs; the alpha channel is dropped.
    /// </summary>
    public static PdfImage LoadFromFilePng(string filename)
    {
        var (rgb, width, height) = PngDecoder.DecodeToRgb(File.ReadAllBytes(filename));
        return Rgb(rgb, width, height);
    }

    /// <summary>
    /// Load a JPEG file and embed verbatim via <c>DCTDecode</c> — no
    /// recompression. The colour space is inferred from the JPEG header:
    /// 1 component → DeviceGray, 3 → DeviceRGB, 4 → DeviceCMYK.
    /// </summary>
    public static PdfImage LoadFromFileJpg(string filename)
    {
        var data = File.ReadAllBytes(filename);
        var (w, h, components) = JpegDecoder.ReadInfo(data);
        var colorSpace = components switch
        {
            1 => ColorSpace.Gray,
            4 => ColorSpace.Cmyk,
            _ => ColorSpace.Rgb,
        };
        return Jpeg(data, w, h, colorSpace);
    }

    private static string DeviceColorSpaceName(ColorSpace cs) => cs switch
    {
        ColorSpace.Gray => "DeviceGray",
        ColorSpace.Cmyk => "DeviceCMYK",
        _ => "DeviceRGB",
    };

    private static PdfImage Build(byte[] data, int width, int height, PdfName? colorSpace,
        int bitsPerComponent, bool imageMask, bool compress)
    {
        byte[] payload = compress ? FlateFilter.Encode(data) : data;
        var filter = compress ? new PdfName("FlateDecode") : null;
        return new PdfImage(payload, width, height, bitsPerComponent, colorSpace, imageMask, filter);
    }

    public PdfReference EmbedIn(PdfDoc doc)
    {
        if (_embeddedRef is { } cached) return cached;
        var stream = BuildStream(doc);
        _embeddedRef = doc.AddObject(stream);
        return _embeddedRef;
    }

    private PdfStream BuildStream(PdfDoc doc)
    {
        var stream = new PdfStream(_payload);
        var typed = new ImageXObjectDictionary
        {
            Width = Width,
            Height = Height,
            BitsPerComponent = _bitsPerComponent,
            ImageMask = _imageMask,
            ColorSpace = _colorSpace,
            Filter = _filter,
            Decode = _decode,
        };
        if (SoftMask is { } smask) typed.SoftMask = smask.EmbedIn(doc);
        if (StencilMaskImage is { } stencil) typed.Mask = stencil.EmbedIn(doc);
        else if (ColorKeyMask is { } key) typed.Mask = key;
        typed.WriteTo(stream.Dictionary);
        return stream;
    }

    internal bool CanInline =>
        SoftMask is null
        && StencilMaskImage is null
        && ColorKeyMask is null
        && (_colorSpace is null
            || _colorSpace.Value == "DeviceGray"
            || _colorSpace.Value == "DeviceRGB"
            || _colorSpace.Value == "DeviceCMYK")
        && (_filter is null
            || _filter.Value == "FlateDecode"
            || _filter.Value == "DCTDecode"
            || _filter.Value == "ASCII85Decode"
            || _filter.Value == "ASCIIHexDecode");

    internal string BuildInlineBody()
    {
        var sb = new StringBuilder();
        sb.Append("BI\n");
        sb.Append("/W ").Append(Width).Append(" /H ").Append(Height);
        sb.Append(" /BPC ").Append(_bitsPerComponent);
        if (_imageMask)
        {
            sb.Append(" /IM true");
        }
        else if (_colorSpace is not null)
        {
            sb.Append(" /CS /").Append(AbbreviateColorSpace(_colorSpace.Value));
        }
        if (_filter is not null)
        {
            sb.Append(" /F /").Append(AbbreviateFilter(_filter.Value));
        }
        if (_decode is not null)
        {
            sb.Append(" /D ");
            using var ms = new MemoryStream();
            _decode.Write(ms);
            sb.Append(Encoding.Latin1.GetString(ms.ToArray()));
        }
        sb.Append("\nID ");
        sb.Append(Encoding.Latin1.GetString(_payload));
        sb.Append("\nEI");
        return sb.ToString();
    }

    private static string AbbreviateColorSpace(string name) => name switch
    {
        "DeviceGray" => "G",
        "DeviceRGB" => "RGB",
        "DeviceCMYK" => "CMYK",
        _ => name,
    };

    private static string AbbreviateFilter(string name) => name switch
    {
        "ASCIIHexDecode" => "AHx",
        "ASCII85Decode" => "A85",
        "LZWDecode" => "LZW",
        "FlateDecode" => "Fl",
        "RunLengthDecode" => "RL",
        "CCITTFaxDecode" => "CCF",
        "DCTDecode" => "DCT",
        _ => name,
    };
}
