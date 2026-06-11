using PdfSpec.Fonts;
using ImperativeElement = PdfSpec.Layout.Element;
using ImperativeParagraph = PdfSpec.Elements.Paragraph;

namespace PdfSpec.Fluent;

/// <summary>
/// Fluent wrapper around <see cref="ImperativeParagraph"/>. No fluent
/// setters of its own — a paragraph is fully described by
/// (<c>text</c>, <c>font</c>, <c>size</c>) at construction.
/// </summary>
public sealed class Paragraph : Element
{
    private readonly ImperativeParagraph _impl;

    internal Paragraph(string text, Font font, double size) =>
        _impl = new(text, font, size);

    internal override ImperativeElement Build() => _impl;
}
