using CSharpPdf.Geometry;
using CSharpPdf.Objects;

namespace CSharpPdf.Annotations;

/// <summary>
/// Factories for annotation dictionaries (Chapter 6). Every annotation has at
/// least Type/Subtype/Rect; colors are given as 1/3/4-component arrays implying
/// DeviceGray/RGB/CMYK. Add the result to a page with PdfPage.AddAnnotation.
/// </summary>
public static class Annotation
{
    // ----- Text markup (Highlight / Underline / StrikeOut / Squiggly) -----

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
        a["C"] = Color(color);
        // QuadPoints: four corners as top-left, top-right, bottom-left, bottom-right.
        a["QuadPoints"] = new PdfArray(
            Num(rect.Left), Num(rect.Top), Num(rect.Right), Num(rect.Top),
            Num(rect.Left), Num(rect.Bottom), Num(rect.Right), Num(rect.Bottom));
        if (contents is not null)
        {
            a["Contents"] = new PdfString(contents);
        }
        return a;
    }

    // ----- Drawing markup (Square / Circle / Line / Polygon / PolyLine / Ink) -----

    public static PdfDictionary Square(PdfRectangle rect, double[]? stroke = null, double[]? fill = null, double borderWidth = 1) =>
        Shape("Square", rect, stroke, fill, borderWidth);

    public static PdfDictionary Circle(PdfRectangle rect, double[]? stroke = null, double[]? fill = null, double borderWidth = 1) =>
        Shape("Circle", rect, stroke, fill, borderWidth);

    private static PdfDictionary Shape(string subtype, PdfRectangle rect, double[]? stroke, double[]? fill, double borderWidth)
    {
        var a = Base(subtype, rect);
        if (stroke is not null) a["C"] = Color(stroke);
        if (fill is not null) a["IC"] = Color(fill);
        a["BS"] = new PdfDictionary { ["W"] = Num(borderWidth) };
        return a;
    }

    public static PdfDictionary Line(double x1, double y1, double x2, double y2,
        double[] color, double borderWidth = 1, string? startStyle = null, string? endStyle = null, double[]? interior = null)
    {
        var rect = new PdfRectangle(
            Math.Min(x1, x2) - 6, Math.Min(y1, y2) - 6, Math.Max(x1, x2) + 6, Math.Max(y1, y2) + 6);
        var a = Base("Line", rect);
        a["L"] = new PdfArray(Num(x1), Num(y1), Num(x2), Num(y2));
        a["C"] = Color(color);
        a["BS"] = new PdfDictionary { ["W"] = Num(borderWidth) };
        if (startStyle is not null || endStyle is not null)
        {
            a["LE"] = new PdfArray(new PdfName(startStyle ?? "None"), new PdfName(endStyle ?? "None"));
        }
        if (interior is not null) a["IC"] = Color(interior);
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
        a["Vertices"] = verts;
        if (stroke is not null) a["C"] = Color(stroke);
        if (fill is not null) a["IC"] = Color(fill);
        a["BS"] = new PdfDictionary { ["W"] = Num(borderWidth) };
        return a;
    }

    /// <summary>Freehand ink: each inner array is one stroke of [x y x y ...] points.</summary>
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
        a["InkList"] = inkList;
        a["C"] = Color(color);
        a["BS"] = new PdfDictionary { ["W"] = Num(borderWidth) };
        return a;
    }

    // ----- Stamp (requires an appearance stream) -----

    /// <summary>
    /// A rubber-stamp annotation. Its appearance is a form XObject (see
    /// FormXObject) referenced through the AP/N entry; <paramref name="opacity"/>
    /// sets the constant alpha (CA) for the whole stamp.
    /// </summary>
    public static PdfDictionary Stamp(PdfRectangle rect, PdfReference appearance, double? opacity = null)
    {
        var a = Base("Stamp", rect);
        a["AP"] = new PdfDictionary { ["N"] = appearance };
        if (opacity is { } o)
        {
            a["CA"] = Num(o);
        }
        return a;
    }

    // ----- Helpers -----

    public static PdfArray Color(params double[] components)
    {
        var array = new PdfArray();
        foreach (double c in components) array.Add(Num(c));
        return array;
    }

    internal static PdfDictionary Base(string subtype, PdfRectangle rect) => new()
    {
        ["Type"] = new PdfName("Annot"),
        ["Subtype"] = new PdfName(subtype),
        ["Rect"] = rect.ToArray(),
    };

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
