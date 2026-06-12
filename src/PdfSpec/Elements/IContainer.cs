using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// A composition slot — somewhere the caller can install one
/// <see cref="Element"/> with optional chrome. Each entry point on
/// <see cref="PdfPage.Header()"/> / <see cref="PdfPage.Body()"/> /
/// <see cref="PdfPage.Footer()"/> / <see cref="IColumn.Item"/> /
/// <see cref="IRow.Item"/> returns one of these. The fluent surface
/// mirrors <see cref="BorderElement"/>'s chrome setters (padding,
/// background, border, sizing, alignment, OnRendered) plus a small set
/// of terminal content shortcuts (<see cref="Paragraph(string)"/>,
/// <see cref="Column"/>, etc.). The slot owner installs a fresh
/// <see cref="BorderElement"/> up-front and hands back a facade onto
/// it; chrome setters and the content terminal mutate that element in
/// place.
/// </summary>
public interface IContainer
{
    // ===== chrome ===========================================================

    IContainer Padding(double all);
    IContainer Padding(double vertical, double horizontal);
    IContainer PaddingTop(double value);
    IContainer PaddingRight(double value);
    IContainer PaddingBottom(double value);
    IContainer PaddingLeft(double value);

    IContainer Padding(double all, Unit unit);
    IContainer Padding(double vertical, double horizontal, Unit unit);
    IContainer PaddingTop(double value, Unit unit);
    IContainer PaddingRight(double value, Unit unit);
    IContainer PaddingBottom(double value, Unit unit);
    IContainer PaddingLeft(double value, Unit unit);

    IContainer Border(double width, PdfColor color);
    IContainer BorderTop(double width, PdfColor color);
    IContainer BorderRight(double width, PdfColor color);
    IContainer BorderBottom(double width, PdfColor color);
    IContainer BorderLeft(double width, PdfColor color);

    IContainer Rounded(double radius);
    IContainer RoundedTop(double radius);
    IContainer RoundedBottom(double radius);
    IContainer RoundedLeft(double radius);
    IContainer RoundedRight(double radius);
    IContainer RoundedX(double radius);
    IContainer RoundedY(double radius);

    IContainer Background(PdfColor color);

    IContainer Width(double points);
    IContainer Width(double value, Unit unit);
    IContainer Height(double points);
    IContainer Height(double value, Unit unit);

    IContainer HAlign(HorizontalAlignment alignment);
    IContainer VAlign(VerticalAlignment alignment);

    IContainer AlignLeft();
    IContainer AlignCenter();
    IContainer AlignRight();

    IContainer AlignTop();
    IContainer AlignMiddle();
    IContainer AlignBottom();

    IContainer OnRendered(Action<RenderedData> hook);

    /// <summary>
    /// Register a named destination <paramref name="name"/> at this
    /// slot's rendered location. Wires an OnRendered hook that calls
    /// <see cref="PdfDoc.AddNamedDestination(string, Actions.Destination)"/>
    /// with an XYZ destination at the slot's top-left corner.
    /// Chainable.
    /// </summary>
    IContainer Anchor(string name);

    /// <summary>
    /// Wire this slot as a clickable link to a named anchor. On render
    /// a Link annotation is added to the page whose Rect matches the
    /// slot's bounds and whose action jumps to the named destination.
    /// Chainable — combine with chrome / content terminals.
    /// </summary>
    IContainer LinkToAnchor(string name);

    /// <summary>
    /// Terminal form of <see cref="LinkToAnchor(string)"/>:
    /// <paramref name="build"/> populates the slot's content; the same
    /// rendered rectangle becomes the link.
    /// </summary>
    void LinkToAnchor(string name, Action<IContainer> build);

    // ===== content terminals ================================================
    //
    // Each terminal assigns one element to the slot's
    // BorderElement.Content. A second terminal call simply overwrites
    // the first.

    /// <summary>Install <paramref name="child"/> directly as the slot's content.</summary>
    void Content(Element child);

    /// <summary>Install a paragraph with explicit font + size.</summary>
    void Paragraph(string text, Font font, double size);

    /// <summary>Install a Helvetica-11 paragraph — the conventional body-text default.</summary>
    void Paragraph(string text);

    /// <summary>
    /// Install a chainable text run starting from Helvetica 11. The
    /// returned <see cref="IText"/> mutates the installed paragraph in
    /// place — <c>c.Text("hi").FontSize(14).Bold().Color(red)</c>.
    /// </summary>
    IText Text(string text);

    /// <summary>Install a vertical column. <paramref name="build"/> receives an <see cref="IColumn"/> to populate items.</summary>
    void Column(Action<IColumn> build);

    /// <summary>Install a horizontal row. <paramref name="build"/> receives an <see cref="IRow"/> to populate cells.</summary>
    void Row(Action<IRow> build);

    /// <summary>Install an imperative drawing surface.</summary>
    void Canvas(double width, double height, Action<ContentStream, PdfSize> draw);

    /// <summary>Install an SVG drawing parsed from <paramref name="svg"/> XML.</summary>
    void Svg(string svg);

    /// <summary>Install a pre-parsed <see cref="SvgImage"/> — use this form to share one parsed SVG across many slots without re-parsing.</summary>
    void Svg(SvgImage svg);

    /// <summary>Hand off this slot to <paramref name="component"/> — it populates the slot via <see cref="IComponent.Compose"/>.</summary>
    void Component(IComponent component);

    /// <summary>Install a deferred "page N of M" stamp — auto-fills at save time.</summary>
    void PageNumber();

    /// <summary>
    /// Install a deferred page-number stamp formatted via
    /// <see cref="string.Format(string, object?, object?)"/>: <c>{0}</c>
    /// = current page, <c>{1}</c> = total pages. Example: <c>"Page {0} of {1}"</c>
    /// or just <c>"{0}"</c>.
    /// </summary>
    void PageNumber(string format);

    /// <summary>Install a <see cref="Element.PageBreak"/> sentinel — forces the next sibling in the parent container onto a new page.</summary>
    void PageBreak();

    /// <summary>
    /// Install a multi-column flow. <paramref name="build"/> populates the
    /// items via an <see cref="IColumn"/> facade — every item flows
    /// top-to-bottom within a column and left-to-right across columns.
    /// </summary>
    void MultiColumn(int columns, double gap, Action<IColumn> build);

    /// <summary>
    /// Same as <see cref="MultiColumn(int, double, Action{IColumn})"/>
    /// with an explicit fixed <paramref name="height"/> on the wrapping
    /// section — items flow within that bounded height and overflow
    /// continues onto the next page.
    /// </summary>
    void MultiColumn(int columns, double height, double gap, Action<IColumn> build);
}

/// <summary>
/// A column ( <see cref="VStack"/> ) being populated. Each call returns a
/// slot whose vertical extent is fixed (<see cref="FixedItem"/>) or
/// shrinks to its content (<see cref="Item"/> / <see cref="AutoItem"/>).
/// <see cref="VStack"/> doesn't support relative slot heights — items
/// claim either a known height or their content's natural height.
/// </summary>
public interface IColumn
{
    /// <summary>Append an auto-height slot — alias for <see cref="AutoItem"/>.</summary>
    IContainer Item();

    /// <summary>Append a slot whose height is exactly <paramref name="height"/> points.</summary>
    IContainer FixedItem(double height);

    /// <summary>Append a slot whose height is whatever its content renders into.</summary>
    IContainer AutoItem();
}

/// <summary>
/// A row ( <see cref="HStack"/> ) being populated. Each call returns a
/// slot whose horizontal extent is fixed, content-driven (auto), or a
/// relative share of the remaining width.
/// </summary>
public interface IRow
{
    /// <summary>Append an auto-width slot — alias for <see cref="AutoItem"/>.</summary>
    IContainer Item();

    /// <summary>Append a slot whose width is exactly <paramref name="width"/> points.</summary>
    IContainer FixedItem(double width);

    /// <summary>Append a slot whose width is whatever its content renders into.</summary>
    IContainer AutoItem();

    /// <summary>
    /// Append a slot claiming <paramref name="units"/> share of the
    /// width left after fixed / auto cells. Default <c>1</c> for
    /// equal-share columns.
    /// </summary>
    IContainer RelativeItem(double units = 1);
}
