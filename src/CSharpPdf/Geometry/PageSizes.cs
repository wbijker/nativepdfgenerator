namespace CSharpPdf.Geometry;

/// <summary>Common page sizes expressed in PDF user-space points (72 per inch).</summary>
public static class PageSizes
{
    public static PdfRectangle Letter => PdfRectangle.FromSize(612, 792);          // 8.5 x 11 in
    public static PdfRectangle Legal => PdfRectangle.FromSize(612, 1008);          // 8.5 x 14 in
    public static PdfRectangle Tabloid => PdfRectangle.FromSize(792, 1224);        // 11 x 17 in
    public static PdfRectangle A3 => PdfRectangle.FromSize(841.890, 1190.551);     // 297 x 420 mm
    public static PdfRectangle A4 => PdfRectangle.FromSize(595.276, 841.890);      // 210 x 297 mm
    public static PdfRectangle A5 => PdfRectangle.FromSize(419.528, 595.276);      // 148 x 210 mm
}
