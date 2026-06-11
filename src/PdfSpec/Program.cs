using PdfSpec.Elements;
using PdfSpec.Fonts;
using PdfSpec.Geometry;

namespace PdfSpec;

internal static class Program
{
    public static void Main(string[] args)
    {
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples/spec"));
        Directory.CreateDirectory(samplesDir);
        var path = Path.Combine(samplesDir, "samples.pdf");

        PdfDoc.Create()
            .Info(title: "PdfSpec Combined Showcase", creator: "PdfSpec", producer: "PdfSpec")
            .DefaultFont(StandardFont.Helvetica, 11)
            .DefaultPageSize(PageSizes.A5) 
            .AddPage(p =>
            {
                p.Header()
                    .Background(PdfColors.Red(200))
                    .Padding(10)
                    .AlignCenter()
                    .Paragraph("Hap de pap...Generated on: " + DateTime.Now.ToLongTimeString());
                
                p.Footer()
                    .Background(PdfColors.Blue(200))
                    .Padding(10)
                    .AlignRight()
                    .Paragraph("Copyright 2024, PdfSpec");
                
                p.Body().Paragraph("Very good, Sire");
            })
            .Save(path);

        Console.WriteLine($"Wrote {path}");
    }
}