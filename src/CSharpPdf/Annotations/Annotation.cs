using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace CSharpPdf.Annotations;

/// <summary>
/// Factories for annotation dictionaries the typed PdfSpec wrappers don't
/// cover yet (Stamp / FileAttachment / Highlight family / drawing markup).
/// Add the result to a page with <c>PdfPage.AddAnnotation(PdfDictionary)</c>.
/// </summary>
public static class Annotation
{
    public static PdfDictionary Highlight(PdfRectangle rect, double[] color, string? contents = null) =>
        TextMarkup("Highlight", rect, color, contents);

    public static PdfDictionary Underline(PdfRectangle rect, double[] color, string? contents = null) =>
        TextMarkup("Underline", rect, color, contents);

    public static PdfDictionary StrikeOut(PdfRectangle rect, double[] color, string? contents = null) =>
        TextMarkup("StrikeOut", rect, color, contents);

    public static PdfDictionary Squiggly(PdfRectangle rect, double[] color, string? contents = null) =>
        TextMarkup("Squiggly", rect, color, contents);

    private static PdfDictionary TextMarkup(string subtype, PdfRectangle rect, double[] color, string? contents)
    {
        var a = Base(subtype, rect);
        a.Add("C", Color(color));
        a.Add("QuadPoints", new PdfArray(
            Num(rect.Left), Num(rect.Top), Num(rect.Right), Num(rect.Top),
            Num(rect.Left), Num(rect.Bottom), Num(rect.Right), Num(rect.Bottom)));
        if (contents is not null) a.SetString("Contents", contents);
        return a;
    }

    public static PdfDictionary Square(PdfRectangle rect, double[]? stroke = null, double[]? fill = null, double borderWidth = 1) =>
        Shape("Square", rect, stroke, fill, borderWidth);

    public static PdfDictionary Circle(PdfRectangle rect, double[]? stroke = null, double[]? fill = null, double borderWidth = 1) =>
        Shape("Circle", rect, stroke, fill, borderWidth);

    private static PdfDictionary Shape(string subtype, PdfRectangle rect, double[]? stroke, double[]? fill, double borderWidth)
    {
        var a = Base(subtype, rect);
        if (stroke is not null) a.Add("C", Color(stroke));
        if (fill is not null) a.Add("IC", Color(fill));
        var bs = new PdfDictionary();
        bs.Add("W", Num(borderWidth));
        a.Add("BS", bs);
        return a;
    }

    public static PdfDictionary Line(double x1, double y1, double x2, double y2,
        double[] color, double borderWidth = 1, string? startStyle = null, string? endStyle = null, double[]? interior = null)
    {
        var rect = new PdfRectangle(
            Math.Min(x1, x2) - 6, Math.Min(y1, y2) - 6, Math.Max(x1, x2) + 6, Math.Max(y1, y2) + 6);
        var a = Base("Line", rect);
        a.Add("L", new PdfArray(Num(x1), Num(y1), Num(x2), Num(y2)));
        a.Add("C", Color(color));
        var bs = new PdfDictionary();
        bs.Add("W", Num(borderWidth));
        a.Add("BS", bs);
        if (startStyle is not null || endStyle is not null)
        {
            a.Add("LE", new PdfArray(new PdfName(startStyle ?? "None"), new PdfName(endStyle ?? "None")));
        }
        if (interior is not null) a.Add("IC", Color(interior));
        return a;
    }

    public static PdfDictionary Polygon(double[] vertices, double[]? stroke = null, double[]? fill = null, double borderWidth = 1) =>
        Poly("Polygon", vertices, stroke, fill, borderWidth);

    public static PdfDictionary PolyLine(double[] vertices, double[]? stroke = null, double[]? fill = null, double borderWidth = 1) =>
        Poly("PolyLine", vertices, stroke, fill, borderWidth);

    private static PdfDictionary Poly(string subtype, double[] vertices, double[]? stroke, double[]? fill, double borderWidth)
    {
        var a = Base(subtype, BoundsOf(vertices, 6));
        var verts = new PdfArray();
        foreach (double v in vertices) verts.Add(Num(v));
        a.Add("Vertices", verts);
        if (stroke is not null) a.Add("C", Color(stroke));
        if (fill is not null) a.Add("IC", Color(fill));
        var bs = new PdfDictionary();
        bs.Add("W", Num(borderWidth));
        a.Add("BS", bs);
        return a;
    }

    public static PdfDictionary Ink(IReadOnlyList<double[]> strokes, double[] color, double borderWidth = 1)
    {
        var all = new List<double>();
        var inkList = new PdfArray();
        foreach (double[] stroke in strokes)
        {
            var path = new PdfArray();
            foreach (double v in stroke) path.Add(Num(v));
            inkList.Add(path);
            all.AddRange(stroke);
        }
        var a = Base("Ink", BoundsOf(all.ToArray(), 6));
        a.Add("InkList", inkList);
        a.Add("C", Color(color));
        var bs = new PdfDictionary();
        bs.Add("W", Num(borderWidth));
        a.Add("BS", bs);
        return a;
    }

    public static PdfDictionary Stamp(PdfRectangle rect, PdfReference appearance, double? opacity = null)
    {
        var a = Base("Stamp", rect);
        var ap = new PdfDictionary();
        ap.Add("N", appearance);
        a.Add("AP", ap);
        if (opacity is { } o) a.Add("CA", Num(o));
        return a;
    }

    public static PdfDictionary FileAttachment(PdfRectangle rect, PdfReference fileSpec, string contents, string icon = "Paperclip")
    {
        var a = Base("FileAttachment", rect);
        a.Add("FS", fileSpec);
        a.SetString("Contents", contents);
        a.SetName("Name", icon);
        return a;
    }

    public static PdfArray Color(params double[] components)
    {
        var array = new PdfArray();
        foreach (double c in components) array.Add(Num(c));
        return array;
    }

    internal static PdfDictionary Base(string subtype, PdfRectangle rect)
    {
        var d = new PdfDictionary();
        d.SetName("Type", "Annot");
        d.SetName("Subtype", subtype);
        d.Add("Rect", rect.ToArray());
        return d;
    }

    private static PdfRectangle BoundsOf(double[] coords, double pad)
    {
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        for (int i = 0; i + 1 < coords.Length; i += 2)
        {
            minX = Math.Min(minX, coords[i]); maxX = Math.Max(maxX, coords[i]);
            minY = Math.Min(minY, coords[i + 1]); maxY = Math.Max(maxY, coords[i + 1]);
        }
        return new PdfRectangle(minX - pad, minY - pad, maxX + pad, maxY + pad);
    }

    private static PdfNumber Num(double value) =>
        value == Math.Floor(value) && !double.IsInfinity(value)
            ? new PdfNumber((long)value)
            : new PdfNumber(value);
}
