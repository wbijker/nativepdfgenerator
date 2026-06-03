namespace PdfSpec.Geometry;

/// <summary>Common page sizes expressed in PDF user-space points (72 per inch).</summary>
public static class PageSizes
{
    public static PdfRectangle Letter => PdfRectangle.FromSize(612, 792);
    public static PdfRectangle Legal => PdfRectangle.FromSize(612, 1008);
    public static PdfRectangle Tabloid => PdfRectangle.FromSize(792, 1224);
    public static PdfRectangle A3 => PdfRectangle.FromSize(841.890, 1190.551);
    public static PdfRectangle A4 => PdfRectangle.FromSize(595.276, 841.890);
    public static PdfRectangle A5 => PdfRectangle.FromSize(419.528, 595.276);
}
