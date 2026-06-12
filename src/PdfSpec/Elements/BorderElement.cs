using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// A <see cref="BoxElement"/> that wraps a single child <see cref="Element"/>.
/// The chrome (background, padding, per-side borders, optional explicit
/// width / height, horizontal / vertical alignment) lives on the base;
/// this subclass owns the wrapped <see cref="Content"/> and implements
/// <see cref="BoxElement.Draw"/> to render it into the inner area.
///
/// <para>
/// Exposes the canonical fluent chainable setters used to build a
/// container — <see cref="Padding(double)"/>, <see cref="BorderBottom"/>,
/// <see cref="Background"/>, etc. — each returning <c>this</c> typed as
/// <see cref="BorderElement"/> so a chain stays on the concrete type.
/// </para>
/// </summary>
public class BorderElement : BoxElement
{
    private Element? _content;

    // ===== chrome — padding ==================================================

    /// <summary>Uniform padding on every side.</summary>
    public BorderElement Padding(double all)
    {
        _paddingTop = _paddingRight = _paddingBottom = _paddingLeft = all;
        return this;
    }

    /// <summary>Vertical + horizontal padding pair.</summary>
    public BorderElement Padding(double vertical, double horizontal)
    {
        _paddingTop = _paddingBottom = vertical;
        _paddingLeft = _paddingRight = horizontal;
        return this;
    }

    public BorderElement PaddingTop(double value)    { _paddingTop = value;    return this; }
    public BorderElement PaddingRight(double value)  { _paddingRight = value;  return this; }
    public BorderElement PaddingBottom(double value) { _paddingBottom = value; return this; }
    public BorderElement PaddingLeft(double value)   { _paddingLeft = value;   return this; }

    /// <summary>Uniform padding on every side, in the given unit.</summary>
    public BorderElement Padding(double value, Unit unit)
    {
        double pt = new Length(value, unit).ToPoints();
        _paddingTop = _paddingRight = _paddingBottom = _paddingLeft = pt;
        return this;
    }

    /// <summary>Vertical + horizontal padding pair, in the given unit.</summary>
    public BorderElement Padding(double vertical, double horizontal, Unit unit)
    {
        _paddingTop = _paddingBottom = new Length(vertical, unit).ToPoints();
        _paddingLeft = _paddingRight = new Length(horizontal, unit).ToPoints();
        return this;
    }

    public BorderElement PaddingTop(double value, Unit unit)    { _paddingTop    = new Length(value, unit).ToPoints(); return this; }
    public BorderElement PaddingRight(double value, Unit unit)  { _paddingRight  = new Length(value, unit).ToPoints(); return this; }
    public BorderElement PaddingBottom(double value, Unit unit) { _paddingBottom = new Length(value, unit).ToPoints(); return this; }
    public BorderElement PaddingLeft(double value, Unit unit)   { _paddingLeft   = new Length(value, unit).ToPoints(); return this; }

    // ===== chrome — border ===================================================

    /// <summary>Uniform border on every side.</summary>
    public BorderElement Border(double width, PdfColor color)
    {
        BorderTopWidth = BorderRightWidth = BorderBottomWidth = BorderLeftWidth = width;
        BorderTopColor = BorderRightColor = BorderBottomColor = BorderLeftColor = color;
        return this;
    }

    public BorderElement BorderTop(double width, PdfColor color)
        { BorderTopWidth = width;    BorderTopColor = color;    return this; }
    public BorderElement BorderRight(double width, PdfColor color)
        { BorderRightWidth = width;  BorderRightColor = color;  return this; }
    public BorderElement BorderBottom(double width, PdfColor color)
        { BorderBottomWidth = width; BorderBottomColor = color; return this; }
    public BorderElement BorderLeft(double width, PdfColor color)
        { BorderLeftWidth = width;   BorderLeftColor = color;   return this; }

    // ===== chrome — corner radius ============================================

    /// <summary>Round all four corners uniformly to <paramref name="radius"/> pt.</summary>
    public BorderElement Rounded(double radius)
    {
        BorderRadiusTopLeft = BorderRadiusTopRight = BorderRadiusBottomRight = BorderRadiusBottomLeft = radius;
        return this;
    }

    /// <summary>Round the two top corners — top-left and top-right.</summary>
    public BorderElement RoundedTop(double radius)
    {
        BorderRadiusTopLeft = BorderRadiusTopRight = radius;
        return this;
    }

    /// <summary>Round the two bottom corners — bottom-left and bottom-right.</summary>
    public BorderElement RoundedBottom(double radius)
    {
        BorderRadiusBottomLeft = BorderRadiusBottomRight = radius;
        return this;
    }

    /// <summary>Round the two left corners — top-left and bottom-left.</summary>
    public BorderElement RoundedLeft(double radius)
    {
        BorderRadiusTopLeft = BorderRadiusBottomLeft = radius;
        return this;
    }

    /// <summary>Round the two right corners — top-right and bottom-right.</summary>
    public BorderElement RoundedRight(double radius)
    {
        BorderRadiusTopRight = BorderRadiusBottomRight = radius;
        return this;
    }

    /// <summary>
    /// Round the corners along the <c>\</c> diagonal — top-left and
    /// bottom-right.
    /// </summary>
    public BorderElement RoundedX(double radius)
    {
        BorderRadiusTopLeft = BorderRadiusBottomRight = radius;
        return this;
    }

    /// <summary>
    /// Round the corners along the <c>/</c> diagonal — top-right and
    /// bottom-left.
    /// </summary>
    public BorderElement RoundedY(double radius)
    {
        BorderRadiusTopRight = BorderRadiusBottomLeft = radius;
        return this;
    }

    // ===== chrome — background, sizing, alignment ============================

    public BorderElement Background(PdfColor color) { _background = color; return this; }

    public BorderElement Width(double points) { _width = new Length(points, Unit.Pt); return this; }
    public BorderElement Width(double value, Unit unit) { _width = new Length(value, unit); return this; }
    public BorderElement Height(double points) { _height = new Length(points, Unit.Pt); return this; }
    public BorderElement Height(double value, Unit unit) { _height = new Length(value, unit); return this; }

    public BorderElement HAlign(HorizontalAlignment alignment) { HorizontalAlignment = alignment; return this; }
    public BorderElement VAlign(VerticalAlignment alignment)   { VerticalAlignment = alignment;   return this; }

    public BorderElement AlignLeft()   { HorizontalAlignment = HorizontalAlignment.Left;   return this; }
    public BorderElement AlignCenter() { HorizontalAlignment = HorizontalAlignment.Center; return this; }
    public BorderElement AlignRight()  { HorizontalAlignment = HorizontalAlignment.Right;  return this; }

    public BorderElement AlignTop()    { VerticalAlignment = VerticalAlignment.Top;    return this; }
    public BorderElement AlignMiddle() { VerticalAlignment = VerticalAlignment.Middle; return this; }
    public BorderElement AlignBottom() { VerticalAlignment = VerticalAlignment.Bottom; return this; }

    /// <summary>
    /// Wire an <see cref="Element.OnRendered"/> hook (page, page number,
    /// on-page bounds) — canonical use: a Link annotation matched to
    /// the rendered box without hand-tracking coordinates. Replaces any
    /// previously-installed hook; use <see cref="AddRenderedListener"/>
    /// to chain. Chainable.
    /// </summary>
    public new BorderElement OnRendered(Action<RenderedData> hook) { ((Element)this).OnRendered = hook; return this; }

    /// <summary>Compose <paramref name="hook"/> with any existing <see cref="Element.OnRendered"/> handler — both fire, in install order. Used by anchor / link helpers so they don't trample a user-installed handler.</summary>
    internal BorderElement AddRenderedListener(Action<RenderedData> hook)
    {
        var existing = ((Element)this).OnRendered;
        ((Element)this).OnRendered = existing is null
            ? hook
            : data => { existing(data); hook(data); };
        return this;
    }

    // ===== content shortcuts =================================================

    /// <summary>Set the wrapped child element.</summary>
    public BorderElement Content(Element child)
    {
        _content = child;
        return this;
    }

    // ===== render ============================================================
    public override PdfSizeHint SizeHint(PdfSize available)
    {
        // Explicit Width/Height short-circuit the content measurement:
        // the box claims exactly that extent (Min and Max collapse to it),
        // so a parent layout sizes the column / band to the requested
        // value rather than to whatever the content wants.
        var explicitW = ResolveWidth(available.Width);
        var explicitH = ResolveHeight(available.Height);

        double chromeW = HorizontalChrome;
        double chromeH = VerticalChrome;

        var inner = new PdfSize(
            Math.Max(0, (explicitW ?? available.Width) - chromeW),
            Math.Max(0, (explicitH ?? available.Height) - chromeH));

        var hint = _content?.SizeHint(inner) ?? new PdfSizeHint(0, 0, null, null);

        double minW = explicitW ?? (_content is null ? chromeW : hint.MinWidth + chromeW);
        double minH = explicitH ?? (_content is null ? chromeH : hint.MinHeight + chromeH);
        double? maxW = explicitW ?? (hint.MaxWidth is null ? null : hint.MaxWidth.Value + chromeW);
        double? maxH = explicitH ?? (hint.MaxHeight is null ? null : hint.MaxHeight.Value + chromeH);

        return new PdfSizeHint(minW, minH, maxW, maxH);
    }

    /// <summary>
    /// The child's natural drawing width — surfaced so
    /// <see cref="BoxElement.HorizontalAlignment"/> can distribute slack
    /// when the child is narrower than the inner area.
    /// </summary>
    protected override double? DrawNaturalWidth(PdfSize innerAvailable) =>
        _content?.SizeHint(innerAvailable).MaxWidth;

    protected override RenderResult Draw(ContentStream cs, PdfSize available)
    {
        if (_content is null) return RenderResult.Done(0);
        return _content.Render(cs, available);
    }
}
