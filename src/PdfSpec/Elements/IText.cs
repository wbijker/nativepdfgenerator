using PdfSpec.Fonts;
using PdfSpec.Geometry;

namespace PdfSpec.Elements;

/// <summary>
/// Chainable text-styling facade returned by <see cref="IContainer.Text(string)"/>.
/// Mutates an underlying <see cref="Paragraph"/> already installed in the
/// slot — every setter takes effect immediately and the chain can be
/// abandoned at any point.
///
/// <para>
/// <see cref="Bold"/> / <see cref="Italic"/> swap the underlying
/// <see cref="StandardFont"/> for the matching variant (Helvetica /
/// HelveticaBold / HelveticaOblique / HelveticaBoldOblique, and the
/// equivalent quartets for Times and Courier). They are no-ops on
/// custom font faces — the caller should set a concrete <see cref="StandardFont"/>
/// first or work with <see cref="IContainer.Paragraph(string, Font, double)"/>
/// for non-standard fonts.
/// </para>
/// </summary>
public interface IText
{
    /// <summary>Set the font size in points.</summary>
    IText FontSize(double size);

    /// <summary>Switch to the bold variant of the current font family.</summary>
    IText Bold();

    /// <summary>Switch to the italic / oblique variant of the current font family.</summary>
    IText Italic();

    /// <summary>Switch to the bold-italic variant of the current font family.</summary>
    IText BoldItalic();

    /// <summary>Set the glyph fill colour.</summary>
    IText Color(PdfColor color);

    /// <summary>Draw a horizontal rule under each wrapped line.</summary>
    IText Underline();
}

internal sealed class TextBuilder : IText
{
    private readonly Paragraph _paragraph;
    private bool _bold;
    private bool _italic;

    public TextBuilder(Paragraph paragraph)
    {
        _paragraph = paragraph;
        // Seed bold/italic from the starting font so chained calls
        // compose with whatever the caller began from.
        var name = paragraph.Font.BaseFont;
        _bold = name.Contains("Bold");
        _italic = name.Contains("Italic") || name.Contains("Oblique");
    }

    public IText FontSize(double size) { _paragraph.FontSize = size; return this; }

    public IText Bold()       { _bold = true; UpdateFont(); return this; }
    public IText Italic()     { _italic = true; UpdateFont(); return this; }
    public IText BoldItalic() { _bold = true; _italic = true; UpdateFont(); return this; }

    public IText Color(PdfColor color) { _paragraph.Color = color; return this; }
    public IText Underline()           { _paragraph.Underline = true; return this; }

    private void UpdateFont()
    {
        // Match family from the current font's BaseFont prefix and pick
        // the (bold, italic) variant. Unknown families (e.g. an embedded
        // TTF) fall through and the font isn't touched — bold/italic are
        // a no-op for them by design.
        var name = _paragraph.Font.BaseFont;
        StandardFont? next = null;
        if (name.StartsWith("Helvetica"))
            next = (_bold, _italic) switch
            {
                (false, false) => StandardFont.Helvetica,
                (true,  false) => StandardFont.HelveticaBold,
                (false, true ) => StandardFont.HelveticaOblique,
                (true,  true ) => StandardFont.HelveticaBoldOblique,
            };
        else if (name.StartsWith("Times"))
            next = (_bold, _italic) switch
            {
                (false, false) => StandardFont.TimesRoman,
                (true,  false) => StandardFont.TimesBold,
                (false, true ) => StandardFont.TimesItalic,
                (true,  true ) => StandardFont.TimesBoldItalic,
            };
        else if (name.StartsWith("Courier"))
            next = (_bold, _italic) switch
            {
                (false, false) => StandardFont.Courier,
                (true,  false) => StandardFont.CourierBold,
                (false, true ) => StandardFont.CourierOblique,
                (true,  true ) => StandardFont.CourierBoldOblique,
            };

        if (next is not null) _paragraph.Font = next;
    }
}
