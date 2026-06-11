using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Layout;
using ImperativeElement = PdfSpec.Layout.Element;
using ImperativeBorder = PdfSpec.Elements.BorderElement;
using ImperativeParagraph = PdfSpec.Elements.Paragraph;

namespace PdfSpec.Fluent;

/// <summary>
/// Fluent wrapper around <see cref="ImperativeBorder"/> — a styled-
/// chrome box (background, padding, per-side borders, sizing,
/// alignment) wrapping a single child. Per-side padding setters live
/// here as methods because the imperative side exposes them as
/// properties; fluent and imperative APIs are completely separated, so
/// the method names don't collide.
/// </summary>
public sealed class Container : Element
{
    private readonly ImperativeBorder _impl = new();

    // ===== padding ===========================================================

    /// <summary>Uniform padding on every side.</summary>
    public Container Padding(double all)
    {
        _impl.PaddingTop = _impl.PaddingRight = _impl.PaddingBottom = _impl.PaddingLeft = all;
        return this;
    }

    /// <summary>Vertical + horizontal padding pair.</summary>
    public Container Padding(double vertical, double horizontal)
    {
        _impl.PaddingTop = _impl.PaddingBottom = vertical;
        _impl.PaddingLeft = _impl.PaddingRight = horizontal;
        return this;
    }

    public Container PaddingTop(double value)    { _impl.PaddingTop = value;    return this; }
    public Container PaddingRight(double value)  { _impl.PaddingRight = value;  return this; }
    public Container PaddingBottom(double value) { _impl.PaddingBottom = value; return this; }
    public Container PaddingLeft(double value)   { _impl.PaddingLeft = value;   return this; }

    // ===== border ============================================================

    /// <summary>Uniform border on every side.</summary>
    public Container Border(double width, PdfColor color)
    {
        _impl.BorderTopWidth = _impl.BorderRightWidth = _impl.BorderBottomWidth = _impl.BorderLeftWidth = width;
        _impl.BorderTopColor = _impl.BorderRightColor = _impl.BorderBottomColor = _impl.BorderLeftColor = color;
        return this;
    }

    public Container BorderTop(double width, PdfColor color)
        { _impl.BorderTopWidth = width;    _impl.BorderTopColor = color;    return this; }
    public Container BorderRight(double width, PdfColor color)
        { _impl.BorderRightWidth = width;  _impl.BorderRightColor = color;  return this; }
    public Container BorderBottom(double width, PdfColor color)
        { _impl.BorderBottomWidth = width; _impl.BorderBottomColor = color; return this; }
    public Container BorderLeft(double width, PdfColor color)
        { _impl.BorderLeftWidth = width;   _impl.BorderLeftColor = color;   return this; }

    // ===== background, sizing, alignment =====================================

    public Container Background(PdfColor color) { _impl.Background = color; return this; }

    public Container Width(double points) { _impl.Width = new Length(points, Unit.Pt); return this; }
    public Container Width(double value, Unit unit) { _impl.Width = new Length(value, unit); return this; }
    public Container Height(double points) { _impl.Height = new Length(points, Unit.Pt); return this; }
    public Container Height(double value, Unit unit) { _impl.Height = new Length(value, unit); return this; }

    public Container HAlign(HorizontalAlignment alignment) { _impl.HorizontalAlignment = alignment; return this; }
    public Container VAlign(VerticalAlignment alignment) { _impl.VerticalAlignment = alignment; return this; }

    /// <summary>
    /// Fires once per render with a <see cref="RenderedData"/> snapshot
    /// (page, page number, on-page bounds). Canonical use: wire a Link
    /// annotation to the rendered box without hand-tracking coordinates.
    /// </summary>
    public Container OnRendered(Action<RenderedData> hook)
    {
        _impl.OnRendered = hook;
        return this;
    }

    // ===== content shortcuts =================================================

    /// <summary>Set the wrapped child element.</summary>
    public Container Content(Element child)
    {
        _impl.Content = child.Build();
        return this;
    }

    /// <summary>Wrap a fresh <see cref="ImperativeParagraph"/> as the child.</summary>
    public new Container Paragraph(string text, Font font, double size) =>
        Content(Element.Paragraph(text, font, size));

    /// <summary>Wrap a fresh Helvetica-11 <see cref="ImperativeParagraph"/> as the child.</summary>
    public new Container Paragraph(string text) =>
        Content(Element.Paragraph(text));

    internal override ImperativeElement Build() => _impl;
}
