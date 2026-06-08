using CSharpPdf.Layout;
using PdfSpec.Fonts;
using Font = PdfSpec.Fonts.Font;

namespace CSharpPdf.Fluent;

/// <summary>
/// Fluent styling on the <see cref="TextElement"/> placed by <see cref="Container.Text(string)"/>.
/// All methods return <c>this</c> so styling chains.
/// </summary>
public sealed class TextDescriptor
{
    private readonly TextElement _text;
    internal TextDescriptor(TextElement t) { _text = t; }

    // ===== Font + size + colour =====

    public TextDescriptor Font(Font font) { _text.Font = font; return this; }
    public TextDescriptor FontSize(double size) { _text.FontSize = size; return this; }
    public TextDescriptor FontSize(double size, Unit unit) { _text.FontSize = Units.ToPoints(size, unit); return this; }
    public TextDescriptor FontColor(Color color) { _text.FontColor = color; return this; }

    public TextDescriptor Bold() { _text.Font = StandardFont.HelveticaBold; return this; }
    public TextDescriptor Italic() { _text.Font = StandardFont.HelveticaOblique; return this; }
    public TextDescriptor LineHeight(double leading) { _text.LineHeight = leading; return this; }
    public TextDescriptor LineHeight(double leading, Unit unit) { _text.LineHeight = Units.ToPoints(leading, unit); return this; }

    /// <summary>Persist per-word width measurements into the canvas-wide cache for reuse.</summary>
    public TextDescriptor SaveMetric() { _text.SaveMetric = true; return this; }

    // ===== Box styling (mirrors Container's so a Text descriptor can be styled in-place) =====

    public TextDescriptor Padding(double v) { _text.Padding = v; return this; }
    public TextDescriptor Padding(double v, Unit unit) { _text.Padding = Units.ToPoints(v, unit); return this; }
    public TextDescriptor Background(Color color) { _text.Background = color; return this; }
    public TextDescriptor Border(Color color, double width = 1) { _text.BorderColor = color; _text.BorderThickness = width; return this; }
    public TextDescriptor Border(Color color, double width, Unit unit) { _text.BorderColor = color; _text.BorderThickness = Units.ToPoints(width, unit); return this; }
    public TextDescriptor BorderRadius(double r) { _text.BorderRadius = r; return this; }
    public TextDescriptor BorderRadius(double r, Unit unit) { _text.BorderRadius = Units.ToPoints(r, unit); return this; }
    public TextDescriptor ExtendHorizontal() { _text.ExtendHorizontal = true; return this; }

    public TextDescriptor AlignLeft() { _text.HAlign = HorizontalAlignment.Left; return this; }
    public TextDescriptor AlignCenter() { _text.HAlign = HorizontalAlignment.Center; return this; }
    public TextDescriptor AlignRight() { _text.HAlign = HorizontalAlignment.Right; return this; }

    /// <summary>Subscribe to the text element's <see cref="Element.OnRendered"/> hook.</summary>
    public TextDescriptor OnRendered(System.Action<RenderedInfo> handler) { _text.OnRendered = handler; return this; }
}

/// <summary>Fluent styling on the <see cref="ImageElement"/> placed by <see cref="Container.Image"/>.</summary>
public sealed class ImageDescriptor
{
    private readonly ImageElement _image;
    internal ImageDescriptor(ImageElement i) { _image = i; }

    /// <summary>Display size in points. Overrides the natural pixel-to-point sizing.</summary>
    public ImageDescriptor Size(double width, double height) { _image.DisplayWidth = width; _image.DisplayHeight = height; return this; }
    /// <summary>Display size in <paramref name="unit"/>.</summary>
    public ImageDescriptor Size(double width, double height, Unit unit) { _image.DisplayWidth = Units.ToPoints(width, unit); _image.DisplayHeight = Units.ToPoints(height, unit); return this; }

    public ImageDescriptor Border(Color color, double width = 1) { _image.BorderColor = color; _image.BorderThickness = width; return this; }
    public ImageDescriptor Border(Color color, double width, Unit unit) { _image.BorderColor = color; _image.BorderThickness = Units.ToPoints(width, unit); return this; }
    public ImageDescriptor BorderRadius(double r) { _image.BorderRadius = r; return this; }
    public ImageDescriptor BorderRadius(double r, Unit unit) { _image.BorderRadius = Units.ToPoints(r, unit); return this; }
    public ImageDescriptor Padding(double v) { _image.Padding = v; return this; }
    public ImageDescriptor Padding(double v, Unit unit) { _image.Padding = Units.ToPoints(v, unit); return this; }

    /// <summary>Subscribe to the image element's <see cref="Element.OnRendered"/> hook.</summary>
    public ImageDescriptor OnRendered(System.Action<RenderedInfo> handler) { _image.OnRendered = handler; return this; }
}

/// <summary>Fluent styling on the <see cref="PageNumberElement"/> placed by <see cref="Container.PageNumber"/>.</summary>
public sealed class PageNumberDescriptor
{
    private readonly PageNumberElement _p;
    internal PageNumberDescriptor(PageNumberElement p) { _p = p; }

    public PageNumberDescriptor Font(Font font) { _p.Font = font; return this; }
    public PageNumberDescriptor FontSize(double size) { _p.FontSize = size; return this; }
    public PageNumberDescriptor FontColor(Color color) { _p.FontColor = color; return this; }
    public PageNumberDescriptor Bold() { _p.Font = StandardFont.HelveticaBold; return this; }

    /// <summary>Subscribe to the element's <see cref="Element.OnRendered"/> hook.</summary>
    public PageNumberDescriptor OnRendered(System.Action<RenderedInfo> handler) { _p.OnRendered = handler; return this; }
}

/// <summary>Fluent styling on the <see cref="PageReferenceElement"/> placed by <see cref="Container.PageReference"/>.</summary>
public sealed class PageReferenceDescriptor
{
    private readonly PageReferenceElement _p;
    internal PageReferenceDescriptor(PageReferenceElement p) { _p = p; }

    public PageReferenceDescriptor Font(Font font) { _p.Font = font; return this; }
    public PageReferenceDescriptor FontSize(double size) { _p.FontSize = size; return this; }
    public PageReferenceDescriptor FontColor(Color color) { _p.FontColor = color; return this; }
    public PageReferenceDescriptor Bold() { _p.Font = StandardFont.HelveticaBold; return this; }

    /// <summary>Subscribe to the element's <see cref="Element.OnRendered"/> hook.</summary>
    public PageReferenceDescriptor OnRendered(System.Action<RenderedInfo> handler) { _p.OnRendered = handler; return this; }
}
