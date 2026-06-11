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
/// <see cref="Column"/>, etc.).
///
/// <para>
/// <b>Lazy wrapping.</b> The implementation only allocates a
/// <see cref="BorderElement"/> if the caller touches at least one
/// chrome setter. If the chain goes straight from the slot entry point
/// to a content terminal (e.g. <c>p.Body().Paragraph("hi")</c>), the
/// content commits to its owning slot directly — no
/// <see cref="BorderElement"/> is built. The container's behaviour at
/// the call site is identical either way; the rendered tree is just
/// leaner when chrome isn't asked for.
/// </para>
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

    IContainer Border(double width, PdfColor color);
    IContainer BorderTop(double width, PdfColor color);
    IContainer BorderRight(double width, PdfColor color);
    IContainer BorderBottom(double width, PdfColor color);
    IContainer BorderLeft(double width, PdfColor color);

    IContainer Background(PdfColor color);

    IContainer Width(double points);
    IContainer Width(double value, Unit unit);
    IContainer Height(double points);
    IContainer Height(double value, Unit unit);

    IContainer HAlign(HorizontalAlignment alignment);
    IContainer VAlign(VerticalAlignment alignment);

    IContainer OnRendered(Action<RenderedData> hook);

    // ===== content terminals ================================================
    //
    // Each terminal commits one element to the slot. After a terminal
    // call the IContainer is consumed; subsequent calls are no-ops.

    /// <summary>Install <paramref name="child"/> directly as the slot's content.</summary>
    void Content(Element child);

    /// <summary>Install a paragraph with explicit font + size.</summary>
    void Paragraph(string text, Font font, double size);

    /// <summary>Install a Helvetica-11 paragraph — the conventional body-text default.</summary>
    void Paragraph(string text);

    /// <summary>Install a vertical column. <paramref name="build"/> receives an <see cref="IColumn"/> to populate items.</summary>
    void Column(Action<IColumn> build);

    /// <summary>Install a horizontal row. <paramref name="build"/> receives an <see cref="IRow"/> to populate cells.</summary>
    void Row(Action<IRow> build);

    /// <summary>Install an imperative drawing surface.</summary>
    void Canvas(double width, double height, Action<ContentStream, PdfSize> draw);

    /// <summary>Install a deferred "page N of M" stamp — auto-fills at save time.</summary>
    void PageNumber();
}

/// <summary>A column ( <see cref="VStack"/> ) being populated. Each <see cref="Item"/> call returns a slot for one auto-sized row of the column.</summary>
public interface IColumn
{
    IContainer Item();
}

/// <summary>A row ( <see cref="HStack"/> ) being populated. Each <see cref="Item"/> call returns a slot for one cell of the row.</summary>
public interface IRow
{
    IContainer Item();
}
