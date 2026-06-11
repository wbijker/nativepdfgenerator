using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Concrete <see cref="IContainer"/> — lazy <see cref="BorderElement"/>
/// proxy. Chrome setters allocate the wrapper on first touch; content
/// terminals commit either the wrapper (chrome touched) or the raw
/// content element (no chrome) to the owning slot via the constructor's
/// <c>commit</c> callback.
/// </summary>
internal sealed class Container : IContainer
{
    private readonly Action<Element> _commit;
    private BorderElement? _border;
    private bool _committed;

    public Container(Action<Element> commit)
    {
        _commit = commit;
    }

    private BorderElement Border() => _border ??= new BorderElement();

    // ===== chrome (lazy BorderElement) ======================================

    public IContainer Padding(double all)              { Border().Padding(all);              return this; }
    public IContainer Padding(double v, double h)      { Border().Padding(v, h);             return this; }
    public IContainer PaddingTop(double v)             { Border().PaddingTop(v);             return this; }
    public IContainer PaddingRight(double v)           { Border().PaddingRight(v);           return this; }
    public IContainer PaddingBottom(double v)          { Border().PaddingBottom(v);          return this; }
    public IContainer PaddingLeft(double v)            { Border().PaddingLeft(v);            return this; }

    public IContainer Border(double w, PdfColor c)     { Border().Border(w, c);              return this; }
    public IContainer BorderTop(double w, PdfColor c)  { Border().BorderTop(w, c);           return this; }
    public IContainer BorderRight(double w, PdfColor c){ Border().BorderRight(w, c);         return this; }
    public IContainer BorderBottom(double w, PdfColor c){Border().BorderBottom(w, c);        return this; }
    public IContainer BorderLeft(double w, PdfColor c) { Border().BorderLeft(w, c);          return this; }

    public IContainer Background(PdfColor c)           { Border().Background(c);             return this; }

    public IContainer Width(double pt)                 { Border().Width(pt);                 return this; }
    public IContainer Width(double v, Unit u)          { Border().Width(v, u);               return this; }
    public IContainer Height(double pt)                { Border().Height(pt);                return this; }
    public IContainer Height(double v, Unit u)         { Border().Height(v, u);              return this; }

    public IContainer HAlign(HorizontalAlignment a)    { Border().HAlign(a);                 return this; }
    public IContainer VAlign(VerticalAlignment a)      { Border().VAlign(a);                 return this; }
    public IContainer OnRendered(Action<RenderedData> hook) { Border().OnRendered(hook);     return this; }

    // ===== content terminals ================================================

    public void Content(Element child) => Commit(child);

    public void Paragraph(string text, Font font, double size) =>
        Commit(new Paragraph(text, font, size));

    public void Paragraph(string text) =>
        Commit(new Paragraph(text, StandardFont.Helvetica, 11));

    public void Column(Action<IColumn> build)
    {
        var stack = new VStack();
        build(new ColumnAdapter(stack));
        Commit(stack);
    }

    public void Row(Action<IRow> build)
    {
        var row = new HStack();
        build(new RowAdapter(row));
        Commit(row);
    }

    public void Canvas(double width, double height, Action<ContentStream, PdfSize> draw) =>
        Commit(new Canvas { Width = width, Height = height, Draw = draw });

    public void PageNumber() =>
        Commit(new DeferredComponent(
            sizeHint: new Paragraph("Page 999 of 999", StandardFont.Helvetica, 11),
            render: data => new Paragraph($"Page {data.PageNumber} of {data.TotalPages}",
                StandardFont.Helvetica, 11)));

    private void Commit(Element child)
    {
        if (_committed) return;
        _committed = true;

        if (_border is not null)
        {
            _border.Content(child);
            _commit(_border);
        }
        else
        {
            _commit(child);
        }
    }
}

/// <summary>Each <see cref="IColumn.Item"/> call hands the caller a fresh <see cref="Container"/> whose commit appends the element as an Auto-sized row of the underlying <see cref="VStack"/>.</summary>
internal sealed class ColumnAdapter : IColumn
{
    private readonly VStack _stack;
    public ColumnAdapter(VStack stack) => _stack = stack;
    public IContainer Item() => new Container(elem => _stack.Auto(elem));
}

/// <summary>Each <see cref="IRow.Item"/> call hands the caller a fresh <see cref="Container"/> whose commit appends the element as an Auto-sized cell of the underlying <see cref="HStack"/>.</summary>
internal sealed class RowAdapter : IRow
{
    private readonly HStack _stack;
    public RowAdapter(HStack stack) => _stack = stack;
    public IContainer Item() => new Container(elem => _stack.Auto(elem));
}
