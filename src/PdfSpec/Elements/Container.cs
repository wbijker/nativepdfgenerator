using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Concrete <see cref="IContainer"/> — a thin facade over a
/// <see cref="BorderElement"/> the slot owner created and installed
/// up-front. Chrome setters delegate straight to the wrapped border;
/// content terminals set <see cref="BorderElement.Content"/>.
/// </summary>
public sealed class Container : IContainer
{
    private readonly BorderElement border;
    private readonly Action? _newPageHandler;

    public Container(BorderElement border) : this(border, null) { }

    /// <summary>
    /// Construct with a handler invoked by <see cref="NewPage"/>. Slot
    /// owners (e.g. <see cref="ColumnAdapter"/>) pass a closure that
    /// flags the just-added <see cref="VStackItem"/> as page-break-before
    /// — the only place page-break semantics make sense.
    /// </summary>
    public Container(BorderElement border, Action? newPageHandler)
    {
        this.border = border;
        _newPageHandler = newPageHandler;
    }

    public IContainer NewPage()
    {
        _newPageHandler?.Invoke();
        return this;
    }

    // ===== chrome ===========================================================

    public IContainer Padding(double all)
    {
        border.Padding(all);
        return this;
    }

    public IContainer Padding(double v, double h)
    {
        border.Padding(v, h);
        return this;
    }

    public IContainer PaddingTop(double v)
    {
        border.PaddingTop(v);
        return this;
    }

    public IContainer PaddingRight(double v)
    {
        border.PaddingRight(v);
        return this;
    }

    public IContainer PaddingBottom(double v)
    {
        border.PaddingBottom(v);
        return this;
    }

    public IContainer PaddingLeft(double v)
    {
        border.PaddingLeft(v);
        return this;
    }

    public IContainer Padding(double all, Unit unit)              { border.Padding(all, unit);     return this; }
    public IContainer Padding(double v, double h, Unit unit)      { border.Padding(v, h, unit);    return this; }
    public IContainer PaddingTop(double v, Unit unit)             { border.PaddingTop(v, unit);    return this; }
    public IContainer PaddingRight(double v, Unit unit)           { border.PaddingRight(v, unit);  return this; }
    public IContainer PaddingBottom(double v, Unit unit)          { border.PaddingBottom(v, unit); return this; }
    public IContainer PaddingLeft(double v, Unit unit)            { border.PaddingLeft(v, unit);   return this; }

    public IContainer Border(double w, PdfColor c)
    {
        border.Border(w, c);
        return this;
    }

    public IContainer BorderTop(double w, PdfColor c)
    {
        border.BorderTop(w, c);
        return this;
    }

    public IContainer BorderRight(double w, PdfColor c)
    {
        border.BorderRight(w, c);
        return this;
    }

    public IContainer BorderBottom(double w, PdfColor c)
    {
        border.BorderBottom(w, c);
        return this;
    }

    public IContainer BorderLeft(double w, PdfColor c)
    {
        border.BorderLeft(w, c);
        return this;
    }

    public IContainer Background(PdfColor c)
    {
        border.Background(c);
        return this;
    }

    public IContainer Rounded(double r)        { border.Rounded(r);        return this; }
    public IContainer RoundedTop(double r)     { border.RoundedTop(r);     return this; }
    public IContainer RoundedBottom(double r)  { border.RoundedBottom(r);  return this; }
    public IContainer RoundedLeft(double r)    { border.RoundedLeft(r);    return this; }
    public IContainer RoundedRight(double r)   { border.RoundedRight(r);   return this; }
    public IContainer RoundedX(double r)       { border.RoundedX(r);       return this; }
    public IContainer RoundedY(double r)       { border.RoundedY(r);       return this; }

    public IContainer Width(double pt)
    {
        border.Width(pt);
        return this;
    }

    public IContainer Width(double v, Unit u)
    {
        border.Width(v, u);
        return this;
    }

    public IContainer Height(double pt)
    {
        border.Height(pt);
        return this;
    }

    public IContainer Height(double v, Unit u)
    {
        border.Height(v, u);
        return this;
    }

    public IContainer HAlign(HorizontalAlignment a)
    {
        border.HAlign(a);
        return this;
    }

    public IContainer VAlign(VerticalAlignment a)
    {
        border.VAlign(a);
        return this;
    }

    public IContainer AlignLeft()
    {
        border.AlignLeft();
        return this;
    }

    public IContainer AlignCenter()
    {
        border.AlignCenter();
        return this;
    }

    public IContainer AlignRight()
    {
        border.AlignRight();
        return this;
    }

    public IContainer AlignTop()
    {
        border.AlignTop();
        return this;
    }

    public IContainer AlignMiddle()
    {
        border.AlignMiddle();
        return this;
    }

    public IContainer AlignBottom()
    {
        border.AlignBottom();
        return this;
    }

    public IContainer OnRendered(Action<RenderedData> hook)
    {
        border.OnRendered(hook);
        return this;
    }

    public IContainer Anchor(string name)
    {
        border.AddRenderedListener(data =>
            data.Page.Document.AddNamedDestination(
                name,
                Actions.Destination.Xyz(data.Page, data.Bounds.Left, data.Bounds.Top, null)));
        return this;
    }

    public IContainer LinkToAnchor(string name)
    {
        border.AddRenderedListener(data =>
            data.Page.AddLink(data.Bounds, new Actions.NamedDestinationAction(name)));
        return this;
    }

    public void LinkToAnchor(string name, Action<IContainer> build)
    {
        LinkToAnchor(name);
        build(this);
    }

    // ===== content terminals ================================================

    public void Content(Element child) => border.Content(child);

    public Element AsElement() => border;

    public void Paragraph(string text, Font font, double size) =>
        border.Content(new Paragraph(text, font, size));

    public void Paragraph(string text) =>
        border.Content(new Paragraph(text, StandardFont.Helvetica, 11));

    /// <summary>Multi-span lambda form — see <see cref="Element.Paragraph(Font, double, Action{Paragraph})"/>.</summary>
    public void Paragraph(Font defaultFont, double size, Action<Paragraph> build) =>
        border.Content(new Paragraph(defaultFont, size, build));

    /// <summary>Multi-span family form — installs the paragraph and returns it for chaining (<c>.Bold(...).Italic(...).Text(...)</c>).</summary>
    public FamilyParagraph Paragraph(FontFamily family, double size)
    {
        var p = new FamilyParagraph(family, size);
        border.Content(p);
        return p;
    }

    /// <summary>Family + lambda form — same face-aware setters as the chained form, populated imperatively.</summary>
    public void Paragraph(FontFamily family, double size, Action<FamilyParagraph> build) =>
        border.Content(new FamilyParagraph(family, size, build));

    /// <summary>Reflow paragraph (family form) — installs and returns it for chaining (<c>.Bold(...).Float(...).Text(...)</c>).</summary>
    public ReflowParagraph ReflowParagraph(FontFamily family, double size)
    {
        var p = new ReflowParagraph(family, size);
        border.Content(p);
        return p;
    }

    /// <summary>Reflow paragraph (family + lambda).</summary>
    public void ReflowParagraph(FontFamily family, double size, Action<ReflowParagraph> build) =>
        border.Content(new ReflowParagraph(family, size, build));

    public IText Text(string text)
    {
        var paragraph = new Paragraph(text, StandardFont.Helvetica, 11);
        border.Content(paragraph);
        return new TextBuilder(paragraph);
    }

    public void Column(Action<IColumn> build)
    {
        var stack = new VStack();
        build(new ColumnAdapter(stack));
        border.Content(stack);
    }

    public void Row(Action<IRow> build)
    {
        var row = new HStack();
        build(new RowAdapter(row));
        border.Content(row);
    }

    public void VFrame(Action<VFrame> build)
    {
        var frame = new VFrame();
        build(frame);
        border.Content(frame);
    }

    public void Canvas(double width, double height, Action<ContentStream, PdfSize> draw) =>
        border.Content(new Canvas { Width = width, Height = height, Draw = draw });

    public void Svg(string svg) => border.Content(SvgImage.Parse(svg));
    public void Svg(SvgImage svg) => border.Content(svg);

    public void Component(IComponent component) => component.Compose(this);

    public void PageNumber() => PageNumber("Page {0} of {1}");

    public void PageNumber(string format) =>
        border.Content(new DeferredComponent(
            sizeHint: new Paragraph(string.Format(format, 999, 999), StandardFont.Helvetica, 11),
            render: data => new Paragraph(string.Format(format, data.PageNumber, data.TotalPages),
                StandardFont.Helvetica, 11)));

    public void PageBreak() => border.Content(new PageBreak());

    public void MultiColumn(int columns, double gap, Action<IColumn> build)
    {
        var mc = new MultiColumn { ColumnCount = columns, ColumnGap = gap };
        build(new MultiColumnAdapter(mc));
        border.Content(mc);
    }

    public void MultiColumn(int columns, double height, double gap, Action<IColumn> build)
    {
        var mc = new MultiColumn { ColumnCount = columns, ColumnGap = gap };
        build(new MultiColumnAdapter(mc));
        border.Height(height).Content(mc);
    }
}

/// <summary>Each call appends a fresh <see cref="BorderElement"/> to the underlying <see cref="VStack"/> at the requested vertical sizing and returns a <see cref="Container"/> facade onto it.</summary>
internal sealed class ColumnAdapter : IColumn
{
    private readonly VStack _stack;
    public ColumnAdapter(VStack stack) => _stack = stack;

    public IContainer Item() => AutoItem();

    public IContainer AutoItem()
    {
        var border = new BorderElement();
        _stack.Auto(border);
        // The item we just added is at the end of _stack.Items — capture
        // it so NewPage() can flag it without rebuilding the list.
        var addedItem = _stack.Items[_stack.Items.Count - 1];
        return new Container(border, () => addedItem.BreakBefore = true);
    }

    public IContainer FixedItem(double height)
    {
        var border = new BorderElement();
        _stack.Fixed(height, border);
        var addedItem = _stack.Items[_stack.Items.Count - 1];
        return new Container(border, () => addedItem.BreakBefore = true);
    }
}

/// <summary>
/// Each call appends a fresh <see cref="BorderElement"/> to the underlying <see cref="Elements.MultiColumn"/> and returns a <see cref="Container"/> facade onto it. MultiColumn has no per-item sizing, so <see cref="FixedItem"/> stamps the height on the wrapping <see cref="BorderElement"/> chrome instead.
/// </summary>
internal sealed class MultiColumnAdapter : IColumn
{
    private readonly MultiColumn _mc;
    public MultiColumnAdapter(MultiColumn mc) => _mc = mc;

    public IContainer Item() => AutoItem();

    public IContainer AutoItem()
    {
        var border = new BorderElement();
        _mc.Add(border);
        return new Container(border);
    }

    public IContainer FixedItem(double height)
    {
        var border = new BorderElement().Height(height);
        _mc.Add(border);
        return new Container(border);
    }
}

/// <summary>Each call appends a fresh <see cref="BorderElement"/> to the underlying <see cref="HStack"/> at the requested horizontal sizing (Fixed / Auto / Relative) and returns a <see cref="Container"/> facade onto it.</summary>
internal sealed class RowAdapter : IRow
{
    private readonly HStack _stack;
    public RowAdapter(HStack stack) => _stack = stack;

    public IContainer Item() => AutoItem();

    public IContainer AutoItem()
    {
        var border = new BorderElement();
        _stack.Auto(border);
        return new Container(border);
    }

    public IContainer FixedItem(double width)
    {
        var border = new BorderElement();
        _stack.Fixed(width, border);
        return new Container(border);
    }

    public IContainer RelativeItem(double units = 1)
    {
        var border = new BorderElement();
        _stack.Relative(units, border);
        return new Container(border);
    }
}