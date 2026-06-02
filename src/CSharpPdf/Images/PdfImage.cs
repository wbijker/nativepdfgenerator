using System.Text;
using CSharpPdf.Filters;
using CSharpPdf.Objects;

namespace CSharpPdf.Images;

/// <summary>
/// A raster image (ISO 32000-1 Chapter 8, "Image XObjects"). Holds everything
/// needed to construct the underlying <c>/Type /XObject /Subtype /Image</c>
/// stream — the encoded pixel payload plus its descriptive keys (width,
/// height, colour space, bits per component, filter, optional masks).
///
/// Construct via the static factories (<see cref="Rgb"/>, <see cref="Gray"/>,
/// <see cref="Jpeg"/>, <see cref="Alpha"/>, <see cref="Stencil"/>). Pass the
/// resulting instance to <c>canvas.DrawImage(...)</c>: the canvas embeds the
/// image as an indirect XObject on first use and emits a <c>Do</c> call for
/// each subsequent painting site, so the encoded bytes appear once in the
/// file regardless of how many times the image is drawn.
///
/// <para>
/// Dedup is by reference identity — the same <see cref="PdfImage"/> instance
/// drawn N times produces one XObject + N short <c>Do</c> calls. Two distinct
/// instances with the same pixel data produce two XObjects (the spec writer
/// can't know they're equivalent without comparing bytes).
/// </para>
///
/// <para>
/// <see cref="PreferInline"/> is a hint for tiny single-use images: when set
/// and the encoded payload is below ~4 KB, the canvas may emit the image
/// inline via <c>BI/ID/EI</c> instead of allocating a dedicated XObject.
/// Inline embedding repeats the bytes at every paint site, so this is only
/// worthwhile for images that are both small and used once. Default false
/// (always XObject) — safer for anything that might be reused.
/// </para>
/// </summary>
public sealed class PdfImage
{
    private readonly byte[] _payload;
    private readonly int _bitsPerComponent;
    private readonly PdfName? _colorSpace;
    private readonly bool _imageMask;
    private readonly PdfName? _filter;
    private readonly PdfArray? _decode;

    // Doc-level dedup: cached after the first time the image is embedded so
    // subsequent canvases on other pages reuse the same indirect reference
    // instead of writing the pixel bytes again.
    private PdfReference? _embeddedRef;

    /// <summary>Pixel width.</summary>
    public int Width { get; }

    /// <summary>Pixel height.</summary>
    public int Height { get; }

    /// <summary>Length of the encoded payload in bytes (after compression, if any).</summary>
    public int EncodedSize => _payload.Length;

    /// <summary>True if this image is a 1-bit stencil mask (an <c>/ImageMask</c>).</summary>
    public bool IsStencilMask => _imageMask;

    /// <summary>Optional soft mask (per-pixel alpha) — attached as <c>/SMask</c>.</summary>
    public PdfImage? SoftMask { get; set; }

    /// <summary>Optional 1-bit stencil mask referenced via <c>/Mask</c>.</summary>
    public PdfImage? StencilMaskImage { get; set; }

    /// <summary>Optional colour-key mask (paired component-value ranges) — attached as <c>/Mask</c>.</summary>
    public PdfArray? ColorKeyMask { get; set; }

    /// <summary>
    /// Hint that this image is small and expected to be painted once. When
    /// set and <see cref="EncodedSize"/> is below 4 KB, the canvas may emit
    /// the image inline (BI/ID/EI) instead of allocating an XObject. Default
    /// false. Inline embedding repeats the bytes at each paint site, so this
    /// is only worthwhile for tiny single-use images.
    /// </summary>
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

    // ===== Factories ====================================================

    /// <summary>An 8-bit-per-component DeviceRGB image (3 bytes per pixel, row-major).</summary>
    public static PdfImage Rgb(byte[] samples, int width, int height, bool compress = true) =>
        Build(samples, width, height, new PdfName("DeviceRGB"), bitsPerComponent: 8, imageMask: false, compress);

    /// <summary>An 8-bit DeviceGray image (1 byte per pixel).</summary>
    public static PdfImage Gray(byte[] samples, int width, int height, bool compress = true) =>
        Build(samples, width, height, new PdfName("DeviceGray"), bitsPerComponent: 8, imageMask: false, compress);

    /// <summary>
    /// A JPEG (JFIF) image embedded verbatim via the DCTDecode filter — the
    /// one raster format PDF accepts without re-encoding.
    /// </summary>
    public static PdfImage Jpeg(byte[] jpegData, int width, int height, string colorSpace = "DeviceRGB") =>
        new(jpegData, width, height, bitsPerComponent: 8,
            colorSpace: new PdfName(colorSpace), imageMask: false,
            filter: new PdfName("DCTDecode"));

    /// <summary>
    /// A grayscale alpha-mask image (Chapter 8, "Soft Masks"): per-pixel alpha
    /// where 0 is fully transparent and 255 fully opaque. Attach to a parent
    /// image via <see cref="SoftMask"/>.
    /// </summary>
    public static PdfImage Alpha(byte[] alpha, int width, int height, bool compress = true) =>
        Gray(alpha, width, height, compress);

    /// <summary>
    /// A 1-bit stencil mask (Chapter 8, "Stencil Masking"): packed bits,
    /// MSB first, rows padded to a byte. 0 paints, 1 leaves alone (set
    /// <paramref name="invert"/> to flip via a [1 0] Decode array). Used
    /// either as an <c>/ImageMask</c> painted in the current fill colour,
    /// or as a parent image's <see cref="StencilMaskImage"/>.
    /// </summary>
    public static PdfImage Stencil(byte[] bits, int width, int height, bool invert = false)
    {
        var decode = invert ? new PdfArray(new PdfNumber(1), new PdfNumber(0)) : null;
        return new PdfImage(bits, width, height, bitsPerComponent: 1,
            colorSpace: null, imageMask: true, filter: null, decode);
    }

    private static PdfImage Build(byte[] data, int width, int height, PdfName? colorSpace,
        int bitsPerComponent, bool imageMask, bool compress)
    {
        byte[] payload = compress ? FlateFilter.Encode(data) : data;
        var filter = compress ? new PdfName("FlateDecode") : null;
        return new PdfImage(payload, width, height, bitsPerComponent, colorSpace, imageMask, filter);
    }

    // ===== Embedding =====================================================

    /// <summary>
    /// Add this image to <paramref name="doc"/> as an indirect XObject stream
    /// and return the reference. Cached on the image — subsequent calls (e.g.
    /// from another page's canvas) return the same reference. Recursively
    /// embeds any attached soft / stencil masks.
    /// </summary>
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
        var d = stream.Dictionary;
        d["Type"] = new PdfName("XObject");
        d["Subtype"] = new PdfName("Image");
        d["Width"] = new PdfNumber(Width);
        d["Height"] = new PdfNumber(Height);
        d["BitsPerComponent"] = new PdfNumber(_bitsPerComponent);

        if (_imageMask)
        {
            d["ImageMask"] = new PdfBoolean(true);
        }
        else if (_colorSpace is not null)
        {
            d["ColorSpace"] = _colorSpace;
        }
        if (_filter is not null) d["Filter"] = _filter;
        if (_decode is not null) d["Decode"] = _decode;

        if (SoftMask is { } smask) d["SMask"] = smask.EmbedIn(doc);
        if (StencilMaskImage is { } stencil) d["Mask"] = stencil.EmbedIn(doc);
        else if (ColorKeyMask is { } key) d["Mask"] = key;

        return stream;
    }

    // ===== Inline emission ===============================================

    /// <summary>
    /// True if the image can be expressed as an inline image (BI/ID/EI).
    /// Inline images don't support soft / stencil masks or non-device colour
    /// spaces, so anything with those features must use the XObject path.
    /// </summary>
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

    /// <summary>
    /// Build the body of a BI…ID…EI sequence for this image. The caller is
    /// responsible for the surrounding <c>q cm</c> / <c>Q</c> wrap.
    /// </summary>
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
