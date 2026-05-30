using CSharpPdf.Layout;
using CSharpPdf.Text;
using Font = CSharpPdf.Text.Font;

namespace CSharpPdf.Fluent;

/// <summary>Fluent styling on a TextElement that has just been placed.</summary>
public sealed class TextBuilder
{
    private readonly TextElement _text;
    internal TextBuilder(TextElement t) { _text = t; }

    public TextBuilder FontSize(double size) { _text.FontSize = size; return this; }
    public TextBuilder FontColor(Color color) { _text.FontColor = color; return this; }
    public TextBuilder Font(Font font) { _text.Font = font; return this; }
    public TextBuilder Bold() { _text.Font = Standard14Font.HelveticaBold; return this; }
    public TextBuilder Italic() { _text.Font = Standard14Font.HelveticaOblique; return this; }
    public TextBuilder LineHeight(double leading) { _text.LineHeight = leading; return this; }
    public TextBuilder Padding(double v) { _text.Padding = v; return this; }
    public TextBuilder Background(Color color) { _text.Background = color; return this; }
    public TextBuilder Border(Color color, double width = 1) { _text.BorderColor = color; _text.BorderThickness = width; return this; }
    public TextBuilder BorderRadius(double r) { _text.BorderRadius = r; return this; }
    public TextBuilder ExtendHorizontal() { _text.ExtendHorizontal = true; return this; }
    public TextBuilder AlignLeft() { _text.HAlign = HorizontalAlignment.Left; return this; }
    public TextBuilder AlignCenter() { _text.HAlign = HorizontalAlignment.Center; return this; }
    public TextBuilder AlignRight() { _text.HAlign = HorizontalAlignment.Right; return this; }
}

/// <summary>Fluent styling on an ImageElement just placed (border, display size override).</summary>
public sealed class ImageBuilder
{
    private readonly ImageElement _image;
    internal ImageBuilder(ImageElement i) { _image = i; }

    public ImageBuilder Size(double width, double height) { _image.DisplayWidth = width; _image.DisplayHeight = height; return this; }
    public ImageBuilder Border(Color color, double width = 1) { _image.BorderColor = color; _image.BorderThickness = width; return this; }
    public ImageBuilder BorderRadius(double r) { _image.BorderRadius = r; return this; }
    public ImageBuilder Padding(double v) { _image.Padding = v; return this; }
}

/// <summary>Fluent styling on a PageNumberElement.</summary>
public sealed class PageNumberBuilder
{
    private readonly PageNumberElement _p;
    internal PageNumberBuilder(PageNumberElement p) { _p = p; }
    public PageNumberBuilder FontSize(double s) { _p.FontSize = s; return this; }
    public PageNumberBuilder FontColor(Color c) { _p.FontColor = c; return this; }
    public PageNumberBuilder Font(Font f) { _p.Font = f; return this; }
}
